"""Check foreground client selection with fake metadata, without capturing the desktop."""
import ctypes
import json
from types import SimpleNamespace
from unittest.mock import Mock,patch
from game_window import GameWindow

checks=[]
api=SimpleNamespace(GetForegroundWindow=Mock(return_value=101),
    GetWindowThreadProcessId=Mock(side_effect=lambda handle,pid:setattr(pid._obj,'value',321)),
    GetClientRect=Mock(),ClientToScreen=Mock())
def rect(handle,value):
    value._obj.right=2560;value._obj.bottom=1440;return 1
def point(handle,value):
    value._obj.x=-2560;value._obj.y=80;return 1
api.GetClientRect.side_effect=rect;api.ClientToScreen.side_effect=point
for configured in ('AION2.exe','AION2-Win64-Shipping.exe','PurpleLauncher.exe'):
    for client in ('aion2.exe','aion2-win64-shipping.exe'):
        with patch.object(ctypes.windll,'user32',api),patch('game_window.process_basename',return_value=client):
            source=GameWindow(configured)
            assert source.bounds()==(-2560,80,0,1520),(configured,client)
            assert source.reason=='waiting-map'
            checks.append(configured+' recognizes '+client+' with physical multi-monitor bounds')
for client in ('chrome.exe','aion2-helper.exe',None):
    with patch.object(ctypes.windll,'user32',api),patch('game_window.process_basename',return_value=client):
        source=GameWindow()
        assert source.bounds() is None and source.reason=='waiting-game'
        checks.append('Unrelated or unidentified foreground process is rejected: '+str(client))
print(json.dumps({'success':True,'checks':checks}))
