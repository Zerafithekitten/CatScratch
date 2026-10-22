"""Recognize Altgard without requiring CatScratch's HUD to disappear first."""
import cv2
import numpy as np


def title_score(gray,title):
    search=gray[8:85,8:300]
    score=float(cv2.matchTemplate(search,title,cv2.TM_CCOEFF_NORMED).max())
    if score>=.70:return score
    # Resource bars commonly cover y=30..54. The top of the heading remains
    # visible. Require a very strong match of the whole strip AND both text
    # halves at the same location; never merely reduce the full-title threshold.
    band=title[:12,:]
    _,partial,_,point=cv2.minMaxLoc(cv2.matchTemplate(search[:34],band,cv2.TM_CCOEFF_NORMED))
    if partial<.93:return score
    x,y=point;candidate=search[y:y+12,x:x+band.shape[1]]
    for left,right in ((40,96),(96,band.shape[1])):
        expected=band[:,left:right];observed=candidate[:,left:right]
        if np.std(expected)<12:return score
        agreement=float(cv2.matchTemplate(observed,expected,cv2.TM_CCOEFF_NORMED)[0,0])
        if agreement<.93:return score
        partial=min(partial,agreement)
    return max(score,float(partial))
