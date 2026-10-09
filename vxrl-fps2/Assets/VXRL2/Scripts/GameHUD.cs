using UnityEngine;
namespace VXRL2 {
/// <summary>Resolution-independent, Doom-inspired status bar and operator selection.</summary>
public sealed class GameHUD : MonoBehaviour {
 Game G => Game.I;
 readonly Color paper=new(.93f,.87f,.72f), gold=new(.94f,.63f,.24f), rust=new(.68f,.16f,.075f), muted=new(.57f,.63f,.65f), ink=new(.025f,.034f,.039f,.97f);
 GUIStyle body, caption, heading, number, hero;
 bool settings;
 float W,H;
 Texture2D white;
 void Init() {
  if(body!=null)return;
  white=Texture2D.whiteTexture;
  body=new GUIStyle {font=G.assets.bodyFont,fontSize=22,wordWrap=true};
  caption=new GUIStyle(body){fontSize=17};
  heading=new GUIStyle {font=G.assets.displayFont,fontSize=25,wordWrap=false};
  number=new GUIStyle(heading){fontSize=46};
  hero=new GUIStyle(heading){fontSize=64};
 }
 void Fill(Rect r,Color c){GUI.color=c;GUI.DrawTexture(r,white);GUI.color=Color.white;}
 void Fill(float x,float y,float w,float h,Color c)=>Fill(new Rect(x,y,w,h),c);
 void Outline(Rect r,Color c,float size=1){Fill(r.x,r.y,r.width,size,c);Fill(r.x,r.yMax-size,r.width,size,c);Fill(r.x,r.y,size,r.height,c);Fill(r.xMax-size,r.y,size,r.height,c);}
 void Label(Rect r,string text,GUIStyle style,Color color,TextAnchor align=TextAnchor.UpperLeft){style.normal.textColor=color;style.alignment=align;GUI.Label(r,text,style);}
 void Label(float x,float y,float w,float h,string text,GUIStyle style,Color color,TextAnchor align=TextAnchor.UpperLeft)=>Label(new Rect(x,y,w,h),text,style,color,align);
 void BevelText(Rect r,string text,GUIStyle style,Color face){
  Label(new Rect(r.x+3,r.y+5,r.width,r.height),text,style,new Color(.12f,.035f,.02f));
  Label(new Rect(r.x,r.y+2,r.width,r.height),text,style,rust);
  Label(r,text,style,face);
 }
 void Panel(Rect r,Color edge){
  Fill(r,ink);Fill(r.x+2,r.y+2,r.width-4,2,new Color(.22f,.25f,.25f));Outline(r,new Color(.25f,.29f,.29f));Fill(r.x,r.y,r.width,3,edge);
 }
 bool Button(Rect r,string text,bool primary=false){
  bool hover=r.Contains(Event.current.mousePosition);
  Fill(r,primary?(hover?new Color(.71f,.22f,.11f):new Color(.47f,.105f,.055f)):(hover?new Color(.17f,.21f,.22f):new Color(.085f,.115f,.125f)));
  Outline(r,primary?gold:new Color(.3f,.37f,.38f));Fill(r.x+2,r.y+2,r.width-4,1,new Color(1,1,1,.1f));
  Label(r.x+17,r.y,r.width-34,r.height,text,heading,primary?paper:Color.white,TextAnchor.MiddleLeft);
  return GUI.Button(r,GUIContent.none,GUIStyle.none);
 }
 void Portrait(Rect r,Texture2D texture){
  Fill(r,new Color(.82f,.83f,.8f));
  if(texture)GUI.DrawTexture(r,texture,ScaleMode.ScaleToFit,true);
  Outline(r,new Color(.035f,.045f,.045f),2);
 }
 void OnGUI(){
  Init();float s=Mathf.Min(Screen.height/720f,Screen.width/1100f);W=Screen.width/s;H=Screen.height/s;GUI.matrix=Matrix4x4.TRS(Vector3.zero,Quaternion.identity,Vector3.one*s);
  if(G.mode==Mode.Playing){DrawHUD();if(G.map)DrawFullMap();return;}
  Fill(0,0,W,H,new Color(.009f,.014f,.017f,.92f));
  for(int y=0;y<H;y+=4)Fill(0,y,W,1,new Color(.4f,.48f,.5f,.025f));
  Fill(0,0,6,H,rust);Fill(40,34,42,3,gold);
  Label(94,23,800,30,"VXRL / FPS2    -    OFFLINE CAMPAIGN",caption,muted);
  BevelText(new Rect(40,59,1000,89),"ROOTBREACH",hero,paper);
  Label(44,144,W-88,32,G.Level.subtitle+"   /   "+G.Level.displayName,caption,gold);
  if(settings){DrawSettings();return;}
  if(G.mode==Mode.Title)DrawTitle();
  else if(G.mode==Mode.Paused)DrawPause();
  else if(G.mode==Mode.Dead)DrawDeath();
  else DrawResults();
  Fill(40,H-60,W-80,1,new Color(.22f,.29f,.3f));
  Label(44,H-44,W-88,25,TouchControls.Active?"LEFT STICK  MOVE     DRAG RIGHT SIDE  AIM     TAP / HOLD FIRE     WEAPON  SWITCH     USE  INTERACT":"WASD  MOVE     MOUSE  AIM / FIRE     1 RANGED  /  2 BLADE     E  USE     TAB  MAP     ESC  PAUSE",caption,muted);
  if(Time.unscaledTime<G.messageUntil)Label(44,H-91,W-88,28,G.message,caption,gold);
 }
 void DrawTitle(){
  float left=44,gap=18,cardW=Mathf.Min(265,(W-500)/2),cardY=206,cardH=332;
  Label(left,179,650,26,"01  /  CHOOSE YOUR OPERATIVE",caption,muted);
  for(int i=0;i<G.assets.campaign.characters.Length;i++){
   var c=G.assets.campaign.characters[i];Rect card=new(left+i*(cardW+gap),cardY,cardW,cardH);bool selected=i==G.characterIndex;
   Panel(card,selected?c.accent:new Color(.23f,.27f,.28f));if(selected)Outline(card,c.accent,2);
   Label(card.x+16,card.y+15,card.width-32,37,c.displayName,heading,selected?paper:muted);
   Portrait(new Rect(card.x+22,card.y+64,card.width-44,156),c.portrait);
   Label(card.x+16,card.y+234,card.width-32,25,c.tagline,caption,c.accent);
   string loadout=c.startingWeapons[0].weapon.displayName+"\n"+c.startingWeapons[1].weapon.displayName;
   Label(card.x+16,card.y+263,card.width-32,58,loadout,body,paper);
   if(GUI.Button(card,GUIContent.none,GUIStyle.none))G.SelectCharacter(i);
   if(selected){Fill(card.x+card.width-29,card.y+23,10,10,c.accent);}
  }
  float right=left+2*(cardW+gap)+24,buttonW=W-right-44;
  Label(right,205,buttonW,35,"02  /  DEPLOY",caption,muted);
  Label(right,242,buttonW,78,G.Character.description,body,paper);
  Label(right,329,buttonW,26,"THREAT LEVEL",caption,muted);
  string[] levels={"OPERATIVE","VETERAN","NIGHTMARE"};float chip=(buttonW-12)/3;
  for(int i=0;i<3;i++){Rect r=new(right+i*(chip+6),363,chip,36);Fill(r,G.difficulty==i?rust:new Color(.09f,.12f,.13f));Outline(r,G.difficulty==i?gold:new Color(.22f,.27f,.28f));Label(r,levels[i],caption,G.difficulty==i?paper:muted,TextAnchor.MiddleCenter);if(GUI.Button(r,GUIContent.none,GUIStyle.none))G.difficulty=i;}
  if(Button(new Rect(right,421,buttonW,58),"ENTER QUARANTINE  >",true))G.StartRun();
  if(G.HasDiskSave && Button(new Rect(right,492,buttonW,45),"CONTINUE CHECKPOINT"))G.ContinueDisk();
  if(Button(new Rect(left,566,cardW,46),"SETTINGS"))settings=true;
  if(Button(new Rect(left+cardW+gap,566,cardW,46),"QUIT"))Quit();
  Label(right,558,buttonW,60,"FAST MOVEMENT. NO RELOADS.\nFIND KEYS. KEEP MOVING.",caption,muted);
  if(G.assets.campaign.levels.Length>1){if(GUI.Button(new Rect(W-280,30,235,30),"SECTOR: "+(Game.levelIndex+1)+"  >")){Game.levelIndex=(Game.levelIndex+1)%G.assets.campaign.levels.Length;G.NewRun();}}
 }
 void DrawPause(){
  Label(44,219,700,50,"RUN SUSPENDED",heading,gold);
  if(Button(new Rect(44,289,380,50),"RESUME  >",true))G.SetMode(Mode.Playing);
  if(Button(new Rect(44,352,380,50),"SETTINGS"))settings=true;
  if(Button(new Rect(44,415,380,50),"RESTART CHECKPOINT"))G.RestoreCheckpoint();
  if(Button(new Rect(44,478,380,50),"RETURN TO TITLE"))G.NewRun();
  Portrait(new Rect(490,253,220,172),G.Character.portrait);Label(490,446,400,40,G.Character.displayName,heading,paper);
 }
 void DrawDeath(){
  BevelText(new Rect(44,218,900,65),"CONNECTION TERMINATED",heading,new Color(.95f,.3f,.17f));
  Label(44,280,650,40,"Your last checkpoint is intact.",body,muted);
  if(Button(new Rect(44,356,445,57),"RESTORE CHECKPOINT  >",true))G.RestoreCheckpoint();
  if(Button(new Rect(44,431,445,50),"RETURN TO TITLE"))G.NewRun();
 }
 void DrawResults(){
  BevelText(new Rect(44,208,800,55),"SECTOR PURGED",heading,gold);
  Label(44,282,650,140,$"OPERATIVE     {G.Character.displayName}\nELIMINATIONS     {G.kills} / {G.totalEnemies}\nSECRETS     {G.secrets} / {G.Level.pickups.FindAll(p=>p.type==PickupType.Secret).Count}\nCLEAR TIME     {TimeText(G.elapsed)}",body,paper);
  if(Game.levelIndex+1<G.assets.campaign.levels.Length && Button(new Rect(44,455,400,50),"NEXT SECTOR  >",true))G.NextLevel();
  if(Button(new Rect(44,523,400,50),"PLAY AGAIN"))G.NewRun();
  if(Button(new Rect(467,523,250,50),"QUIT"))Quit();
  Portrait(new Rect(710,258,240,188),G.Character.portrait);
 }
 void DrawSettings(){
  Label(44,209,650,43,"FIELD CONFIGURATION",heading,paper);
  Label(44,274,500,26,"AIM SENSITIVITY",body,muted);G.sensitivity=GUI.HorizontalSlider(new Rect(44,312,430,22),G.sensitivity,.035f,.3f);
  Label(44,357,500,26,"FIELD OF VIEW  /  "+Mathf.RoundToInt(G.fov),body,muted);G.fov=GUI.HorizontalSlider(new Rect(44,395,430,22),G.fov,70,110);
  Label(44,440,500,26,"MASTER VOLUME",body,muted);G.volume=GUI.HorizontalSlider(new Rect(44,478,430,22),G.volume,0,1);AudioListener.volume=G.volume;
  if(Button(new Rect(560,276,400,49),"CAMERA BOB: "+(G.bob?"ON":"OFF")))G.bob=!G.bob;
  if(Button(new Rect(560,340,400,49),"MINIMAP: "+(G.minimap?"ON":"OFF")))G.minimap=!G.minimap;
  Label(562,410,400,75,TouchControls.Active?"The minimap shows explored space, nearby hostiles and access gates. Tap MAP to open the full sector map.":"The minimap shows explored space, nearby hostiles and access gates. TAB opens the full sector map.",body,muted);
  if(Button(new Rect(44,554,430,54),"SAVE & BACK",true)){G.SaveSettings();settings=false;}
 }
 void DrawHUD(){
  var p=G.player;Color accent=G.Character.accent;
  Fill(22,23,4,52,accent);Label(39,19,W-310,33,G.area,heading,paper);Label(40,56,W-350,45,G.Objective,caption,accent);
  float cx=W*.5f,cy=H*.5f;
  Color cross=G.hitMarker>0?gold:new Color(.95f,.94f,.86f,.9f);
  Fill(cx-9,cy-1,5,2,cross);Fill(cx+4,cy-1,5,2,cross);Fill(cx-1,cy-9,2,5,cross);Fill(cx-1,cy+4,2,5,cross);
  if(G.hitMarker>0){Line(new(cx-10,cy-10),new(cx-5,cy-5),gold,2);Line(new(cx+10,cy-10),new(cx+5,cy-5),gold,2);Line(new(cx-10,cy+10),new(cx-5,cy+5),gold,2);Line(new(cx+10,cy+10),new(cx+5,cy+5),gold,2);}
  if(p.prompt!=""){Panel(new Rect(cx-235,cy+57,470,39),gold);Label(cx-225,cy+62,450,32,TouchControls.Active?p.prompt.Replace("[E]","[USE]"):p.prompt,body,paper,TextAnchor.UpperCenter);}
  if(Time.unscaledTime<G.messageUntil){Rect notice=new(cx-310,111,620,36);Fill(notice,new Color(.025f,.04f,.045f,.88f));Label(notice,G.message,caption,gold,TextAnchor.MiddleCenter);}
  DrawStatusBar();
  if(G.minimap&&!G.map){DrawMiniMap(new Rect(W-230,21,208,224));Label(W-230,252,208,25,TimeText(G.elapsed)+"   /   "+G.kills+" ELIMINATED",caption,muted,TextAnchor.UpperCenter);}
  if(G.damageFlash>0){Fill(0,0,W,8,new Color(.9f,.1f,.02f,G.damageFlash));Fill(0,0,8,H,new Color(.9f,.1f,.02f,G.damageFlash));Fill(W-8,0,8,H,new Color(.9f,.1f,.02f,G.damageFlash));}
  foreach(var e in G.enemies)if(e.definition.role==EnemyRole.Boss&&!e.dead&&Vector3.Distance(e.transform.position,p.transform.position)<30){float x=cx-200;Fill(x,89,400,8,ink);Fill(x,89,400*e.health/e.definition.health,8,rust);break;}
 }
 void DrawStatusBar(){
  var p=G.player;float width=Mathf.Min(1180,W-32),x=(W-width)/2,y=H-126,c=width/7;
  Panel(new Rect(x,y,width,114),new Color(.46f,.36f,.22f));
  for(int i=0;i<7;i++)if(i!=3)Fill(x+i*c,y+12,1,88,new Color(.22f,.26f,.27f));
  Label(x+16,y+10,c-20,25,"HEALTH",caption,muted);BevelText(new Rect(x+15,y+31,c-15,62),Mathf.CeilToInt(p.health).ToString(),number,p.health<30?new Color(1,.26f,.12f):paper);Fill(x+16,y+95,c-32,3,new Color(.17f,.19f,.19f));Fill(x+16,y+95,(c-32)*p.health/100,3,p.health<30?rust:G.Character.accent);
  Label(x+c+16,y+10,c-20,25,"ARMOR",caption,muted);BevelText(new Rect(x+c+15,y+31,c-15,62),Mathf.CeilToInt(p.armor).ToString(),number,G.Character.accent);
  Rect portrait=new(x+c*2+8,y+8,c-16,78);Portrait(portrait,G.Character.portrait);Label(x+c*2,y+89,c,23,G.Character.displayName,caption,G.Character.accent,TextAnchor.UpperCenter);
  Label(x+c*3+15,y+10,c*1.4f-20,28,p.Current.displayName,caption,paper);BevelText(new Rect(x+c*3+15,y+33,c,60),p.Current.fireMode==FireMode.Melee?"--":p.ammo[p.weapon].ToString(),number,gold);Label(x+c*3+15,y+91,c*1.4f,24,p.Current.fireMode==FireMode.Melee?"UNLIMITED":"AMMO / "+p.Current.maxAmmo,caption,muted);
  float ax=x+c*4.4f;Label(ax,y+10,c*1.55f,24,"ARSENAL",caption,muted);var slots=p.InventorySlots;
  for(int i=0;i<slots.Length;i++){int slot=slots[i];if(slot<0)continue;Rect r=new(ax+(i%2)*c*.75f,y+38+(i/2)*31,c*.7f,27);bool selected=slot==p.weapon;Fill(r,selected?new Color(.4f,.17f,.08f):new Color(.08f,.105f,.11f));if(selected)Outline(r,gold);string name=p.Definitions[slot].displayName.Split(' ')[0];Label(r.x+7,r.y+2,r.width-10,25,(i+1)+" "+name,caption,p.owned[slot]?paper:new Color(.27f,.32f,.33f));}
  float kx=x+c*6.1f;Label(kx,y+10,c*.85f,25,"ACCESS",caption,muted);KeyBadge(new Rect(kx,y+40,c*.78f,25),"B",G.blueKey,new Color(.2f,.8f,1));KeyBadge(new Rect(kx,y+73,c*.78f,25),"R",G.redKey,new Color(1,.26f,.12f));
 }
 void KeyBadge(Rect r,string letter,bool owned,Color color){Fill(r,owned?new Color(color.r*.18f,color.g*.18f,color.b*.18f):new Color(.06f,.08f,.09f));Outline(r,owned?color:new Color(.17f,.22f,.23f));Label(r.x+8,r.y+2,r.width-10,r.height,letter+" / "+(owned?"ONLINE":"LOCKED"),caption,owned?color:muted);}
 void DrawMiniMap(Rect r){
  Panel(r,G.Character.accent);Label(r.x+11,r.y+7,r.width-22,23,"MOTION SCANNER",caption,paper);
  Rect content=new(r.x+9,r.y+35,r.width-18,158);Fill(content,new Color(.015f,.025f,.03f));
  GUI.BeginGroup(content);Vector2 center=new(content.width*.5f,content.height*.5f);float cellSize=12;var pos=G.player.transform.position/World.Cell;
  foreach(var cell in G.world.discovered){Vector2 at=center+new Vector2((cell.x-pos.x)*cellSize,-(cell.y-pos.z)*cellSize);Rect tile=new(at.x-cellSize*.5f,at.y-cellSize*.5f,cellSize-.6f,cellSize-.6f);if(tile.xMax<0||tile.yMax<0||tile.x>content.width||tile.y>content.height)continue;Fill(tile,new Color(.14f,.23f,.25f));}
  foreach(var door in G.doors){if(door.secret&&!door.open)continue;var c=World.Coord(door.closedPosition);if(!G.world.discovered.Contains(c))continue;Vector2 at=center+new Vector2((c.x-pos.x)*cellSize,-(c.y-pos.z)*cellSize);Fill(at.x-3,at.y-3,6,6,door.open?muted:door.key==1?new Color(.2f,.8f,1):rust);}
  int threats=0;foreach(var e in G.enemies){if(e.dead||Vector3.Distance(e.transform.position,G.player.transform.position)>25)continue;var cell=World.Coord(e.transform.position);if(!G.world.discovered.Contains(cell))continue;var ep=e.transform.position/World.Cell;Vector2 at=center+new Vector2((ep.x-pos.x)*cellSize,-(ep.z-pos.z)*cellSize);if(at.x<3||at.y<3||at.x>content.width-3||at.y>content.height-3)continue;Fill(at.x-2.5f,at.y-2.5f,5,5,new Color(1,.25f,.1f));threats++;}
  foreach(var item in G.pickups){if(item.taken||(item.type!=PickupType.BlueKey&&item.type!=PickupType.RedKey))continue;var cell=World.Coord(item.transform.position);if(!G.world.discovered.Contains(cell))continue;var pp=item.transform.position/World.Cell;Vector2 at=center+new Vector2((pp.x-pos.x)*cellSize,-(pp.z-pos.z)*cellSize);Outline(new Rect(at.x-3,at.y-3,6,6),gold,2);}
  Fill(0,Mathf.Repeat(Time.unscaledTime*23,content.height),content.width,1,new Color(.2f,.85f,.85f,.13f));
  Arrow(center,G.player.transform.eulerAngles.y,paper);GUI.EndGroup();
  Label(r.x+12,r.y+197,r.width-24,24,"N ^   "+threats+(TouchControls.Active?" CONTACTS    [MAP]":" CONTACTS    [TAB]"),caption,threats>0?gold:muted);
 }
 void Arrow(Vector2 center,float yaw,Color color){float a=yaw*Mathf.Deg2Rad;Vector2 f=new(Mathf.Sin(a),-Mathf.Cos(a)),side=new(-f.y,f.x);Vector2 tip=center+f*8,left=center-f*5+side*5,right=center-f*5-side*5;Line(tip,left,color,2);Line(left,right,color,2);Line(right,tip,color,2);}
 void Line(Vector2 from,Vector2 to,Color color,float thickness){Vector2 delta=to-from;int steps=Mathf.Max(1,Mathf.CeilToInt(Mathf.Max(Mathf.Abs(delta.x),Mathf.Abs(delta.y))));for(int i=0;i<=steps;i++){Vector2 at=Vector2.Lerp(from,to,(float)i/steps);Fill(at.x-thickness*.5f,at.y-thickness*.5f,thickness,thickness,color);}}

 void DrawFullMap(){
  Rect panel=new(W*.5f-400,89,800,H-246);Panel(panel,G.Character.accent);Label(panel.x+21,panel.y+14,700,35,"QUARANTINE / SECTOR MAP",heading,paper);
  float minX=float.MaxValue,minZ=float.MaxValue,maxX=float.MinValue,maxZ=float.MinValue;foreach(var c in G.world.cells){minX=Mathf.Min(minX,c.x);minZ=Mathf.Min(minZ,c.y);maxX=Mathf.Max(maxX,c.x);maxZ=Mathf.Max(maxZ,c.y);}
  float scale=Mathf.Min((panel.width-70)/(maxX-minX+1),(panel.height-105)/(maxZ-minZ+1));float ox=panel.center.x-(maxX-minX+1)*scale*.5f,oy=panel.yMax-32;
  foreach(var c in G.world.discovered)Fill(ox+(c.x-minX)*scale,oy-(c.y-minZ+1)*scale,scale-1,scale-1,new Color(.19f,.31f,.33f));
  foreach(var d in G.doors){if(d.secret&&!d.open)continue;var c=World.Coord(d.closedPosition);if(G.world.discovered.Contains(c))Fill(ox+(c.x-minX)*scale,oy-(c.y-minZ+1)*scale,Mathf.Max(4,scale-1),Mathf.Max(4,scale-1),d.open?muted:d.key==1?Color.cyan:rust);}
  var pos=G.player.transform.position/World.Cell;Arrow(new Vector2(ox+(pos.x-minX+.5f)*scale,oy-(pos.z-minZ+.5f)*scale),G.player.transform.eulerAngles.y,paper);
  Label(panel.x+22,panel.yMax-27,panel.width-44,24,TouchControls.Active?"WHITE: YOU    CYAN / RED: ACCESS GATES    MAP: CLOSE":"WHITE: YOU    CYAN / RED: ACCESS GATES    TAB: CLOSE",caption,muted);
 }
 static string TimeText(float time)=>$"{(int)time/60:00}:{(int)time%60:00}";
 void Quit(){Application.Quit();
 #if UNITY_EDITOR
 UnityEditor.EditorApplication.isPlaying=false;
 #endif
 }
}
}
