using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace VXRL2 {
public sealed class World:MonoBehaviour {
 public const float Cell=4;
 [System.NonSerialized] public HashSet<Vector2Int> cells=new(), discovered=new();
 public List<RoomDefinition> rooms;
 public Transform root; public NavMeshSurface surface;
 GameAssets a; LevelDefinition level;bool checkpointUsed;
 public static Vector3 Pos(int x,int z)=>new(x*Cell,0,z*Cell);
 public static Vector3 Pos(Vector2Int cell)=>Pos(cell.x,cell.y);
 public static Vector2Int Coord(Vector3 p)=>new(Mathf.RoundToInt(p.x/Cell),Mathf.RoundToInt(p.z/Cell));
 public void Build(GameAssets assets,LevelDefinition definition) {
  a=assets;level=definition;rooms=level.rooms;
  root=new GameObject("Quarantine architecture").transform;
  foreach(var room in rooms)for(int x=room.cells.xMin;x<room.cells.xMax;x++)for(int z=room.cells.yMin;z<room.cells.yMax;z++)cells.Add(new(x,z));
  var dirs=new[]{Vector2Int.up,Vector2Int.right,Vector2Int.down,Vector2Int.left};
  foreach(var cell in cells) {
   var p=Pos(cell);var room=RoomAt(cell); var accent=room!=null?room.accent:Color.cyan;var mat=accent.r>.7f?a.amber:a.cyan;
   Box("Deck",p+Vector3.down*.2f,new(4,.4f,4),a.floor);
   Box("Ceiling",p+Vector3.up*5.8f,new(4,.35f,4),a.dark);
   foreach(var d in dirs)if(!cells.Contains(cell+d)) {
    bool side=d.x!=0;var wp=p+new Vector3(d.x*2,2.8f,d.y*2);
    Box("Bulkhead",wp,side?new(.35f,5.6f,4):new(4,5.6f,.35f),a.wall);
    Box("Skirting",wp+Vector3.down*2.45f,side?new(.43f,.4f,4):new(4,.4f,.43f),a.trim);
    Box("Structural rib",wp,side?new(.5f,5.4f,.16f):new(.16f,5.4f,.5f),a.metal);
    if((cell.x+cell.y)%2==0){var lp=wp-new Vector3(d.x*.24f,-1.4f,d.y*.24f);Box("Access light",lp,side?new(.07f,.09f,2.6f):new(2.6f,.09f,.07f),mat,false);}
   }
   if(cell.x%3==0 && cell.y%3==0) {
    Box("Ceiling conduit",p+new Vector3(0,5.5f,0),new(.24f,.18f,4),a.metal,false);
    Box("Luminaire",p+new Vector3(0,5.42f,0),new(1.9f,.08f,.38f),mat,false);
    LightAt(p+Vector3.up*4.7f,Color.Lerp(Color.white,accent,.3f),2.6f,13);
   }
  }
  foreach(var c in level.pillars) {
   var p=Pos(c);Box("Reactor support",p+Vector3.up*2.8f,new(1.8f,5.6f,1.8f),a.trim);
   Box("Support collar",p+Vector3.up*.4f,new(2.1f,.6f,2.1f),a.metal);
   Box("Reactor core",p+Vector3.up*3,new(1.85f,.15f,1.85f),a.cyan,false);
  }
  foreach(var room in rooms)if(room.decorate){
   var center=new Vector2Int(room.cells.xMin+room.cells.width/2,room.cells.yMin+room.cells.height/2);
   var label=Text(room.name,Pos(center)+new Vector3(0,4.2f,room.cells.height*2-2.25f),.09f,room.accent); label.transform.rotation=Quaternion.identity;
   // Raised machinery along room edges leaves the combat lanes open.
   if(room.cells.width>=6)for(int j=0;j<2;j++){
    var p=Pos(room.cells.xMin+1,room.cells.yMin+2+j*2);Box("Server pedestal",p+Vector3.up*.6f,new(1.2f,1.2f,2),a.trim);
    Box("Server screen",p+new Vector3(.62f,.9f,0),new(.04f,.45f,1.3f),a.cyan,false);
   }
  }
  foreach(var room in rooms)if(room.decorate && room.cells.width>=6){
   for(int z=room.cells.yMin+1;z<room.cells.yMax-1;z+=3){var p=Pos(room.cells.xMax-1,z);Box("Service cabinet",p+new Vector3(.8f,1.5f,0),new(1.1f,3,1.5f),a.trim);for(int j=0;j<5;j++)Box("Vent slat",p+new Vector3(.22f,.7f+j*.3f,0),new(.05f,.09f,1.2f),a.metal,false);Box("Status display",p+new Vector3(.2f,2.4f,0),new(.05f,.3f,.7f),a.cyan,false);}
   // Floor edge markings make the combat space legible.
   var center=Pos(room.cells.xMin+room.cells.width/2,room.cells.yMin+room.cells.height/2);
   Box("Lane guide",new Vector3((room.cells.xMin+.6f)*Cell,.015f,center.z),new(.08f,.02f,(room.cells.height-1)*Cell),a.amber,false);
  }
  surface=root.gameObject.AddComponent<NavMeshSurface>();surface.collectObjects=CollectObjects.Children;surface.useGeometry=NavMeshCollectGeometry.PhysicsColliders;surface.BuildNavMesh();
  StaticBatchingUtility.Combine(root.gameObject);
  foreach(var sign in level.signs){var t=Text(sign.text,Pos(sign.cell)+Vector3.up*3.5f,.075f,sign.color);t.transform.rotation=Quaternion.Euler(0,sign.yaw,0);}
  foreach(var d in level.doors)MakeDoor(d);
  foreach(var s in level.enemies) {
   var e=new GameObject(s.definition.displayName).AddComponent<Enemy>();e.Build(s.definition,Pos(s.cell));Game.I.enemies.Add(e);
  }
  Game.I.totalEnemies=Game.I.enemies.Count;
  foreach(var s in level.pickups){var p=new GameObject(s.type.ToString()).AddComponent<Pickup>();p.Build(s,a);Game.I.pickups.Add(p);}
  var exit=Box("Purge terminal",Pos(level.exit)+new Vector3(0,1.3f,0),new(2.5f,2.6f,1),a.trim);
  Box("Purge display",exit.transform.position+new Vector3(0,.3f,-.52f),new(1.9f,1.1f,.04f),a.green,false);
  var terminal=exit.AddComponent<Terminal>();terminal.prompt="E  •  PURGE ROOT INFECTION";
  Text("PURGE / EXIT",exit.transform.position+new Vector3(0,2,-.6f),.26f,Color.green).transform.rotation=Quaternion.identity;
  var volume=new GameObject("Atmosphere").AddComponent<Volume>();volume.isGlobal=true;var profile=ScriptableObject.CreateInstance<VolumeProfile>();volume.profile=profile;
  var tonemap=profile.Add<Tonemapping>();tonemap.mode.Override(TonemappingMode.ACES);
  var bloom=profile.Add<Bloom>();bloom.intensity.Override(.18f);bloom.threshold.Override(1.1f);
  var grading=profile.Add<ColorAdjustments>();grading.contrast.Override(15);grading.saturation.Override(-8);
  var vignette=profile.Add<Vignette>();vignette.intensity.Override(.2f);
  var sun=new GameObject("Soft directional").AddComponent<Light>();sun.type=LightType.Directional;sun.intensity=.65f;sun.color=new Color(.65f,.78f,1);sun.transform.rotation=Quaternion.Euler(55,-30,0);sun.shadows=LightShadows.Soft;
  RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.12f,.16f,.21f);RenderSettings.fog=true;RenderSettings.fogMode=FogMode.ExponentialSquared;RenderSettings.fogDensity=.008f;RenderSettings.fogColor=new Color(.025f,.045f,.06f);
 }
 void MakeDoor(DoorSpawn s) {
  var p=Pos(s.cell);var go=Box(s.label,p+Vector3.up*2.6f,s.alongX?new(.5f,5.2f,11.6f):new(11.6f,5.2f,.5f),s.secret?a.wall:a.trim);
  var d=go.AddComponent<Door>();d.secret=s.secret;d.key=s.key;d.label=s.label;d.closedPosition=go.transform.position;
  var n=go.AddComponent<NavMeshObstacle>();n.shape=NavMeshObstacleShape.Box;n.size=go.transform.localScale;n.carving=true; // dimensions corrected below: collider box is in local units
  n.size=Vector3.one;d.obstacle=n;
  if(!s.secret){var line=Box("Gate authorization strip",p+Vector3.up*2.5f,s.alongX?new(.54f,.16f,11.5f):new(11.5f,.16f,.54f),s.key==1?a.cyan:a.red,false);line.transform.SetParent(go.transform,true);}
  Game.I.doors.Add(d);
 }
 public GameObject Box(string name,Vector3 pos,Vector3 scale,Material mat,bool collision=true) {
  var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(root);go.transform.position=pos;go.transform.localScale=scale;go.GetComponent<Renderer>().sharedMaterial=mat;
  if(!collision){DestroyImmediate(go.GetComponent<Collider>());go.layer=2;}
  return go;
 }
 public static TextMesh Text(string text,Vector3 pos,float size,Color color){var go=new GameObject(text);go.transform.position=pos;var tm=go.AddComponent<TextMesh>();tm.text=text;tm.characterSize=size;tm.fontSize=60;tm.anchor=TextAnchor.MiddleCenter;tm.color=color;var mr=go.GetComponent<MeshRenderer>();var mat=new Material(Game.I.assets.worldTextShader);mat.mainTexture=tm.font.material.mainTexture;mr.sharedMaterial=mat;return tm;}
 public static Light LightAt(Vector3 pos,Color color,float intensity,float range){var light=new GameObject("Facility light").AddComponent<Light>();light.transform.position=pos;light.type=LightType.Point;light.color=color;light.intensity=intensity;light.range=range;light.shadows=LightShadows.None;return light;}
 public RoomDefinition RoomAt(Vector2Int cell){foreach(var r in rooms)if(r.cells.Contains(cell)&&r.decorate)return r;return null;}
 public void Discover(Vector3 p) {
  var c=Coord(p);for(int x=-3;x<=3;x++)for(int z=-3;z<=3;z++){var cc=c+new Vector2Int(x,z);if(cells.Contains(cc))discovered.Add(cc);}
  var room=RoomAt(c);if(room!=null)Game.I.area=room.name;
  if(!checkpointUsed && Game.I.redKey && Vector3.Distance(p,Pos(level.checkpoint))<5){checkpointUsed=true;Game.I.player.health=Mathf.Max(75,Game.I.player.health);Game.I.SaveCheckpoint();Game.I.Say("CHECKPOINT  /  WARDEN APPROACH",4);}
 }
 public void ResetCheckpointTrigger(){checkpointUsed=Game.I.redKey;}
}
public class Interactable:MonoBehaviour {public string prompt;public virtual string Prompt=>prompt;public virtual void Use(){} }
public sealed class Door:Interactable {
 public bool secret;public int key;public string label;public bool open;public Vector3 closedPosition;public NavMeshObstacle obstacle;
 public bool Authorized=>key==0 || key==1&&Game.I.blueKey || key==2&&Game.I.redKey;
 public override string Prompt=>open?"":secret?"E  •  EXAMINE LOOSE PANEL":Authorized?"E  •  OPEN "+label:(key==1?"BLUE":"RED")+" ACCESS REQUIRED";
 public override void Use(){if(open)return;if(!Authorized){Game.I.Say(Prompt);return;}open=true;obstacle.enabled=false;Game.I.Sound(Game.I.assets.pickup,.65f,.65f);}
 void Update(){transform.position=Vector3.MoveTowards(transform.position,closedPosition+(open?Vector3.up*5.8f:Vector3.zero),Time.deltaTime*5);}
 public void Restore(bool value){open=value;transform.position=closedPosition+(open?Vector3.up*5.8f:Vector3.zero);obstacle.enabled=!open;}
}
public sealed class Terminal:Interactable {public override void Use(){Game.I.Win();}}
public enum PickupType { Health, Armor, Ammo, Weapon, BlueKey, RedKey, Secret }
public sealed class Pickup:MonoBehaviour {
 public PickupType type;public int amount,weaponIndex;
 public int EffectiveWeaponIndex=>Game.I.player!=null&&!Game.I.player.CanUse(weaponIndex)?Game.I.player.StartingRangedIndex:weaponIndex;public bool taken;Transform visual;Vector3 home;int visualCharacter=-1;
 public void Build(PickupSpawn s,GameAssets a){type=s.type;amount=s.amount;weaponIndex=s.weaponIndex;transform.position=World.Pos(s.cell)+Vector3.up*.8f;home=transform.position;
  visual=new GameObject("Pickup visual").transform;visual.SetParent(transform,false);
  if(type==PickupType.Weapon){RefreshWeaponVisual();}
  else if(type==PickupType.Health){Piece(new Vector3(.7f,.4f,.55f),Vector3.zero,a.trim);Piece(new Vector3(.45f,.05f,.13f),Vector3.up*.23f,a.green);Piece(new Vector3(.13f,.05f,.4f),Vector3.up*.23f,a.green);}
  else if(type==PickupType.Ammo){Piece(new Vector3(.6f,.35f,.45f),Vector3.zero,a.metal);for(int i=0;i<3;i++)Piece(new Vector3(.09f,.4f,.12f),new Vector3((i-1)*.16f,.04f,-.23f),a.amber);}
  else if(type==PickupType.Armor){Piece(new Vector3(.65f,.8f,.16f),Vector3.zero,a.trim);Piece(new Vector3(.42f,.58f,.19f),Vector3.zero,a.cyan);}
  else if(type==PickupType.BlueKey||type==PickupType.RedKey){Piece(new Vector3(.6f,.85f,.12f),Vector3.zero,a.metal);Piece(new Vector3(.46f,.12f,.14f),Vector3.up*.2f,type==PickupType.BlueKey?a.cyan:a.red);Piece(new Vector3(.15f,.2f,.15f),Vector3.down*.13f,a.amber);}
  else {Piece(new Vector3(.7f,.55f,.6f),Vector3.zero,a.amber);Piece(new Vector3(.75f,.08f,.65f),Vector3.zero,a.trim);}
 }
 void RefreshWeaponVisual(){if(type!=PickupType.Weapon||visualCharacter==Game.I.characterIndex)return;visualCharacter=Game.I.characterIndex;foreach(Transform child in visual)Destroy(child.gameObject);var definition=Game.I.assets.campaign.weapons[weaponIndex];if(System.Array.IndexOf(Game.I.Character.inventoryOrder,definition)<0)definition=Game.I.Character.startingWeapons[0].weapon;var model=Instantiate(definition.model,visual);model.transform.localScale=Vector3.one*1.8f;}
 void Piece(Vector3 scale,Vector3 pos,Material mat){var go=GameObject.CreatePrimitive(PrimitiveType.Cube);Destroy(go.GetComponent<Collider>());go.layer=2;go.transform.SetParent(visual,false);go.transform.localPosition=pos;go.transform.localScale=scale;go.GetComponent<Renderer>().sharedMaterial=mat;
 }
 void Update(){RefreshWeaponVisual();if(taken||Game.I.mode!=Mode.Playing)return;transform.position=home+Vector3.up*(Mathf.Sin(Time.time*2+home.x)*.12f);visual.Rotate(0,60*Time.deltaTime,0);if(Vector3.Distance(Game.I.player.transform.position+Vector3.up*.8f,transform.position)<1.3f)Take();}
 public bool Take(){if(taken)return false;var g=Game.I;var p=g.player;string msg="";int wi=EffectiveWeaponIndex;int granted=wi!=weaponIndex?Mathf.Max(1,Mathf.RoundToInt((float)amount/Mathf.Max(1,p.Definitions[weaponIndex].pickupAmmo)*p.Definitions[wi].pickupAmmo)):amount;
  switch(type){
   case PickupType.Health:if(p.health>=100)return false;p.health=Mathf.Min(100,p.health+amount);msg="HEALTH +"+amount;break;
   case PickupType.Armor:if(p.armor>=100)return false;p.armor=Mathf.Min(100,p.armor+amount);msg="ARMOR +"+amount;break;
   case PickupType.Ammo:if(wi<0||wi>=p.ammo.Length||p.ammo[wi]>=p.Definitions[wi].maxAmmo)return false;p.ammo[wi]=Mathf.Min(p.Definitions[wi].maxAmmo,p.ammo[wi]+granted);msg=p.Definitions[wi].displayName+" AMMO +"+granted;break;
   case PickupType.Weapon:bool fresh=!p.owned[wi];p.owned[wi]=true;p.ammo[wi]=Mathf.Min(p.Definitions[wi].maxAmmo,p.ammo[wi]+granted);if(fresh)p.Select(wi);msg=p.Definitions[wi].displayName+(fresh?" ACQUIRED":" AMMO +"+granted);break;
   case PickupType.BlueKey:g.blueKey=true;msg="BLUE ACCESS ACQUIRED  /  RETURN TO THE SECURITY HUB";break;
   case PickupType.RedKey:g.redKey=true;msg="RED ACCESS ACQUIRED  /  OPEN THE WARDEN GATE";break;
   case PickupType.Secret:g.secrets++;p.armor=Mathf.Min(100,p.armor+40);msg="SECRET FOUND  /  ARMOR +40";break;
  }taken=true;gameObject.SetActive(false);g.Sound(g.assets.pickup,.7f,1.15f);g.Say(msg,4);return true;
 }
 public void Restore(bool value){taken=value;gameObject.SetActive(!value);}
}
public sealed class Billboard:MonoBehaviour {void LateUpdate(){if(Game.I.player)transform.rotation=Quaternion.LookRotation(transform.position-Game.I.player.cam.transform.position);}}
}
