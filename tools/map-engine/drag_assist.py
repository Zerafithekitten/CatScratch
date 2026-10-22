"""Measured pointer assistance between confirmed image transforms, never velocity prediction."""
import ctypes
from ctypes import wintypes
from dataclasses import dataclass
import time
import numpy as np

ctypes.windll.user32.GetCursorPos.argtypes=[ctypes.POINTER(wintypes.POINT)]
ctypes.windll.user32.GetAsyncKeyState.argtypes=[ctypes.c_int]
ctypes.windll.user32.GetAsyncKeyState.restype=ctypes.c_short


@dataclass(frozen=True)
class Pointer:
    x: float
    y: float
    buttons: int
    stamp: float


def read_pointer():
    api=ctypes.windll.user32
    point=wintypes.POINT()
    api.GetCursorPos(ctypes.byref(point))
    buttons=(1 if api.GetAsyncKeyState(1)&0x8000 else 0)|(2 if api.GetAsyncKeyState(2)&0x8000 else 0)
    return Pointer(point.x,point.y,buttons,time.perf_counter())


class DragAssist:
    def __init__(self):
        self.reset()

    def reset(self):
        self.previous=None
        self.bounds=None
        self.gain=1.0
        self.votes=0
        self.gesture_start=0
        self.held=0
        self.released=None

    def observe(self,pointer):
        if pointer.buttons and pointer.buttons!=self.held:
            self.gesture_start=pointer.stamp
            self.released=None
            self.votes=0
        elif self.held and not pointer.buttons:
            self.released=pointer
        self.held=pointer.buttons

    def confirm(self,matrix,pointer,bounds):
        if self.bounds!=bounds:
            self.reset();self.bounds=bounds
        matrix=np.array(matrix,dtype=float)
        if self.previous is not None:
            old_matrix,old_pointer=self.previous
            dt=pointer.stamp-old_pointer.stamp
            if pointer.buttons and pointer.buttons==old_pointer.buttons and .02<dt<.35 and self.gesture_start<=old_pointer.stamp:
                step=np.vstack([matrix,[0,0,1]])@np.linalg.inv(np.vstack([old_matrix,[0,0,1]]))
                delta=np.array([pointer.x-old_pointer.x,pointer.y-old_pointer.y])
                distance=float(np.linalg.norm(delta))
                if np.max(np.abs(step[:2,:2]-np.eye(2)))>.005:
                    self.votes=0
                elif distance>=5:
                    gain=float(np.dot(step[:2,2],delta)/np.dot(delta,delta))
                    error=float(np.linalg.norm(step[:2,2]-gain*delta))
                    if .8<=gain<=1.2 and error<=max(3,distance*.2):
                        self.gain=self.gain*.7+gain*.3
                        self.votes=min(6,self.votes+1)
                    else:self.votes=0
        self.previous=(matrix,pointer)

    def adjusted(self,matrix,source,now,bounds):
        output=np.array(matrix,dtype=float).copy()
        if self.votes<2 or bounds!=self.bounds or not source.buttons:return output.tolist(),0.0
        if not 0<=now.stamp-source.stamp<.25 or self.gesture_start>source.stamp:return output.tolist(),0.0
        pointer=now
        if now.buttons!=source.buttons:
            if now.buttons or self.released is None or self.released.stamp<source.stamp:return output.tolist(),0.0
            pointer=self.released
        left,top,right,bottom=bounds
        if not (left+(right-left)*330/1919<=source.x<left+(right-left)*1820/1919 and top+(bottom-top)*110/1079<=source.y<top+(bottom-top)*1040/1079):return output.tolist(),0.0
        delta=np.array([pointer.x-source.x,pointer.y-source.y])*self.gain
        length=float(np.linalg.norm(delta))
        if length>120:delta*=120/length
        output[:,2]+=delta
        return output.tolist(),min(length,120.0)
