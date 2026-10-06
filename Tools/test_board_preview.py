"""Production board/circuit preview isolation and parity, with Unity API stubs (not play mode)."""
from pathlib import Path
from run_csharp_harness import run

ROOT = Path(__file__).resolve().parents[1]
SCRIPTS = ROOT / 'Assets/Survivor_Game/Scripts'

def source(path):
    return '\n'.join(x for x in (SCRIPTS/path).read_text(encoding='utf-8-sig').splitlines() if not x.startswith('using '))

code = r'''
using System; using System.Collections.Generic; using System.Reflection; using System.Text; using UnityEngine;
namespace UnityEngine {
 public class ScriptableObject {public string name="fixture";} public class Sprite {} public class MonoBehaviour {}
 public class DisallowMultipleComponent:Attribute {} public class AddComponentMenu:Attribute {public AddComponentMenu(string s){}}
 public class SerializeField:Attribute {} public class Header:Attribute {public Header(string s){}}
 public class Tooltip:Attribute {public Tooltip(string s){}} public class TextArea:Attribute {public TextArea(int a,int b){}}
 public class Min:Attribute {public Min(float x){}} public class RangeAttribute:Attribute {public RangeAttribute(float a,float b){}}
 public class CreateAssetMenu:Attribute {public string fileName,menuName;}
 public struct Color {public static Color white=>new Color();}
 public struct Vector2Int {
  public int x,y; public Vector2Int(int a,int b){x=a;y=b;} public static Vector2Int zero=>new Vector2Int();
  public static Vector2Int operator+(Vector2Int a,Vector2Int b)=>new Vector2Int(a.x+b.x,a.y+b.y);
  public static bool operator==(Vector2Int a,Vector2Int b)=>a.x==b.x&&a.y==b.y;
  public static bool operator!=(Vector2Int a,Vector2Int b)=>!(a==b);
  public override bool Equals(object o)=>o is Vector2Int v&&this==v; public override int GetHashCode()=>HashCode.Combine(x,y);
  public static Vector2Int Min(Vector2Int a,Vector2Int b)=>new Vector2Int(Math.Min(a.x,b.x),Math.Min(a.y,b.y));
  public static Vector2Int Max(Vector2Int a,Vector2Int b)=>new Vector2Int(Math.Max(a.x,b.x),Math.Max(a.y,b.y));
 }
 public static class Mathf {
  public static int Max(int a,int b)=>Math.Max(a,b); public static float Max(float a,float b)=>Math.Max(a,b);
  public static int Min(int a,int b)=>Math.Min(a,b); public static int Clamp(int a,int b,int c)=>Math.Clamp(a,b,c);
  public static float Clamp(float a,float b,float c)=>Math.Clamp(a,b,c); public static int RoundToInt(float a)=>(int)Math.Round(a);
  public static bool Approximately(float a,float b)=>Math.Abs(a-b)<.00001f;
  public static int FloorToInt(float a)=>(int)Math.Floor(a); public static float Min(float a,float b)=>Math.Min(a,b);
 }
 public static class Debug {public static void LogWarning(string s,object o){}}
}
'''
for path in ['Rooms/RoomTypes.cs', 'CoreBoard/CoreBoardTypes.cs', 'CoreBoard/ChipStatModifier.cs',
             'Equipment/ReactiveItemEffect.cs', 'CoreBoard/ChipDefinition.cs', 'CoreBoard/CoreBoardLayout.cs',
             'CoreBoard/CircuitSolver.cs', 'CoreBoard/CoreBoardState.cs', 'Meta/MetaEffect.cs',
             'CoreBoard/UI/BoardPreviewPresentation.cs', 'CoreBoard/ChipInventory.cs', 'CoreBoard/UI/ChipTrayLayout.cs']:
    code += source(path)

code += r'''
public static class Checks {
 static int count;
 static void Check(bool ok,string name){if(!ok)throw new Exception(name);count++;}
 static void Set(object o,string field,object value)=>o.GetType().GetField(field,BindingFlags.NonPublic|BindingFlags.Instance).SetValue(o,value);
 static ChipPin Pin(RoomDirection d,PinType t)=>new ChipPin{direction=d,type=t};
 static ChipDefinition Chip(string name,ChipCategory category,float damage,params ChipPin[] pins){
  var c=new ChipDefinition();Set(c,"displayName",name);Set(c,"category",category);Set(c,"family",ChipFamily.Electric);
  Set(c,"pins",pins);Set(c,"modifiers",new[]{new ChipStatModifier{stat=ChipStatKind.FlatAttack,value=damage}});return c;
 }
 static CoreBoardLayout Layout(){var l=new CoreBoardLayout();l.Resize(7,5);l.SetCell(new Vector2Int(0,2),BoardCellType.PowerRail);return l;}
 static void Equal(CoreBoardState a,CoreBoardState b,string label){
  Check(a.PlacedCount==b.PlacedCount,label+" count");
  foreach(ChipStatKind k in Enum.GetValues(typeof(ChipStatKind))) Check(Math.Abs(a.Stats.Get(k)-b.Stats.Get(k))<.0001f,label+" stat "+k);
  foreach(var p in a.Placements){var q=b.GetChip(p.Id);Check(q!=null&&q.Chip==p.Chip&&q.Origin==p.Origin&&q.Rotation==p.Rotation,label+" placement");
   Check(a.Solution.GetMultiplier(p.Id)==b.Solution.GetMultiplier(p.Id),label+" multiplier");}
  Check(a.Solution.Circuits.Count==b.Solution.Circuits.Count,label+" circuits");
 }
 public static void Run(){
  var layout=Layout();var state=new CoreBoardState(layout);
  var source=Chip("전원",ChipCategory.Source,0,Pin(RoomDirection.East,PinType.Output));
  var amp=Chip("증폭",ChipCategory.Amplifier,2,Pin(RoomDirection.West,PinType.Input),Pin(RoomDirection.East,PinType.Output));
  var terminal=Chip("종단",ChipCategory.Terminal,10,Pin(RoomDirection.West,PinType.Input));
  Set(terminal,"reactiveEffect",new ReactiveItemEffect{amount=5,trigger=ReactiveItemTrigger.Stagger,action=ReactiveItemAction.Heal,cooldown=6});
  state.TryPlace(source,new Vector2Int(0,2),0,out var s);state.TryPlace(amp,new Vector2Int(1,2),0,out var a);
  int events=0;state.OnChanged+=()=>events++;var stats=state.Stats;var solution=state.Solution;
  Check(state.TryPreviewPlacement(terminal,new Vector2Int(2,2),0,0,out var preview,out var p),"valid add preview");
  Check(state.PlacedCount==2&&state.GetChipAt(new Vector2Int(2,2))==null,"no occupancy mutation");
  Check(events==0&&ReferenceEquals(stats,state.Stats)&&ReferenceEquals(solution,state.Solution),"no source events/stat replacement");
  Check(preview.IsEnergized(p)&&Math.Abs(preview.Stats.Get(ChipStatKind.FlatAttack)-14.5f)<.001f,"length/family scaling");
  string report=BoardPreviewPresentation.Describe(state,preview,terminal,p);
  Check(report.Contains("+14.5")&&report.Contains("반응 효과 추가: 종단"),"fractional bonus and reaction added");
  state.TryPlace(terminal,new Vector2Int(2,2),0,out var t);Check(t.Id==p.Id,"preview preserves next ID");Equal(state,preview,"addition parity");
  Check(events==1,"only commit fires event");

  Check(state.TryPreviewPlacement(amp,a.Origin,2,a.Id,out preview,out p),"rotate self overlap valid");
  Check(!preview.IsEnergized(preview.GetChip(t.Id))&&state.IsEnergized(t),"rotation disconnect isolated");
  report=BoardPreviewPresentation.Describe(state,preview,amp,p);
  Check(report.Contains("비활성화: 종단")&&report.Contains("반응 효과 해제: 종단"),"downstream loss reported");
  Check(a.Rotation==0&&preview.GetChip(a.Id).Rotation==2,"placed objects copied");
  state.TryMove(a.Id,a.Origin,2);Equal(state,preview,"rotation parity");state.TryMove(a.Id,a.Origin,0);

  Check(state.TryPreviewRemoval(s.Id,out preview),"source removal preview");
  Check(preview.Solution.EnergizedCount==0&&state.IsEnergized(t),"remove disconnects all downstream only in copy");
  report=BoardPreviewPresentation.Describe(state,preview,source,null);Check(report.Contains("제거 미리보기")&&report.Contains("반응 효과 해제"),"removal description");
  int beforeEvents=events;
  Check(!state.TryPreviewRemoval(999,out preview)&&preview==null,"unknown removal rejected");
  Check(!state.TryPreviewPlacement(terminal,new Vector2Int(1,2),0,0,out preview,out p)&&preview==null,"overlap rejected");
  Check(!state.TryPreviewPlacement(terminal,new Vector2Int(0,2),0,0,out preview,out p),"non-source on rail rejected");
  Check(!state.TryPreviewPlacement(source,new Vector2Int(4,2),0,0,out preview,out p),"source without rail rejected");
  Check(!state.TryPreviewPlacement(amp,a.Origin,0,999,out preview,out p),"unknown move rejected");
  Check(!state.TryPreviewPlacement(terminal,a.Origin,0,a.Id,out preview,out p),"mismatched moving definition rejected");
  Check(!state.TryPreviewPlacement(null,a.Origin,0,0,out preview,out p),"null chip rejected");
  Check(!state.TryPreviewPlacement(terminal,new Vector2Int(7,1),0,0,out preview,out p),"out of board rejected");
  layout.SetCell(new Vector2Int(5,4),BoardCellType.Blocked);
  Check(!state.TryPreviewPlacement(terminal,new Vector2Int(5,4),0,0,out preview,out p),"blocked cell rejected");
  Check(events==beforeEvents,"invalid and discarded previews never notify");

  var passive=Chip("수동",ChipCategory.Passive,1.25f);
  Set(passive,"reactiveEffect",new ReactiveItemEffect{amount=3});
  state.TryPlace(passive,new Vector2Int(4,3),0,out var firstPassive);
  Check(state.TryPreviewPlacement(passive,new Vector2Int(5,3),0,0,out preview,out p)&&preview.IsEnergized(p),"passive needs no wire");
  report=BoardPreviewPresentation.Describe(state,preview,passive,p);Check(!report.Contains("반응 효과 추가"),"duplicate reaction not added");
  state.TryPlace(passive,new Vector2Int(5,3),0);
  state.TryPreviewRemoval(firstPassive.Id,out preview);report=BoardPreviewPresentation.Describe(state,preview,passive,null);
  Check(!report.Contains("반응 효과 해제"),"another energized duplicate retains reaction");

  var branchLayout=Layout();branchLayout.SetCell(new Vector2Int(2,2),BoardCellType.Bus);
  var branch=new CoreBoardState(branchLayout);
  var junction=Chip("분기",ChipCategory.Junction,0,Pin(RoomDirection.West,PinType.Input),Pin(RoomDirection.North,PinType.Output),Pin(RoomDirection.East,PinType.Output));
  branch.TryPlace(source,new Vector2Int(0,2),0);branch.TryPlace(junction,new Vector2Int(1,2),0);
  branch.TryPlace(terminal,new Vector2Int(1,3),3,out var upper);
  Check(branch.TryPreviewPlacement(terminal,new Vector2Int(3,2),0,0,out preview,out p),"bus branch preview");
  Check(preview.IsEnergized(p)&&preview.Solution.Circuits.Count==2,"bus reaches second terminal");
  Check(preview.Solution.GetMultiplier(upper.Id)<branch.Solution.GetMultiplier(upper.Id),"new branch reduces existing terminal efficiency");
  report=BoardPreviewPresentation.Describe(branch,preview,terminal,p);Check(!report.Contains("반응 효과 추가"),"powered duplicate shares reaction");
  branch.TryPlace(terminal,new Vector2Int(3,2),0);Equal(branch,preview,"branch parity");

  // 여러 칸 모양의 회전·보드 경계와 이동/제거를 반복해 ID·점유·회로 결과를 비교한다.
  var longChip=Chip("긴 수동",ChipCategory.Passive,2);
  Set(longChip,"shapeCells",new[]{Vector2Int.zero,new Vector2Int(1,0)});
  var random=new Random(14);var fuzz=new CoreBoardState(Layout());int moves=0,removes=0,adds=0;
  for(int i=0;i<250;i++){
   var placements=new List<PlacedChip>(fuzz.Placements);
   if(placements.Count>0&&i%4==0){var existing=placements[random.Next(placements.Count)];fuzz.TryPreviewRemoval(existing.Id,out preview);fuzz.Remove(existing.Id);Equal(fuzz,preview,"random remove");removes++;}
   else {
    var cell=new Vector2Int(random.Next(-1,8),random.Next(-1,6));int rotation=random.Next(-4,8);
    var existing=placements.Count>0&&i%3==0?placements[random.Next(placements.Count)]:null;
    int id=existing!=null?existing.Id:0;var c=existing!=null?existing.Chip:longChip;
    var originalStats=fuzz.Stats;bool valid=fuzz.TryPreviewPlacement(c,cell,rotation,id,out preview,out p);
    Check(ReferenceEquals(fuzz.Stats,originalStats),"random preview isolation");
    bool committed=id!=0?fuzz.TryMove(id,cell,rotation):fuzz.TryPlace(c,cell,rotation);
    Check(valid==committed,"random placement validity");if(valid){Equal(fuzz,preview,"random parity");if(id==0)adds++;else moves++;}
   }
  }
  Check(adds>0&&moves>0&&removes>0,"all operations exercised");
  var inventory=new ChipInventory();inventory.Add(amp);inventory.Add(terminal);inventory.Add(amp);
  int inventoryEvents=0;inventory.OnInventoryChanged+=()=>inventoryEvents++;
  var grouped=ChipTrayLayout.Group(inventory.Chips);
  Check(grouped.Count==2&&grouped[0].Chip==amp&&grouped[0].Count==2&&grouped[1].Chip==terminal,"tray groups by definition and sorts by category");
  Check(inventory.Count==3&&inventoryEvents==0,"tray grouping never mutates inventory");
  Check(inventory.Remove(amp)&&inventory.Count==2,"group drag removes exactly one chip");
  grouped=ChipTrayLayout.Group(inventory.Chips);Check(grouped.Count==2&&grouped[0].Chip==amp&&grouped[0].Count==1,"remaining copy stays in same displayed order after removal");
  Check(ChipTrayLayout.Group(null).Count==0&&ChipTrayLayout.Group(new ChipDefinition[]{null,amp,null}).Count==1,"null entries ignored");
  for(int n=0;n<150;n++){
   var many=new List<ChipDefinition>();for(int i=0;i<n;i++)many.Add(Chip("칩"+i,ChipCategory.Passive,0));
   grouped=ChipTrayLayout.Group(many);Check(grouped.Count==n,"all distinct chips remain visible");
   int columns=ChipTrayLayout.Columns(402f,115.2f);float contentHeight=ChipTrayLayout.Height(n,columns,115.2f,120f);
   Check(columns==3&&contentHeight>=120f,"tray columns and minimum height");
   if(n>0)Check(contentHeight>=((n-1)/columns+1)*115.2f,"last tray row inside scroll extent");
  }
  Check(ChipTrayLayout.Columns(30f,115f)==1,"narrow tray keeps one column");
  for(int w=1;w<=8;w++)for(int h=1;h<=8;h++){
   var shape=Chip("모양",ChipCategory.Passive,0);Set(shape,"shapeCells",new[]{new Vector2Int(-2,-3),new Vector2Int(w-3,h-4)});
   float scale=ChipTrayLayout.ShapeScale(shape,48f,103.2f,75.2f);
   Check(scale>0&&scale<=1&&w*48*scale<=103.201f&&h*48*scale<=75.201f,"shape including negative offsets fits tile");
  }
  Console.WriteLine("PASS: "+count+" production board preview, circuit, isolation, effect-description and commit-parity checks (Unity APIs mocked).");
 }
}
'''
run(code, 'Checks.Run()', 'BoardPreviewHarness')

view = (SCRIPTS/'CoreBoard/UI/CoreBoardView.cs').read_text(encoding='utf-8-sig')
assert 'inventory.Contains(chip)' in view
assert 'if (dragGhost != null || view == null' in view
assert 'previewBaseline == board.State.Stats' in view
assert 'BoardPreviewPresentation.Describe(board.State, preview' in view
assert 'detailsScroll.horizontal = false;' in view
assert 'if (!EditingAllowed) { CancelDrag(); RefreshLabels(); return; }' in view
assert 'trayViewport.gameObject.AddComponent<RectMask2D>()' in view
assert 'trayScroll.verticalScrollbar = scrollbar;' in view
assert 'trayScroll.enabled = false;' in view and 'trayScroll.enabled = true;' in view
assert 'ChipTrayLayout.Group(inventory.Chips)' in view
print('PASS: live UI preview wiring, inventory/edit guards, cache and scroll configuration.')
