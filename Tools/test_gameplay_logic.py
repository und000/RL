"""Exercise production reward, pause ownership, damage and dodge logic with Unity stubs."""
from pathlib import Path
from run_csharp_harness import run

ROOT = Path(__file__).resolve().parents[1]
SCRIPTS = ROOT / 'Assets/Survivor_Game/Scripts'


def source(path):
    return '\n'.join(x for x in (SCRIPTS / path).read_text(encoding='utf-8-sig').splitlines() if not x.startswith('using '))


def method(path, signature):
    text = source(path)
    start = text.index(signature)
    left = text.index('{', start)
    depth = 1
    end = left + 1
    while depth:
        depth += (text[end] == '{') - (text[end] == '}')
        end += 1
    return text[start:end]


code = r'''
using System; using System.Collections.Generic; using System.Text; using System.Reflection; using UnityEngine;
namespace UnityEngine {
 public class Object {public static T FindFirstObjectByType<T>()=>default;}
 public class Min:Attribute{public Min(float x){}} public class SerializeField:Attribute{} public class Header:Attribute{public Header(string s){}} public class Tooltip:Attribute{public Tooltip(string s){}} public class TextArea:Attribute{public TextArea(int a,int b){}}
 public class CreateAssetMenu:Attribute{public string fileName,menuName;} public class ScriptableObject{public string name="fixture";} public class Sprite{}
 public struct Vector2Int{public int x,y;public Vector2Int(int x,int y){this.x=x;this.y=y;}}
 public class GameObject{public bool activeSelf=true;public object component;public void SetActive(bool x){activeSelf=x;}public T GetComponentInChildren<T>()=>component is T t?t:default;}
 public class RectTransform{public GameObject gameObject=new GameObject();public void SetAsLastSibling(){}}
 public static class Mathf{public static bool Approximately(float a,float b)=>Math.Abs(a-b)<.00001f;public static int RoundToInt(float f)=>(int)Math.Round(f);public static int Max(int a,int b)=>Math.Max(a,b);public static float Max(float a,float b)=>Math.Max(a,b);}
 public static class Time{public static float time,timeScale=1;public static int frameCount;}
}
public enum EquipmentSlot{Frame,Armor,Motor,Coolant} public enum ItemGrade{Standard,Rare}
public static class EquipmentSlotInfo{public static string Name(EquipmentSlot x)=>x.ToString();}
public enum ChipFamily{None,Thermal,Electric,Kinetic,Nano}
public class ChipDefinition{public ReactiveItemEffect ReactiveEffect;public int CellCount=3;public ChipFamily Family;public bool NeedsCurrent=true;public void ApplyModifiers(CoreBoardStats stats){stats.Add(ChipStatKind.FlatAttack,3);}}
public class CoreBoardLayout{public int Height=2,Width=2;}
public struct PlacementResult{public bool IsValid;}
public class CoreBoardState{public CoreBoardLayout Layout=new CoreBoardLayout();public bool fits;public PlacementResult CanPlace(ChipDefinition c,Vector2Int p,int r)=>new PlacementResult{IsValid=fits};}
public class CoreBoardController{public bool IsReady=true;public CoreBoardState State=new CoreBoardState();}
public class PlayerEquipment{public EquipmentDefinition current;public EquipmentDefinition GetEquipped(EquipmentSlot slot)=>current;}
public enum RoomRewardKind{Equipment,Chip,Other}
public class RoomRewardDefinition{public RoomRewardKind Kind;public EquipmentDefinition Equipment;public ChipDefinition Chip;public string Description;public string BuildLabel()=>"보상";}
public static class LevelUpUI{public static bool IsPopupOpen;}
public static class RoomChoiceUI{public static bool IsBlockingGameplay;}
public static class StageTransitionUI{public static bool IsBlockingGameplay;}
public static class RunPauseUI{public static bool IsBlockingGameplay;}
public class RunManager{public bool IsRunOver,IsTransitioning;}
public static class StaggerImpactFeedback{public static void CancelActive(){}}
public class Facing{public void PlayHit(){}}
public static class GameInputKeys{public static bool IsGameplayBlocked;}
public class KeyState{public bool wasPressedThisFrame,wasReleasedThisFrame,isPressed;}
public class Keyboard{public static Keyboard current=new Keyboard();public KeyState spaceKey=new KeyState(),leftShiftKey=new KeyState(),escapeKey=new KeyState();}
public class Movement{public bool sprint;public void SetSprinting(bool value){sprint=value;}}
'''
code += source('CoreBoard/ChipStatModifier.cs') + source('Equipment/EquipmentDefinition.cs')
code += source('Equipment/ReactiveItemEffect.cs')
code += source('Player/PlayerDamageHistory.cs')
code += 'public static class MetaEffectFormat' + source('Meta/MetaEffect.cs').split('public static class MetaEffectFormat', 1)[1]
presentation = source('UI/RewardPresentation.cs')
code += 'public static class RewardPresentation {\n' + presentation[presentation.index('    public static string Describe('):]
code += r'''
public class DamageFixture {
 public PlayerDamageHistory DamageHistory {get;}=new PlayerDamageHistory();
 public int currentHealth=20;public bool isDead,isDodgeInvulnerable,isTeleportInvulnerable,isReviveInvulnerable;
 public float hitInvulnerableUntil,hitInvulnerableDuration=.35f;public Facing lowerbodyFacing=new Facing();public Action OnHealthChanged;
 public void Die(){isDead=true;}
'''
code += method('Player/PlayerHealth.cs', 'public void TakeDamage(int damage,') + '}\n'
code += r'''
public class CoreBoardView {
 public static CoreBoardView active;public static int blockedThroughFrame=-1;
 public bool isOpen,ownsPause,needsRebuild;public float previousTimeScale;public RunManager runManager=new RunManager();
 public RectTransform panel=new RectTransform(),root=new RectTransform();public void CancelDrag(){}public void RebuildAll(){}public void RefreshLabels(){}
'''
code += method('CoreBoard/UI/CoreBoardView.cs', 'public void SetOpen(bool open)') + '}\n'
code += r'''
public class DodgeFixture {
 public bool separateSprintInput=true,isDodging,spaceHeld;public float spacePressedTime,holdThreshold=.18f,nextDodgeTime;public int dodges;
 public Movement playerMovement=new Movement();public void TryStartDodge(){if(!isDodging&&Time.time>=nextDodgeTime)dodges++;}
 public void ResetSpaceInput(){spaceHeld=false;playerMovement.SetSprinting(false);}
'''
code += method('Player/PlayerDodge.cs', 'private void Update()').replace('private void Update()', 'public void Update()') + '}\n'
code += r'''
public class PauseFixture {
 public static PauseFixture active;public static int blockedThroughFrame;
 public RunManager run=new RunManager();public GameObject panel=new GameObject();public float previousTimeScale;
'''
code += method('UI/RunPauseUI.cs', 'private void Update()').replace('private void Update()', 'public void Update()')
code += method('UI/RunPauseUI.cs', 'public void Close()') + '}\n'
code += r'''
public static class GameplayHarness {
 static int count;static void Check(bool b,string message){if(!b)throw new Exception(message);count++;}
 static void Set(object o,string field,object value){o.GetType().GetField(field,BindingFlags.NonPublic|BindingFlags.Instance).SetValue(o,value);}
 static EquipmentDefinition Equipment(params ChipStatModifier[] mods){var e=new EquipmentDefinition();Set(e,"modifiers",mods);return e;}
 static ChipStatModifier Stat(ChipStatKind kind,float value)=>new ChipStatModifier{stat=kind,value=value};
 public static void Run(){
  Check(MetaEffectFormat.StatValue(ChipStatKind.MaxHealth,-6)=="-6","negative health has one sign");
  Check(MetaEffectFormat.StatValue(ChipStatKind.MoveSpeedRate,-.04f)=="-4%","negative percent has one sign");
  var old=Equipment(Stat(ChipStatKind.FlatAttack,10),Stat(ChipStatKind.MaxHealth,5));
  var weak=Equipment(Stat(ChipStatKind.FlatAttack,7));
  var trade=Equipment(Stat(ChipStatKind.FlatAttack,12),Stat(ChipStatKind.MaxHealth,-4));
  Check(RewardPresentation.IsStrictDowngrade(weak,old),"strictly dominated equipment excluded");
  Check(!RewardPresentation.IsStrictDowngrade(trade,old),"tradeoffs remain eligible");
  Check(!RewardPresentation.IsStrictDowngrade(old,old),"equal stats not mislabeled downgrade");
  Check(!RewardPresentation.IsStrictDowngrade(weak,null),"empty equipment slot eligible");
  var reactive=Equipment(Stat(ChipStatKind.FlatAttack,1));
  Set(reactive,"reactiveEffect",new ReactiveItemEffect{trigger=ReactiveItemTrigger.Kill,amount=3,cooldown=1});
  Check(!RewardPresentation.IsStrictDowngrade(reactive,old),"unique reaction cannot be discarded by flat stats");
  string reactionText=RewardPresentation.Describe(new RoomRewardDefinition{Kind=RoomRewardKind.Equipment,Equipment=old},new GameObject{component=new PlayerEquipment{current=reactive}});
  Check(reactionText.Contains("교체로 잃음")&&reactionText.Contains("MP +3"),"lost reaction shown before replacement");
  var player=new GameObject{component=new PlayerEquipment{current=old}};
  string desc=RewardPresentation.Describe(new RoomRewardDefinition{Kind=RoomRewardKind.Equipment,Equipment=trade},player);
  Check(desc.Contains("+2")&&desc.Contains("-9")&&!desc.Contains("+-"),"comparison includes gain and lost old modifier");
  var chip=new ChipDefinition();var board=new CoreBoardController();
  Check(RewardPresentation.DescribeChip(chip,board).Contains("재정리 필요"),"full board does not promise activation");
  board.State.fits=true;Check(RewardPresentation.DescribeChip(chip,board).Contains("배치 가능"),"fit shown separately from power");
  Check(RewardPresentation.DescribeChip(chip,board).Contains("전원 연결 필요"),"powered chip explains requirement");
  chip.NeedsCurrent=false;Check(RewardPresentation.DescribeChip(chip,board).Contains("전원 불필요"),"passive chip explains placement only");
  Time.time=10;var health=new DamageFixture();health.TakeDamage(4);health.TakeDamage(7);Check(health.currentHealth==16,"simultaneous attackers cannot stack damage");
  Time.time=10.34f;health.TakeDamage(2);Check(health.currentHealth==16,"grace period protects player");
  Time.time=10.36f;health.TakeDamage(2);Check(health.currentHealth==14,"grace period expires in game time");
  health.isDodgeInvulnerable=true;Time.time=11;health.TakeDamage(2);Check(health.currentHealth==14,"dodge protection retained");health.isDodgeInvulnerable=false;
  health.hitInvulnerableDuration=0;health.TakeDamage(1);health.TakeDamage(1);Check(health.currentHealth==12,"inspector zero disables grace period");health.TakeDamage(0);Check(health.currentHealth==12,"invalid damage has no side effect");
  Check(health.DamageHistory.HitCount==4&&health.DamageHistory.TotalHealthLost==8,"only accepted damage enters statistics");
  health.TakeDamage(3,"돌진 적",PlayerDamageKind.Contact);health.isDodgeInvulnerable=true;health.TakeDamage(50,"보스",PlayerDamageKind.Projectile);
  Check(health.DamageHistory.LastSource=="돌진 적"&&health.DamageHistory.LastKind==PlayerDamageKind.Contact,"invulnerable collision cannot overwrite damage cause");health.isDodgeInvulnerable=false;
  health.TakeDamage(1000,"보스",PlayerDamageKind.Projectile);Check(health.DamageHistory.LastHealthLost==9&&health.DamageHistory.TotalHealthLost==20,"overkill counts actual lost HP only");
  Time.timeScale=.6f;var view=new CoreBoardView();view.SetOpen(false);Check(!view.panel.gameObject.activeSelf,"initial closed board stays hidden");
  view.SetOpen(true);Check(Time.timeScale==0&&view.ownsPause,"board owns its own pause");view.SetOpen(false);Check(Time.timeScale==.6f&&CoreBoardView.active==null,"board restores exact prior speed");
  LevelUpUI.IsPopupOpen=true;Time.timeScale=0;view.SetOpen(true);Check(!view.ownsPause,"reward overlay borrows pause");view.SetOpen(false);Check(Time.timeScale==0,"closing board never resumes underlying reward");LevelUpUI.IsPopupOpen=false;
  Time.timeScale=1;view.SetOpen(true);view.runManager.IsRunOver=true;view.SetOpen(false);Check(Time.timeScale==0,"death pause retained");view.runManager.IsRunOver=false;
  StageTransitionUI.IsBlockingGameplay=true;view.SetOpen(true);Check(!view.isOpen,"board cannot open during transition");StageTransitionUI.IsBlockingGameplay=false;
  RunPauseUI.IsBlockingGameplay=true;view.SetOpen(true);Check(!view.isOpen,"pause and board do not stack");RunPauseUI.IsBlockingGameplay=false;
  var d=new DodgeFixture();Keyboard.current.spaceKey.wasPressedThisFrame=true;d.Update();Check(d.dodges==1,"dodge starts on press");Keyboard.current.spaceKey.wasPressedThisFrame=false;Keyboard.current.spaceKey.wasReleasedThisFrame=true;d.Update();Check(d.dodges==1,"release does not dodge twice");
  Keyboard.current.leftShiftKey.isPressed=true;d.Update();Check(d.playerMovement.sprint,"separate sprint works");GameInputKeys.IsGameplayBlocked=true;d.Update();Check(!d.playerMovement.sprint,"popup cancels held sprint");GameInputKeys.IsGameplayBlocked=false;
  d=new DodgeFixture{separateSprintInput=false};Keyboard.current=new Keyboard();Time.time=1;
  Keyboard.current.spaceKey.wasPressedThisFrame=true;Keyboard.current.spaceKey.isPressed=true;d.Update();Check(d.dodges==1,"shared Space press dodges immediately");
  Keyboard.current.spaceKey.wasPressedThisFrame=false;d.isDodging=true;Time.time=1.3f;d.Update();Check(!d.playerMovement.sprint&&d.dodges==1,"hold cannot repeat dodge or sprint during dodge");
  d.isDodging=false;d.Update();Check(d.playerMovement.sprint&&d.dodges==1,"hold becomes sprint after dodge");
  Time.time=5;d.Update();Check(d.dodges==1,"long hold never repeats after cooldown");
  Keyboard.current.spaceKey.isPressed=false;Keyboard.current.spaceKey.wasReleasedThisFrame=true;d.Update();Check(d.dodges==1&&!d.playerMovement.sprint,"release stops sprint without dodge");
  Keyboard.current.spaceKey.wasReleasedThisFrame=false;Keyboard.current.spaceKey.wasPressedThisFrame=true;Keyboard.current.spaceKey.isPressed=true;d.Update();Check(d.dodges==2,"release and fresh press dodges again");
  d.nextDodgeTime=10;Time.time=6;d.Update();Check(d.dodges==2,"cooldown press rejected");Keyboard.current.spaceKey.wasPressedThisFrame=false;Time.time=11;d.Update();Check(d.dodges==2,"rejected press is not queued while held");
  GameInputKeys.IsGameplayBlocked=true;Keyboard.current.spaceKey.wasPressedThisFrame=true;d.Update();Check(d.dodges==2&&!d.playerMovement.sprint,"blocked press does not dodge");
  GameInputKeys.IsGameplayBlocked=false;Keyboard.current.spaceKey.wasPressedThisFrame=false;d.Update();Check(d.dodges==2&&!d.playerMovement.sprint,"held key does not resume action after popup");
  Keyboard.current.spaceKey.isPressed=false;d.Update();Keyboard.current.spaceKey.isPressed=true;Keyboard.current.spaceKey.wasPressedThisFrame=true;d.Update();Check(d.dodges==3,"new press after popup works");
  Keyboard.current=new Keyboard();Keyboard.current.escapeKey.wasPressedThisFrame=true;Time.timeScale=.75f;var pause=new PauseFixture();pause.Update();Check(Time.timeScale==0&&PauseFixture.active==pause,"Esc pause owns prior speed");pause.Update();Check(Time.timeScale==.75f&&PauseFixture.active==null&&!pause.panel.activeSelf,"Esc resumes and hides panel");Check(PauseFixture.blockedThroughFrame==Time.frameCount,"resume blocks same-frame gameplay");
  GameInputKeys.IsGameplayBlocked=true;pause.Update();Check(PauseFixture.active==null,"Esc cannot open through another modal");GameInputKeys.IsGameplayBlocked=false;
  pause.Update();pause.run.IsRunOver=true;pause.Update();Check(Time.timeScale==0&&PauseFixture.active==null,"run-end pause never resumes game");
  Console.WriteLine("PASS: "+count+" production reward comparison, board/pause ownership, damage protection and dodge checks (Unity API mocked).");
 }
}
'''
run(code, 'GameplayHarness.Run()', 'GameplayHarness')

# 확인한 라이브 씬/프리팹에 Space 유지 달리기를 명시한다.
dodge = source('Player/PlayerDodge.cs')
assert 'wasReleasedThisFrame' not in dodge
assert 'playerMovement.RefreshMoveInput();' in dodge
for path in ['Scenes/Game.unity', 'Scenes/CharacterAnimationTest.unity', 'Prefabs/Player/Player.prefab']:
    asset = (ROOT/'Assets/Survivor_Game'/path).read_text(encoding='utf-8-sig')
    assert '  separateSprintInput: 0' in asset, path
print('PASS: press-only dodge and explicit Space-hold sprint settings in both scenes and player prefab.')
