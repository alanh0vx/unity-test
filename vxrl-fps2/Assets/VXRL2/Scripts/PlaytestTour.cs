using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.AI;
namespace VXRL2 {
// Explicit command-line QA only. Runs ordinary navigation and weapon damage, with
// recorded health/ammo assistance so it is never mistaken for a balance playtest.
public sealed class PlaytestTour:MonoBehaviour {
 string output;List<float> frames=new();List<string> notes=new();int assists;float started;bool running;
 IEnumerator Start(){
  var args=Environment.GetCommandLineArgs();output=Path.GetFullPath("Validation");for(int i=0;i<args.Length-1;i++)if(args[i]=="--qa-output")output=args[i+1];Directory.CreateDirectory(output);
  yield return new WaitForSecondsRealtime(2);ScreenCapture.CaptureScreenshot(Path.Combine(output,"01-title.png"));yield return new WaitForSecondsRealtime(.5f);
  var g=Game.I;g.SelectCharacter(Array.Exists(args,x=>x=="--qa-shogun")?1:0);g.StartRun();g.testing=true;g.player.automation=true;g.difficulty=0;started=Time.realtimeSinceStartup;running=true;
  ScreenCapture.CaptureScreenshot(Path.Combine(output,"02-entry.png"));
  var route=new[]{new Vector2Int(15,7),new(15,11),new(5,11),new(5,10),new(5,14),new(5,18),new(4,24),new(12,23),new(15,20),new(15,12),new(20,12),new(24,12),new(25,9),new(28,18),new(28,21),new(29,28),new(28,21),new(28,12),new(22,12),new(15,12),new(15,25),new(15,29),new(15,34),new(15,36)};
  int waypoint=0;var path=new NavMeshPath();float nextPath=0;int corner=1;float timeout=Time.realtimeSinceStartup+300;
  while(waypoint<route.Length && Time.realtimeSinceStartup<timeout){
   if(g.mode==Mode.Paused)g.SetMode(Mode.Playing);if(g.mode==Mode.Dead){notes.Add("Unexpected death");break;}
   if(g.player.health<60){g.player.health=100;assists++;}
   foreach(var d in g.doors)if(!d.open&&d.Authorized&&Vector3.Distance(g.player.transform.position,d.closedPosition)<10)d.Use();
   Enemy target=null;float nearest=26;
   foreach(var e in g.enemies){if(e.dead)continue;float distance=Vector3.Distance(e.transform.position,g.player.transform.position);if(distance<nearest&&!Physics.Linecast(g.player.cam.transform.position,e.transform.position+Vector3.up*e.definition.height*.6f,1<<0)){target=e;nearest=distance;}}
   g.player.autoFire=target;
   if(target){
    int w=g.player.owned[2]&&nearest<17?2:g.player.StartingRangedIndex;g.player.SelectIfChanged(w);if(g.player.ammo[w]<5){g.player.ammo[w]+=40;assists++;}g.player.AimAt(target.transform.position+Vector3.up*target.definition.height*.55f);g.player.autoMove=Vector3.zero;
   }else{
    Vector3 dest=World.Pos(route[waypoint]);if(Vector3.Distance(g.player.transform.position,dest)<1.2f){notes.Add("Reached "+route[waypoint]+" at "+Mathf.Round(Time.realtimeSinceStartup-started)+"s");waypoint++;nextPath=0;if(waypoint==7||waypoint==14||waypoint==22)ScreenCapture.CaptureScreenshot(Path.Combine(output,"tour-"+waypoint+".png"));yield return null;continue;}
    if(Time.time>=nextPath){NavMesh.CalculatePath(g.player.transform.position,dest,NavMesh.AllAreas,path);corner=1;nextPath=Time.time+.5f;}
    Vector3 next=path.corners.Length>1?path.corners[Mathf.Min(corner,path.corners.Length-1)]:dest;
    if(Vector3.Distance(g.player.transform.position,next)<.8f && corner<path.corners.Length-1)corner++;
    var direction=next-g.player.transform.position;direction.y=0;g.player.autoMove=direction.normalized;g.player.AimAt(g.player.cam.transform.position+direction.normalized*8);
   }
   yield return null;
  }
  g.player.autoMove=Vector3.zero;g.player.autoFire=false;
  if(waypoint==route.Length){g.Win();notes.Add("Completion mode: "+g.mode);}else notes.Add("INCOMPLETE: waypoint "+waypoint);
  ScreenCapture.CaptureScreenshot(Path.Combine(output,"99-results.png"));running=false;frames.Sort();float sum=0;foreach(float f in frames)sum+=f;
  notes.Add("Frames: "+frames.Count+", average fps: "+(frames.Count/Mathf.Max(.001f,sum)).ToString("F1")+", p95 frame ms: "+(frames.Count>0?frames[(int)(frames.Count*.95f)]*1000:0).ToString("F2"));
  notes.Add("Health/ammo assistance count: "+assists+". This is a functional traversal test, not an unassisted difficulty/balance test.");notes.Add(SystemInfo.processorType+" / "+SystemInfo.graphicsDeviceName+" / "+Screen.width+"x"+Screen.height);File.WriteAllLines(Path.Combine(output,"playtest-tour.txt"),notes);
  yield return new WaitForSecondsRealtime(1);Application.Quit(waypoint==route.Length&&g.mode==Mode.Won?0:1);
 }
 void Update(){if(running)frames.Add(Time.unscaledDeltaTime);}
}
}
