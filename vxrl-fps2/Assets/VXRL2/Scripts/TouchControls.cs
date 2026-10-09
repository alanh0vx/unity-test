using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch=UnityEngine.InputSystem.EnhancedTouch.Touch;
namespace VXRL2 {
// Each finger owns its gesture until release: move, aim and fire work simultaneously.
[DefaultExecutionOrder(-100)]
public sealed class TouchControls:MonoBehaviour {
 public static TouchControls I; public static bool Active=>I&&I.active;
 public bool active;public Vector2 move,look;public bool firing,fireTap,use;
 enum Role {Ignored,Move,Look,Fire}
 sealed class Contact {public Role role;public Vector2 start,last;public float began,travel;}
 readonly Dictionary<int,Contact> contacts=new();readonly HashSet<int> seen=new();readonly List<int> ended=new();
 float scale;Rect safe,stick,fire,next,interact,pause,map;int width,height;GUIStyle label;Texture2D disc,ring;
 internal Rect MoveArea=>stick;internal Rect FireArea=>fire;internal Rect WeaponArea=>next;internal Rect PauseArea=>pause;
 void Awake(){I=this;EnhancedTouchSupport.Enable();active=Application.isMobilePlatform;Layout();}
 void OnDestroy(){if(I==this)I=null;EnhancedTouchSupport.Disable();if(disc)Destroy(disc);if(ring)Destroy(ring);}
 void Layout(){width=Screen.width;height=Screen.height;scale=Mathf.Min(width/850f,height/430f);safe=Screen.safeArea;safe.y=height-safe.yMax;float bottom=Mathf.Min(safe.yMax,height-70*scale);stick=new(safe.x+24*scale,bottom-145*scale,130*scale,130*scale);fire=new(safe.xMax-112*scale,bottom-108*scale,88*scale,88*scale);next=new(safe.xMax-193*scale,bottom-70*scale,70*scale,70*scale);interact=new(safe.xMax-98*scale,bottom-190*scale,74*scale,74*scale);pause=new(safe.x+width*.48f,safe.y+12*scale,68*scale,44*scale);map=new(pause.xMax+10*scale,pause.y,68*scale,44*scale);}
 public void Clear(){contacts.Clear();move=look=Vector2.zero;firing=fireTap=use=false;}
 void OnApplicationFocus(bool focused){if(!focused){Clear();if(Game.I&&Game.I.mode==Mode.Playing)Game.I.SetMode(Mode.Paused);}}
 void OnApplicationPause(bool paused){if(paused)OnApplicationFocus(false);}
 void Update(){
  if(width!=Screen.width||height!=Screen.height){Clear();Layout();}
  if(Touch.activeTouches.Count>0&&!active){active=true;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;}
  if(!active||!Game.I)return;
  if(Game.I.mode!=Mode.Playing){Clear();return;}
  move=look=Vector2.zero;firing=fireTap=use=false;seen.Clear();
  foreach(var t in Touch.activeTouches){
   int id=t.touchId;Vector2 pos=new(t.screenPosition.x,height-t.screenPosition.y);seen.Add(id);
   bool released=t.phase==UnityEngine.InputSystem.TouchPhase.Ended||t.phase==UnityEngine.InputSystem.TouchPhase.Canceled;
   if(!contacts.TryGetValue(id,out var c)){
    // Never interpret an already-held menu finger as a gameplay press.
    if(t.phase!=UnityEngine.InputSystem.TouchPhase.Began)continue;
    c=new Contact{start=pos,last=pos,began=Time.unscaledTime};contacts[id]=c;
    if(pause.Contains(pos)){Game.I.SetMode(Mode.Paused);Clear();return;}
    if(map.Contains(pos))Game.I.map=!Game.I.map;
    else if(InCircle(next,pos))CycleWeapon();
    else if(InCircle(interact,pos))use=true;
    else if(InCircle(fire,pos))c.role=Role.Fire;
    else if(InCircle(stick,pos)&&!HasRole(Role.Move))c.role=Role.Move;
    else if(pos.x>width*.38f&&pos.y<height-70*scale&&!HasRole(Role.Look))c.role=Role.Look;
   }
   Vector2 delta=pos-c.last;c.last=pos;c.travel+=delta.magnitude;
   if(released){if(c.role==Role.Look&&t.phase==UnityEngine.InputSystem.TouchPhase.Ended&&c.travel<14*scale&&Time.unscaledTime-c.began<.25f)fireTap=true;contacts.Remove(id);continue;}
   if(c.role==Role.Move){Vector2 v=Vector2.ClampMagnitude((pos-stick.center)/(48*scale),1);move=new(v.x,-v.y);if(move.magnitude<.12f)move=Vector2.zero;}
   if(c.role==Role.Look||c.role==Role.Fire)look+=new Vector2(delta.x,-delta.y)/scale*.16f*(Game.I.sensitivity/.12f);
   if(c.role==Role.Fire)firing=true;
  }
  ended.Clear();foreach(var id in contacts.Keys)if(!seen.Contains(id))ended.Add(id);foreach(var id in ended)contacts.Remove(id);
 }
 internal static bool InCircle(Rect area,Vector2 point)=>((point-area.center)/(area.width*.5f)).sqrMagnitude<=1;
 void InitCircles(){
  if(disc)return;const int size=128;disc=new Texture2D(size,size,TextureFormat.RGBA32,false);ring=new Texture2D(size,size,TextureFormat.RGBA32,false);disc.wrapMode=ring.wrapMode=TextureWrapMode.Clamp;disc.filterMode=ring.filterMode=FilterMode.Bilinear;
  var fill=new Color[size*size];var edge=new Color[size*size];
  for(int y=0;y<size;y++)for(int x=0;x<size;x++){float radius=new Vector2((x+.5f-size*.5f)/(size*.5f),(y+.5f-size*.5f)/(size*.5f)).magnitude;float alpha=Mathf.Clamp01((1-radius)*size*.5f);fill[y*size+x]=new Color(1,1,1,alpha);edge[y*size+x]=new Color(1,1,1,alpha*Mathf.Clamp01((radius-.95f)*size*.5f));}
  disc.SetPixels(fill);disc.Apply(false,true);ring.SetPixels(edge);ring.Apply(false,true);
 }
 void Circle(Rect rect,string text,bool pressed=false){InitCircles();GUI.color=pressed?new Color(.18f,.55f,.57f,.7f):new Color(.035f,.085f,.10f,.6f);GUI.DrawTexture(rect,disc);GUI.color=pressed?new Color(.75f,1,1,.95f):new Color(.55f,.92f,.9f,.75f);GUI.DrawTexture(rect,ring);GUI.color=Color.white;if(text.Length>0)GUI.Label(rect,text,label);}
 bool HasRole(Role role){foreach(var c in contacts.Values)if(c.role==role)return true;return false;}
 public void CycleWeapon(){var p=Game.I.player;var slots=p.InventorySlots;int current=System.Array.IndexOf(slots,p.weapon);for(int n=1;n<slots.Length;n++){int i=slots[(current+n+slots.Length)%slots.Length];if(i>=0&&p.owned[i]){p.Select(i);return;}}}
 void Box(Rect rect,string text,bool pressed=false){GUI.color=pressed?new Color(.4f,.8f,.8f,.8f):new Color(.04f,.09f,.11f,.65f);GUI.DrawTexture(rect,Texture2D.whiteTexture);GUI.color=new Color(.55f,.92f,.9f,.9f);GUI.DrawTexture(new Rect(rect.x,rect.y,rect.width,2*scale),Texture2D.whiteTexture);GUI.color=Color.white;GUI.Label(rect,text,label);}
 void OnGUI(){
  if(!active||!Game.I||Game.I.mode!=Mode.Playing)return;GUI.matrix=Matrix4x4.identity;GUI.depth=-20;
  label??=new GUIStyle{alignment=TextAnchor.MiddleCenter,font=Game.I.assets.bodyFont};label.fontSize=Mathf.RoundToInt(17*scale);label.normal.textColor=Color.white;
  Circle(stick,"");GUI.Label(new Rect(stick.x,stick.yMax-27*scale,stick.width,20*scale),"MOVE",label);var center=stick.center+new Vector2(move.x,-move.y)*48*scale;Circle(new Rect(center.x-20*scale,center.y-20*scale,40*scale,40*scale),"",move.sqrMagnitude>0);
  Circle(fire,"FIRE",firing);Circle(next,"WEAPON");Circle(interact,"USE",use);Box(pause,"PAUSE");Box(map,Game.I.map?"CLOSE MAP":"MAP");
  label.fontSize=Mathf.RoundToInt(13*scale);GUI.Label(new Rect(width*.35f,height-109*scale,width*.35f,20*scale),"DRAG TO AIM  /  TAP TO FIRE",label);
 }
}
}
