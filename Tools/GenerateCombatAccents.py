"""Two short, deliberately soft original accents. No downloaded samples."""
import math, random, struct, wave
from pathlib import Path
out=Path(__file__).resolve().parents[1]/'CircusGame/Assets/Prototype/Audio/Combat'
rng=random.Random(721)
for name,duration,bass in [('MeleeAccent',.12,115),('SlamAccent',.42,65)]:
    data=[]; filtered=0
    for i in range(int(44100*duration)):
        t=i/44100;u=t/duration
        filtered+=.1*(rng.uniform(-1,1)-filtered)
        attack=min(1,t/.006);envelope=attack*math.exp(-u*6)*(1-u)
        signal=(.6*math.sin(2*math.pi*(bass*t+18*t*math.exp(-t*18)))+.5*filtered)*envelope
        data.append(signal)
    peak=max(abs(v) for v in data)
    with wave.open(str(out/(name+'.wav')),'wb') as f:
        f.setparams((1,2,44100,0,'NONE','not compressed'))
        f.writeframes(b''.join(struct.pack('<h',int(v/peak*.55*32767)) for v in data))
