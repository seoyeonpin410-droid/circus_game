"""Original procedural temp music/effects; no external samples or melodies.
Percussion is an approximation, not recordings of traditional Korean instruments.
"""
import math, random, wave, struct
from pathlib import Path
from array import array
ROOT=Path(__file__).resolve().parents[1]/'CircusGame/Assets/Prototype/Audio'
RATE=22050
random.seed(731)
def save(path,data,peak=.7):
    path.parent.mkdir(parents=True,exist_ok=True)
    maximum=max(abs(x) for x in data) or 1
    with wave.open(str(path),'wb') as out:
        out.setparams((1,2,RATE,0,'NONE','not compressed'))
        out.writeframes(b''.join(struct.pack('<h',int(max(-1,min(1,x/maximum*peak))*32767)) for x in data))
def strike(data,time,kind,gain=1,freq=90):
    length={'drum':.65,'rim':.16,'metal':1.6,'sweep':.5,'rise':.75,'thump':.55}[kind]
    start=int(time*RATE);low=0
    for i in range(int(length*RATE)):
        t=i/RATE;noise=random.uniform(-1,1);low+=.13*(noise-low)
        if kind in ('drum','thump'):
            x=(math.sin(2*math.pi*(freq*t+2*(1-math.exp(-t*22))))*.7*math.exp(-t*9)+low*.6*math.exp(-t*22)+noise*.05*math.exp(-t*70))
        elif kind=='rim':x=(noise-low)*math.exp(-t*35)*.4+math.sin(2*math.pi*540*t)*math.exp(-t*45)*.3
        elif kind=='metal':x=sum(a*math.sin(2*math.pi*freq*r*t)*math.exp(-t*d) for a,r,d in [(.4,1,3),(.2,2.73,6),(.1,5.2,10)])
        elif kind=='sweep':x=(low*.8+noise*.1)*math.sin(math.pi*t/length)**1.4
        else:x=(math.sin(2*math.pi*(65*t+75*t*t))*.35+low*.3)*math.sin(math.pi*t/length)**1.5
        x*=gain*min(1,t/.003,(length-t)/.03)
        data[(start+i)%len(data)]+=x
def music(name,bpm,boss=False,layer=False):
    pulse=60/bpm/3;duration=12*pulse*8;n=round(duration*RATE);data=array('f',[0])*n
    if not layer:
        # Slow non-heroic tension bed and an inharmonic overtone; exact loop harmonics.
        cycles=round(73.4*duration);upper=round(110*duration)
        for i in range(n):
            p=i/n;t=i/RATE
            data[i]=.10*math.sin(2*math.pi*cycles*p)*(.8+.2*math.sin(2*math.pi*4*p))+.045*math.sin(2*math.pi*upper*p)+.018*math.sin(2*math.pi*(cycles+1)*p)
    for bar in range(8):
        base=bar*12*pulse
        if not layer:
            for step,gain,f in [(0,.8,70),(4,.55,105),(6,.7,74),(10,.45,115)]:strike(data,base+step*pulse,'drum',gain,f)
            for step in ([2,5,8,11] if boss else [5,11]):strike(data,base+step*pulse,'rim',.25 if boss else .15)
            if bar%2==0:strike(data,base+9*pulse,'metal',.13,587 if bar%4==0 else 783)
        else:
            for step in [1,3,7,9,11]:strike(data,base+step*pulse,'rim',.28)
            for step in [2,8]:strike(data,base+step*pulse,'drum',.35,130)
    save(ROOT/'Music'/f'{name}.wav',data,.55 if not layer else .45)
music('OrdinaryPerformance',84)
music('DokkaebiFinale',96,True)
music('FinalPercussion',96,True,True)
for name,kind,freq,gain in [('BossPrepare','sweep',80,.3),('SlamPrepare','rise',65,.4),('BossSlam','thump',48,1),('DashPrepare','rise',90,.5),('BossDash','sweep',80,1)]:
    data=array('f',[0])*int(RATE*.8);strike(data,0,kind,gain,freq);save(ROOT/'Combat'/f'{name}.wav',data)
# A heavier original death impact and a restrained ritual transition.
for name in ['PhaseShift','BossDeath']:
    data=array('f',[0])*int(RATE*1.5);strike(data,0,'thump',.7,55)
    if name=='PhaseShift':strike(data,.08,'metal',.2,520)
    else:
        strike(data,.12,'sweep',.65);strike(data,.35,'drum',.35,45)
    save(ROOT/'Combat'/f'{name}.wav',data)
print('Generated 3 looping music stems, 5 pattern cues, and 2 revised ritual/death cues')
