"""Check live boss phase/reward assets and execute production transition/drop logic with API stubs."""
from pathlib import Path
import re
import yaml
from run_csharp_harness import run
ROOT=Path(__file__).resolve().parents[1]
GAME=ROOT/'Assets/Survivor_Game'
SCRIPTS=GAME/'Scripts'
def source(path):
    return '\n'.join(x for x in (SCRIPTS/path).read_text(encoding='utf-8-sig').splitlines() if not x.startswith('using '))
def method(path,signature):
    s=source(path); start=s.index(signature); end=s.index('{',start)+1; depth=1
    while depth:
        depth+=(s[end]=='{')-(s[end]=='}'); end+=1
    return s[start:end]
def documents(path):
    s=path.read_text(encoding='utf-8-sig')
    s=re.sub(r'^%.*\n','',s,flags=re.M)
    s=re.sub(r'^--- !u!\d+ &-?\d+.*$', '---',s,flags=re.M)
    return list(yaml.safe_load_all(s))
base=next(d['MonoBehaviour'] for d in documents(GAME/'Prefabs/Enemies/Enemy101.prefab')
          if d and d.get('MonoBehaviour',{}).get('m_EditorClassIdentifier','').endswith('::EnemyProjectileAttackPattern'))
guid_map={re.search(r'^guid: (\w+)',p.read_text(encoding='utf-8-sig'),re.M)[1]:Path(str(p)[:-5])
          for p in GAME.rglob('*.meta') if re.search(r'^guid: (\w+)',p.read_text(encoding='utf-8-sig'),re.M)}
rewards=set(); names=set(); signatures=set()
for number in range(101,107):
    profile=documents(GAME/f'Data/Enemies/EnemyProfile_Enemy{number}.asset')[0]['MonoBehaviour']
    assert profile['rank']==2 and profile['displayName'] not in names
    names.add(profile['displayName'])
    assert '70%' in profile['encounterDescription'] and '40%' in profile['encounterDescription']
    reward=profile['signatureReward']['guid']; assert reward not in rewards;rewards.add(reward)
    reward_asset=documents(guid_map[reward])[0]['MonoBehaviour'];assert reward_asset['kind']==5
    phases=[dict(v) for v in base['phaseVolleys']]
    if number!=101:
        mods=documents(GAME/f'Prefabs/Enemies/Enemy{number}.prefab')[0]['PrefabInstance']['m_Modification']['m_Modifications']
        for mod in mods:
            m=re.fullmatch(r'phaseVolleys.Array.data\[(\d+)\].(\w+)',mod['propertyPath'])
            if m: phases[int(m[1])][m[2]]=float(mod['value'])
    assert [int(p['minimumPhase']) for p in phases]==[1,2,3]
    assert all(1<=p['projectileCount']<=14 and 1<=p['shots']<=4 and 0<=p['spread']<=360 for p in phases)
    assert phases[0]!=phases[1]!=phases[2]
    signatures.add(tuple(tuple(p[k] for k in ('projectileCount','shots','spread','angleStep')) for p in phases))
assert len(signatures)==6
for chapter in (GAME/'Data/Rooms').glob('ChapterProfile_C*.asset'):
    candidates=documents(chapter)[0]['MonoBehaviour']['bossCandidates']
    assert 1<=len(candidates)<=3 and len({c['guid'] for c in candidates})==len(candidates)
    assert all(re.fullmatch(r'Enemy10[1-6]',guid_map[c['guid']].stem) for c in candidates)
print('PASS: six distinct live boss phase configurations, six chip rewards, and all chapter candidate references.')

code=r'''
using System;using System.Collections;using System.Collections.Generic;using System.Reflection;using UnityEngine;
namespace UnityEngine {
 public class MonoBehaviour{public T GetComponent<T>() where T:class=>null;}
 public class RequireComponent:Attribute{public RequireComponent(Type t){}} public class AddComponentMenu:Attribute{public AddComponentMenu(string s){}}
 public class Tooltip:Attribute{public Tooltip(string s){}}public class SerializeField:Attribute{}
 public static class Mathf{public static float Clamp01(float f)=>Math.Max(0,Math.Min(1,f));public static float Max(float a,float b)=>Math.Max(a,b);}
 public static class Time{public static float time;}
 public class Rigidbody2D{public Vector2 linearVelocity;}public struct Vector2{public static Vector2 zero=>new Vector2();}
 public class Coroutine{}
}
public interface IEnemyPoolLifecycle{void OnEnemySpawned();void OnEnemyDespawned();}
public class EnemyHealth {public int hp=100;public int GetCurrentHealth()=>hp;public event Action<int,int> OnHealthChanged;public void Change(int n){hp=n;OnHealthChanged?.Invoke(n,100);}}
public class EnemyAttackPattern{public int hides,initialized;public void CancelPresentation(){hides++;}public void InitializeAvailability(){initialized++;}}
public class EnemyMovement{public bool enabled;}
public class EnemyStagger{public bool IsStaggered;}
public class RoomRewardDefinition{}
'''
code+=source('Enemies/Patterns/EnemyPhaseController.cs')+source('Rooms/BossRewardDrops.cs')
code+=r'''
public class ControllerFixture {
 public EnemyHealth health=new EnemyHealth();public EnemyStagger stagger=new EnemyStagger();public EnemyAttackPattern[] patterns={new EnemyAttackPattern()};
 public EnemyMovement enemyMovement=new EnemyMovement();public bool movementDisabledByPattern=true;
 public Rigidbody2D body=new Rigidbody2D();public Coroutine runningPattern=new Coroutine();public object target=new object();
 public float nextDecisionTime,phaseTransitionUntil,phaseTransitionDelay=.9f,decisionInterval=.15f;public int starts,stops;
 void StopCoroutine(Coroutine r){stops++;} Coroutine StartCoroutine(IEnumerator r){starts++;return new Coroutine();}
 EnemyAttackPattern SelectPattern()=>patterns[0];IEnumerator RunPattern(EnemyAttackPattern p){yield return null;}
'''
controller='Enemies/Patterns/EnemyPatternController.cs'
code+='\n'+next(line for line in source(controller).splitlines() if 'public bool IsChangingPhase =>' in line)+'\n'
for signature in ['private void StopRunningPattern()', 'private void HandlePhaseChanged(int phase)', 'private void Update()',
                  'public void InterruptForStagger()', 'public void OnEnemySpawned()', 'public void OnEnemyDespawned()']:
    code+=method(controller,signature).replace('private void','public void')
code+='}\n'
code+=r'''
public class RoomFixture {
 public bool cleared,combatActive=true;public int drops,choices,opened,notifications;public Action<RoomFixture> OnRoomCleared;
 void SetDoorsOpen(bool b){if(b)opened++;}void SpawnDrops(){drops++;}void SpawnRewards(){choices++;}
'''
code+=method('Rooms/RoomInstance.cs','private void MarkCleared()').replace('private void','public void')+'}\n'
code+=r'''
public static class BossHarness {
 static int checks;static void Check(bool ok,string s){if(!ok)throw new Exception(s);checks++;}
 static void Set(object o,string f,object v)=>o.GetType().GetField(f,BindingFlags.NonPublic|BindingFlags.Instance).SetValue(o,v);
 static void Call(object o,string n)=>o.GetType().GetMethod(n,BindingFlags.NonPublic|BindingFlags.Instance).Invoke(o,null);
 public static void Run(){
  var health=new EnemyHealth();var phase=new EnemyPhaseController();Set(phase,"enemyHealth",health);Call(phase,"OnEnable");int events=0;phase.OnPhaseChanged+=p=>events++;
  health.Change(71);Check(phase.CurrentPhase==1&&events==0,"phase one above threshold");health.Change(70);Check(phase.CurrentPhase==2&&events==1,"exact 70 percent transitions once");health.Change(60);Check(events==1,"same phase does not restart delay");health.Change(40);Check(phase.CurrentPhase==3&&events==2,"exact 40 percent phase three");
  health.Change(90);Check(phase.CurrentPhase==3,"healing does not reverse phase");phase.OnEnemySpawned();Check(phase.CurrentPhase==1,"pool spawn resets phase");health.Change(30);Check(phase.CurrentPhase==3&&events==3,"large surviving hit can skip directly to phase three");phase.OnEnemySpawned();health.Change(0);Check(phase.CurrentPhase==1&&events==3,"lethal hit does not emit phase transition");
  Call(phase,"OnDisable");health.Change(30);Check(phase.CurrentPhase==1,"disabled phase unsubscribes health events");Call(phase,"OnEnable");health.Change(60);Check(events==4,"pool enable has single event subscription");phase.OnEnemyDespawned();Check(phase.CurrentPhase==1,"pool despawn resets phase");
  Time.time=10;var controller=new ControllerFixture();controller.HandlePhaseChanged(2);Check(controller.runningPattern==null&&controller.stops==1&&controller.patterns[0].hides==1,"phase cancels current attack and warning");Check(controller.IsChangingPhase&&controller.nextDecisionTime>=10.9f,"phase establishes visible firing delay");
  controller.Update();Check(controller.starts==0,"no firing during phase transition");Time.time=10.2f;controller.InterruptForStagger();controller.Update();Check(controller.starts==0,"stagger interruption cannot shorten phase guard");Time.time=11;controller.Update();Check(controller.starts==1&&!controller.IsChangingPhase,"attack selection resumes after phase delay");
  controller.health.hp=0;int stops=controller.stops;controller.HandlePhaseChanged(3);Check(controller.stops==stops,"dead enemy ignores phase callback");controller.health.hp=100;controller.HandlePhaseChanged(3);controller.OnEnemySpawned();Check(!controller.IsChangingPhase&&controller.patterns[0].initialized==1,"pool spawn clears old transition delay");controller.HandlePhaseChanged(2);controller.OnEnemyDespawned();Check(!controller.IsChangingPhase,"pool despawn clears transition cue");
  var normal=new RoomRewardDefinition();var reward=new RoomRewardDefinition();var common=new List<RoomRewardDefinition>{normal};var result=BossRewardDrops.Build(common,reward);Check(result.Count==2&&result[0]==normal&&result[1]==reward,"signature is additional to common drops");Check(common.Count==1,"building drops does not mutate floor reward data");Check(BossRewardDrops.Build(common,null)==common,"no signature preserves ordinary room path");Check(BossRewardDrops.Build(null,reward).Count==1,"boss signature works with empty common drops");Check(BossRewardDrops.Build(null,null).Count==0,"empty fallback safe");
  Check(BossRewardDrops.Build(new[]{reward},reward).Count==2,"additional guarantee remains additional even if common drop matches");var room=new RoomFixture();room.OnRoomCleared+=r=>r.notifications++;room.MarkCleared();room.MarkCleared();Check(room.drops==1&&room.choices==1&&room.opened==1&&room.notifications==1,"duplicate clear cannot duplicate signature or regular rewards");
  Console.WriteLine("PASS: "+checks+" production boss phase, transition, reuse and additive reward checks (Unity APIs mocked).");
 }
}
'''
run(code,'BossHarness.Run()','BossEncounterHarness')
room=source('Rooms/RoomInstance.cs')
choose=method('Rooms/RoomInstance.cs','public bool TryChooseBoss(GameObject prefab)')
assert choose.index('if (enemy == null)')<choose.index('chosenBossReward = enemy.Profile')
assert 'chosenBossReward = null;' in method('Rooms/RoomInstance.cs','public void Initialize(')
assert 'kind == RoomKind.Boss ? chosenBossReward : null' in method('Rooms/RoomInstance.cs','private void SpawnDrops()')
assert 'profile.SignatureReward.DisplayName' in source('Rooms/BossChoicePedestal.cs')
print('PASS: successful boss selection captures displayed reward; ordinary rooms and retries do not reuse old reward.')
