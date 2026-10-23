"""Synthetic map/overlay feedback checks; no desktop capture or game interaction."""
import argparse,json,os,time
from pathlib import Path
import cv2
import numpy as np

parser=argparse.ArgumentParser();parser.add_argument('--data',required=True);args=parser.parse_args()
os.environ['CATSCRATCH_MAP_DATA']=str(Path(args.data).resolve())
import map_matcher as engine
checks=[]
def check(value,label):
    assert value,label
    checks.append(label)
base=engine.terrain_mask((1079,1919))
outside=[[-100,-100,-10,-10],[2000,1100,2100,1200]]
check(np.array_equal(base,engine.terrain_mask((1079,1919),outside)),'Offscreen symbols never mask unrelated terrain')
regions=[[500,240,620,340],[1150,500,1250,600],[1550,110,1820,650]]
mask=engine.terrain_mask((1079,1919),regions)
check(mask[280,550]==0 and mask[400,800]==255,'Only rendered marker and filter areas are excluded')
transform=np.vstack([engine.poses[0],[0,0,1]])
terrain=cv2.warpAffine(engine.map_raster,(transform@np.linalg.inv(engine.atlas_to_map))[:2],(1919,1079))
title=cv2.imread(str(Path(args.data)/'altgard-title.png'),0)
terrain[18:18+title.shape[0],18:18+title.shape[1]]=title
tracker=engine.Tracker()
try:
    tracker.regions=regions
    tracker.gray=terrain.copy();tracker.points=tracker.track_points(terrain)
    tracker.result=dict(reference=0,matrix=engine.poses[0].tolist())
    tracker.confirmed=time.monotonic();tracker.verification_at=time.monotonic()+60
    rng=np.random.default_rng(117)
    for index in range(12):
        frame=terrain.copy()
        for left,top,right,bottom in regions:
            frame[top+12:bottom-12,left+12:right-12]=rng.integers(0,256,(bottom-top-24,right-left-24),dtype=np.uint8)
        result=tracker.update(cv2.cvtColor(frame,cv2.COLOR_GRAY2BGR),regions)
        check(result is not None and np.max(np.abs(np.array(result['matrix'])-engine.poses[0]))<1.5,'Stationary map remains tracked with changing overlay pixels: '+str(index+1))
    points=tracker.track_points(frame).reshape(-1,2)
    check(all(mask[min(1078,round(y)),min(1918,round(x))]!=0 for x,y in points),'Fresh optical-flow features exclude the overlay regions')
finally:tracker.close()
print(json.dumps({'success':True,'checks':checks}))
