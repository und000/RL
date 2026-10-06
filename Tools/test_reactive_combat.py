"""Production combat/proc/telegraph logic with deterministic Unity API stubs, not play mode."""
from pathlib import Path
import re
from run_csharp_harness import run

ROOT = Path(__file__).resolve().parents[1]
SCRIPTS = ROOT / 'Assets/Survivor_Game/Scripts'
def source(path):
    return '\n'.join(line for line in (SCRIPTS/path).read_text(encoding='utf-8-sig').splitlines() if not line.startswith('using '))
def method(path, signature):
    text = source(path)
    start = text.index(signature)
    end = text.index('{', start) + 1
    depth = 1
    while depth:
        depth += (text[end] == '{') - (text[end] == '}')
        end += 1
    return text[start:end]

code = r'''
using System; using System.Collections; using System.Collections.Generic; using System.Reflection; using UnityEngine;
using Random = UnityEngine.Random;
namespace UnityEngine {
 public class Object {
  public static T FindFirstObjectByType<T>() where T:class=>null;
  public static T Instantiate<T>(T o,Transform t) where T:class=>o;
 }
 public class ScriptableObject:Object{}
 public class MonoBehaviour:Object {
  public bool isActiveAndEnabled=true,enabled=true; public Transform transform=new Transform();
  public T GetComponent<T>() where T:class=>null;public T GetComponentInChildren<T>() where T:class=>null;
  public T[] GetComponents<T>()=>Array.Empty<T>();
 }
 public class Transform {public Vector3 position;public GameObject gameObject;public T GetComponent<T>() where T:class=>null;}
 public class GameObject {public string name="test";public T AddComponent<T>() where T:new()=>new T();}
 public class SerializeField:Attribute{} public class DisallowMultipleComponent:Attribute{}
 public class Header:Attribute{public Header(string s){}} public class Tooltip:Attribute{public Tooltip(string s){}}
 public class AddComponentMenu:Attribute{public AddComponentMenu(string s){}} public class RequireComponent:Attribute{public RequireComponent(Type t){}}
 public class Min:Attribute{public Min(float x){}} public class RangeAttribute:Attribute{public RangeAttribute(float a,float b){}}
 public static class Time{public static float time,timeScale=1,deltaTime=.1f;}
 public static class Mathf {
  public const float Deg2Rad=(float)Math.PI/180;
  public static float Max(float a,float b)=>Math.Max(a,b); public static int Max(int a,int b)=>Math.Max(a,b);
  public static float Clamp01(float v)=>Clamp(v,0,1);public static float Clamp(float v,float a,float b)=>Math.Max(a,Math.Min(b,v));
  public static float Sin(float v)=>(float)Math.Sin(v);public static float Cos(float v)=>(float)Math.Cos(v);
  public static int RoundToInt(float f)=>(int)Math.Round(f);
 }
 public struct Vector2 {
  public float x,y;public Vector2(float x,float y){this.x=x;this.y=y;}
  public float sqrMagnitude=>x*x+y*y;public Vector2 normalized=>sqrMagnitude>.000001f?this*(1/(float)Math.Sqrt(sqrMagnitude)):zero;
  public static Vector2 zero=>new Vector2();public static Vector2 down=>new Vector2(0,-1);
  public static Vector2 operator*(Vector2 a,float f)=>new Vector2(a.x*f,a.y*f);
  public static Vector2 operator-(Vector2 a,Vector2 b)=>new Vector2(a.x-b.x,a.y-b.y);
  public static implicit operator Vector2(Vector3 v)=>new Vector2(v.x,v.y);
 }
 public struct Vector3 {public float x,y,z;public Vector3(float x,float y,float z=0){this.x=x;this.y=y;this.z=z;}}
 public static class Random{public static Vector2 insideUnitCircle=>new Vector2(.6f,.8f);}
 public class WaitForSeconds{public WaitForSeconds(float f){}}
 public static class Debug{public static void LogError(string s,object o){throw new Exception(s);}}
 public class Rigidbody2D {public Vector2 linearVelocity;}
 public class Coroutine{}
}
public class PlayerLevel {public int GetCurrentLevel()=>1;}
public class PlayerDodge {public event Action OnDodgeStarted;public void Dodge()=>OnDodgeStarted?.Invoke();}
public class PlayerHealth {public bool IsDead;public int hp=20,max=100;public int GetCurrentHealth()=>hp;public int GetMaxHealth()=>max;public bool Heal(int n){if(IsDead)return false;hp=Math.Min(max,hp+n);return true;}}
public class PlayerEnergy {public float CurrentEnergy;public int MaxEnergy=20;public Action restored;public void Restore(float n){CurrentEnergy=Math.Min(MaxEnergy,CurrentEnergy+n);restored?.Invoke();}}
public enum EquipmentSlot{Frame,Coolant}
public static class EquipmentSlotInfo{public static EquipmentSlot[] All={EquipmentSlot.Frame,EquipmentSlot.Coolant};}
public class EquipmentDefinition:ScriptableObject{public ReactiveItemEffect ReactiveEffect;}
public class PlayerEquipment{public Dictionary<EquipmentSlot,EquipmentDefinition> Items=new Dictionary<EquipmentSlot,EquipmentDefinition>();public EquipmentDefinition GetEquipped(EquipmentSlot s)=>Items.TryGetValue(s,out var item)?item:null;}
public class ChipDefinition:ScriptableObject{public ReactiveItemEffect ReactiveEffect;public bool NeedsCurrent=true;}
public class PlacedChip{public ChipDefinition Chip;public bool powered;}
public class CoreBoardState{public List<PlacedChip> Placements=new List<PlacedChip>();public bool IsEnergized(PlacedChip p)=>p.powered;}
public class CoreBoardController{public bool IsReady=true;public CoreBoardState State=new CoreBoardState();}
public class RoomInstance{public bool IsCombatActive;}
public class GeneratedFloor{public List<RoomInstance> Rooms=new List<RoomInstance>();}
public class RunManager {public bool IsRunOver,IsTransitioning;public GeneratedFloor CurrentFloor=new GeneratedFloor();}
public class EnemyStagger{public bool IsStaggered;public float impact;public void ApplyImpact(float n){impact+=n;if(impact>=10)IsStaggered=true;}}
public class EnemyHealth {public EnemyAttackContext AttackContext=new EnemyAttackContext("Enemy");}
public class EnemyPhaseController {public int CurrentPhase=1;}
public abstract class EnemyAttackPattern:MonoBehaviour {
 protected virtual void Awake(){} protected abstract float Cooldown{get;} protected virtual float InitialDelay=>0;
 protected abstract IEnumerator ExecutePattern(Transform target); protected virtual bool CanExecutePattern(Transform t)=>true;
 public virtual void CancelPresentation(){} public IEnumerator Execute(Transform t)=>ExecutePattern(t);
}
public interface IEnemyAttackGate{bool CanUseEnemyAttacks{get;}}
public class EnemyAttackTelegraph:MonoBehaviour{
 public bool visible;public List<Vector2> rays=new List<Vector2>();
 public void Show(Vector3 p,Vector2 d,int c,float s,float o,float l,float progress){visible=true;rays.Clear();for(int i=0;i<c;i++)rays.Add(EnemyAttackGeometry.VolleyDirection(d,i,c,s,o));}
 public void Hide(){visible=false;}
}
public class EnemyProjectile {
 public static List<Vector2> Shots=new List<Vector2>();
 public static EnemyProjectile Spawn(GameObject p,Vector3 o,Vector2 d,float s,int damage,float life,bool align,EnemyAttackContext context=null){Shots.Add(d);return new EnemyProjectile();}
}
'''
code += source('Equipment/ReactiveItemEffect.cs') + source('Equipment/PlayerReactiveItems.cs')
code += source('Combat/PlayerCombatStats.cs')
code += source('Enemies/EnemyAttackContext.cs')
code += 'public static class EnemyAttackGeometry' + source('Enemies/EnemyAttackTelegraph.cs').split('public static class EnemyAttackGeometry',1)[1]
code += source('Enemies/Patterns/EnemyProjectileAttackPattern.cs')
code += r'''
public class EnemyDamageFixture:MonoBehaviour {
 public int currentHealth=100;public EnemyStagger stagger=new EnemyStagger();public Action OnDamaged;public Action<int,int> OnHealthChanged;public int deaths;
 public int GetDefense()=>0;public int GetMaxHealth()=>100;public void Die(){deaths++;}
'''
code += method('Enemies/EnemyHealth.cs','public void TakeDamage(DamageData damageData)') + '}\n'
code += r'''
public abstract class EnemyMovementPatternBase:MonoBehaviour{
 public Rigidbody2D Body=new Rigidbody2D();public Vector2 targetDirection=new Vector2(1,0);
 protected Vector2 DirectionToTarget()=>targetDirection;protected abstract void ResetPattern();protected abstract void UpdatePattern(float dt);
 protected virtual void OnDisable(){}
 public void Tick(float dt)=>UpdatePattern(dt); public void Reset()=>ResetPattern();
}
'''
code += source('Enemies/Patterns/Enemy2MovementPattern.cs')
code += r'''
public class EnemyMovement {public bool enabled;}
public class ControllerFixture {
 public EnemyAttackPattern[] patterns;public Coroutine runningPattern=new Coroutine();public EnemyMovement enemyMovement=new EnemyMovement();public bool movementDisabledByPattern=true;public int stops;
 void StopCoroutine(Coroutine c){stops++;}
'''
code += method('Enemies/Patterns/EnemyPatternController.cs','private void StopRunningPattern()').replace('private void','public void') + '}\n'
code += r'''
public static class ReactiveHarness {
 static int count;static void Check(bool ok,string text){if(!ok)throw new Exception(text);count++;}
 static void Set(object o,string f,object v)=>o.GetType().GetField(f,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(o,v);
 static void Call(object o,string name,params object[] args)=>o.GetType().GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(o,args);
 static bool Near(Vector2 a,Vector2 b)=>(a-b).sqrMagnitude<.00001f;
 static ReactiveItemEffect Effect(ReactiveItemTrigger t,ReactiveItemAction a,int n,float c)=>new ReactiveItemEffect{trigger=t,action=a,amount=n,cooldown=c};
 static void Exhaust(IEnumerator e){int max=100;while(e.MoveNext()&&--max>0){}if(max==0)throw new Exception("coroutine did not finish");}
 public static void Run(){
  var pc=new PlayerCombatStats();var itemSystem=new PlayerReactiveItems();var hp=new PlayerHealth();var mp=new PlayerEnergy();var gear=new PlayerEquipment();var board=new CoreBoardController();var run=new RunManager();var dodge=new PlayerDodge();
  Set(itemSystem,"combat",pc);Set(itemSystem,"health",hp);Set(itemSystem,"energy",mp);Set(itemSystem,"equipment",gear);Set(itemSystem,"board",board);Set(itemSystem,"run",run);Set(itemSystem,"dodge",dodge);Call(itemSystem,"OnEnable");
  var killItem=new EquipmentDefinition{ReactiveEffect=Effect(ReactiveItemTrigger.Kill,ReactiveItemAction.RestoreEnergy,3,1)};
  gear.Items[EquipmentSlot.Coolant]=killItem;
  DamageData damage=pc.CreateWeaponDamageData(200).WithStaggerImpact(20);
  Check(damage.Source==pc&&damage.StaggerImpact==20,"weapon damage preserves attacker through stagger copy");
  Check(pc.CreateDamageData().Source==pc,"reusable projectile source attributed");
  var foe=new EnemyDamageFixture();foe.TakeDamage(damage);Check(mp.CurrentEnergy==3&&foe.deaths==1,"weapon kill grants MP once");
  foe.TakeDamage(damage);Check(mp.CurrentEnergy==3&&foe.deaths==1,"dead enemy cannot proc twice");
  new EnemyDamageFixture().TakeDamage(damage);Check(mp.CurrentEnergy==3,"same-frame kills share cooldown");
  gear.Items.Clear();Time.time=.5f;pc.ReportEnemyHit(true,false);gear.Items[EquipmentSlot.Coolant]=killItem;pc.ReportEnemyHit(true,false);Check(mp.CurrentEnergy==3,"re-equipping cannot reset cooldown");
  Time.time=1;pc.ReportEnemyHit(true,false);Check(mp.CurrentEnergy==6,"game-time cooldown expires");
  run.CurrentFloor=new GeneratedFloor();Time.time=1.5f;pc.ReportEnemyHit(true,false);Check(mp.CurrentEnergy==6,"stage replacement preserves cooldown");
  Time.time=3;mp.CurrentEnergy=20;pc.ReportEnemyHit(true,false);mp.CurrentEnergy=0;pc.ReportEnemyHit(true,false);Check(mp.CurrentEnergy==3,"full MP does not consume trigger");
  Time.time=5;hp.IsDead=true;pc.ReportEnemyHit(true,false);Check(mp.CurrentEnergy==3,"dead owner cannot gain proc");hp.IsDead=false;
  run.IsTransitioning=true;pc.ReportEnemyHit(true,false);Check(mp.CurrentEnergy==3,"transition blocks procs");run.IsTransitioning=false;
  Time.timeScale=0;pc.ReportEnemyHit(true,false);Check(mp.CurrentEnergy==3,"pause blocks procs");Time.timeScale=1;
  var other=new PlayerCombatStats();new EnemyDamageFixture().TakeDamage(other.CreateWeaponDamageData(200));Check(mp.CurrentEnergy==3,"other attacker cannot trigger owner items");
  new EnemyDamageFixture().TakeDamage(new DamageData(200,0,0,0,1));Check(mp.CurrentEnergy==3,"unattributed damage does not claim kill");
  var repair=new ChipDefinition{ReactiveEffect=Effect(ReactiveItemTrigger.Stagger,ReactiveItemAction.Heal,5,6)};
  var placed=new PlacedChip{Chip=repair};board.State.Placements.Add(placed);
  foe=new EnemyDamageFixture();foe.TakeDamage(pc.CreateWeaponDamageData(1).WithStaggerImpact(10));Check(hp.hp==20,"unpowered chip inactive");
  placed.powered=true;foe=new EnemyDamageFixture();foe.TakeDamage(pc.CreateWeaponDamageData(1).WithStaggerImpact(10));Check(hp.hp==25,"new stagger heals");
  Time.time=20;foe.TakeDamage(pc.CreateWeaponDamageData(1).WithStaggerImpact(10));Check(hp.hp==25,"hitting already staggered enemy does not proc");
  board.State.Placements.Add(new PlacedChip{Chip=repair,powered=true});repair.ReactiveEffect.cooldown=0;
  pc.ReportEnemyHit(false,true);Check(hp.hp==30,"same chip definition deduplicated even with zero cooldown");
  board.State.Placements.Clear();pc.ReportEnemyHit(false,true);Check(hp.hp==30,"inventory/unplaced chip inactive");
  board.State.Placements.Add(placed);placed.powered=false;repair.NeedsCurrent=false;pc.ReportEnemyHit(false,true);Check(hp.hp==35,"placed passive works without power");
  hp.hp=100;pc.ReportEnemyHit(false,true);hp.hp=35;pc.ReportEnemyHit(false,true);Check(hp.hp==40,"full health does not consume trigger");
  gear.Items[EquipmentSlot.Frame]=new EquipmentDefinition{ReactiveEffect=Effect(ReactiveItemTrigger.CombatDodge,ReactiveItemAction.RestoreEnergy,3,8)};
  dodge.Dodge();Check(mp.CurrentEnergy==3,"safe room dodge cannot farm MP");
  run.CurrentFloor.Rooms.Add(new RoomInstance{IsCombatActive=true});dodge.Dodge();Check(mp.CurrentEnergy==6,"combat dodge grants MP");dodge.Dodge();Check(mp.CurrentEnergy==6,"dodge cooldown prevents spam");
  Call(itemSystem,"OnDisable");Time.time=50;pc.ReportEnemyHit(true,true);dodge.Dodge();Check(mp.CurrentEnergy==6&&hp.hp==40,"disabled system unsubscribes all triggers");Call(itemSystem,"OnEnable");pc.ReportEnemyHit(true,false);Check(mp.CurrentEnergy==9,"re-enable subscribes once");
  Time.time=60;mp.CurrentEnergy=0;killItem.ReactiveEffect.cooldown=0;mp.restored=()=>pc.ReportEnemyHit(true,false);pc.ReportEnemyHit(true,false);Check(mp.CurrentEnergy==3,"zero-cooldown effects cannot recursively trigger through restore callbacks");mp.restored=null;
  run.IsRunOver=true;pc.ReportEnemyHit(true,false);Check(mp.CurrentEnergy==3,"completed run blocks effects");run.IsRunOver=false;
  var directions=new List<Vector2>();for(int i=0;i<8;i++)directions.Add(EnemyAttackGeometry.VolleyDirection(new Vector2(1,0),i,8,360,0));
  bool unique=true;for(int i=0;i<8;i++)for(int j=i+1;j<8;j++)if(Near(directions[i],directions[j]))unique=false;
  Check(unique,"360-degree volley does not duplicate endpoints");
  Check(Near(EnemyAttackGeometry.VolleyDirection(new Vector2(1,0),0,1,90,90),new Vector2(0,1)),"single shot respects angular offset");
  var pattern=new EnemyProjectileAttackPattern();var warning=new EnemyAttackTelegraph();Set(pattern,"telegraph",warning);Set(pattern,"projectilePrefab",new GameObject());Set(pattern,"windupDuration",.4f);Set(pattern,"projectilesPerShot",3);Set(pattern,"spreadAngle",60f);
  var target=new Transform{position=new Vector3(5,0)};var seq=pattern.Execute(target);Check(seq.MoveNext()&&warning.visible&&EnemyProjectile.Shots.Count==0,"warning precedes damage");
  var expected=warning.rays.ToArray();target.position=new Vector3(0,5);Time.deltaTime=0;for(int i=0;i<10;i++)seq.MoveNext();Check(EnemyProjectile.Shots.Count==0,"paused windup does not fire");Time.deltaTime=.1f;Exhaust(seq);
  Check(EnemyProjectile.Shots.Count==3&&!warning.visible,"volley fires once and hides guide");for(int i=0;i<3;i++)Check(Near(EnemyProjectile.Shots[i],expected[i]),"shot matches warned direction despite moving target");
  EnemyProjectile.Shots.Clear();seq=pattern.Execute(target);seq.MoveNext();pattern.isActiveAndEnabled=false;Exhaust(seq);Check(!warning.visible&&EnemyProjectile.Shots.Count==0,"disabled pattern cannot fire stale attack");pattern.isActiveAndEnabled=true;
  seq=pattern.Execute(target);seq.MoveNext();var controller=new ControllerFixture{patterns=new[]{pattern}};controller.StopRunningPattern();Check(!warning.visible&&controller.runningPattern==null&&controller.stops==1,"controller interruption stops coroutine and presentation");
  Set(pattern,"shotCount",2);EnemyProjectile.Shots.Clear();seq=pattern.Execute(target);Exhaust(seq);Check(EnemyProjectile.Shots.Count==6,"every burst shot completes a warning");
  var phaseController=new EnemyPhaseController{CurrentPhase=3};Set(pattern,"phaseController",phaseController);
  Set(pattern,"phaseVolleys",new[]{new EnemyProjectileAttackPattern.PhaseVolley{minimumPhase=3,projectileCount=4,spread=360,shots=2,angleStep=45},new EnemyProjectileAttackPattern.PhaseVolley{minimumPhase=2,projectileCount=2,spread=20,shots=1}});
  Check(pattern.ResolveVolley(1).projectileCount==3&&pattern.ResolveVolley(1).shots==2,"no eligible phase override keeps base configuration");
  Check(pattern.ResolveVolley(2).projectileCount==2&&pattern.ResolveVolley(3).projectileCount==4,"highest eligible override wins regardless of array order");
  EnemyProjectile.Shots.Clear();seq=pattern.Execute(target);Exhaust(seq);Check(EnemyProjectile.Shots.Count==8,"phase applies burst and projectile counts together");
  Check(!Near(EnemyProjectile.Shots[0],EnemyProjectile.Shots[4]),"later ring rotates its safe gaps");
  Check(Near(EnemyProjectile.Shots[4],EnemyAttackGeometry.VolleyDirection(new Vector2(0,1),0,4,360,45)),"actual rotated volley matches phase angle step");
  var dash=new Enemy2MovementPattern();var dashWarning=new EnemyAttackTelegraph();Set(dash,"telegraph",dashWarning);dash.Reset();dash.Tick(2);dash.targetDirection=new Vector2(0,1);dash.Tick(.1f);Check(dashWarning.visible,"dash stop phase shows warning");var dashDirection=dashWarning.rays[0];dash.Tick(.4f);dash.Tick(.02f);Check(Near(dash.Body.linearVelocity.normalized,dashDirection),"dash follows locked warning, not late target");dash.Reset();Check(!dashWarning.visible,"stagger/pool reset removes dash warning");
  Console.WriteLine("PASS: "+count+" production reactive-item, attribution, telegraph and interruption checks (Unity APIs mocked).");
 }
}
'''
run(code, 'ReactiveHarness.Run()', 'ReactiveCombatHarness')

# References and serialized settings used by current live encounter/reward assets.
assets = ROOT / 'Assets/Survivor_Game'
for enemy in ('Enemy101','Enemy3','Enemy2'):
    text = (assets/f'Prefabs/Enemies/{enemy}.prefab').read_text(encoding='utf-8')
    assert re.search(r'telegraphPrefab: \{fileID: 1003, guid: [0-9a-f]{32}', text)
for asset in ('Equipment/Equipment_Coolant_Loop', 'Equipment/Equipment_Frame_Light', 'CoreBoard/Chip_Term_Repair'):
    text = (assets/f'Data/{asset}.asset').read_text(encoding='utf-8')
    assert '  reactiveEffect:' in text
assert 'PlayerReactiveItems.Create(player, this)' in source('Rooms/RunManager.cs')
assert 'health.OnDied += StopRunningPattern' in source('Enemies/Patterns/EnemyPatternController.cs')
print('PASS: live warning/item assets, runtime installation and death cancellation wiring.')
