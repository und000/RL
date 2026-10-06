"""Execute production projectile lifecycle/scheduler and result summary using Unity API stubs."""
from pathlib import Path
import re
from run_csharp_harness import run
ROOT = Path(__file__).resolve().parents[1]
SCRIPTS = ROOT/'Assets/Survivor_Game/Scripts'
def source(path):
    return '\n'.join(x for x in (SCRIPTS/path).read_text(encoding='utf-8-sig').splitlines() if not x.startswith('using '))
code = r'''
using System; using System.Collections.Generic; using System.Text; using System.Reflection; using UnityEngine;
namespace UnityEngine.SceneManagement {
 public struct Scene{public int id;public static bool operator==(Scene a,Scene b)=>a.id==b.id;public static bool operator!=(Scene a,Scene b)=>a.id!=b.id;public override bool Equals(object x)=>x is Scene s&&s==this;public override int GetHashCode()=>id;}
}
namespace UnityEngine {
 public class Object{}
 public class MonoBehaviour:Object {
  public GameObject gameObject=new GameObject();public Transform transform=>gameObject.transform;
  public T GetComponent<T>()=>gameObject.GetComponent<T>();public T[] GetComponents<T>()=>gameObject.GetComponents<T>();
 }
 public class GameObject:Object {
  public string name;public UnityEngine.SceneManagement.Scene scene=new UnityEngine.SceneManagement.Scene{id=1};public Transform transform;
  public Dictionary<Type,object> parts=new Dictionary<Type,object>();
  public GameObject(string n="test"){name=n;transform=new Transform{gameObject=this};}
  public T AddComponent<T>() where T:new(){var t=new T();parts[typeof(T)]=t;if(t is MonoBehaviour m)m.gameObject=this;return t;}
  public T GetComponent<T>()=>parts.TryGetValue(typeof(T),out var x)?(T)x:default;
  public bool TryGetComponent<T>(out T value){value=GetComponent<T>();return value!=null;}
  public T[] GetComponents<T>(){var r=new List<T>();foreach(var x in parts.Values)if(x is T t)r.Add(t);return r.ToArray();}
 }
 public class Transform{public Vector3 position;public Vector2 right;public GameObject gameObject;public T GetComponentInChildren<T>()=>gameObject.GetComponent<T>();}
 public class Rigidbody2D{public Vector2 linearVelocity;}
 public class Collider2D{public PlayerHealth health;public bool TryGetComponent<T>(out T t){t=health is T h?h:default;return t!=null;}}
 public struct Vector2{public float x,y;public Vector2(float x,float y){this.x=x;this.y=y;}public float sqrMagnitude=>x*x+y*y;public Vector2 normalized=>sqrMagnitude>.000001f?this*(1/(float)Math.Sqrt(sqrMagnitude)):zero;public static Vector2 zero=>new Vector2();public static Vector2 down=>new Vector2(0,-1);public static Vector2 operator*(Vector2 v,float x)=>new Vector2(v.x*x,v.y*x);}
 public struct Vector3{public float x,y,z;}
 public struct Quaternion{public static Quaternion identity=>new Quaternion();}
 public class SerializeField:Attribute{} public class Header:Attribute{public Header(string s){}} public class DisallowMultipleComponent:Attribute{}
 public class AddComponentMenu:Attribute{public AddComponentMenu(string s){}} public class RequireComponent:Attribute{public RequireComponent(Type a,Type b){}}
 public class Min:Attribute{public Min(float x){}} public class RangeAttribute:Attribute{public RangeAttribute(float a,float b){}}
 public enum RuntimeInitializeLoadType{SubsystemRegistration} public class RuntimeInitializeOnLoadMethod:Attribute{public RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType t){}}
 public static class Time{public static float time;}
 public static class Mathf{public const float Deg2Rad=(float)Math.PI/180;public static int Max(int a,int b)=>Math.Max(a,b);public static float Max(float a,float b)=>Math.Max(a,b);public static float Clamp(float x,float a,float b)=>Math.Max(a,Math.Min(x,b));public static float Cos(float x)=>(float)Math.Cos(x);public static float Sin(float x)=>(float)Math.Sin(x);}
}
public interface IPrefabPoolLifecycle{void OnPrefabSpawned();void OnPrefabDespawned();}
public class ProjectileImpactVisual{public GameObject gameObject=new GameObject();public void Play(){}}
public class PlayerHealth {
 public PlayerDamageHistory DamageHistory=new PlayerDamageHistory();public Action hit;public bool dead;
 public void TakeDamage(int n,string s,PlayerDamageKind k){if(dead)return;DamageHistory.Record(n,s,k);hit?.Invoke();}
}
public class PrefabPool {
 public static int spawns,releases;public static EnemyProjectile last;public static GameObject reusable;
 public static GameObject Spawn(GameObject prefab,Vector3 p,Quaternion q){
  spawns++;GameObject g=reusable;reusable=null;
  if(g==null){g=new GameObject();g.AddComponent<Rigidbody2D>();var bullet=g.AddComponent<EnemyProjectile>();CleanupHarness.Call(bullet,"Awake");}
  last=g.GetComponent<EnemyProjectile>();last.OnPrefabSpawned();return g;
 }
 public static void Release(GameObject g){releases++;g.GetComponent<EnemyProjectile>().OnPrefabDespawned();reusable=g;}
}
public class ModuleCounter:IEnemyProjectileLifecycle{public int finishes,launches;public void OnProjectileLaunched(EnemyProjectile p,Vector2 d){launches++;}public void OnProjectileFinished(EnemyProjectile p,EnemyProjectileFinishReason r){finishes++;}}
public enum EquipmentSlot{Frame,Plating,Actuator,Coolant}
public static class EquipmentSlotInfo{public static EquipmentSlot[] All={EquipmentSlot.Frame,EquipmentSlot.Plating,EquipmentSlot.Actuator,EquipmentSlot.Coolant};public static string Name(EquipmentSlot s)=>s.ToString();}
public class EquipmentDefinition{public string DisplayName;public ReactiveItemEffect ReactiveEffect;}
public class PlayerEquipment{public Dictionary<EquipmentSlot,EquipmentDefinition> items=new Dictionary<EquipmentSlot,EquipmentDefinition>();public EquipmentDefinition GetEquipped(EquipmentSlot s)=>items.TryGetValue(s,out var e)?e:null;}
public class ChipDefinition{public string DisplayName;public bool NeedsCurrent=true;}
public class PlacedChip{public ChipDefinition Chip;public bool active;}
public class CoreBoardState{public List<PlacedChip> Placements=new List<PlacedChip>();public bool IsEnergized(PlacedChip p)=>p.active;}
public class CoreBoardController{public bool IsReady=true;public CoreBoardState State=new CoreBoardState();}
public class ChipInventory{public int Count;}
public class PlayerLevel{public int GetCurrentLevel()=>7;}
public class WeaponStatsProfile{public string DisplayName;}
public class WeaponSkillProfile{public string DisplayName;}
public class PlayerWeaponEquipment{public WeaponStatsProfile EquippedWeapon;public WeaponSkillProfile EquippedSpecialAttack;}
'''
for f in ['Enemies/EnemyAttackContext.cs','Player/PlayerDamageHistory.cs','Enemies/EnemyProjectile.cs',
          'Enemies/EnemyProjectileEmissionScheduler.cs','Enemies/EnemyProjectileSpawnOnFinish.cs',
          'Equipment/ReactiveItemEffect.cs','UI/RunResultSummary.cs']:
    code += source(f)
code += 'public static class EnemyAttackGeometry'+source('Enemies/EnemyAttackTelegraph.cs').split('public static class EnemyAttackGeometry',1)[1]
code += r'''
public static class CleanupHarness {
 static int checks;static void Check(bool ok,string s){if(!ok)throw new Exception(s);checks++;}
 public static void Call(object o,string n,params object[] args)=>o.GetType().GetMethod(n,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(o,args);
 static void Set(object o,string n,object v)=>o.GetType().GetField(n,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(o,v);
 static object Scheduler=>typeof(EnemyProjectileEmissionScheduler).GetField("instance",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null);
 static int Pending=>((System.Collections.ICollection)Scheduler.GetType().GetField("emissions",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(Scheduler)).Count;
 static GameObject prefab=new GameObject("bullet");
 static EnemyProjectile Spawn(EnemyAttackContext c)=>EnemyProjectile.Spawn(prefab,new Vector3(),new Vector2(1,0),5,10,3,false,c);
 static void Schedule(EnemyAttackContext c,float delay=2)=>EnemyProjectileEmissionScheduler.Schedule(delay,prefab,new Vector3(),new Vector2(1,0),3,360,0,5,10,3,false,c);
 static EnemyProjectileSpawnOnFinish Children(EnemyProjectile parent,float delay,bool onCancelled=false){
  var module=new EnemyProjectileSpawnOnFinish();Set(module,"childProjectilePrefab",prefab);Set(module,"spawnDelay",delay);Set(module,"spawnOnCancelled",onCancelled);Set(parent,"modules",new IEnemyProjectileLifecycle[]{module});return module;
 }
 public static void Run(){
  var boss=new EnemyAttackContext("보스 A");var other=new EnemyAttackContext("적 B");var a=Spawn(boss);var b=Spawn(other);int released=PrefabPool.releases;
  boss.Cancel();Check(PrefabPool.releases==released+1&&a.AttackContext==null&&b.AttackContext==other,"death removes only owner's bullets immediately");
  boss.Cancel();Check(PrefabPool.releases==released+1,"cancel is idempotent");
  int spawned=PrefabPool.spawns;Check(Spawn(boss)==null&&PrefabPool.spawns==spawned,"cancelled lifetime cannot spawn again");
  var newLife=new EnemyAttackContext("보스 A");var reused=Spawn(newLife);boss.Cancel();Check(reused.AttackContext==newLife,"pool reuse gets a distinct attack lifetime");
  Schedule(newLife);Check(Pending==1,"delayed child is scheduled");newLife.Cancel();Call(Scheduler,"Update");Check(Pending==0&&PrefabPool.spawns==spawned+1,"cancelled delayed shot removed before its deadline");
  Schedule(boss,0);Check(PrefabPool.spawns==spawned+1,"cancelled zero-delay emission rejected");
  Time.time=10;var lifetime=new EnemyAttackContext("분열 보스");var parent=Spawn(lifetime);Children(parent,1);Set(parent,"expiresAt",0f);Call(parent,"Update");Check(Pending==1,"expired parent schedules children");
  Time.time=11;Call(Scheduler,"Update");Check(Pending==0&&PrefabPool.last.AttackContext==lifetime,"child inherits attack owner after parent returns to pool");
  var child=PrefabPool.last;Children(child,2);Set(child,"expiresAt",0f);Call(child,"Update");Check(Pending==1,"grandchild keeps pending chain");lifetime.Cancel();Time.time=20;spawned=PrefabPool.spawns;Call(Scheduler,"Update");Check(Pending==0&&PrefabPool.spawns==spawned,"death stops entire delayed descendant chain");
  var live=new EnemyAttackContext("live");var cancelledParent=Spawn(live);Children(cancelledParent,0,true);spawned=PrefabPool.spawns;cancelledParent.Cancel(true);Check(PrefabPool.spawns==spawned,"cleanup suppresses spawnOnCancelled children");
  cancelledParent=Spawn(live);Children(cancelledParent,0,true);spawned=PrefabPool.spawns;cancelledParent.Cancel();Check(PrefabPool.spawns==spawned+2,"explicit legacy cancellation still supports configured child effect");
  Schedule(null);Check(Pending==1,"legacy unowned delayed emission supported");EnemyProjectileEmissionScheduler.CancelPendingInScene(new UnityEngine.SceneManagement.Scene{id=2});Check(Pending==1,"cleanup scoped to matching scene");
  EnemyProjectileEmissionScheduler.CancelPendingInScene(new UnityEngine.SceneManagement.Scene{id=1});Time.time=30;spawned=PrefabPool.spawns;Call(Scheduler,"Update");Check(Pending==0&&PrefabPool.spawns==spawned,"stage clears even unowned future emissions");
  var hitContext=new EnemyAttackContext("마지막 보스");var hitBullet=Spawn(hitContext);var counter=new ModuleCounter();Set(hitBullet,"modules",new IEnemyProjectileLifecycle[]{counter});
  var player=new PlayerHealth();player.hit=()=>hitContext.Cancel();released=PrefabPool.releases;Call(hitBullet,"OnTriggerEnter2D",new Collider2D{health=player});
  Check(PrefabPool.releases==released+1&&counter.finishes==1,"death inside hit callback cannot release projectile twice");
  Check(player.DamageHistory.LastSource=="마지막 보스"&&player.DamageHistory.LastKind==PlayerDamageKind.Projectile,"source name survives cancellation in fatal callback");
  var detached=Spawn(new EnemyAttackContext("old"));var old=detached.AttackContext;detached.Cancel(true);var newOwner=new EnemyAttackContext("new");var next=Spawn(newOwner);old.Cancel();Check(next.AttackContext==newOwner,"returned bullet unsubscribes old owner event");
  var unknown=new PlayerDamageHistory();Check(unknown.DescribeLastHit()=="기록된 피해 없음","missing source is not invented");unknown.Record(0,"ignored",PlayerDamageKind.Contact);Check(unknown.HitCount==0,"zero HP loss ignored");unknown.Record(5,null,PlayerDamageKind.Unknown);Check(unknown.DescribeLastHit().Contains("알 수 없는 원인"),"unattributed damage uses honest fallback");
  var go=new GameObject();go.parts[typeof(PlayerHealth)]=player;go.parts[typeof(PlayerLevel)]=new PlayerLevel();go.parts[typeof(PlayerWeaponEquipment)]=new PlayerWeaponEquipment{EquippedWeapon=new WeaponStatsProfile{DisplayName="시작 검"},EquippedSpecialAttack=new WeaponSkillProfile{DisplayName="돌진 베기"}};
  string failed=RunResultSummary.PlayerStatus(go.transform,true);Check(failed.Contains("사망 원인")&&failed.Contains("마지막 보스")&&failed.Contains("도달 레벨 7")&&failed.Contains("시작 검"),"failure summary includes actual cause and loadout");Check(!RunResultSummary.PlayerStatus(go.transform,false).Contains("사망 원인"),"success never labels last hit as death");
  var equipment=new PlayerEquipment();equipment.items[EquipmentSlot.Frame]=new EquipmentDefinition{DisplayName="경량 골격",ReactiveEffect=new ReactiveItemEffect{amount=3,cooldown=8,trigger=ReactiveItemTrigger.CombatDodge}};
  var board=new CoreBoardController();var chip=new ChipDefinition{DisplayName="자가 수복"};board.State.Placements.Add(new PlacedChip{Chip=chip,active=true});board.State.Placements.Add(new PlacedChip{Chip=chip,active=false});board.State.Placements.Add(new PlacedChip{Chip=new ChipDefinition{DisplayName="수동 칩",NeedsCurrent=false}});
  string build=RunResultSummary.Loadout(equipment,board,new ChipInventory{Count=4});Check(build.Contains("자가 수복 ×2 (활성 1)")&&build.Contains("활성 2 / 배치 3")&&build.Contains("미장착 칩 4개"),"chip totals distinguish inventory, powered and passive placements");Check(build.Contains("경량 골격")&&build.Contains("MP +3"),"equipped reaction included");
  for(int i=0;i<100;i++)board.State.Placements.Add(new PlacedChip{Chip=new ChipDefinition{DisplayName="칩"+i},active=true});build=RunResultSummary.Loadout(equipment,board,null);Check(build.Contains("칩99")&&build.Contains("활성 102 / 배치 103"),"long build list is preserved for scrolling");
  Check(RunResultSummary.Loadout(null,null,null).Contains("배치한 칩 없음"),"empty build handled");
  Console.WriteLine("PASS: "+checks+" production projectile cleanup, delayed emission, pool ownership and result summary checks (Unity APIs mocked).");
 }
}
'''
run(code,'CleanupHarness.Run()','AttackCleanupHarness')
health=source('Enemies/EnemyHealth.cs')
assert health.index('attackContext?.Cancel();',health.index('private void Die()')) < health.index('OnDied?.Invoke();',health.index('private void Die()'))
assert 'CancelEnemyAttacks();' in source('Rooms/RunManager.cs').split('private void HandlePlayerDied()',1)[1].split('public void BeginRun()',1)[0]
panel=(ROOT/'Assets/Survivor_Game/Prefabs/UI/Result/UI_RunResultPanel.prefab').read_text(encoding='utf-8')
assert 'buildText: {fileID: 7606000000000000004}' in panel
assert panel.count('m_SizeDelta: {x: 740, y: 590}')==2
assert 'm_AnchoredPosition: {x: 410, y: 25}' in panel
assert 'm_AnchoredPosition: {x: -410, y: 25}' in panel
print('PASS: cleanup-before-reward wiring, failure cleanup, two-column result prefab connection and bounds.')
