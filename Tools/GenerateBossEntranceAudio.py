"""Original quiet, low-frequency landing and breathy threat accents; no external samples."""
import math,random,struct,wave
from pathlib import Path
target=Path(__file__).resolve().parents[1]/'CircusGame/Assets/Prototype/Audio/Combat'
rng=random.Random(41021)
for name,duration in [('BossLandingAccent',.48),('BossRoarAccent',.82)]:
    values=[];low=0;phase=0
    for i in range(int(44100*duration)):
        t=i/44100;u=t/duration;low+=.055*(rng.uniform(-1,1)-low)
        if name.startswith('BossLanding'):
            frequency=48+40*math.exp(-t*20);phase+=2*math.pi*frequency/44100
            envelope=min(1,t/.006)*math.exp(-u*7)*(1-u)
            value=(.72*math.sin(phase)+low*.9)*envelope
        else:
            frequency=76+15*math.sin(math.pi*u)+2*math.sin(t*32);phase+=2*math.pi*frequency/44100
            envelope=min(1,t/.09)*min(1,(duration-t)/.18)
            value=(.44*math.sin(phase)+.16*math.sin(phase*2)+.12*math.sin(phase*3)+low*.8)*envelope*(.8+.2*math.sin(t*24))
        values.append(value)
    peak=max(abs(v) for v in values)
    with wave.open(str(target/(name+'.wav')),'wb') as f:
        f.setparams((1,2,44100,0,'NONE','not compressed'))
        f.writeframes(b''.join(struct.pack('<h',int(v/peak*.5*32767)) for v in values))
