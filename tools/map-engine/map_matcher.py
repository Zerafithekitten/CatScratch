from pathlib import Path
import sys,json,time,base64,os

import cv2,numpy as np
from map_presence import title_score
from concurrent.futures import ThreadPoolExecutor
cv2.setRNGSeed(117)
cv2.setNumThreads(2)
ROOT=Path(os.environ['CATSCRATCH_MAP_DATA']);poses=np.array(json.loads((ROOT/'reference-poses.json').read_text(encoding='utf-8')))
sift=cv2.SIFT_create(nfeatures=16000,contrastThreshold=.015);matcher=cv2.FlannBasedMatcher(dict(algorithm=1,trees=5),dict(checks=80))
def extract(im):
 gray=im if im.ndim==2 else cv2.cvtColor(im,cv2.COLOR_BGR2GRAY);mask=np.zeros(gray.shape,np.uint8);h,w=gray.shape;mask[int(h*110/1079):int(h*1040/1079),int(w*330/1919):int(w*1820/1919)]=255;return sift.detectAndCompute(gray,mask)
map_raster=cv2.imread(str(ROOT/'altgard.webp'),cv2.IMREAD_GRAYSCALE)
atlas_to_map=np.vstack([json.loads((ROOT/'alignment.json').read_text(encoding='utf-8'))['atlas_to_map'],[0,0,1]])
assert map_raster is not None, 'Missing map reference'
refs=[extract(cv2.warpAffine(map_raster,(np.vstack([pose,[0,0,1]])@np.linalg.inv(atlas_to_map))[:2],(1919,1079))) for pose in poses]


def verify_full_map(im,estimated,kt,dt):
 global map_raster,atlas_to_map
 if not (ROOT/'alignment.json').exists():return None
 if map_raster is None:
  map_raster=cv2.imread(str(ROOT/'altgard.webp'),cv2.IMREAD_GRAYSCALE)
  if map_raster is None:return None
  atlas_to_map=np.vstack([json.loads((ROOT/'alignment.json').read_text(encoding='utf-8'))['atlas_to_map'],[0,0,1]])
 base=np.vstack([estimated,[0,0,1]])
 projected=base@np.linalg.inv(atlas_to_map)
 h,w=im.shape[:2]
 rendered=cv2.warpAffine(map_raster,projected[:2],(w,h),flags=cv2.INTER_LINEAR)
 ks,ds=extract(rendered)
 if ds is None:return None
 pairs=matcher.knnMatch(ds,dt,k=2)
 good=[p[0] for p in pairs if len(p)==2 and p[0].distance<.68*p[1].distance]
 if len(good)<50:return None
 source=np.float32([ks[g.queryIdx].pt for g in good]);target=np.float32([kt[g.trainIdx].pt for g in good])
 cells=(np.floor(source[:,0]/12).astype(int)*73+np.floor(source[:,1]/12).astype(int)*37)%5
 train=cells!=0;test=~train
 if train.sum()<35 or test.sum()<8:return None
 step,inside=cv2.estimateAffinePartial2D(source[train],target[train],method=cv2.RANSAC,ransacReprojThreshold=2.5,maxIters=5000)
 if step is None or inside.sum()<30:return None
 used=inside.ravel().astype(bool)
 if cv2.contourArea(cv2.convexHull(source[train][used]))<20000:return None
 if not .85<np.hypot(step[0,0],step[1,0])<1.15:return None
 if abs(np.degrees(np.arctan2(step[1,0],step[0,0])))>.4:return None
 error=np.linalg.norm(cv2.transform(source.reshape(-1,1,2),step).reshape(-1,2)-target,axis=1)
 valid=test&(error<3)
 if valid.sum()<8:return None
 result=np.vstack([step,[0,0,1]])@base
 return dict(reference=-1,train_inliers=int(inside.sum()),independent_matches=int(valid.sum()),median_validation_error=float(np.median(error[valid])),matrix=result[:2].tolist(),scale=float(np.hypot(result[0,0],result[1,0])))


def locate(im, preferred=None, estimated=None):
 kt,dt=extract(im)
 if dt is None or len(kt)<30:return None
 if estimated is not None:
  verified=verify_full_map(im,estimated,kt,dt)
  if verified is not None:return verified
 candidates=[]
 order=list(range(len(refs)))
 if preferred in order:order.remove(preferred);order.insert(0,preferred)
 for i in order:
  ks,ds=refs[i]
  good=[m for m,n in matcher.knnMatch(ds,dt,k=2) if m.distance<.68*n.distance]
  if len(good)<40:continue
  src=np.float32([ks[m.queryIdx].pt for m in good]);dst=np.float32([kt[m.trainIdx].pt for m in good])
  # Spatial cells, rather than descriptor indices, keep repeated SIFT orientations together.
  cells=(np.floor(src[:,0]/12).astype(int)*73+np.floor(src[:,1]/12).astype(int)*37)%5
  train=cells!=0;test=~train
  if train.sum()<30 or test.sum()<8:continue
  mat,f=cv2.estimateAffinePartial2D(src[train],dst[train],method=cv2.RANSAC,ransacReprojThreshold=3,maxIters=10000,confidence=.999)
  if mat is None:continue
  use=f.ravel().astype(bool);n=int(use.sum());scale=float(np.hypot(mat[0,0],mat[1,0]));angle=float(np.degrees(np.arctan2(mat[1,0],mat[0,0])))
  if n<30 or not .1<scale<12 or abs(angle)>.4:continue
  area=min(cv2.contourArea(cv2.convexHull(src[train][use])),cv2.contourArea(cv2.convexHull(dst[train][use])))
  error=np.linalg.norm(cv2.transform(src.reshape(-1,1,2),mat).reshape(-1,2)-dst,axis=1);valid=test&(error<4)
  if area<8000 or valid.sum()<8 or np.median(error[valid])>2:continue
  global_mat=(np.vstack([mat,[0,0,1]])@np.vstack([poses[i],[0,0,1]]))[:2]
  candidates.append(dict(reference=i,train_inliers=n,independent_matches=int(valid.sum()),median_validation_error=float(np.median(error[valid])),p95_validation_error=float(np.quantile(error[valid],.95)),scale=scale,matrix=global_mat.tolist()))
  if preferred==i:return candidates[-1]
 return max(candidates,key=lambda c:c['train_inliers']) if candidates else None


class Tracker:
 """Short-lived frame-to-frame tracking; periodic reference checks prevent drift."""
 def __init__(self):
  self.pool=ThreadPoolExecutor(max_workers=1)
  self.pending=None;self.epoch=0
  self.orb=cv2.ORB_create(nfeatures=1800,fastThreshold=12)
  self.zoom_sift=cv2.SIFT_create(nfeatures=7000,contrastThreshold=.015)
  self.zoom_matcher=cv2.FlannBasedMatcher(dict(algorithm=1,trees=3),dict(checks=48))
  self.zoom_cache=[]
  self.last_reason='Referenzsuche wird vorbereitet'
  self.reset()
  self.title=cv2.imread(str(ROOT/'altgard-title.png'),cv2.IMREAD_GRAYSCALE)
  self.map_open=False

 def reset(self):
  self.epoch+=1
  self.gray=None;self.points=None;self.result=None;self.confirmed=0
  self.next_points=None;self.points_at=0;self.point_steps=0;self.stationary=False
  self.verification_at=0
  self.lost_since=None
  self.zoom_cache=[]

 def close(self):
  self.pool.shutdown(wait=False,cancel_futures=True)

 def schedule(self,frame,gray):
  if self.pending is None:
   mask=np.zeros_like(gray);mask[110:1040,330:1820]=255
   points=self.track_points(gray)
   self.verification_at=time.monotonic()
   self.pending=[self.pool.submit(locate,frame.copy(),self.result['reference'] if self.result else None,self.result['matrix'] if self.result else None),self.epoch,gray.copy(),dict(self.result) if self.result else None,np.eye(3),points,True]

 def motion(self,previous,points,gray):
  self.next_points=None;self.stationary=False
  # Exact terrain equality is safe even when an animated cursor/header changes.
  # Keep reference expiry and periodic independent validation unchanged.
  if points is not None and len(points)>=30 and np.array_equal(previous[110:1040,330:1820],gray[110:1040,330:1820]):
   self.next_points=points;self.stationary=True
   return np.eye(3)
  step=self.flow_motion(previous,points,gray)
  if step is not None:return step
  step=self.orb_motion(previous,gray)
  if step is not None:return step
  return self.zoom_motion(previous,gray)

 def track_points(self,gray):
  small=cv2.resize(gray,(960,540),interpolation=cv2.INTER_AREA)
  mask=np.zeros_like(small);mask[55:520,165:910]=255
  points=cv2.goodFeaturesToTrack(small,maxCorners=160,qualityLevel=.015,minDistance=10,mask=mask)
  if points is not None:points*=np.float32([1919/960,1079/540])
  return points

 def zoom_features(self,gray):
  for cached,features in self.zoom_cache:
   if cached is gray:return features
  mask=np.zeros_like(gray);mask[110:1040,330:1820]=255
  features=self.zoom_sift.detectAndCompute(gray,mask)
  self.zoom_cache.append((gray,features));self.zoom_cache=self.zoom_cache[-3:]
  return features

 def zoom_motion(self,previous,gray):
  # SIFT keeps its descriptors across scale changes, unlike the fast flow
  # and ORB paths. Use only on recovery, with its own detector/matcher so
  # background reference searches cannot race on OpenCV objects.
  ka,da=self.zoom_features(previous);kb,db=self.zoom_features(gray)
  if da is None or db is None or len(db)<2:return None
  pairs=self.zoom_matcher.knnMatch(da,db,k=2)
  good=[p[0] for p in pairs if len(p)==2 and p[0].distance<.68*p[1].distance]
  if len(good)<24:return None
  source=np.float32([ka[p.queryIdx].pt for p in good]);target=np.float32([kb[p.trainIdx].pt for p in good])
  cells=(np.floor(source[:,0]/12).astype(int)*73+np.floor(source[:,1]/12).astype(int)*37)%5
  train=cells!=0;test=~train
  if train.sum()<18 or test.sum()<5:return None
  mat,inliers=cv2.estimateAffinePartial2D(source[train],target[train],method=cv2.RANSAC,ransacReprojThreshold=2.5,maxIters=5000)
  if mat is None or inliers.sum()<18:return None
  used=inliers.ravel().astype(bool)
  if min(cv2.contourArea(cv2.convexHull(source[train][used])),cv2.contourArea(cv2.convexHull(target[train][used])))<3500:return None
  error=np.linalg.norm(cv2.transform(source.reshape(-1,1,2),mat).reshape(-1,2)-target,axis=1)
  if ((error<3)&test).sum()<5:return None
  if not .1<np.hypot(mat[0,0],mat[1,0])<10:return None
  if abs(np.degrees(np.arctan2(mat[1,0],mat[0,0])))>.5:return None
  return np.vstack([mat,[0,0,1]])

 def orb_motion(self,previous,gray):
  # Optical flow is limited to small motion. Descriptor matching searches
  # the whole terrain area and can recover a large drag in the next frame.
  mask=np.zeros_like(gray);mask[110:1040,330:1820]=255
  ka,da=self.orb.detectAndCompute(previous,mask)
  kb,db=self.orb.detectAndCompute(gray,mask)
  if da is None or db is None or len(db)<2:return None
  pairs=cv2.BFMatcher(cv2.NORM_HAMMING).knnMatch(da,db,k=2)
  good=[p[0] for p in pairs if len(p)==2 and p[0].distance<.72*p[1].distance]
  if len(good)<35:return None
  source=np.float32([ka[p.queryIdx].pt for p in good])
  target=np.float32([kb[p.trainIdx].pt for p in good])
  step,inliers=cv2.estimateAffinePartial2D(source,target,method=cv2.RANSAC,ransacReprojThreshold=2,maxIters=3000)
  if step is None or inliers.sum()<30 or inliers.mean()<.55:return None
  used=inliers.ravel().astype(bool)
  if min(cv2.contourArea(cv2.convexHull(source[used])),cv2.contourArea(cv2.convexHull(target[used])))<20000:return None
  if not .45<np.hypot(step[0,0],step[1,0])<2.2:return None
  if abs(np.degrees(np.arctan2(step[1,0],step[0,0])))>.5:return None
  return np.vstack([step,[0,0,1]])

 def flow_motion(self,previous,points,gray):
  if points is None or len(points)<30:return None
  previous=cv2.resize(previous,(960,540),interpolation=cv2.INTER_AREA)
  gray=cv2.resize(gray,(960,540),interpolation=cv2.INTER_AREA)
  factors=np.float32([960/1919,540/1079])
  points=points*factors
  nxt,ok,_=cv2.calcOpticalFlowPyrLK(previous,gray,points,None,winSize=(21,21),maxLevel=3)
  if nxt is None:return None
  back,back_ok,_=cv2.calcOpticalFlowPyrLK(gray,previous,nxt,None,winSize=(21,21),maxLevel=3)
  if back is None:return None
  good=(ok.ravel()==1)&(back_ok.ravel()==1)&(np.linalg.norm(back-points,axis=2).ravel()<1)
  old=points[good]/factors;new=nxt[good]/factors
  if len(old)<30:return None
  step,inliers=cv2.estimateAffinePartial2D(old,new,method=cv2.RANSAC,ransacReprojThreshold=1.8)
  if step is None or inliers.sum()<30 or inliers.mean()<=.65:return None
  if not .7<np.hypot(step[0,0],step[1,0])<1.4:return None
  if cv2.contourArea(cv2.convexHull(new[inliers.ravel().astype(bool)]))<=15000:return None
  if abs(np.degrees(np.arctan2(step[1,0],step[0,0])))>=.4:return None
  retained=new[inliers.ravel().astype(bool)].reshape(-1,2)
  retained=retained[(retained[:,0]>=330)&(retained[:,0]<1820)&(retained[:,1]>=110)&(retained[:,1]<1040)]
  self.next_points=retained.reshape(-1,1,2).astype(np.float32)
  return np.vstack([step,[0,0,1]])

 def update(self,frame,map_candidate=False):
  h,w=frame.shape[:2]
  normalized=cv2.resize(frame,(1919,1079))
  gray=cv2.cvtColor(normalized,cv2.COLOR_BGR2GRAY)
  # A terrain match alone must not keep markers over normal gameplay.
  header_score=title_score(gray,self.title)
  self.map_open=header_score>=.70 or map_candidate
  if not self.map_open:
   self.last_reason=f'Altgard-Titel nicht erkannt ({header_score:.2f})'
   self.reset();return None
  # Follow intermediate frames while the first reference search is running.
  # Otherwise a moving map gets compared against a seconds-old screenshot
  # and acquisition can repeatedly fail even though every frame is valid.
  if self.pending is not None and self.pending[1]==self.epoch and self.pending[3] is None and self.pending[6]:
   movement=self.motion(self.pending[2],self.pending[5],gray)
   if movement is None:self.pending[6]=False
   else:
    self.pending[4]=movement@self.pending[4]
    self.pending[2]=gray.copy()
    mask=np.zeros_like(gray);mask[110:1040,330:1820]=255
    self.pending[5]=self.track_points(gray)
  if self.pending is not None and self.pending[0].done():
   future,epoch,source_gray,predicted,transport,source_points,transport_valid=self.pending;self.pending=None
   anchor=future.result()
   if epoch==self.epoch:
    if predicted is None:
     if anchor is not None and transport_valid:
      anchor=dict(anchor,matrix=(transport@np.vstack([anchor['matrix'],[0,0,1]]))[:2].tolist())
      self.result=anchor;self.gray=gray
      mask=np.zeros_like(gray);mask[110:1040,330:1820]=255
      self.points=self.track_points(gray)
      self.confirmed=time.monotonic()
    elif anchor is None:
     # A failed independent check must not discard a still-valid track.
     # Keep the original confirmation deadline; retry without blanking.
     self.last_reason='Nachführung aktiv · Referenzkontrolle ohne Treffer'
    else:
     # Verify the transform of the submitted frame, never apply an old
     # reference result directly to the current moving map.
     samples=np.float32([[600,300,1],[900,600,1],[1300,850,1]])
     inverse=np.linalg.inv(np.vstack([predicted['matrix'],[0,0,1]]))
     world=samples@inverse.T
     error=np.linalg.norm(world@np.array(anchor['matrix']).T-samples[:,:2],axis=1)
     if error.max()>40:self.reset();return None
     if error.max()>3:
      # Correct accumulated drift without discarding the live transform.
      # Current<-snapshot times snapshot<-world retains all motion since
      # the background job was submitted, including intervening zoom.
      current=np.vstack([self.result['matrix'],[0,0,1]])
      corrected=current@inverse@np.vstack([anchor['matrix'],[0,0,1]])
      self.result=dict(self.result,matrix=corrected[:2].tolist())
     self.confirmed=time.monotonic()
  now=time.monotonic();result=None
  if self.gray is not None and self.points is not None and len(self.points)>=30 and now-self.confirmed<12:
   step=self.motion(self.gray,self.points,gray)
   if step is not None:
    combined=step@np.vstack([self.result['matrix'],[0,0,1]])
    result=dict(self.result,matrix=combined[:2].tolist(),mode='tracking')
  if result is None:
   if self.result is not None:
    if self.lost_since is None:self.lost_since=now
    if now-self.lost_since<1.5:
     # Zoom animation can blur one or more frames. Hide markers but keep
     # the last sharp, localized frame for the next direct comparison.
     self.last_reason='Zoom/Bewegung · Abgleich mit letzter sicherer Kartenposition'
     return None
    self.reset()
   self.schedule(normalized,gray)
   self.last_reason='Altgard-Titel erkannt · Kartenposition wird im Hintergrund gesucht'
   return None
  # The verification snapshot and its transform must refer to the same frame.
  self.lost_since=None
  self.result=result
  if now-self.verification_at>3:self.schedule(normalized,gray)
  self.gray=gray
  # Flow already supplied independently checked feature positions. Replenish
  # periodically or after descriptor recovery instead of finding 160 new
  # corners after every accepted frame.
  if self.next_points is not None and (self.stationary or (len(self.next_points)>=80 and self.point_steps<5 and now-self.points_at<.25)):
   self.points=self.next_points
   if not self.stationary:self.point_steps+=1
  else:
   self.points=self.track_points(gray);self.points_at=now;self.point_steps=0
  self.result=result
  output=dict(result)
  m=np.array(result['matrix']);m[0]*=w/1919;m[1]*=h/1079
  output['matrix']=m.tolist();output['scale']=float(np.hypot(m[0,0],m[1,0]))
  return output
