"""Small original role-specific boss cues. No external recordings or copyrighted samples."""
from pathlib import Path
import math,random,struct,wave
out=Path(__file__).resolve().parents[1]/'CircusGame/Assets/Prototype/Audio/Arena';out.mkdir(exist_ok=True)
profiles=[('SequencePrepare',.3,280,430,.2),('SequenceRelease',.16,340,190,1.8),('OrbitPrepare',.55,523,585,.1),('OrbitRelease',.18,680,260,.5),('GroundMark',.4,110,130,.25),('GroundImpact',.22,110,55,1),('DashPrepare',.32,180,90,1.2),('DashRelease',.23,140,200,3),('PhaseShift',.55,180,270,.2),('UltimateCharge',.75,65,100,.8),('UltimateBurst',.4,120,400,1.7),('UltimateImpact',.45,90,42,1.3)]
rng=random.Random(20261022)
for name,duration,start,end,noise in profiles:
    samples=[];phase=low=0
    for i in range(int(44100*duration)):
        t=i/44100;u=t/duration;phase+=2*math.pi*(start+(end-start)*u)/44100
        low+=.08*(rng.uniform(-1,1)-low)
        if name.endswith('Prepare') or name in ['GroundMark','UltimateCharge','PhaseShift']:
            env=math.sin(math.pi*u)**.8;signal=.45*math.sin(phase)+.14*math.sin(phase*1.5)
            if name=='OrbitPrepare':signal+=.1*math.sin(phase*2.8)*math.exp(-u*3)
        else:
            env=min(1,t/.008)*(1-u)*math.exp(-u*3);signal=.6*math.sin(phase)+.15*math.sin(phase*2)
        samples.append((signal+low*noise)*env)
    peak=max(abs(v) for v in samples)
    with wave.open(str(out/(name+'.wav')),'wb') as f:
        f.setparams((1,2,44100,0,'NONE','not compressed'));f.writeframes(b''.join(struct.pack('<h',int(v/peak*.48*32767)) for v in samples))
