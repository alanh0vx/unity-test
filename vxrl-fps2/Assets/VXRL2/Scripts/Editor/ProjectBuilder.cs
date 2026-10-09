using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace VXRL2.Editor {
public static class ProjectBuilder {
 const string Data="Assets/VXRL2/Data/";
 [MenuItem("VXRL2/Generate First Campaign")]
 public static void Generate(){
  Directory.CreateDirectory(Data);Directory.CreateDirectory("Assets/VXRL2/Prefabs");Directory.CreateDirectory("Assets/VXRL2/Materials");Directory.CreateDirectory("Assets/VXRL2/Scenes");
  var renderer=Asset<UniversalRendererData>(Data+"Renderer.asset");
  var pipeline=Asset<UniversalRenderPipelineAsset>(Data+"Pipeline.asset");
  var so=new SerializedObject(pipeline);so.FindProperty("m_RendererDataList").arraySize=1;so.FindProperty("m_RendererDataList").GetArrayElementAtIndex(0).objectReferenceValue=renderer;so.ApplyModifiedPropertiesWithoutUndo();
  pipeline.renderScale=1;pipeline.msaaSampleCount=2;pipeline.supportsHDR=true;pipeline.shadowDistance=45;pipeline.maxAdditionalLightsCount=4;
  GraphicsSettings.defaultRenderPipeline=pipeline;QualitySettings.renderPipeline=pipeline;QualitySettings.shadows=UnityEngine.ShadowQuality.All;
  PlayerSettings.SplashScreen.show=false;PlayerSettings.companyName="VXRL";PlayerSettings.productName="VXRL-FPS2 ROOTBREACH";PlayerSettings.defaultScreenWidth=1440;PlayerSettings.defaultScreenHeight=900;PlayerSettings.fullScreenMode=FullScreenMode.Windowed;PlayerSettings.runInBackground=true;PlayerSettings.colorSpace=ColorSpace.Linear;PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone,ScriptingImplementation.Mono2x);
  var settings=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);var input=settings.FindProperty("activeInputHandler");if(input!=null){input.intValue=1;settings.ApplyModifiedPropertiesWithoutUndo();}
  var a=Asset<GameAssets>(Data+"GameAssets.asset");
  a.effects=Mat("Effects",Color.white);a.effects.shader=Shader.Find("Universal Render Pipeline/Unlit");EditorUtility.SetDirty(a.effects);
  a.floor=Mat("Deck",new Color(.3f,.34f,.38f),"Assets/SourceArt/Environment/Tileables/Tex_FloorTileable1x1_BaseColor.png",.2f);
  a.wall=Mat("Bulkhead",new Color(.44f,.48f,.52f),"Assets/SourceArt/Environment/Tileables/Tex_WallTileable1x1_BaseColor.png",.1f);
  a.trim=Mat("Graphite",new Color(.1f,.15f,.18f),null,.4f);a.dark=Mat("Ceiling",new Color(.095f,.12f,.14f));a.metal=Mat("Steel",new Color(.23f,.29f,.31f),null,.65f);
  a.cyan=Mat("Cyan",new Color(.04f,.65f,.8f),null,.2f,3);a.amber=Mat("Amber",new Color(1,.4f,.07f),null,.2f,3);a.red=Mat("Red",new Color(.9f,.05f,.025f),null,.2f,3);a.green=Mat("Green",new Color(.12f,.9f,.28f),null,.2f,3);
  a.shot=Clip("Shuriken","Assault Rifle","Shoot");a.blast=Clip("Grenade Launcher","Shoot");a.blade=Clip("TargetHit-001");a.hurt=Clip("PlayerHit");a.death=Clip("PlayerDead");a.pickup=Clip("CountdownBeep");a.step=Clip("Player_Footstep_01");a.portrait=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/SourceArt/Portraits/shogun.png");
  string[] enemyNames={"Virus Scout","Malware Brawler","Worm Burrower","Ransomware Warden","Rootkit Overlord"};string[] models={"Virus_Scout","Malware_Brawler","Worm_Burrower","Ransomware_Warden","Rootkit_Overlord"};
  var enemies=new EnemyDefinition[5];
  for(int i=0;i<5;i++){var e=Asset<EnemyDefinition>(Data+models[i]+".asset");e.id=models[i];e.displayName=enemyNames[i];e.height=new[]{1.6f,2.2f,1.1f,3.2f,3.5f}[i];e.model=Model("Assets/SourceArt/Enemies/"+models[i]+"/"+models[i]+".fbx",models[i],e.height,true);e.role=new[]{EnemyRole.Ranged,EnemyRole.Charger,EnemyRole.Ambusher,EnemyRole.Boss,EnemyRole.Boss}[i];e.health=new[]{45f,110,55,700,1000}[i];e.speed=new[]{3.8f,4.8f,5.5f,2.8f,2.4f}[i];e.damage=new[]{9f,17,12,15,20}[i];e.attackInterval=new[]{1.8f,1.3f,1.1f,2.2f,2f}[i];e.projectileSpeed=i>=3?11:12;e.color=i==0?new Color(.2f,1,.35f):i==1?new Color(1,.3f,.07f):i==2?Color.magenta:new Color(1,.16f,.08f);EditorUtility.SetDirty(e);enemies[i]=e;}
  string[] wn={"KATANA","SHURIKEN CASTER","BREACH SHOTGUN","YARI LAUNCHER","PURGE RIFLE"};string[] paths={"Weapons/Katana/Katana_FPS.fbx","Weapons/Shuriken/Shuriken.fbx","Guns/Shotgun/Geo_shotgun.fbx","Weapons/YariSpearLauncher/Yari_Spear_Launcher.fbx","Guns/AssaultRifle/Geo_assaultRifle.fbx"};
  var weapons=new WeaponDefinition[5];
  for(int i=0;i<5;i++){
   var w=Asset<WeaponDefinition>(Data+"Weapon"+i+".asset");w.id=wn[i].ToLowerInvariant().Replace(' ','_');w.displayName=wn[i];w.model=Model("Assets/SourceArt/"+paths[i],"Weapon"+i,i==0?.9f:.65f,false);w.fireMode=new[]{FireMode.Melee,FireMode.Bolt,FireMode.Scatter,FireMode.Explosive,FireMode.Automatic}[i];w.damage=new[]{45f,24,13,150,16}[i];w.interval=new[]{.4f,.19f,.72f,.9f,.1f}[i];w.pellets=i==2?8:1;w.spread=i==2?4.2f:.7f;w.range=i==0?2.5f:90;w.startAmmo=i==1?90:0;w.maxAmmo=new[]{0,250,80,30,200}[i];w.pickupAmmo=new[]{0,35,12,5,40}[i];w.availableAtStart=i<2;w.projectileSpeed=i==1?48:28;w.color=i==3?new Color(1,.45f,.08f):new Color(.1f,.9f,1);w.fireSound=i==2?Clip("Shotgun","Shoot"):i==3?a.blast:i==0?a.blade:a.shot;w.viewPosition=i==0?new(.32f,-.3f,.58f):new(.24f,-.23f,.55f);w.viewRotation=Vector3.zero;w.viewScale=1;EditorUtility.SetDirty(w);weapons[i]=w;
  }
  var level=Asset<LevelDefinition>(Data+"E1M1_QuarantineGate.asset");level.rooms.Clear();level.enemies.Clear();level.pickups.Clear();level.doors.Clear();level.pillars.Clear();
  Color blue=new(.12f,.8f,1),orange=new(1,.45f,.1f),red=new(1,.1f,.1f);
  Room(level,"ENTRY / 01",13,0,5,5,blue);Room(level,"",14,5,3,3,blue,false);Room(level,"SECURITY HUB",11,8,9,8,blue);
  Room(level,"",8,11,3,3,orange,false);Room(level,"MAINTENANCE",2,9,6,7,orange);Room(level,"",4,16,3,3,orange,false);Room(level,"FOUNDRY / BLUE ACCESS",1,19,8,7,orange);
  Room(level,"",9,22,5,3,orange,false);Room(level,"",14,16,3,13,red,false);
  Room(level,"",20,11,4,3,blue,false);Room(level,"REACTOR RING",24,8,8,12,blue);Room(level,"",27,20,3,3,blue,false);Room(level,"ARCHIVE / RED ACCESS",24,23,8,7,red);
  Room(level,"WARDEN CHAMBER",11,29,10,9,red);Room(level,"HIDDEN CACHE",0,13,2,2,blue);Room(level,"SERVICE CACHE",30,20,2,2,blue);Room(level,"FORGOTTEN CACHE",9,24,2,2,orange);
  level.start=new(15,2);level.exit=new(15,36);level.checkpoint=new(15,25);
  level.doors.Add(new DoorSpawn{cell=new(21,12),alongX=true,key=1,label="BLUE GATE"});level.doors.Add(new DoorSpawn{cell=new(15,27),alongX=false,key=2,label="RED GATE"});
  level.pillars.AddRange(new[]{new Vector2Int(26,11),new(29,11),new(26,16),new(29,16),new(13,32),new(18,32),new(13,35),new(18,35)});
  Spawn(level,enemies[0],15,7);Spawn(level,enemies[0],12,11);Spawn(level,enemies[0],18,13);
  Spawn(level,enemies[0],4,11);Spawn(level,enemies[1],5,14);Spawn(level,enemies[2],3,13);
  Spawn(level,enemies[0],2,21);Spawn(level,enemies[0],7,21);Spawn(level,enemies[1],4,23);Spawn(level,enemies[2],6,24);Spawn(level,enemies[0],8,24);
  Spawn(level,enemies[1],12,23);Spawn(level,enemies[0],15,20);
  Spawn(level,enemies[0],25,10);Spawn(level,enemies[0],30,10);Spawn(level,enemies[1],28,14);Spawn(level,enemies[2],25,16);Spawn(level,enemies[0],30,18);Spawn(level,enemies[0],28,19);
  Spawn(level,enemies[2],28,21);Spawn(level,enemies[2],25,25);Spawn(level,enemies[0],30,25);Spawn(level,enemies[1],28,27);Spawn(level,enemies[0],25,28);
  Spawn(level,enemies[0],12,30);Spawn(level,enemies[0],19,30);Spawn(level,enemies[1],12,34);Spawn(level,enemies[1],19,35);Spawn(level,enemies[3],16,35);
  Pick(level,PickupType.Weapon,5,10,24,2);Pick(level,PickupType.Weapon,25,9,10,3);Pick(level,PickupType.BlueKey,4,24);Pick(level,PickupType.RedKey,29,28);
  foreach(var c in new[]{new Vector2Int(15,4),new(11,9),new(6,15),new(3,22),new(18,14),new(25,18),new(30,27),new(14,25),new(16,25),new(12,31),new(19,34)})Pick(level,PickupType.Health,c.x,c.y,30);
  foreach(var c in new[]{new Vector2Int(16,3),new(3,10),new(7,24),new(12,14),new(30,12),new(25,26),new(15,24)})Pick(level,PickupType.Ammo,c.x,c.y,45,1);
  foreach(var c in new[]{new Vector2Int(6,12),new(2,24),new(18,11),new(29,17),new(26,28),new(14,26),new(12,33),new(19,36)})Pick(level,PickupType.Ammo,c.x,c.y,16,2);
  foreach(var c in new[]{new Vector2Int(25,15),new(30,29),new(16,26),new(18,36)})Pick(level,PickupType.Ammo,c.x,c.y,6,3);
  Pick(level,PickupType.Armor,17,9,40);Pick(level,PickupType.Armor,26,24,40);Pick(level,PickupType.Armor,15,26,75);
  Pick(level,PickupType.Secret,0,14);Pick(level,PickupType.Secret,31,21);Pick(level,PickupType.Secret,10,25);
  EditorUtility.SetDirty(level);
  var campaign=Asset<CampaignDefinition>(Data+"Campaign.asset");campaign.levels=new[]{level};campaign.weapons=weapons;EditorUtility.SetDirty(campaign);a.campaign=campaign;EditorUtility.SetDirty(a);
  var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);var game=new GameObject("VXRL2 / Bootstrap").AddComponent<Game>();game.assets=a;
  EditorSceneManager.SaveScene(scene,"Assets/VXRL2/Scenes/Rootbreach.unity");EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/VXRL2/Scenes/Rootbreach.unity",true)};
  EditorUtility.SetDirty(pipeline);AssetDatabase.SaveAssets();TunePresentation();InterfaceSetup.Configure();CombatFeelSetup.Configure();Debug.Log("VXRL2_GENERATION_OK");
 }
 public static void BuildTuned(){if(!AssetDatabase.LoadAssetAtPath<GameAssets>(Data+"GameAssets.asset"))Generate();else TunePresentation();CombatFeelSetup.Configure();Build();}
 public static void TunePresentation(){
  var first=AssetDatabase.LoadAssetAtPath<LevelDefinition>(Data+"E1M1_QuarantineGate.asset");if(first.signs.Count==0){first.signs.Add(new SignSpawn{cell=new(15,10),text="< MAINTENANCE     /     REACTOR >"});first.signs.Add(new SignSpawn{cell=new(5,15),text="FOUNDRY  /  BLUE ACCESS",color=new Color(1,.6f,.2f)});first.signs.Add(new SignSpawn{cell=new(28,19),text="ARCHIVE  /  RED ACCESS"});first.signs.Add(new SignSpawn{cell=new(15,25),text="WARNING  /  WARDEN CONTAINMENT",color=Color.red});EditorUtility.SetDirty(first);}
  if(!first.doors.Any(x=>x.secret)){first.doors.Add(new DoorSpawn{cell=new(1,14),alongX=true,key=0,secret=true,label="CACHE PANEL"});first.doors.Add(new DoorSpawn{cell=new(30,21),alongX=true,key=0,secret=true,label="CACHE PANEL"});first.doors.Add(new DoorSpawn{cell=new(9,25),alongX=true,key=0,secret=true,label="CACHE PANEL"});EditorUtility.SetDirty(first);}
  var a=AssetDatabase.LoadAssetAtPath<GameAssets>(Data+"GameAssets.asset");a.worldTextShader=Shader.Find("VXRL2/WorldText");a.shot=Clip("Rifle_Shoot-001");a.blast=Clip("Grenade Launcher-001");EditorUtility.SetDirty(a);
  var gun=AssetDatabase.LoadAssetAtPath<WeaponDefinition>(Data+"Weapon2.asset");gun.fireSound=Clip("Shotgun_Shoot-001");EditorUtility.SetDirty(gun);
  foreach(int n in new[]{1,4}){var w=AssetDatabase.LoadAssetAtPath<WeaponDefinition>(Data+"Weapon"+n+".asset");w.fireSound=a.shot;EditorUtility.SetDirty(w);}
  foreach(var path in AssetDatabase.FindAssets("t:Material",new[]{"Assets/VXRL2/Materials"}).Select(AssetDatabase.GUIDToAssetPath)){
   var mat=AssetDatabase.LoadAssetAtPath<Material>(path);string tex=null;
   if(mat.name.StartsWith("Weapon2"))tex="Assets/SourceArt/Guns/Shotgun/Tex_Shotgun_BaseColor.png";
   if(mat.name.StartsWith("Weapon4"))tex="Assets/SourceArt/Guns/AssaultRifle/Tex_AssaultRifle_BaseColor.png";
   if(tex!=null){mat.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(tex));mat.color=Color.white;EditorUtility.SetDirty(mat);}
  }
  var floor=a.floor;floor.color=new Color(.5f,.56f,.59f);floor.mainTextureScale=new Vector2(2,2);floor.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/SourceArt/Environment/Tileables/Tex_FloorTileable1x1_Normal.png"));floor.EnableKeyword("_NORMALMAP");EditorUtility.SetDirty(floor);
  a.wall.color=new Color(.52f,.58f,.62f);a.wall.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/SourceArt/Environment/Tileables/Tex_WallTileable1x1_Normal.png"));a.wall.EnableKeyword("_NORMALMAP");EditorUtility.SetDirty(a.wall);

  var w0=AssetDatabase.LoadAssetAtPath<WeaponDefinition>(Data+"Weapon0.asset");w0.viewRotation=new Vector3(0,0,-25);w0.viewPosition=new Vector3(.38f,-.28f,.65f);EditorUtility.SetDirty(w0);
  var w1=AssetDatabase.LoadAssetAtPath<WeaponDefinition>(Data+"Weapon1.asset");w1.viewScale=.43f;w1.viewRotation=new Vector3(12,15,0);EditorUtility.SetDirty(w1);
  foreach(int i in new[]{2,4}){var w=AssetDatabase.LoadAssetAtPath<WeaponDefinition>(Data+"Weapon"+i+".asset");w.viewRotation=new Vector3(0,-90,0);w.viewPosition=new Vector3(.2f,-.21f,.52f);EditorUtility.SetDirty(w);}
  var w3=AssetDatabase.LoadAssetAtPath<WeaponDefinition>(Data+"Weapon3.asset");w3.viewRotation=new Vector3(90,0,0);EditorUtility.SetDirty(w3);AssetDatabase.SaveAssets();
 }
 [MenuItem("VXRL2/Build WebGL")]
 public static void BuildWebGL(){
  PlayerSettings.SplashScreen.show=false;if(!AssetDatabase.LoadAssetAtPath<GameAssets>(Data+"GameAssets.asset"))Generate();ValidateContent();
  // Gzip + decompression fallback so the build runs on any static host without custom Content-Encoding headers.
  PlayerSettings.WebGL.template="PROJECT:Rootbreach";
  // Preserve the working runtime profile; hashed assets prevent mixed cached releases.
  PlayerSettings.WebGL.wasm2023=true;PlayerSettings.WebGL.threadsSupport=false;
  PlayerSettings.WebGL.nameFilesAsHashes=false;PlayerSettings.WebGL.debugSymbolMode=WebGLDebugSymbolMode.Off;
  PlayerSettings.WebGL.initialMemorySize=32;PlayerSettings.WebGL.maximumMemorySize=2048;
  PlayerSettings.WebGL.memoryGrowthMode=WebGLMemoryGrowthMode.Geometric;
  PlayerSettings.WebGL.compressionFormat=WebGLCompressionFormat.Gzip;PlayerSettings.WebGL.decompressionFallback=true;PlayerSettings.WebGL.dataCaching=true;PlayerSettings.runInBackground=false;
  // Start clean: hashed file names would otherwise leave every previous release behind.
  if(Directory.Exists("Builds/WebGL-Circular"))Directory.Delete("Builds/WebGL-Circular",true);
  var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/VXRL2/Scenes/Rootbreach.unity"},locationPathName="Builds/WebGL-Circular",target=BuildTarget.WebGL,options=BuildOptions.None});
  if(report.summary.result!=BuildResult.Succeeded)throw new Exception("WebGL build failed: "+report.summary.result);VersionWebAssets("Builds/WebGL-Circular");Debug.Log("VXRL2_WEBGL_BUILD_OK "+report.summary.totalSize);
 }
 static void VersionWebAssets(string root){
  string index=Path.Combine(root,"index.html"),html=File.ReadAllText(index);
  foreach(string path in Directory.GetFiles(Path.Combine(root,"Build"))){
   string name=Path.GetFileName(path);if(!html.Contains(name))continue;
   using var sha=System.Security.Cryptography.SHA256.Create();string hash=BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-","").ToLowerInvariant().Substring(0,16);
   string versioned=hash+name.Substring(name.IndexOf('.'));string target=Path.Combine(root,"Build",versioned);
   if(path!=target){File.Copy(path,target,true);File.Delete(path);html=html.Replace(name,versioned);}
  }
  File.WriteAllText(index,html);
 }
 public static void Build(){PlayerSettings.SplashScreen.show=false;if(!AssetDatabase.LoadAssetAtPath<GameAssets>(Data+"GameAssets.asset"))Generate();ValidateContent();var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/VXRL2/Scenes/Rootbreach.unity"},locationPathName="Builds/macOS/VXRL-FPS2.app",target=BuildTarget.StandaloneOSX,options=BuildOptions.None});if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Build failed: "+report.summary.result);Debug.Log("VXRL2_BUILD_OK "+report.summary.totalSize);}
 [MenuItem("VXRL2/Validate Campaign")]
 public static void ValidateContent(){
  var assets=AssetDatabase.LoadAssetAtPath<GameAssets>(Data+"GameAssets.asset");if(!assets||!assets.campaign)throw new Exception("Missing campaign");
  var campaign=assets.campaign;if(campaign.characters==null||campaign.characters.Length==0)throw new Exception("Campaign needs a character definition");foreach(var c in campaign.characters){if(!c||!c.portrait||c.startingWeapons==null||c.startingWeapons.Length<2||c.inventoryOrder==null)throw new Exception("Invalid character definition");foreach(var grant in c.startingWeapons)if(!campaign.weapons.Contains(grant.weapon)||!c.inventoryOrder.Contains(grant.weapon))throw new Exception("Starting weapon missing from campaign or character inventory");}
  if(campaign.weapons.Length<2||campaign.levels.Length==0)throw new Exception("Campaign needs weapons and a level");
  foreach(var w in campaign.weapons)if(!w||!w.model||w.interval<=0||w.damage<=0)throw new Exception("Invalid weapon definition");
  foreach(var l in campaign.levels){if(!l||l.rooms.Count==0)throw new Exception("Empty level");var cells=new System.Collections.Generic.HashSet<Vector2Int>();foreach(var r in l.rooms)for(int x=r.cells.xMin;x<r.cells.xMax;x++)for(int z=r.cells.yMin;z<r.cells.yMax;z++)cells.Add(new(x,z));
   if(!cells.Contains(l.start)||!cells.Contains(l.exit))throw new Exception("Start/exit outside map");
   foreach(var e in l.enemies)if(!e.definition||!e.definition.model||!cells.Contains(e.cell))throw new Exception("Invalid enemy spawn");
   foreach(var p in l.pickups)if(!cells.Contains(p.cell)||((p.type==PickupType.Weapon||p.type==PickupType.Ammo)&&(p.weaponIndex<0||p.weaponIndex>=campaign.weapons.Length)))throw new Exception("Invalid pickup");
   if(l.requiresBoss&&!l.enemies.Any(e=>e.definition.role==EnemyRole.Boss))throw new Exception("Boss-required level needs a boss");
  }
  Debug.Log("VXRL2_CONTENT_VALIDATION_OK");
 }
 static T Asset<T>(string path)where T:ScriptableObject{var a=AssetDatabase.LoadAssetAtPath<T>(path);if(!a){a=ScriptableObject.CreateInstance<T>();AssetDatabase.CreateAsset(a,path);}return a;}
 static Material Mat(string name,Color color,string texture=null,float metal=0,float emission=0){var path="Assets/VXRL2/Materials/"+name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);if(!mat){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,path);}mat.color=color;mat.SetFloat("_Metallic",metal);mat.SetFloat("_Smoothness",.25f);if(texture!=null)mat.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(texture));if(emission>0){mat.EnableKeyword("_EMISSION");mat.SetColor("_EmissionColor",color*emission);}EditorUtility.SetDirty(mat);return mat;}
 static GameObject Model(string path,string name,float size,bool ground){
  var source=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(!source)throw new Exception("Missing model: "+path);var root=new GameObject(name);var visual=UnityEngine.Object.Instantiate(source,root.transform);visual.name="Mesh";
  foreach(var c in visual.GetComponentsInChildren<Collider>())UnityEngine.Object.DestroyImmediate(c);
  var rs=visual.GetComponentsInChildren<Renderer>();Bounds bounds=rs[0].bounds;foreach(var r in rs)bounds.Encapsulate(r.bounds);
  Debug.Log($"MODEL_BOUNDS {name} {bounds.size} center={bounds.center}");
  float factor=size/(ground?bounds.size.y:Mathf.Max(bounds.size.x,bounds.size.y,bounds.size.z));visual.transform.localScale*=factor;
  bounds=rs[0].bounds;foreach(var r in rs)bounds.Encapsulate(r.bounds);visual.transform.position-=ground?new Vector3(bounds.center.x,bounds.min.y,bounds.center.z):bounds.center;
  foreach(var r in rs){var mats=r.sharedMaterials;for(int i=0;i<mats.Length;i++){
   var original=mats[i];if(!original)continue;string matName=name+"_"+original.name.Replace('/','_');string matPath="Assets/VXRL2/Materials/"+matName+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(matPath);
   if(!mat){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));mat.color=original.HasProperty("_BaseColor")?original.GetColor("_BaseColor"):original.HasProperty("_Color")?original.color:Color.gray;var tex=original.HasProperty("_BaseMap")?original.GetTexture("_BaseMap"):original.mainTexture;if(tex)mat.SetTexture("_BaseMap",tex);mat.SetFloat("_Metallic",.45f);mat.SetFloat("_Smoothness",.35f);mat.EnableKeyword("_EMISSION");if(original.HasProperty("_EmissionColor"))mat.SetColor("_EmissionColor",original.GetColor("_EmissionColor"));AssetDatabase.CreateAsset(mat,matPath);}mats[i]=mat;
  }r.sharedMaterials=mats;}
  var prefab=PrefabUtility.SaveAsPrefabAsset(root,"Assets/VXRL2/Prefabs/"+name+".prefab");UnityEngine.Object.DestroyImmediate(root);return prefab;
 }
 static AudioClip Clip(params string[] words){var paths=AssetDatabase.FindAssets("t:AudioClip",new[]{"Assets/SourceAudio"}).Select(AssetDatabase.GUIDToAssetPath).ToArray();foreach(var word in words){var p=paths.FirstOrDefault(p=>p.IndexOf(word,StringComparison.OrdinalIgnoreCase)>=0);if(p!=null)return AssetDatabase.LoadAssetAtPath<AudioClip>(p);}return null;}
 static void Room(LevelDefinition l,string name,int x,int z,int w,int h,Color c,bool decorate=true){l.rooms.Add(new RoomDefinition{name=name,cells=new RectInt(x,z,w,h),accent=c,decorate=decorate});}
 static void Spawn(LevelDefinition l,EnemyDefinition d,int x,int z){l.enemies.Add(new EnemySpawn{definition=d,cell=new(x,z)});}
 static void Pick(LevelDefinition l,PickupType type,int x,int z,int amount=25,int weapon=1){l.pickups.Add(new PickupSpawn{type=type,cell=new(x,z),amount=amount,weaponIndex=weapon});}
}
}
