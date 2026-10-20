"""Original deterministic PCM effects. No recordings, copyrighted samples, or AI model.
Physical hits use filtered noise and decaying body resonance; magic alone uses metal partials.
"""
import math, random, wave, struct
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]/'CircusGame/Assets/Prototype/Audio/Combat'
ROOT.mkdir(parents=True,exist_ok=True)
RATE=44100
random.seed(20261020)
def write(name,duration,kind,freq=100):
    data=[]; low=0.0
    for i in range(int(RATE*duration)):
        t=i/RATE; p=t/duration; noise=random.uniform(-1,1)
        low+=.16*(noise-low)
        if kind=='hit':
            x=.6*low*math.exp(-t*38)+.45*math.sin(2*math.pi*(freq*t+3*(1-math.exp(-t*20))))*math.exp(-t*24)+.18*noise*math.exp(-t*110)
        elif kind=='heavy':
            x=.55*low*math.exp(-t*8)+.5*math.sin(2*math.pi*(freq*t+5*(1-math.exp(-t*9))))*math.exp(-t*10)
        elif kind=='swing':
            x=(.9*low+.15*noise)*math.sin(math.pi*p)**1.3+.18*math.sin(2*math.pi*(freq*t+35*t*t))*math.sin(math.pi*p)**2
        elif kind=='launch':
            x=(noise-low)*.4*math.sin(math.pi*p)**.6+.4*math.sin(2*math.pi*(freq*t-180*t*t))*math.exp(-t*12)
        elif kind=='charge':
            x=(.5*math.sin(2*math.pi*(freq*t+80*t*t))+.13*math.sin(2*math.pi*freq*2.37*t))*(.2+.8*p)*math.sin(math.pi*p)+low*.2
        elif kind=='metal':
            x=sum(a*math.sin(2*math.pi*freq*r*t)*math.exp(-t*d) for a,r,d in [(0.5,1,8),(.3,2.76,13),(.16,5.4,22)])
        elif kind=='dissolve':
            x=(low*.6+.3*math.sin(2*math.pi*(freq*t-30*t*t)))*math.exp(-t*4)
        else: # Short electronic UI cue: never a bell.
            x=math.sin(2*math.pi*freq*t)*.5*math.sin(math.pi*p)**2
        fade=min(1,t/.003,(duration-t)/.025)
        data.append(x*max(0,fade))
    peak=max(abs(x) for x in data) or 1
    with wave.open(str(ROOT/(name+'.wav')),'wb') as f:
        f.setparams((1,2,RATE,0,'NONE','not compressed'))
        f.writeframes(b''.join(struct.pack('<h',round(x/peak*.68*32767)) for x in data))
for args in [
 ('Countdown',.11,'beep',780),('Start',.23,'beep',1080),('ActChange',.26,'beep',620),('Clear',.5,'beep',920),
 ('PlayerShot',.27,'launch',750),('PlayerHit',.19,'hit',125),('PlayerDeath',.65,'dissolve',145),
 ('EnemyHit',.13,'hit',210),('EnemyDeath',.32,'dissolve',180),('BossArrival',.9,'charge',80),
 ('BossMelee',.4,'swing',65),('BossShot',.4,'launch',280),('BossHit',.2,'hit',95),('BossDeath',1.1,'dissolve',90),
 ('FlameImpact',.24,'hit',430),('Warning',.24,'metal',1150),('FlameLaunch',.32,'launch',520),
 ('MagicCharge',.65,'charge',95),('PhaseShift',.7,'charge',65)]: write(*args)
print('Created 19 original, role-specific effects in',ROOT)
