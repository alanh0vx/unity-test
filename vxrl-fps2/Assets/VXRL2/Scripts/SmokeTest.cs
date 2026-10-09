using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.AI;
namespace VXRL2 {
public sealed class SmokeTest:MonoBehaviour {
 List<string> checks=new();int failures;
 void Check(bool pass,string label){checks.Add((pass?"PASS ":"FAIL ")+label);if(!pass)failures++;Debug.Log("QA "+checks[^1]);}
 IEnumerator Start(){
  yield return null;var g=Game.I;g.SelectCharacter(0);var p=g.player;
  Check(Application.isMobilePlatform||!TouchControls.Active,"desktop retains mouse input without an actual touch");
  Check(p.owned[1]&&p.owned[5]&&!p.owned[0]&&!p.owned[3],"Ninja owns Shuriken and Tanto only");
  p.SelectSlot(0);Check(p.weapon==1,"Ninja key 1 selects Shuriken");p.SelectSlot(1);Check(p.weapon==5,"Ninja key 2 selects Tanto");
  g.SelectCharacter(1);Check(p.owned[3]&&p.owned[0]&&!p.owned[1]&&!p.owned[5],"Shogun owns Yari and Katana only");p.SelectSlot(0);Check(p.weapon==3,"Shogun key 1 selects Yari");p.SelectSlot(1);Check(p.weapon==0,"Shogun key 2 selects Katana");
  var supply=g.pickups.Find(x=>x.type==PickupType.Ammo&&x.weaponIndex==1);Check(supply.EffectiveWeaponIndex==3,"class-specific ammo adapts to Shogun");
  Check(g.assets.displayFont&&g.assets.bodyFont,"display and HUD fonts assigned");Check(Mathf.Abs((float)g.Character.portrait.width/g.Character.portrait.height-952f/744)<.001f,"Shogun portrait retains original aspect ratio");
  g.SelectCharacter(0);Check(Mathf.Abs((float)g.Character.portrait.width/g.Character.portrait.height-736f/678)<.001f,"Ninja portrait retains original aspect ratio");g.StartRun();foreach(var enemy in g.enemies)enemy.enabled=false;yield return new WaitForSeconds(.6f);
  Check(g.world.cells.Count>200,"authored map generated");Check(g.enemies.Count>=25,"enemy roster spawned");
  Check(p.Current.model!=null&&p.cam!=null,"player/viewmodel loaded");
  foreach(var e in g.enemies)Check(e.agent.isOnNavMesh,e.definition.id+" on navigation mesh");
  var gate=g.doors[0];gate.Use();Check(!gate.open,"blue gate refuses player without key");
  var path=new NavMeshPath();bool route=NavMesh.CalculatePath(World.Pos(g.Level.start),World.Pos(4,24),NavMesh.AllAreas,path);Check(route&&path.status==NavMeshPathStatus.PathComplete,"entry to foundry route is connected");
  g.blueKey=true;gate.Use();Check(gate.open,"blue gate accepts key");gate.Restore(true);yield return new WaitForSeconds(.3f);
  route=NavMesh.CalculatePath(World.Pos(15,12),World.Pos(29,28),NavMesh.AllAreas,path);Check(route&&path.status==NavMeshPathStatus.PathComplete,"hub to archive route after gate opens");
  p.health=100;p.armor=50;p.TakeDamage(20);Check(Mathf.Approximately(p.health,90)&&Mathf.Approximately(p.armor,40),"armor absorbs half of incoming damage");
  p.ammo[1]=10;p.Select(1);bool fired=p.Fire();Check(fired&&p.ammo[1]==9,"weapon shot consumes one ammo");
  p.ammo[1]=0;p.ResetCombat();Check(!p.Fire(),"empty weapon does not fire");
  var ammoPickup=g.pickups.Find(x=>x.type==PickupType.Ammo&&x.weaponIndex==1);p.ammo[1]=p.Definitions[1].maxAmmo;Check(!ammoPickup.Take()&&!ammoPickup.taken,"full ammo preserves pickup");
  p.ammo[1]=0;Check(ammoPickup.Take()&&p.ammo[1]>0,"ammo pickup grants ammunition");
  var victim=g.enemies[0];var previous=new EnemyState{position=victim.transform.position,health=victim.health,dead=false};victim.agent.enabled=false;victim.enabled=false;victim.transform.position=new Vector3(0,0,-28);
  var barrier=GameObject.CreatePrimitive(PrimitiveType.Cube);barrier.transform.position=new Vector3(0,1.5f,-30);barrier.transform.localScale=new Vector3(4,3,.15f);p.Teleport(new Vector3(0,0,-30.42f));p.AimAt(victim.transform.position+Vector3.up);p.ammo[1]=10;p.Select(1);p.ResetCombat();Physics.SyncTransforms();p.Fire();yield return new WaitForSeconds(.18f);Check(victim.health==previous.health,"point-blank projectile cannot cross a thin wall");
  barrier.transform.position+=Vector3.right*8;Physics.SyncTransforms();p.ResetCombat();p.Fire();yield return new WaitForSeconds(.18f);Check(victim.health<previous.health,"unobstructed projectile applies damage");Destroy(barrier);victim.Restore(previous);victim.enabled=true;p.Teleport(World.Pos(g.Level.start));
  // Isolate blade timing from frame/input updates, using the same runtime contact method.
  p.enabled=false;victim.enabled=false;victim.agent.enabled=false;victim.transform.position=new Vector3(0,0,-28);victim.health=500;
  p.Teleport(new Vector3(0,0,-30));p.AimAt(victim.transform.position+Vector3.up);p.Select(5);p.ResetCombat();Physics.SyncTransforms();
  Check(p.Current.fireSound&&p.Current.fireSound.name=="TantoSlash"&&p.Current.cutSound,"Tanto has dedicated slash and cut audio");
  Check(p.Definitions[1].fireSound.name=="ShurikenThrow"&&p.Definitions[1].throwingMotion,"Shuriken uses throwing Foley and motion");
  Check(p.Definitions[0].fireSound.name=="KatanaChop"&&p.Definitions[0].cutRotation.z>90,"Katana has chopping sound and broad stroke");
  p.Fire();Check(victim.health==500,"blade windup does not deal instant damage");
  p.AdvanceAttack(.1f);Check(victim.health==466,"blade contact applies configured damage");p.AdvanceAttack(.1f);Check(victim.health==466,"one hit per cutting stroke");
  p.ResetCombat();p.Fire();p.Select(1);p.AdvanceAttack(.2f);Check(victim.health==466,"switching weapon cancels pending blade hit");
  p.Select(5);p.ResetCombat();p.Fire();g.mode=Mode.Paused;p.AdvanceAttack(.2f);Check(victim.health==466,"paused blade does not hit");g.mode=Mode.Playing;p.ResetCombat();
  var bladeWall=GameObject.CreatePrimitive(PrimitiveType.Cube);bladeWall.transform.position=new Vector3(0,1,-29);bladeWall.transform.localScale=new Vector3(4,3,.1f);Physics.SyncTransforms();p.Fire();p.AdvanceAttack(.15f);Check(victim.health==466,"blade cannot cut through wall");Destroy(bladeWall);
  victim.Restore(previous);victim.enabled=true;p.enabled=true;p.ResetCombat();p.Select(1);p.Teleport(World.Pos(g.Level.start));
  victim.Damage(999,victim.transform.position);Check(victim.dead&&g.kills==1,"enemy death counted once");victim.Damage(999,victim.transform.position);Check(g.kills==1,"repeat damage cannot double count kill");
  g.SaveCheckpoint();float health=p.health;int ammo=p.ammo[1];g.redKey=true;p.health=2;p.ammo[1]=1;g.characterIndex=1;g.checkpoint=null;g.ContinueDisk();Check(p.health==health&&p.ammo[1]==ammo&&!g.redKey,"disk checkpoint restores player inventory and keys");Check(g.characterIndex==0&&p.owned[5],"checkpoint restores selected character and class loadout");Check(victim.dead&&ammoPickup.taken,"checkpoint restores enemy and pickup state");
  g.redKey=true;g.doors[1].Use();g.doors[1].Restore(true);yield return new WaitForSeconds(.3f);
  route=NavMesh.CalculatePath(World.Pos(15,25),World.Pos(g.Level.exit),NavMesh.AllAreas,path);Check(route&&path.status==NavMeshPathStatus.PathComplete,"boss gate to terminal route is connected");
  foreach(var d in g.doors)if(d.secret){d.Use();d.Restore(true);}yield return new WaitForSeconds(.3f);
  foreach(var cache in g.pickups)if(cache.type==PickupType.Secret){route=NavMesh.CalculatePath(World.Pos(g.Level.start),cache.transform.position,NavMesh.AllAreas,path);Check(route&&path.status==NavMeshPathStatus.PathComplete,"hidden cache reachable after opening panel");}
  yield return TouchControlQA.Run(Check);
  yield return new WaitForSeconds(.6f);p.TakeDamage(10000);Check(g.mode==Mode.Dead,"lethal damage opens death menu");g.RestoreCheckpoint();Check(g.mode==Mode.Playing&&p.health==health,"death restart restores checkpoint");
  g.Win();Check(g.mode==Mode.Playing,"exit refuses active boss");foreach(var e in g.enemies)if(e.definition.role==EnemyRole.Boss)e.Damage(10000,e.transform.position);g.Win();Check(g.mode==Mode.Won,"boss death enables level completion");
  string dir=Path.Combine(Application.dataPath,"../../../../Validation");var args=Environment.GetCommandLineArgs();for(int i=0;i<args.Length-1;i++)if(args[i]=="--qa-output")dir=args[i+1];Directory.CreateDirectory(dir);File.WriteAllLines(Path.Combine(dir,"smoke-test.txt"),checks);Debug.Log("VXRL2_SMOKE_"+(failures==0?"PASS":"FAIL")+" "+checks.Count+" checks");Application.Quit(failures==0?0:1);
 }
}
}
