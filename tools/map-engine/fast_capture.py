"""Reusable GDI buffer for just the foreground game's client rectangle."""
import ctypes as c
from ctypes import wintypes as w
import numpy as np


class BitmapHeader(c.Structure):
    _fields_=[('size',w.DWORD),('width',w.LONG),('height',w.LONG),
              ('planes',w.WORD),('bits',w.WORD),('compression',w.DWORD),
              ('image_size',w.DWORD),('xppm',w.LONG),('yppm',w.LONG),
              ('used',w.DWORD),('important',w.DWORD)]


class FastCapture:
    def __init__(self):
        self.user=c.windll.user32
        self.gdi=c.windll.gdi32
        self.user.GetDC.argtypes=[w.HWND];self.user.GetDC.restype=w.HDC
        self.user.ReleaseDC.argtypes=[w.HWND,w.HDC]
        self.gdi.CreateCompatibleDC.argtypes=[w.HDC];self.gdi.CreateCompatibleDC.restype=w.HDC
        self.gdi.CreateDIBSection.argtypes=[w.HDC,c.POINTER(BitmapHeader),w.UINT,c.POINTER(c.c_void_p),w.HANDLE,w.DWORD]
        self.gdi.CreateDIBSection.restype=w.HBITMAP
        self.gdi.SelectObject.argtypes=[w.HDC,w.HANDLE];self.gdi.SelectObject.restype=w.HANDLE
        self.gdi.DeleteObject.argtypes=[w.HANDLE]
        self.gdi.DeleteDC.argtypes=[w.HDC]
        self.gdi.BitBlt.argtypes=[w.HDC,c.c_int,c.c_int,c.c_int,c.c_int,w.HDC,c.c_int,c.c_int,w.DWORD]
        self.dc=None;self.memory=None;self.bitmap=None;self.original=None;self.shape=None

    def close(self):
        if self.original and self.memory:self.gdi.SelectObject(self.memory,self.original)
        if self.bitmap:self.gdi.DeleteObject(self.bitmap)
        if self.memory:self.gdi.DeleteDC(self.memory)
        if self.dc:self.user.ReleaseDC(None,self.dc)
        self.dc=self.memory=self.bitmap=self.original=self.shape=None

    def grab(self,bounds):
        left,top,right,bottom=bounds
        width,height=right-left,bottom-top
        if self.shape!=(height,width):
            self.close()
            self.dc=self.user.GetDC(None)
            self.memory=self.gdi.CreateCompatibleDC(self.dc)
            header=BitmapHeader(c.sizeof(BitmapHeader),width,-height,1,32,0,0,0,0,0,0)
            pointer=c.c_void_p()
            self.bitmap=self.gdi.CreateDIBSection(self.dc,c.byref(header),0,c.byref(pointer),None,0)
            if not self.dc or not self.memory or not self.bitmap or not pointer.value:
                self.close();raise OSError('Windows konnte keinen Aufnahme-Puffer erstellen.')
            self.original=self.gdi.SelectObject(self.memory,self.bitmap)
            self.pixels=np.ctypeslib.as_array((c.c_ubyte*(width*height*4)).from_address(pointer.value)).reshape(height,width,4)
            self.shape=(height,width)
        if not self.gdi.BitBlt(self.memory,0,0,width,height,self.dc,left,top,0x40CC0020):
            raise OSError('Windows konnte das Spielfenster nicht aufnehmen.')
        self.gdi.GdiFlush()
        # The only copied image is the requested rectangle, already in BGR.
        return self.pixels[:,:,:3].copy()
