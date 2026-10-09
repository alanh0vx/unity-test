"""Original procedural air/cutting Foley; deterministic, no sampled recordings."""
import math, random, wave, struct
from pathlib import Path
root=Path(__file__).resolve().parents[1]/'Assets/VXRL2/Audio'
rate=44100
for name,duration,kind in [('ShurikenThrow',.18,'throw'),('TantoSlash',.23,'slash'),('KatanaChop',.36,'slash'),('BladeCut',.17,'cut'),('BladeWall',.12,'wall')]:
    rng=random.Random(name); values=[]; low=0; slow=0
    for i in range(int(rate*duration)):
        t=i/rate; u=t/duration; noise=rng.uniform(-1,1)
        low+=.20*(noise-low);slow+=.025*(noise-slow)
        if kind in ('throw','slash'):
            env=math.sin(math.pi*u)**(2 if kind=='throw' else 1.6)
            flutter= .78+.22*math.sin(2*math.pi*(65*t-90*t*t)) if kind=='throw' else 1
            v=(low-slow)*env*flutter
        else:
            env=(1-math.exp(-t*1800))*math.exp(-t*36)
            v=(.6*low+.24*math.sin(2*math.pi*(125*t-180*t*t)))*env
            if kind=='cut':v+=(noise-low)*.12*math.exp(-t*55)*(1-math.exp(-t*1500))
        values.append(v)
    scale=(.68 if kind in ('cut','wall') else .52)/max(abs(x) for x in values)
    with wave.open(str(root/(name+'.wav')),'wb') as out:
        out.setparams((1,2,rate,0,'NONE','not compressed'))
        out.writeframes(b''.join(struct.pack('<h',round(x*scale*32767)) for x in values))
print('Generated five original mono 44.1kHz Foley clips')
