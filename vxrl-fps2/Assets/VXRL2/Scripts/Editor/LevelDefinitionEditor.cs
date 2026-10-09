using UnityEditor;
using UnityEngine;
namespace VXRL2.Editor {
[CustomEditor(typeof(LevelDefinition))]
public sealed class LevelDefinitionEditor:UnityEditor.Editor {
 public override void OnInspectorGUI(){
  var level=(LevelDefinition)target;
  EditorGUILayout.HelpBox("Each cell is 4 metres. Overlapping rooms merge into connected floor space. Add corridors as rooms with Decorate off. Spawns use cell coordinates.",MessageType.Info);
  Rect canvas=GUILayoutUtility.GetRect(240,300);EditorGUI.DrawRect(canvas,new Color(.025f,.05f,.065f));
  if(level.rooms.Count>0){int maxX=1,maxZ=1;foreach(var r in level.rooms){maxX=Mathf.Max(maxX,r.cells.xMax);maxZ=Mathf.Max(maxZ,r.cells.yMax);}float scale=Mathf.Min((canvas.width-16)/maxX,(canvas.height-16)/maxZ);
   foreach(var room in level.rooms){var r=room.cells;EditorGUI.DrawRect(new Rect(canvas.x+8+r.x*scale,canvas.yMax-8-r.yMax*scale,r.width*scale-1,r.height*scale-1),Color.Lerp(room.accent,Color.black,.6f));}
   foreach(var e in level.enemies)EditorGUI.DrawRect(new Rect(canvas.x+8+e.cell.x*scale,canvas.yMax-8-(e.cell.y+1)*scale,4,4),Color.red);
   foreach(var p in level.pickups)EditorGUI.DrawRect(new Rect(canvas.x+8+p.cell.x*scale,canvas.yMax-8-(p.cell.y+1)*scale,3,3),Color.yellow);
   EditorGUI.DrawRect(new Rect(canvas.x+8+level.start.x*scale,canvas.yMax-8-(level.start.y+1)*scale,5,5),Color.green);
  }
  EditorGUILayout.LabelField("Green: start   Red: enemies   Yellow: pickups",EditorStyles.miniLabel);
  DrawDefaultInspector();
 }
}
}
