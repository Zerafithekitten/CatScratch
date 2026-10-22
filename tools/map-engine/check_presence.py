"""Focused regression checks for the HUD/title bootstrap, without screen capture."""
import argparse
import json
from pathlib import Path
import cv2
import numpy as np
from map_presence import title_score

parser=argparse.ArgumentParser();parser.add_argument('--data',required=True);args=parser.parse_args()
title=cv2.imread(str(Path(args.data)/'altgard-title.png'),0)
checks=[]
def check(condition,label):
    assert condition,label
    checks.append(label)
header=np.full((85,300),24,np.uint8)
header[18:18+title.shape[0],18:18+title.shape[1]]=title
check(title_score(header,title)>.99,'Uncovered title remains recognized')
covered=header.copy();covered[30:78,:]=28
check(float(cv2.matchTemplate(covered[8:85,8:300],title,cv2.TM_CCOEFF_NORMED).max())<.7,'Resource-bar fixture reproduces the original missed title')
check(title_score(covered,title)>.99,'HUD-covered heading is recognized from matching text fragments')
altered=covered.copy();altered[18:30,114:230]=altered[18:30,114:230][:,::-1]
check(title_score(altered,title)<.7,'Matching Map prefix with a different region fragment is rejected')
check(title_score(np.full((85,300),24,np.uint8),title)<.7,'Empty gameplay header does not look like a map')
rng=np.random.default_rng(117)
check(all(title_score(rng.integers(0,256,(85,300),dtype=np.uint8),title)<.7 for _ in range(50)),'Unrelated headers are rejected')
print(json.dumps({'success':True,'checks':checks}))
