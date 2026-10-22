"""Identify the foreground game using window/process metadata only, including protected clients."""
import ctypes as c
from ctypes import wintypes as w
from pathlib import Path
import time


class ProcessEntry(c.Structure):
    _fields_=[('size',w.DWORD),('usage',w.DWORD),('pid',w.DWORD),('heap',c.c_size_t),
              ('module',w.DWORD),('threads',w.DWORD),('parent',w.DWORD),
              ('priority',w.LONG),('flags',w.DWORD),('exe',w.WCHAR*260)]


def process_basename(pid):
    # Toolhelp enumerates process metadata; it never opens or reads game memory.
    kernel=c.windll.kernel32
    kernel.CreateToolhelp32Snapshot.argtypes=[w.DWORD,w.DWORD]
    kernel.CreateToolhelp32Snapshot.restype=w.HANDLE
    kernel.Process32FirstW.argtypes=[w.HANDLE,c.POINTER(ProcessEntry)]
    kernel.Process32NextW.argtypes=[w.HANDLE,c.POINTER(ProcessEntry)]
    kernel.CloseHandle.argtypes=[w.HANDLE]
    snapshot=kernel.CreateToolhelp32Snapshot(2,0)
    if snapshot in (None,c.c_void_p(-1).value):return None
    try:
        entry=ProcessEntry();entry.size=c.sizeof(entry)
        found=kernel.Process32FirstW(snapshot,c.byref(entry))
        while found:
            if entry.pid==pid:return Path(entry.exe).name.lower()
            found=kernel.Process32NextW(snapshot,c.byref(entry))
    finally:kernel.CloseHandle(snapshot)
    return None


class GameWindow:
    def __init__(self,executable='AION2.exe'):
        self.executable=Path(executable or 'AION2.exe').name.lower()
        # Match the same two clients as CatScratch's packet-capture locator.
        # The configured executable may be a launcher; it must not disable the known clients.
        self.executables={'aion2.exe','aion2-win64-shipping.exe',self.executable}
        self.cached_pid=None;self.cached_at=0;self.cached_name=None
        self.reason='waiting-game'

    def bounds(self):
        api=c.windll.user32
        api.GetForegroundWindow.restype=w.HWND
        api.GetWindowThreadProcessId.argtypes=[w.HWND,c.POINTER(w.DWORD)]
        api.GetClientRect.argtypes=[w.HWND,c.POINTER(w.RECT)]
        api.ClientToScreen.argtypes=[w.HWND,c.POINTER(w.POINT)]
        hwnd=api.GetForegroundWindow();pid=w.DWORD()
        api.GetWindowThreadProcessId(hwnd,c.byref(pid))
        now=time.perf_counter()
        if pid.value!=self.cached_pid or now-self.cached_at>1:
            self.cached_pid=pid.value;self.cached_at=now;self.cached_name=process_basename(pid.value)
        if self.cached_name not in self.executables:
            self.reason='waiting-game';return None
        rect=w.RECT();point=w.POINT()
        if not api.GetClientRect(hwnd,c.byref(rect)) or not api.ClientToScreen(hwnd,c.byref(point)):
            self.reason='window-unavailable';return None
        if rect.right<600 or rect.bottom<400:
            self.reason='window-unavailable';return None
        self.reason='waiting-map'
        return (point.x,point.y,point.x+rect.right,point.y+rect.bottom)
