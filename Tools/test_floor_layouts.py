from pathlib import Path
import re,json,yaml
root=Path(__file__).resolve().parents[1];base=root/'Assets/Survivor_Game';out=root/'Temp/ProjectVerification'
def read(p):return p.read_text(encoding='utf-8-sig')
def source(p):return re.sub(r'^using .*?;\s*','',read(p),flags=re.M)
def mono(p,cls):
 s=re.sub(r'^%.*\n','',read(p),flags=re.M)
 s=re.sub(r'^--- !u!\d+ &-?\d+.*$','---',s,flags=re.M)
 return next(d['MonoBehaviour'] for d in yaml.load_all(s,Loader=yaml.CSafeLoader) if d and 'MonoBehaviour' in d and d['MonoBehaviour'].get('m_EditorClassIdentifier','').endswith('::'+cls))
metas={re.search(r'^guid: (\w+)',read(p),re.M)[1]:p.with_suffix('') for p in base.rglob('*.meta') if re.search(r'^guid: (\w+)',read(p),re.M)}
profiles=[]
for p in sorted((base/'Data/Rooms/Floors').glob('*.asset')):
 f=mono(p,'FloorProfile');boss=bool(f.get('isBossStage',False)); pools=[]
 for pool in f['roomPools']:
  rooms=[]
  for ref in pool['prefabs']:
   r=mono(metas[ref['guid']],'RoomInstance')
   rooms.append(f'new RoomInstance((RoomKind){r["kind"]},{json.dumps(r["designId"])})')
  pools.append(f'new FloorProfile.RoomPool{{kind=(RoomKind){pool["kind"]},prefabs=new RoomInstance[]{{'+','.join(rooms)+'}}')
 vals={'RoomCount':2 if boss else f['roomCount'],'EliteRoomCount':0 if boss else f['eliteRoomCount'],'TreasureRoomCount':0 if boss else f['treasureRoomCount'],'ShopRoomCount':0 if boss else f['shopRoomCount']}
 fields=','.join(f'{k}={v}' for k,v in vals.items())
 profiles.append('new FloorProfile{DisplayName='+json.dumps(p.stem)+','+fields+',IsBossStage='+str(boss).lower()+',KeepNonCombatRoomsAwayFromStart='+str(bool(f.get('keepNonCombatRoomsAwayFromStart',False))).lower()+',roomPools=new FloorProfile.RoomPool[]{'+','.join(pools)+'}}')
old=read(root/'Tools/FloorLayoutTest.template.txt')
code=old[:old.index('public static class StageRulesHarness')]+source(base/'Scripts/Rooms/FloorGenerator.cs')+'\n'+old[old.index('public static class StageRulesHarness'):]
code=code.replace('public struct Vector3 { public Vector3','public struct Vector3 { public static Vector3 zero=>new Vector3(); public Vector3')
a=code.index('new FloorProfile[]',code.index('static FloorProfile[] Profiles()'));b=code.index(';',a)
code=code[:a]+'new FloorProfile[]{'+','.join(profiles)+'}'+code[b:]
(out/'MultiroomLayoutsHarness.cs').write_text(code,encoding='utf-8')
run=read(base/'Scripts/Rooms/RunManager.cs')
assert 'FloorGenerator.Generate(profile, context, floorRoot)' in run and 'FloorGenerator.GenerateRoom(' not in run
assert 'currentFloor.IsCombatCleared' in run and 'RoomRoutePlanner.CreateChoices' not in run
first=mono(base/'Data/Rooms/Floors/FloorProfile_C1_F1.asset','FloorProfile')
assert first['roomCount']==7 and first['shopRoomCount']==0 and first['treasureRoomCount']==1 and first['keepNonCombatRoomsAwayFromStart']==1
for c in range(1,7):
 chapter=mono(base/f'Data/Rooms/ChapterProfile_C{c}.asset','ChapterProfile')
 assert len(chapter['floors'])==(4 if c<=3 else 5) and 1<=len(chapter['bossCandidates'])<=3
 assert mono(metas[chapter['bossFloor']['guid']],'FloorProfile')['isBossStage']==1
print('Generated fresh production multiroom generator tests for all',len(profiles),'current profiles; verified 6 maps, 4/5 stages, separate selectable bosses and first-stage rules.')

from run_csharp_harness import run
run(code,"StageRulesHarness.Run()","MultiroomLayoutsHarness")
