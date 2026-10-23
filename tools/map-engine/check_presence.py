"""Focused regression checks for the HUD/title bootstrap, without screen capture."""
import argparse
import json
from pathlib import Path
import cv2
import numpy as np
from map_presence import title_score,map_ui_candidate

parser=argparse.ArgumentParser();parser.add_argument('--data',required=True);args=parser.parse_args()
title=cv2.imread(str(Path(args.data)/'altgard-title.png'),0)
checks=[]
def check(condition,label):
    assert condition,label
    checks.append(label)
header=np.full((85,300),24,np.uint8)
header[18:18+title.shape[0],18:18+title.shape[1]]=title
check(title_score(header,title)>.99,'Uncovered title remains recognized')
localized=np.full((85,300),24,np.uint8)
region=title[:,112:]
localized[18:18+region.shape[0],152:152+region.shape[1]]=region
check(float(cv2.matchTemplate(localized[8:85,8:300],title,cv2.TM_CCOEFF_NORMED).max())<.90,'Localized-prefix fixture reduces confidence in the English full-title detector')
check(title_score(localized,title)>.99,'Altgard is recognized independently of the translated Map/Karte prefix')
localized[30:78,:]=28
check(title_score(localized,title)>.99,'Localized heading stays recognizable under a resource bar')
covered=header.copy();covered[30:78,:]=28
check(float(cv2.matchTemplate(covered[8:85,8:300],title,cv2.TM_CCOEFF_NORMED).max())<.7,'Resource-bar fixture reproduces the original missed title')
check(title_score(covered,title)>.99,'HUD-covered heading is recognized from matching text fragments')
altered=covered.copy();altered[18:30,114:230]=altered[18:30,114:230][:,::-1]
check(title_score(altered,title)<.7,'Matching Map prefix with a different region fragment is rejected')
check(title_score(np.full((85,300),24,np.uint8),title)<.7,'Empty gameplay header does not look like a map')
rng=np.random.default_rng(117)
check(all(title_score(rng.integers(0,256,(85,300),dtype=np.uint8),title)<.7 for _ in range(50)),'Unrelated headers are rejected')
close=cv2.imread(str(Path(args.data)/'map-close.png'),0)
left=np.full((85,300),24,np.uint8);left[18:18+title.shape[0],18:43]=title[:,:25]
right=np.full((85,75),24,np.uint8);right[21:21+close.shape[0],23:23+close.shape[1]]=close
check(map_ui_candidate(left,right,title,close),'Emblem and close button authorize a terrain search when text is covered')
check(not map_ui_candidate(np.full_like(left,24),right,title,close),'Close button alone cannot authorize a terrain search')
check(not map_ui_candidate(left,np.full_like(right,24),title,close),'Emblem alone cannot authorize a terrain search')
print(json.dumps({'success':True,'checks':checks}))
