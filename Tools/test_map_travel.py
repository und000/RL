"""Production map policy, room travel guards/landing/transition with explicit Unity stubs."""
from pathlib import Path
import re
from run_csharp_harness import run
ROOT=Path(__file__).resolve().parents[1]
GAME=ROOT/'Assets/Survivor_Game'
SCRIPTS=GAME/'Scripts'
def source(path):
    return '\n'.join(x for x in (SCRIPTS/path).read_text(encoding='utf-8-sig').splitlines() if not x.startswith('using '))
def method(path,signature):
    s=source(path);start=s.index(signature);end=s.index('{',start)+1;depth=1
    while depth:
        depth+=(s[end]=='{')-(s[end]=='}');end+=1
    return s[start:end]

code=r'''
using System;using System.Collections;using System.Collections.Generic;using UnityEngine;
namespace UnityEngine {
 public struct Vector2 {public float x,y;public Vector2(float a,float b){x=a;y=b;}public static Vector2 zero=>new Vector2();public static Vector2 one=>new Vector2(1,1);
  public static Vector2 operator+(Vector2 a,Vector2 b)=>new Vector2(a.x+b.x,a.y+b.y);public static Vector2 operator*(Vector2 a,float b)=>new Vector2(a.x*b,a.y*b);
  public static implicit operator Vector2(Vector3 a)=>new Vector2(a.x,a.y);}
 public struct Vector3 {public float x,y,z;public Vector3(float a,float b,float c=0){x=a;y=b;z=c;}public static Vector3 operator-(Vector3 a,Vector3 b)=>new Vector3(a.x-b.x,a.y-b.y,a.z-b.z);public static implicit operator Vector3(Vector2 a)=>new Vector3(a.x,a.y);}
 public class Transform {public Vector3 position;public Collider2D collider;public T GetComponent<T>() where T:class=>collider as T;}
 public class Collider2D {public Bounds bounds=new Bounds();} public class Bounds{public Vector3 size=new Vector3(1,1),center;}
 public class ContactFilter2D{public bool useTriggers;public void SetLayerMask(int m){}}
 public static class LayerMask{public static int GetMask(string s)=>1;}
 public static class Physics2D{public static Func<Vector2, bool> blocked=p=>false;public static int OverlapBox(Vector2 p,Vector2 size,float a,ContactFilter2D f,Collider2D[] hits){if(f.useTriggers)throw new Exception("trigger filter");return blocked(p)?1:0;}}
 public static class Mathf {public static float Max(float a,float b)=>Math.Max(a,b);public static int Max(int a,int b)=>Math.Max(a,b);public static float Abs(float a)=>Math.Abs(a);public static int Abs(int a)=>Math.Abs(a);public static bool Approximately(float a,float b)=>Math.Abs(a-b)<.00001f;}
 public static class Time {public static float timeScale=1;public static int frameCount;}
 public class CanvasGroup {public float alpha;public bool blocksRaycasts;}
 public class WaitForSecondsRealtime {public float duration;public WaitForSecondsRealtime(float d){duration=d;}}
}
public enum RoomKind{Start,Normal,Elite,Treasure,Shop,Boss}
public class RoomInstance {public bool HasEntered,IsCombatActive;public Transform transform=new Transform();public Vector2 CellSize=new Vector2(48,32);}
public class GeneratedFloor {public List<RoomInstance> Rooms=new List<RoomInstance>();}
public static class LevelUpUI {public static bool IsPopupOpen;}
public static class RoomChoiceUI {public static bool IsBlockingGameplay;}
public static class RunPauseUI {public static bool IsBlockingGameplay;}
public static class StaggerImpactFeedback {public static void CancelActive(){}}
public class CoreBoardView {public bool IsOpen=true;public void SetOpen(bool open){IsOpen=open;Time.timeScale=open?0:1;}}
public class StageTransitionUI {
 public CanvasGroup curtain=new CanvasGroup();public bool isActiveAndEnabled=true,IsConfigured=true,ownsPause;public float previousTimeScale;
 public static StageTransitionUI active;public static int blockedThroughFrame=-1;public static bool IsBlockingGameplay=>active!=null||Time.frameCount<=blockedThroughFrame;
 public IEnumerator FadeOut(){curtain.alpha=1;yield return null;}public IEnumerator FadeIn(){curtain.alpha=0;yield return null;}
'''
code+=method('UI/StageTransitionUI.cs','public bool Begin()')+method('UI/StageTransitionUI.cs','public void End(bool resume)')+'}\n'
code+=source('UI/RoomMapState.cs')
code+=r'''
public class TravelFixture {
 public GeneratedFloor currentFloor=new GeneratedFloor();public Transform player=new Transform();public bool isActiveAndEnabled=true,runOver,transitioning;
 public StageTransitionUI transitionUI=new StageTransitionUI();public float roomTravelBlackDuration=.15f;public IEnumerator running;public int moves,cancels;
 public void StartCoroutine(IEnumerator routine){running=routine;}
 public void CancelPlayerProjectiles(){cancels++;}public void MovePlayerToPosition(Vector3 p){moves++;player.position=p;}
'''
for signature in ['public bool IsAnyRoomInCombat','public bool CanTravelToRoom(RoomInstance target)',
                  'public bool TryTravelToRoom(RoomInstance target, CoreBoardView menu)',
                  'private bool TryFindRoomLanding(RoomInstance target, out Vector2 landing)',
                  'private IEnumerator TravelToRoom(RoomInstance target, Vector2 landing, GeneratedFloor expectedFloor)']:
    code+=method('Rooms/RunManager.cs',signature).replace('private bool TryFindRoomLanding','public bool TryFindRoomLanding')
code+='}\n'
code+=r'''
public static class Checks {
 static int count;static void Check(bool ok,string name){if(!ok)throw new Exception(name);count++;}
 static void Drain(IEnumerator e){while(e.MoveNext())if(e.Current is IEnumerator nested)Drain(nested);}
 public static void Run(){
  Time.frameCount=100;
  Check(!RoomMapState.Visible(false)&&RoomMapState.Visible(true),"all rooms including exit hidden until visited");
  foreach(var kind in new[]{RoomKind.Normal,RoomKind.Elite,RoomKind.Boss}){
   Check(RoomMapState.Symbol(kind,true,false,false)==RoomMapSymbol.Enemy,"one enemy icon for combat types");
   Check(RoomMapState.Symbol(kind,true,true,true)==RoomMapSymbol.Reward,"clear with reward uses chest");
   Check(RoomMapState.Symbol(kind,true,true,false)==RoomMapSymbol.Complete,"claimed combat room complete");
  }
  Check(RoomMapState.Symbol(RoomKind.Shop,true,true,true)==RoomMapSymbol.Shop,"shop stock icon");
  Check(RoomMapState.Symbol(RoomKind.Treasure,true,true,true)==RoomMapSymbol.Reward,"treasure reward icon");
  Check(RoomMapState.Symbol(RoomKind.Start,true,true,false)==RoomMapSymbol.Start,"start icon");
  Check(RoomMapState.Symbol(RoomKind.Normal,false,false,false)==RoomMapSymbol.Exit,"unvisited exit hides combat type");
  var f=new TravelFixture();var target=new RoomInstance{HasEntered=true};target.transform.position=new Vector3(48,0);f.currentFloor.Rooms.Add(target);
  var other=new RoomInstance{HasEntered=true};f.currentFloor.Rooms.Add(other);
  Check(f.CanTravelToRoom(target),"visited other room valid");
  target.HasEntered=false;Check(!f.CanTravelToRoom(target),"hidden unvisited exit cannot teleport");target.HasEntered=true;
  Check(!f.CanTravelToRoom(other),"current room cannot teleport");
  Check(!f.CanTravelToRoom(new RoomInstance{HasEntered=true}),"old floor room rejected");
  other.IsCombatActive=true;Check(!f.CanTravelToRoom(target),"combat anywhere blocks travel");other.IsCombatActive=false;
  f.runOver=true;Check(!f.CanTravelToRoom(target),"dead run blocked");f.runOver=false;
  f.transitioning=true;Check(!f.CanTravelToRoom(target),"duplicate transition blocked");f.transitioning=false;
  LevelUpUI.IsPopupOpen=true;Check(!f.CanTravelToRoom(target),"reward popup blocks travel");LevelUpUI.IsPopupOpen=false;
  RoomChoiceUI.IsBlockingGameplay=true;Check(!f.CanTravelToRoom(target),"choice popup blocks travel");RoomChoiceUI.IsBlockingGameplay=false;
  RunPauseUI.IsBlockingGameplay=true;Check(!f.CanTravelToRoom(target),"pause popup blocks travel");RunPauseUI.IsBlockingGameplay=false;
  Check(f.TryFindRoomLanding(target,out var landing)&&landing.x==48&&landing.y==0,"clear center chosen");
  Physics2D.blocked=p=>Math.Abs(p.x-48)<.5f&&Math.Abs(p.y)<.5f;
  Check(f.TryFindRoomLanding(target,out landing)&&(landing.x!=48||landing.y!=0),"obstacle center replaced with free interior point");
  Physics2D.blocked=p=>true;var menu=new CoreBoardView();Time.timeScale=0;
  Check(!f.TryTravelToRoom(target,menu)&&menu.IsOpen&&!f.transitioning,"no safe landing leaves menu intact");Physics2D.blocked=p=>false;
  f.transitionUI.IsConfigured=false;Check(!f.TryTravelToRoom(target,menu)&&menu.IsOpen&&Time.timeScale==0,"curtain failure restores menu pause");f.transitionUI.IsConfigured=true;
  Check(f.TryTravelToRoom(target,menu)&&!menu.IsOpen&&Time.timeScale==0&&f.transitioning,"menu pause handed to curtain");
  Check(!f.TryTravelToRoom(target,menu),"double click rejected");
  Check(f.running.MoveNext()&&f.moves==0,"fade out before movement");Drain((IEnumerator)f.running.Current);
  Check(f.transitionUI.curtain.alpha==1,"black before movement");
  Check(f.running.MoveNext()&&f.moves==0,"black rendered at least one frame");
  Check(f.running.MoveNext()&&f.moves==1&&f.cancels==1&&f.player.position.x==48,"move and projectile cleanup while black");
  Check(f.running.Current is WaitForSecondsRealtime,"unscaled black hold");Drain(f.running);
  Check(!f.transitioning&&Time.timeScale==1&&StageTransitionUI.active==null&&f.currentFloor.Rooms.Count==2,"resume without regenerating floor");
  Time.frameCount++;f.player.position=new Vector3();menu.SetOpen(true);Check(f.TryTravelToRoom(target,menu),"second travel starts");
  f.currentFloor=new GeneratedFloor();Drain(f.running);Check(f.moves==1&&!f.transitioning,"changed floor cancels pending movement");
  Time.frameCount++;f.currentFloor.Rooms.Add(target);menu.SetOpen(true);Check(f.TryTravelToRoom(target,menu),"third travel starts");
  f.runOver=true;Drain(f.running);Check(f.moves==1&&Time.timeScale==0&&!f.transitioning,"death preserves pause and cancels movement");
  Console.WriteLine("PASS: "+count+" production map policy, travel guards, landing and curtain handoff checks (Unity APIs/physics mocked).");
 }
}
'''
run(code,'Checks.Run()','MapTravelHarness')

starter=(GAME/'Data/CoreBoard/CoreBoard_Starter.asset').read_text(encoding='utf-8-sig')
data=bytes.fromhex(re.search(r'cells: (\w+)',starter)[1]);cells=[int.from_bytes(data[i:i+4],'little') for i in range(0,len(data),4)]
assert len(cells)==25 and cells.count(1)==0 and cells.count(2)==1
for path,speed in [('Scenes/Game.unity',18),('Scenes/CharacterAnimationTest.unity',15),('Prefabs/Player/Player.prefab',18)]:
    assert f'  runSpeed: {speed}\n' in (GAME/path).read_text(encoding='utf-8-sig')
minimap=(GAME/'Prefabs/UI/Run/UI_Minimap.prefab').read_text(encoding='utf-8-sig')
assert 'panelSize: {x: 480, y: 340}' in minimap and 'roomSize: {x: 40, y: 28}' in minimap
ui=source('UI/MinimapUI.cs')
assert 'corridor.from.HasEntered && corridor.to.HasEntered' in ui
assert 'RoomMapState.Visible(visited)' in ui
assert 'room.HasMapRewards' in ui and 'visitedRooms.Add' not in ui
assert 'travelMap.ConfigureEmbedded(runManager, TravelToRoom);' in source('CoreBoard/UI/CoreBoardView.cs')
assert 'dodge.CancelForStageTransition();' in method('Rooms/RunManager.cs','private void MovePlayerToPosition(Vector3 target)')
assert 'pedestal != null && !pedestal.IsClaimed' in source('Rooms/RoomInstance.cs')
assert 'drop != null && !drop.IsClaimed' in source('Rooms/RoomInstance.cs')
assert 'reward.Kind == RoomRewardKind.BoardRepair' in method('Rooms/RoomInstance.cs','private bool IsRewardAllowed(RoomRewardDefinition reward)')
print('PASS: sprint values, doubled minimap, 25-cell board without holes, actual visit/reward/Tab/travel wiring.')
