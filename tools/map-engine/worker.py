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


def emit(value):
    try:
        with output_lock:
            print(json.dumps(value,separators=(',',':')),flush=True)
    except (BrokenPipeError,OSError):
        stop.set()


def foreground_bounds():
    api=ctypes.windll.user32;kernel=ctypes.windll.kernel32
    api.GetForegroundWindow.restype=wintypes.HWND
    api.GetWindowThreadProcessId.argtypes=[wintypes.HWND,ctypes.POINTER(wintypes.DWORD)]
    hwnd=api.GetForegroundWindow();pid=wintypes.DWORD()
    api.GetWindowThreadProcessId(hwnd,ctypes.byref(pid))
    kernel.OpenProcess.argtypes=[wintypes.DWORD,wintypes.BOOL,wintypes.DWORD];kernel.OpenProcess.restype=wintypes.HANDLE
    handle=kernel.OpenProcess(0x1000,False,pid.value)
    if not handle:return None
    try:
        size=wintypes.DWORD(32768);name=ctypes.create_unicode_buffer(size.value)
        kernel.QueryFullProcessImageNameW.argtypes=[wintypes.HANDLE,wintypes.DWORD,wintypes.LPWSTR,ctypes.POINTER(wintypes.DWORD)]
        if not kernel.QueryFullProcessImageNameW(handle,0,name,ctypes.byref(size)):return None
        if 'aion' not in Path(name.value).name.lower():return None
    finally:
        kernel.CloseHandle.argtypes=[wintypes.HANDLE];kernel.CloseHandle(handle)
    rect=wintypes.RECT();point=wintypes.POINT()
    api.GetClientRect.argtypes=[wintypes.HWND,ctypes.POINTER(wintypes.RECT)]
    api.ClientToScreen.argtypes=[wintypes.HWND,ctypes.POINTER(wintypes.POINT)]
    if not api.GetClientRect(hwnd,ctypes.byref(rect)) or not api.ClientToScreen(hwnd,ctypes.byref(point)):return None
    if rect.right<600 or rect.bottom<400:return None
    return (point.x,point.y,point.x+rect.right,point.y+rect.bottom)


def read_control():
    try:
        for line in sys.stdin:
            if line.strip()=='stop':break
    finally:stop.set()


def capture(engine,title):
    global latest,current_bounds,current_open
    from fast_capture import FastCapture
    from drag_assist import read_pointer
    camera=FastCapture();last_emit=0;previous=None;last_seen=0
    try:
        while not stop.wait(.015):
            bounds=foreground_bounds();visible=False;frame=None
            pointer=read_pointer();stamp=time.perf_counter()
            if bounds:
                frame=camera.grab(bounds)
                h,w=frame.shape[:2]
                header=engine.cv2.resize(frame[:round(h*85/1079),:round(w*300/1919)],(300,85))
                gray=engine.cv2.cvtColor(header,engine.cv2.COLOR_BGR2GRAY)
                visible=float(engine.cv2.matchTemplate(gray[8:85,8:300],title,engine.cv2.TM_CCOEFF_NORMED).max())>=.70
            now=time.perf_counter()
            if visible:last_seen=now
            # A brief header miss during a map animation must not flash the HUD.
            visible=visible or (bounds is not None and now-last_seen<.20)
            with state_lock:
                current_bounds=bounds;current_open=visible
                latest=(stamp,frame,bounds,pointer) if visible else None
            state=(bounds,visible)
            if state!=previous or now-last_emit>.18:
                emit(dict(kind='presence',open=visible,rect=bounds,stamp=now))
                previous=state;last_emit=now
            if not bounds:stop.wait(.12)
    except Exception as error:
        emit(dict(kind='error',message=type(error).__name__));stop.set()
    finally:
        camera.close()
        emit(dict(kind='presence',open=False,stamp=time.perf_counter()))


def main():
    global latest
    parser=argparse.ArgumentParser()
    parser.add_argument('--data',required=True)
    parser.add_argument('--verify',action='store_true')
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
    threading.Thread(target=capture,args=(engine,tracker.title),daemon=True).start()
    emit(dict(kind='ready'))
    previous_stamp=0
    try:
        while not stop.wait(.004):
            with state_lock:packet=latest
            if packet is None:
                tracker.reset();continue
            stamp,frame,bounds,pointer=packet
            if stamp==previous_stamp:continue
            previous_stamp=stamp
            result=tracker.update(frame)
            with state_lock:valid=current_open and current_bounds==bounds
            if not valid or time.perf_counter()-stamp>.4:continue
            emit(dict(kind='pose',rect=bounds,stamp=stamp,matrix=result['matrix'] if result else None,pointer=[pointer.x,pointer.y,pointer.buttons,pointer.stamp]))
    finally:
        stop.set();tracker.close()


if __name__=='__main__':
    try:main()
    except Exception as error:
        emit(dict(kind='error',message=type(error).__name__));sys.exit(1)
