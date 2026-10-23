"""Exercise capture scheduling with fake buffers; never capture the desktop."""
import json
from types import SimpleNamespace
from unittest.mock import patch
import cv2
import numpy as np
import worker

checks=[]
for opened in (False,True):
    cameras=[];messages=[]
    class Stop:
        def __init__(self):self.calls=0;self.intervals=[]
        def wait(self,seconds):self.calls+=1;self.intervals.append(seconds);return self.calls>5
        def set(self):pass
    class Camera:
        def __init__(self):self.calls=[];self.closed=False;cameras.append(self)
        def grab(self,bounds):
            self.calls.append(bounds);return np.zeros((bounds[3]-bounds[1],bounds[2]-bounds[0],3),np.uint8)
        def close(self):self.closed=True
    class Source:
        reason='waiting-map'
        def __init__(self,game):pass
        def bounds(self):return (0,0,1920,1080)
    stop=Stop();worker.stop=stop;worker.current_open=False;worker.latest=None
    worker.overlay_regions={'kind':'overlay','bounds':[0,0,1920,1080],'regions':[[500,200,550,250]]}
    pointer=SimpleNamespace(x=600,y=500,buttons=0,stamp=1.)
    with patch('fast_capture.FastCapture',Camera),patch('game_window.GameWindow',Source),patch('drag_assist.read_pointer',lambda:pointer),patch('map_presence.title_score',lambda gray,title:.99 if opened else 0.),patch.object(worker,'emit',messages.append):
        worker.capture(SimpleNamespace(cv2=cv2),np.zeros((47,212),np.uint8),'AION2.exe')
    assert all(c.closed for c in cameras),'Both capture buffers must close'
    assert len(cameras[1].calls)==5 and all(b[2]<=301 and b[3]<=86 for b in cameras[1].calls),'Only a small title strip is polled'
    if opened:
        assert len(cameras[0].calls)==5 and worker.latest is not None,'Open maps supply full frames to localization'
        assert worker.latest[-1]==worker.overlay_regions,'The captured frame retains its matching overlay exclusions'
        assert .015 in stop.intervals,'Open maps retain fast tracking'
        checks.append('Open maps supply full frames and keep fast tracking')
    else:
        assert not cameras[0].calls and worker.latest is None,'Closed maps must not capture full frames'
        assert all(i==.12 for i in stop.intervals),'Closed maps use a slower title-only poll'
        checks.append('Closed maps poll only a small heading; no full-screen frames')
    assert messages[-1]['kind']=='presence' and not messages[-1]['open'],'Stopping releases map presence'
checks.append('Stopping releases presence and both capture buffers')
print(json.dumps({'success':True,'checks':checks}))
