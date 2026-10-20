"""Package actual Unity entrance footage and verification output for review."""
from pathlib import Path
from PIL import Image,ImageDraw
project=Path(__file__).resolve().parents[1]/'CircusGame'
source=project/'Temp/BossEntranceFrames'
target=project/'Recordings/BossEntrance'
target.mkdir(parents=True,exist_ok=True)
images=[]
for i,file in enumerate(sorted(source.glob('*.png'))):
    image=Image.open(file).convert('RGB');draw=ImageDraw.Draw(image)
    draw.rounded_rectangle((12,106,258,134),radius=4,fill=(20,16,22));draw.text((22,114),f'ACT 3 ENTRANCE | {i/30:.2f}s',fill=(255,230,185))
    images.append(image.quantize(colors=224,dither=Image.Dither.NONE))
images[0].save(target/'boss-entrance.gif',save_all=True,append_images=images[1:],duration=33,loop=0,optimize=False)
sheet=Image.new('RGB',(960,876),(20,16,22))
for n,i in enumerate([15,28,35,67,80,120]):
    image=Image.open(source/f'{i:03}.png').convert('RGB');image.thumbnail((480,270))
    x=n%2*480;y=n//2*292;sheet.paste(image,(x,y+22));ImageDraw.Draw(sheet).text((x+12,y+6),f'Entrance {i/30:.2f}s',fill=(255,230,185))
sheet.save(target/'entrance-frames.jpg',quality=94)
for name in ['boss-entrance-verification','three-act-verification','finale-verification','ground-pattern-verification']:
    path=project/'Temp'/(name+'.txt')
    if path.exists(): (target/(name+'.txt')).write_bytes(path.read_bytes())
(target/'recording.txt').write_bytes((source/'recording.txt').read_bytes())
print(len(images),'actual frames exported;',round((target/'boss-entrance.gif').stat().st_size/1024),'KiB')
