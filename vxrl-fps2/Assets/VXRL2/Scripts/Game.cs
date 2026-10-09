using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
namespace VXRL2 {
public enum Mode { Title, Playing, Paused, Dead, Won }
public sealed class Game : MonoBehaviour {
 public static Game I;
 public GameAssets assets;
 public static int levelIndex;
 public int characterIndex;
 public CharacterDefinition Character => assets.campaign.characters[Mathf.Clamp(characterIndex,0,assets.campaign.characters.Length-1)];
 public LevelDefinition Level => assets.campaign.levels[Mathf.Clamp(levelIndex,0,assets.campaign.levels.Length-1)];
 public Mode mode = Mode.Title;
 public Player player;
 public World world;
 public List<Enemy> enemies = new();
 public List<Pickup> pickups = new();
 public List<Door> doors = new();
 public int kills, secrets, totalEnemies;
 public bool blueKey, redKey, bossDead;
 public float elapsed, hitMarker, damageFlash;
 public string message = "", area = "QUARANTINE GATE";
 public float messageUntil;
 public float sensitivity = 0.12f, volume = .7f, fov = 88;
 public bool bob = true, map, minimap=true;
 public int difficulty = 1;
 public Snapshot checkpoint;
 string SavePath => Path.Combine(Application.persistentDataPath,testing?"checkpoint-qa.json":"checkpoint.json");
 public bool testing;
 AudioSource audioSource;
 public string Objective => bossDead ? Level.exitObjective : redKey ? Level.redObjective : blueKey ? Level.blueObjective : Level.entryObjective;
 static Snapshot pendingContinue;
 string ContentSignature {get {var text=new System.Text.StringBuilder(Level.id);foreach(var e in Level.enemies)text.Append('|').Append(e.definition.id).Append(e.cell);foreach(var p in Level.pickups)text.Append('|').Append(p.type).Append(p.cell).Append(p.weaponIndex);foreach(var d in Level.doors)text.Append('|').Append(d.cell).Append(d.key);foreach(var c in assets.campaign.characters)text.Append('|').Append(c.id);foreach(var w in assets.campaign.weapons)text.Append('|').Append(w.id);return text.ToString();}}

 void Awake() {
  if(Array.Exists(Environment.GetCommandLineArgs(),s=>s=="--ui-gallery"))Application.runInBackground=true;
  I=this; Time.timeScale=1; Application.targetFrameRate=120; QualitySettings.vSyncCount=1;
  sensitivity=PlayerPrefs.GetFloat("sensitivity",.12f); volume=PlayerPrefs.GetFloat("volume",.7f); fov=PlayerPrefs.GetFloat("fov",88); bob=PlayerPrefs.GetInt("bob",1)==1;
  minimap=PlayerPrefs.GetInt("minimap",1)==1;AudioListener.volume=volume;
  audioSource=gameObject.AddComponent<AudioSource>(); audioSource.spatialBlend=0;
  testing=Array.Exists(Environment.GetCommandLineArgs(),s=>s=="--smoke-test");
  characterIndex=Mathf.Clamp(PlayerPrefs.GetInt("characterIndex",0),0,assets.campaign.characters.Length-1);
  world=gameObject.AddComponent<World>(); world.Build(assets,Level);
  player=new GameObject("Player").AddComponent<Player>(); player.Build(assets); player.Teleport(World.Pos(Level.start.x,Level.start.y));
  gameObject.AddComponent<TouchControls>();
  gameObject.AddComponent<GameHUD>();
  gameObject.AddComponent<Ambience>();
  SetMode(Mode.Title);
  if(pendingContinue!=null){checkpoint=pendingContinue;pendingContinue=null;if(checkpoint.signature==ContentSignature)RestoreCheckpoint();else Say("SAVE DOES NOT MATCH UPDATED LEVEL",5);}
  if(testing) gameObject.AddComponent<SmokeTest>();
  if(Array.Exists(Environment.GetCommandLineArgs(),s=>s=="--ui-gallery")){testing=true;gameObject.AddComponent<InterfaceGallery>();}
  if(Array.Exists(Environment.GetCommandLineArgs(),s=>s=="--playtest-tour")){testing=true;gameObject.AddComponent<PlaytestTour>();}
 }
 void Update() {
  var k=Keyboard.current;
  if(k!=null && k.escapeKey.wasPressedThisFrame) { if(mode==Mode.Playing) SetMode(Mode.Paused); else if(mode==Mode.Paused) SetMode(Mode.Playing); }
  if(mode==Mode.Playing) {
   elapsed+=Time.deltaTime;
   if(k!=null && k.tabKey.wasPressedThisFrame) map=!map;
   world.Discover(player.transform.position);
  }
  hitMarker=Mathf.MoveTowards(hitMarker,0,Time.unscaledDeltaTime*4);
  damageFlash=Mathf.MoveTowards(damageFlash,0,Time.unscaledDeltaTime*1.8f);
 }
 public void SelectCharacter(int index) {characterIndex=Mathf.Clamp(index,0,assets.campaign.characters.Length-1);player.ApplyCharacter();if(!testing){PlayerPrefs.SetInt("characterIndex",characterIndex);PlayerPrefs.Save();}}
 public void StartRun() {
  player.ApplyCharacter();SetMode(Mode.Playing); SaveCheckpoint(); Say(Character.displayName+" DEPLOYED / "+Level.entryObjective,5);
 }
 public void SetMode(Mode next) {
  mode=next; bool play=next==Mode.Playing;
  Time.timeScale=play?1:0; Cursor.lockState=play&&!TouchControls.Active?CursorLockMode.Locked:CursorLockMode.None; Cursor.visible=!play||TouchControls.Active;
  if(TouchControls.I)TouchControls.I.Clear();
  if(play) map=false;
 }
 public void Say(string text,float seconds=2.5f) {message=text;messageUntil=Time.unscaledTime+seconds;}
 public void Sound(AudioClip clip,float loudness=1,float pitch=1) { if(!clip)return; audioSource.pitch=pitch; audioSource.PlayOneShot(clip,loudness); }
 public void Kill(Enemy e) { kills++; if(e.definition.role==EnemyRole.Boss) {bossDead=enemies.TrueForAll(x=>x.definition.role!=EnemyRole.Boss||x.dead);Say("WARDEN DESTROYED  /  ACTIVATE THE PURGE TERMINAL",6);} }
 public void Win() { if(Level.requiresBoss && !bossDead) {Say("ROOT LOCK ACTIVE • DESTROY THE WARDEN");return;} SetMode(Mode.Won); }
 public void SaveSettings() { PlayerPrefs.SetFloat("sensitivity",sensitivity);PlayerPrefs.SetFloat("volume",volume);PlayerPrefs.SetFloat("fov",fov);PlayerPrefs.SetInt("bob",bob?1:0);PlayerPrefs.SetInt("minimap",minimap?1:0);PlayerPrefs.Save();AudioListener.volume=volume; }
 public void SaveCheckpoint() {
  checkpoint=new Snapshot {level=levelIndex,character=characterIndex,signature=ContentSignature,difficulty=difficulty,discovered=new List<Vector2Int>(world.discovered),position=player.transform.position,yaw=player.transform.eulerAngles.y,health=player.health,armor=player.armor,ammo=(int[])player.ammo.Clone(),owned=(bool[])player.owned.Clone(),weapon=player.weapon,blue=blueKey,red=redKey,boss=bossDead,kills=kills,secrets=secrets,elapsed=elapsed};
  foreach(var e in enemies)checkpoint.enemies.Add(new EnemyState{position=e.transform.position,health=e.health,dead=e.dead});
  foreach(var p in pickups)checkpoint.pickups.Add(p.taken);
  foreach(var d in doors)checkpoint.doors.Add(d.open);
  try {File.WriteAllText(SavePath,JsonUtility.ToJson(checkpoint));}catch(Exception ex){Debug.LogWarning(ex.Message);}
 }
 public bool HasDiskSave => File.Exists(SavePath);
 public void ContinueDisk() {
  try {var s=JsonUtility.FromJson<Snapshot>(File.ReadAllText(SavePath));
   if(s.version!=3||s.character<0||s.character>=assets.campaign.characters.Length||s.level<0||s.level>=assets.campaign.levels.Length)throw new Exception("Unsupported save version or level.");
   if(s.level!=levelIndex){pendingContinue=s;levelIndex=s.level;NewRun();return;}
   if(s.signature!=ContentSignature||s.enemies.Count!=enemies.Count||s.pickups.Count!=pickups.Count||s.doors.Count!=doors.Count||s.ammo.Length!=assets.campaign.weapons.Length||s.owned.Length!=s.ammo.Length)throw new Exception("Checkpoint does not match this content revision.");checkpoint=s;RestoreCheckpoint();}
  catch(Exception ex){Say("SAVE UNAVAILABLE • START A NEW RUN",5);Debug.LogWarning(ex.Message);}
 }
 public void RestoreCheckpoint() {
  if(checkpoint==null)return; var s=checkpoint;
  foreach(var p in FindObjectsByType<Projectile>())Destroy(p.gameObject);
  foreach(var fx in FindObjectsByType<Transient>())Destroy(fx.gameObject);
  characterIndex=s.character;difficulty=s.difficulty;world.discovered=new HashSet<Vector2Int>(s.discovered);blueKey=s.blue;redKey=s.red;bossDead=s.boss;kills=s.kills;secrets=s.secrets;elapsed=s.elapsed;
  player.health=s.health;player.armor=s.armor;player.ammo=(int[])s.ammo.Clone();player.owned=(bool[])s.owned.Clone();player.Select(s.weapon);player.Teleport(s.position,s.yaw);player.ResetCombat();
  for(int i=0;i<enemies.Count;i++)enemies[i].Restore(s.enemies[i]);
  for(int i=0;i<pickups.Count;i++)pickups[i].Restore(s.pickups[i]);
  for(int i=0;i<doors.Count;i++)doors[i].Restore(s.doors[i]);
  world.ResetCheckpointTrigger(); SetMode(Mode.Playing); Say("CHECKPOINT RESTORED");
 }
 public void NextLevel() {levelIndex++;NewRun();}
 public void NewRun() {Time.timeScale=1;SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);}
 void OnApplicationFocus(bool focus){if(!focus && mode==Mode.Playing && !testing)SetMode(Mode.Paused);}
 void OnDestroy(){Time.timeScale=1;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;}
}
[Serializable] public class Snapshot {
 public int version=3,level,difficulty,character;public string signature;public List<Vector2Int> discovered=new();public Vector3 position;public float yaw,health,armor,elapsed;public int[] ammo;public bool[] owned;public int weapon,kills,secrets;public bool blue,red,boss;
 public List<EnemyState> enemies=new();public List<bool> pickups=new();public List<bool> doors=new();
}
[Serializable] public class EnemyState {public Vector3 position;public float health;public bool dead;}
public sealed class Transient:MonoBehaviour {public float life=.3f; void Update(){life-=Time.deltaTime;if(life<=0)Destroy(gameObject);}}
public sealed class Ambience:MonoBehaviour {
 AudioSource source;
 void Start(){
  int rate=22050;float[] samples=new float[rate*8];
  for(int i=0;i<samples.Length;i++){float t=(float)i/rate;float pulse=.45f+.15f*Mathf.Sin(t*Mathf.PI*2);samples[i]=(.045f*Mathf.Sin(t*55*2*Mathf.PI)+.018f*Mathf.Sin(t*82.5f*2*Mathf.PI)+.012f*Mathf.Sin(t*110*2*Mathf.PI))*pulse;}
  var clip=AudioClip.Create("Original reactor drone",samples.Length,1,rate,false);clip.SetData(samples,0);source=gameObject.AddComponent<AudioSource>();source.clip=clip;source.loop=true;source.volume=.7f;source.Play();
 }
}
}
