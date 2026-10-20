"""Export real Unity Play Mode frames as reviewable animations (not mocked footage)."""
from pathlib import Path
from PIL import Image, ImageDraw
root=Path(__file__).resolve().parents[1]/'CircusGame'
out=root/'Recordings/CombatPresentation'
out.mkdir(parents=True,exist_ok=True)
for folder,title in [('player-melee','PLAYER MELEE'),('enemy-ranged','RANGED ENEMY'),('boss-slam','BOSS SLAM')]:
    frames=[]
    files=sorted((root/'Temp/Presentation'/folder).glob('*.png'))
    for i,file in enumerate(files):
        frame=Image.open(file).convert('RGB')
        draw=ImageDraw.Draw(frame)
        draw.rounded_rectangle((12,106,257,134),radius=4,fill=(20,16,22))
        draw.text((22,114),f'{title}  |  {i/30:.2f}s',fill=(255,230,185))
        frames.append(frame.quantize(colors=192,dither=Image.Dither.NONE))
    frames[0].save(out/(folder+'.gif'),save_all=True,append_images=frames[1:],duration=33,loop=0,optimize=False)
    print(folder,len(frames),'frames',round((out/(folder+'.gif')).stat().st_size/1024),'KiB')
# A frame strip makes the timing reviewable without an animation player.
for folder,indices in [('player-melee',[5,8,10,20]),('enemy-ranged',[7,13,17,24]),('boss-slam',[3,20,30,34])]:
    sheet=Image.new('RGB',(960,584),(20,16,22))
    for n,i in enumerate(indices):
        im=Image.open(root/'Temp/Presentation'/folder/(f'{i:03}.png')).convert('RGB');im.thumbnail((480,270))
        x=(n%2)*480;y=(n//2)*292;sheet.paste(im,(x,y+22));ImageDraw.Draw(sheet).text((x+12,y+6),f'{folder}: {i/30:.2f}s',fill=(255,230,185))
    sheet.save(out/(folder+'-frames.jpg'),quality=93)
for name in ['presentation-verification','finale-verification','three-act-verification']:
    (out/(name+'.txt')).write_bytes((root/'Temp'/(name+'.txt')).read_bytes())
for name in ['balance','recording']:
    source=root/'Temp/Presentation'/(name+'.txt')
    if source.exists(): (out/(name+'.txt')).write_bytes(source.read_bytes())
