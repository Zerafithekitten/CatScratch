"""Recognize Altgard without requiring CatScratch's HUD to disappear first."""
import cv2
import numpy as np


def title_score(gray,title):
    search=gray[8:85,8:300]
    score=float(cv2.matchTemplate(search,title,cv2.TM_CCOEFF_NORMED).max())
    if score>=.90:return score
    # The region name is shared by English and German; the localized Map/Karte
    # prefix changes its position. Require a strong complete region match.
    region=title[:,112:]
    region_score=float(cv2.matchTemplate(search,region,cv2.TM_CCOEFF_NORMED).max())
    if region_score>=.90:return region_score
    if score>=.70:return score
    # A HUD bar can cover the lower part of the localized heading as well.
    band=region[:12,:]
    _,partial,_,point=cv2.minMaxLoc(cv2.matchTemplate(search[:34],band,cv2.TM_CCOEFF_NORMED))
    if partial>=.96:
        x,y=point;candidate=search[y:y+12,x:x+band.shape[1]];middle=band.shape[1]//2
        agreements=[float(cv2.matchTemplate(candidate[:,a:b],band[:,a:b],cv2.TM_CCOEFF_NORMED)[0,0]) for a,b in ((0,middle),(middle,band.shape[1]))]
        if min(agreements)>=.96:return min(partial,*agreements)
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
