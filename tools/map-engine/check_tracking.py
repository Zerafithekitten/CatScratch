"""Offline motion regression checks; never capture the screen or query the game."""
import argparse
import os
from pathlib import Path
import time

parser=argparse.ArgumentParser()
parser.add_argument('--data',required=True)
parser.add_argument('--frame')
args=parser.parse_args()
os.environ['CATSCRATCH_MAP_DATA']=str(Path(args.data).resolve())
import map_matcher as engine
cv2,np=engine.cv2,engine.np

if args.frame:
 frame=cv2.imread(args.frame)
 assert frame is not None,'Missing offline frame'
 frame=cv2.resize(frame,(1919,1079))
else:
 # A reproducible textured surface exercises the flow, not map recognition.
 frame=np.random.default_rng(117).integers(0,256,(1079,1919,3),dtype=np.uint8)
 frame=cv2.GaussianBlur(frame,(5,5),0)

def seed():
 tracker=engine.Tracker()
 tracker.result={'matrix':[[1.,0.,0.],[0.,1.,0.]],'reference':0}
 tracker.gray=cv2.cvtColor(frame,cv2.COLOR_BGR2GRAY)
 tracker.points=tracker.track_points(tracker.gray)
 tracker.confirmed=time.monotonic()
 tracker.verification_at=time.monotonic()+120
 return tracker

checks=[]
for mode,count in [('stationary',30),('pan',60),('zoom',30)]:
 tracker=seed();maximum=0.;initial_points=tracker.points
 try:
  for i in range(count):
   scale=1+.005*(i+1) if mode=='zoom' else 1.
   step=cv2.getRotationMatrix2D((960,540),0,scale)
   if mode=='pan':step[:,2]+=[4*(i+1),2*(i+1)]
   image=frame if mode=='stationary' else cv2.warpAffine(frame,step,(1919,1079))
   if mode!='stationary':image[:110]=frame[:110];image[:,:330]=frame[:,:330]
   result=tracker.update(image,map_candidate=True)
   assert result is not None,(mode,i,tracker.last_reason)
   probes=np.float32([[[650,350]],[[1000,600]],[[1300,800]]])
   error=np.linalg.norm(cv2.transform(probes,np.array(result['matrix']))-cv2.transform(probes,step),axis=2).max()
   maximum=max(maximum,float(error))
   assert error<2.,(mode,i,error)
  if mode=='stationary':assert tracker.points is initial_points,'Static map needlessly replaced tracking points'
  checks.append({'mode':mode,'accepted':count,'max_error_px':maximum})
 finally:tracker.close()

tracker=seed()
try:
 tracker.confirmed=time.monotonic()-13
 assert tracker.update(frame,map_candidate=True) is None,'Identical pixels must not extend expired verification'
finally:tracker.close()
tracker=seed()
try:
 assert tracker.update(np.zeros_like(frame)) is None and tracker.result is None,'Leaving the map must clear the track'
finally:tracker.close()
print(engine.json.dumps({'success':True,'motion':checks,'expired_reference_rejected':True,'closed_map_cleared':True}),flush=True)
