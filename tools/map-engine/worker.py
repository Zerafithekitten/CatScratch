"""Private CatScratch image worker. JSON pipes only; no windows, writes or game-memory access."""
import argparse
import ctypes
from ctypes import wintypes
import json
import os
from pathlib import Path
import sys
import threading
import time

stop=threading.Event()
output_lock=threading.Lock()
state_lock=threading.Lock()
latest=None
current_bounds=None
current_open=False
current_candidate=False
confirmed_bounds=None


def emit(value):
    try:
        with output_lock:
            print(json.dumps(value,separators=(',',':')),flush=True)
    except (BrokenPipeError,OSError):
        stop.set()


def read_control():
    try:
        for line in sys.stdin:
            if line.strip()=='stop':break
    finally:stop.set()


def capture(engine,title,game,close=None):
    global latest,current_bounds,current_open,current_candidate,confirmed_bounds
    from game_window import GameWindow
    from map_presence import title_score,map_ui_candidate
    from fast_capture import FastCapture
    from drag_assist import read_pointer
    source=GameWindow(game)
    camera=FastCapture();header_camera=FastCapture();last_emit=0;previous=None;last_seen=0
    close_camera=FastCapture() if close is not None else None
    try:
        while not stop.wait(.015 if current_open else .12):
            bounds=source.bounds();visible=False;candidate=False;frame=None;score=None
            pointer=read_pointer();stamp=time.perf_counter()
            if bounds:
                left,top,right,bottom=bounds;h=bottom-top;w=right-left
                header=engine.cv2.resize(header_camera.grab((left,top,left+round(w*300/1919),top+round(h*85/1079))),(300,85))
                gray=engine.cv2.cvtColor(header,engine.cv2.COLOR_BGR2GRAY)
                score=title_score(gray,title)
                visible=score>=.70
                candidate=visible
                if not visible and close_camera is not None:
                    right_header=engine.cv2.resize(close_camera.grab((right-round(w*75/1919),top,right,top+round(h*85/1079))),(75,85))
                    candidate=map_ui_candidate(gray,engine.cv2.cvtColor(right_header,engine.cv2.COLOR_BGR2GRAY),title,close)
            now=time.perf_counter()
            if visible:last_seen=now
            # A brief header miss during a map animation must not flash the HUD.
            visible=visible or (bounds is not None and now-last_seen<.20)
            with state_lock:
                if not candidate and not visible:confirmed_bounds=None
                visible=visible or (candidate and bounds==confirmed_bounds)
            if visible or candidate:
                pointer=read_pointer();stamp=time.perf_counter();frame=camera.grab(bounds)
            with state_lock:
                current_bounds=bounds;current_open=visible;current_candidate=candidate or visible
                latest=(stamp,frame,bounds,pointer,candidate) if visible or candidate else None
            state=(bounds,visible)
            if state!=previous or now-last_emit>.18:
                emit(dict(kind='presence',open=visible,rect=bounds,stamp=now,reason=source.reason,score=score,foreground=getattr(source,'cached_name',None)))
                previous=state;last_emit=now
            if not bounds:stop.wait(.12)
    except Exception as error:
        emit(dict(kind='error',message=type(error).__name__));stop.set()
    finally:
        camera.close();header_camera.close()
        if close_camera is not None:close_camera.close()
        emit(dict(kind='presence',open=False,stamp=time.perf_counter()))


def main():
    global latest,confirmed_bounds
    parser=argparse.ArgumentParser()
    parser.add_argument('--data',required=True)
    parser.add_argument('--verify',action='store_true')
    parser.add_argument('--game',default='AION2.exe')
    parser.add_argument('--check-frame')
    args=parser.parse_args()
    os.environ['CATSCRATCH_MAP_DATA']=str(Path(args.data).resolve())
    try:ctypes.windll.user32.SetProcessDpiAwarenessContext(ctypes.c_void_p(-4))
    except Exception:pass
    import map_matcher as engine
    if args.verify:
        emit(dict(kind='verified',references=len(engine.refs),opencv=engine.cv2.__version__));return
    if args.check_frame:
        image=engine.cv2.imread(args.check_frame)
        result=engine.locate(image)
        emit(dict(kind='check',accepted=result is not None,result=result));return
    tracker=engine.Tracker()
    threading.Thread(target=read_control,daemon=True).start()
    close=engine.cv2.imread(str(Path(args.data)/'map-close.png'),engine.cv2.IMREAD_GRAYSCALE)
    threading.Thread(target=capture,args=(engine,tracker.title,args.game,close),daemon=True).start()
    emit(dict(kind='ready'))
    previous_stamp=0
    try:
        while not stop.wait(.004):
            with state_lock:packet=latest
            if packet is None:
                tracker.reset();continue
            stamp,frame,bounds,pointer,candidate=packet
            if stamp==previous_stamp:continue
            previous_stamp=stamp
            result=tracker.update(frame,map_candidate=candidate)
            with state_lock:
                valid=current_candidate and current_bounds==bounds
                if valid and result is not None:confirmed_bounds=bounds
                elif tracker.result is None:confirmed_bounds=None
            if not valid or time.perf_counter()-stamp>.4:continue
            emit(dict(kind='pose',rect=bounds,stamp=stamp,matrix=result['matrix'] if result else None,pointer=[pointer.x,pointer.y,pointer.buttons,pointer.stamp]))
    finally:
        stop.set();tracker.close()


if __name__=='__main__':
    try:main()
    except Exception as error:
        emit(dict(kind='error',message=type(error).__name__));sys.exit(1)
