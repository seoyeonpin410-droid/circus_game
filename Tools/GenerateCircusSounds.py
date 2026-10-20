"""Generate original Circus sound effects. Python standard library only.
No recordings, samples, or third-party sounds are used.
Run from repository root: python Tools/GenerateCircusSounds.py
"""
from pathlib import Path
import math, random, struct, wave
RATE=22050
ROOT=Path(__file__).resolve().parents[1]/'CircusGame/Assets/Prototype/Audio/Generated'
CUES={
 'Countdown':(.23,880,'bell'), 'Start':(.72,880,'up'),
 'ActChange':(.95,440,'up'), 'Clear':(1.4,523.25,'up'),
 'PlayerMelee':(.32,660,'swing'), 'PlayerShot':(.52,1320,'magic'),
 'PlayerHit':(.18,180,'hit'), 'PlayerDeath':(.85,330,'down'),
 'EnemyHit':(.14,250,'hit'), 'EnemyDeath':(.45,200,'down'),
 'BossArrival':(1.4,110,'arrival'), 'BossMelee':(.48,100,'swing'),
 'BossShot':(.75,440,'magic'), 'BossHit':(.23,140,'hit'),
 'BossDeath':(1.65,220,'down'), 'FlameFlight':(.65,720,'flight'),
 'FlameImpact':(.35,990,'impact'), 'Warning':(.36,660,'warning'),
 'FlameLaunch':(.6,1100,'magic')}
def render(name,duration,freq,kind):
 rng=random.Random(713+list(CUES).index(name)); samples=[]
 for i in range(round(duration*RATE)):
  t=i/RATE; u=t/duration; noise=rng.uniform(-1,1)
  def bell(f,at=0):
   x=t-at
   if x<0:return 0
   return sum(a*math.sin(2*math.pi*f*m*x)*math.exp(-(5+2*m)*x/duration) for m,a in ((1,1),(1.497,.38),(2.401,.17)))
  if kind=='bell':v=bell(freq)
  elif kind=='up':v=sum(bell(freq*r,j*duration*.16)*.55 for j,r in enumerate((1,1.25,1.5,2)))
  elif kind=='down':v=math.sin(2*math.pi*freq*(t-.32*t*t/duration))*(1-u)**2+.17*bell(freq*.5)
  elif kind=='hit':v=(.45*noise+.65*math.sin(2*math.pi*freq*t))*math.exp(-8*u)
  elif kind=='swing':v=.36*noise*math.sin(math.pi*u)**2+.7*math.sin(2*math.pi*freq*(t+.6*t*t/duration))*math.exp(-5*u)+.3*bell(freq*3)
  elif kind=='magic':v=.65*bell(freq)+.35*math.sin(2*math.pi*(freq*.35*t+freq*.75*t*t/duration))*math.exp(-7*u)+noise*.08*math.sin(math.pi*u)**2
  elif kind=='flight':v=(math.sin(2*math.pi*freq*t)+.24*math.sin(2*math.pi*freq*1.5*t))*(.7+.3*math.sin(2*math.pi*3*t))*.3
  elif kind=='impact':v=.6*bell(freq)+.25*noise*math.exp(-14*u)+.35*math.sin(2*math.pi*160*t)*math.exp(-10*u)
  elif kind=='warning':v=math.sin(2*math.pi*freq*t)*(.5+.5*math.sin(2*math.pi*10*t))*math.exp(-2*u)
  else:v=(math.sin(2*math.pi*freq*t)+.4*math.sin(2*math.pi*freq*1.5*t))*math.sin(math.pi*u)**2+.2*bell(freq*4)
  v*=min(1,t/.004)*min(1,(duration-t)/.018)
  if kind=='flight':v*=min(1,t/.04)*min(1,(duration-t)/.04)
  samples.append(v)
 peak=max(abs(v) for v in samples) or 1
 return b''.join(struct.pack('<h',int(max(-1,min(1,v/peak*.62))*32767)) for v in samples)
def main():
 ROOT.mkdir(parents=True,exist_ok=True)
 for name,(duration,freq,kind) in CUES.items():
  with wave.open(str(ROOT/(name+'.wav')),'wb') as stream:
   stream.setnchannels(1);stream.setsampwidth(2);stream.setframerate(RATE);stream.writeframes(render(name,duration,freq,kind))
 print(f'Generated {len(CUES)} original mono PCM WAV effects in {ROOT}')
if __name__=='__main__':main()
