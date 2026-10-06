from pathlib import Path
import re
root=Path(__file__).resolve().parents[1]
scripts=root/'Assets/Survivor_Game/Scripts'
def source(path):
    return '\n'.join(line for line in path.read_text(encoding='utf-8-sig').splitlines() if not line.startswith('using '))
code=r'''using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
namespace UnityEngine {
 public class SerializeField:Attribute{} public class DisallowMultipleComponent:Attribute{} public class Tooltip:Attribute{public Tooltip(string x){}} public class Min:Attribute{public Min(float x){}}
 public class Header:Attribute{public Header(string x){}} public class AddComponentMenu:Attribute{public AddComponentMenu(string x){}}
 public enum RuntimeInitializeLoadType{SubsystemRegistration} public class RuntimeInitializeOnLoadMethod:Attribute{public RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType t){}}
 public class CreateAssetMenu:Attribute{public string fileName,menuName;} public class ScriptableObject{}
 public class GameObject{public bool activeSelf=true;public void SetActive(bool b){activeSelf=b;}public static GameObject FindWithTag(string t)=>null;public T GetComponentInChildren<T>(bool b=false)=>default(T);}
 public class Sprite{} public struct Color{}
 public class MonoBehaviour{public GameObject gameObject=new GameObject();public bool enabled=true;public bool isActiveAndEnabled=>enabled;public T[] GetComponentsInChildren<T>(bool b)=>new T[0];public T GetComponentInParent<T>()=>default(T);public static T FindFirstObjectByType<T>()=>default(T);}
 public static class Debug{public static int errors;public static void LogError(string s,object o){errors++;}}
 public static class Time{public static int frameCount;public static float timeScale=1;}
 public static class Random{static System.Random r=new System.Random(42);public static int Range(int a,int b)=>r.Next(a,b);}
 public static class Mathf{public static int Max(int a,int b)=>Math.Max(a,b);public static bool Approximately(float a,float b)=>Math.Abs(a-b)<0.00001f;}
}
namespace TMPro{public class TMP_Text{public string text;public Color color;}}
namespace UnityEngine.UI{public class Image{public Sprite sprite;public bool preserveAspect,enabled;} public class Button{public GameObject gameObject=new GameObject();public bool interactable;public Event onClick=new Event();public T GetComponentInChildren<T>(bool b)where T:new()=>new T();}public class Event{public void AddListener(Action a){}public void RemoveListener(Action a){}}}
namespace UnityEngine.EventSystems{public class EventSystem{public static EventSystem current;public void SetSelectedGameObject(GameObject g){}}}
public static class GameFontManager{public static void ApplyFont(TMP_Text t){}}
public static class StaggerImpactFeedback{public static void CancelActive(){}}
public class PlayerHealth{public bool IsDead;}
public class RunManager{public bool IsRunOver;}
public static class RoomChoiceUI{public static bool IsBlockingGameplay;}
public static class StageTransitionUI{public static bool IsBlockingGameplay;}
public class PlayerLevel:MonoBehaviour{public int level=1;public event Action<int> OnLevelUp;public int GetCurrentLevel()=>level;public void Raise(int n){level=n;OnLevelUp?.Invoke(n);}}
public enum RoomRewardKind{Chip,Equipment,Weapon,Experience}
public class RoomRewardDefinition{public EquipmentDefinition Equipment;public RoomRewardKind Kind;public bool allowed=true,grantSuccess=true;public int grants;public string DisplayName="item",Description="description";public Color GradeColor;public Sprite Icon;public string BuildLabel()=>"item";public bool CanGrant(GameObject p)=>allowed;public bool Grant(GameObject p){if(!grantSuccess)return false;grants++;return true;}}
'''
code+=r'''
public static class CoreBoardView {public static bool IsBlockingGameplay;}
public static class RunPauseUI {public static bool IsBlockingGameplay;}
public class EquipmentDefinition {public int Slot;}
public class PlayerEquipment {public EquipmentDefinition GetEquipped(int slot)=>null;}
public static class RewardPresentation {public static string Describe(RoomRewardDefinition r,GameObject p)=>"효과";public static bool IsStrictDowngrade(EquipmentDefinition a,EquipmentDefinition b)=>false;public static void RenderIcon(Image i,RoomRewardDefinition r){}}
'''
code+=source(scripts/'Player/LevelUpRewardPool.cs')+'\n'+source(scripts/'UI/LevelUpUI.cs')
code+=r'''
public static class LevelUpHarness{
 static int checks;
 static void Check(bool b,string m){if(!b)throw new Exception(m);checks++;}
 static void Set(object o,string f,object v){o.GetType().GetField(f,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(o,v);}
 static T Get<T>(object o,string f)=>(T)o.GetType().GetField(f,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(o);
 static void Call(object o,string m){o.GetType().GetMethod(m,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(o,null);}
 static void Reset(){typeof(LevelUpUI).GetMethod("ResetState",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null);Time.timeScale=1;Time.frameCount++;RoomChoiceUI.IsBlockingGameplay=false;}
 static LevelUpUI Boot(LevelUpRewardPool pool,PlayerLevel level){Reset();var ui=new LevelUpUI();Set(ui,"playerLevel",level);Set(ui,"levelUpPanel",new GameObject());Set(ui,"rewardPool",pool);Set(ui,"rerollButton",new Button());Set(ui,"titleText",new TMP_Text());var slots=new LevelUpUI.ChoiceSlot[4];for(int i=0;i<4;i++)slots[i]=new LevelUpUI.ChoiceSlot{button=new Button(),title=new TMP_Text(),description=new TMP_Text(),icon=new Image()};Set(ui,"slots",slots);Call(ui,"Awake");Call(ui,"Start");return ui;}
 static int Total(List<RoomRewardDefinition> items){int n=0;foreach(var i in items)n+=i.grants;return n;}
 public static void Run(){
  var items=new List<RoomRewardDefinition>();for(int i=0;i<12;i++)items.Add(new RoomRewardDefinition{Kind=i<6?RoomRewardKind.Chip:RoomRewardKind.Equipment});
  var pool=new LevelUpRewardPool();var entries=new List<RoomRewardDefinition>(items);entries.Add(items[0]);entries.Add(null);entries.Add(new RoomRewardDefinition{Kind=RoomRewardKind.Weapon});entries.Add(new RoomRewardDefinition{Kind=RoomRewardKind.Experience});Set(pool,"items",entries.ToArray());
  Check(pool.GetEligible(null).Count==12,"deduplicate and exclude weapon/experience");items[0].allowed=false;Check(pool.GetEligible(i=>i.CanGrant(null)).Count==11,"filter unavailable item");items[0].allowed=true;
  var draft=new LevelUpRewardDraft(3);Check(draft.Refresh(items,false)&&draft.Choices.Count==4,"four choices");
  for(int n=0;n<3;n++){var old=new List<RoomRewardDefinition>(draft.Choices);Check(draft.Refresh(items,true),"reroll works");foreach(var i in draft.Choices)Check(!old.Contains(i),"prefer new choices");Check(draft.RemainingRerolls==2-n,"consume once");draft.Refresh(items,false);Check(draft.RemainingRerolls==2-n,"new level preserves budget");}
  Check(!draft.Refresh(items,true)&&draft.RemainingRerolls==0,"fourth reroll rejected");
  draft=new LevelUpRewardDraft(3);draft.Refresh(items,false);var same=new List<RoomRewardDefinition>(draft.Choices);Check(!draft.Refresh(same,true)&&draft.RemainingRerolls==3,"same four do not spend budget");
  same.Add(items.Find(i=>!same.Contains(i)));Check(draft.Refresh(same,true)&&draft.RemainingRerolls==2,"five eligible allow changed draft");
  Check(!draft.Refresh(new List<RoomRewardDefinition>{items[0],items[0],null,items[1]},true)&&draft.RemainingRerolls==2,"too few unique items do not spend");
  var level=new PlayerLevel();var ui=Boot(pool,level);Time.timeScale=.6f;level.Raise(2);level.Raise(3);level.Raise(4);
  Check(Time.timeScale==0&&LevelUpUI.IsPopupOpen,"pause for queued levels");ui.Choose(0);Check(Total(items)==0,"opening frame cannot grant");
  for(int n=0;n<3;n++){
   Time.frameCount++;ui.Reroll();ui.Reroll();Check(ui.RemainingRerolls==2-n,"duplicate reroll ignored");ui.Choose(0);Check(Total(items)==n,"same-frame select after reroll ignored");
   Time.frameCount++;ui.Choose(0);ui.Choose(0);Check(Total(items)==n+1,"one item per level");Check(Mathf.Approximately(Time.timeScale,.6f),"restore prior speed");Check(LevelUpUI.IsPopupOpen,"block closing frame inputs");
   Time.frameCount++;Call(ui,"Update");Check(n==2?!LevelUpUI.IsPopupOpen:Time.timeScale==0,"drain every queued level");
  }
  level.Raise(5);Time.frameCount++;ui.Reroll();Check(ui.RemainingRerolls==0,"later stage/level does not recharge");Time.frameCount++;ui.Choose(0);Call(ui,"OnDisable");Call(ui,"OnDestroy");
  ui=Boot(pool,new PlayerLevel());Check(ui.RemainingRerolls==3,"new run resets budget");Call(ui,"OnDestroy");
  level=new PlayerLevel();ui=Boot(pool,level);RoomChoiceUI.IsBlockingGameplay=true;level.Raise(2);Check(!LevelUpUI.IsPopupOpen&&Time.timeScale==1,"defer behind room choice");RoomChoiceUI.IsBlockingGameplay=false;Call(ui,"Update");Check(LevelUpUI.IsPopupOpen,"show deferred reward");
  var chosen=Get<LevelUpRewardDraft>(ui,"draft").Choices[0];chosen.grantSuccess=false;Time.frameCount++;int before=Total(items);ui.Choose(0);Check(Total(items)==before&&Time.timeScale==0,"grant failure does not consume reward");chosen.grantSuccess=true;
  var run=new RunManager{IsRunOver=true};Set(ui,"run",run);Time.frameCount++;ui.Choose(1);ui.Reroll();Check(Total(items)==before&&ui.RemainingRerolls==3,"run over blocks grant/reroll");Call(ui,"Update");Check(Time.timeScale==0&&!Get<GameObject>(ui,"levelUpPanel").activeSelf,"death/result pause retained");Call(ui,"OnDestroy");
  level=new PlayerLevel{level=4};ui=Boot(pool,level);Call(ui,"Update");Check(Get<Queue<int>>(ui,"pendingLevels").Count==2,"levels before Start caught up");Call(ui,"OnDisable");Check(Time.timeScale==1,"disable restores owned pause");Call(ui,"OnDestroy");
  level=new PlayerLevel();ui=Boot(pool,level);CoreBoardView.IsBlockingGameplay=true;level.Raise(2);Check(!LevelUpUI.IsPopupOpen,"board defers pending reward");CoreBoardView.IsBlockingGameplay=false;Call(ui,"Update");Time.frameCount++;CoreBoardView.IsBlockingGameplay=true;before=Total(items);ui.Choose(0);ui.Reroll();Check(Total(items)==before&&ui.RemainingRerolls==3,"board read-only overlay blocks underlying reward actions");CoreBoardView.IsBlockingGameplay=false;Time.frameCount++;ui.Choose(0);Check(Total(items)==before+1,"selection resumes after closing board");Call(ui,"OnDestroy");
  Console.WriteLine("PASS: "+checks+" production level-up selection/reroll/queue/pause checks (Unity API and grants mocked).");
 }
}
'''
(root/'Temp/ProjectVerification/LevelUpHarness.cs').write_text(code,encoding='utf-8')
base=root/'Assets/Survivor_Game'
pooltext=(base/'Data/Rooms/LevelUpRewards.asset').read_text(encoding='utf-8-sig')
guids=re.findall(r'^  - \{fileID: 11400000, guid: ([0-9a-f]+), type: 2\}',pooltext,re.M)
assert len(guids)==30 and len(set(guids))==30
mapping={re.search(r'^guid: (\w+)',p.read_text(encoding='utf-8-sig'),re.M)[1]:p.with_suffix('') for p in (base/'Data/Rooms/Rewards').glob('*.meta')}
kinds=[]
for guid in guids:
    text=mapping[guid].read_text(encoding='utf-8-sig')
    kind=int(re.search(r'^  kind: (\d+)',text,re.M)[1]);assert kind in (5,12)
    kinds.append(kind)
prefab=(base/'Prefabs/UI/LevelUp/UI_LevelUpPanel.prefab').read_text(encoding='utf-8-sig')
assert len(re.findall(r'^  - button:',prefab,re.M))==4 and '  rerollsPerRun: 3' in prefab
for path in base.rglob('*'):
    if path.suffix not in ('.prefab','.unity','.asset'):continue
    text=path.read_text(encoding='utf-8-sig')
    assert 'guid: f7777777777777777777777777777777' not in text and 'guid: e169e8f7243a4de6a06a03603171fc8b' not in text,path
print(f'PASS: 30 distinct level-up reward references ({kinds.count(5)} chips, {kinds.count(12)} equipment), four UI slots, three rerolls, no removed shockwave GUID references.')

from run_csharp_harness import run
run(code,"LevelUpHarness.Run()","LevelUpHarness")
