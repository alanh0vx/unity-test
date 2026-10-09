using System;
using System.Collections;
using System.IO;
using UnityEngine;
namespace VXRL2 {
// Opt-in visual QA: captures the real UI at several sizes, never alters player saves.
public sealed class InterfaceGallery:MonoBehaviour {
 IEnumerator Start(){
  string output=Path.GetFullPath("Validation/UI");var args=Environment.GetCommandLineArgs();for(int i=0;i<args.Length-1;i++)if(args[i]=="--qa-output")output=args[i+1];Directory.CreateDirectory(output);
  var g=Game.I;foreach(var e in g.enemies)e.enabled=false;
  Screen.SetResolution(1440,900,FullScreenMode.Windowed);yield return new WaitForSecondsRealtime(.7f);
  if(Array.Exists(args,x=>x=="--combat-gallery")){
   for(int character=0;character<2;character++){
    g.SelectCharacter(character);g.StartRun();var p=g.player;p.automation=true;p.enabled=false;p.SelectSlot(1);p.ResetCombat();
    string name=character==0?"tanto":"katana";
    yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,name+"-idle.png"));yield return new WaitForSecondsRealtime(.2f);
    p.Fire();p.AdvanceAttack(p.Current.interval*.35f);p.SendMessage("PresentAttack");yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,name+"-contact.png"));yield return new WaitForSecondsRealtime(.2f);
    p.AdvanceAttack(p.Current.interval*.18f);p.SendMessage("PresentAttack");yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,name+"-follow.png"));yield return new WaitForSecondsRealtime(.2f);
    p.ResetCombat();p.enabled=true;
   }
   Application.Quit();yield break;
  }
  if(Array.Exists(args,x=>x=="--touch-gallery")){
   TouchControls.I.active=true;Screen.SetResolution(896,414,FullScreenMode.Windowed);yield return new WaitForSecondsRealtime(.6f);
   ScreenCapture.CaptureScreenshot(Path.Combine(output,"mobile-menu.png"));yield return new WaitForSecondsRealtime(.3f);
   g.StartRun();g.player.automation=true;yield return new WaitForSecondsRealtime(.3f);ScreenCapture.CaptureScreenshot(Path.Combine(output,"mobile-controls.png"));yield return new WaitForSecondsRealtime(.3f);
   Screen.SetResolution(667,375,FullScreenMode.Windowed);yield return new WaitForSecondsRealtime(.5f);ScreenCapture.CaptureScreenshot(Path.Combine(output,"mobile-small.png"));yield return new WaitForSecondsRealtime(.3f);
   Application.Quit();yield break;
  }
  g.SelectCharacter(0);yield return new WaitForSecondsRealtime(.3f);ScreenCapture.CaptureScreenshot(Path.Combine(output,"01-ninja-selection.png"));yield return new WaitForSecondsRealtime(.3f);
  g.SelectCharacter(1);yield return new WaitForSecondsRealtime(.3f);ScreenCapture.CaptureScreenshot(Path.Combine(output,"02-shogun-selection.png"));yield return new WaitForSecondsRealtime(.3f);
  g.StartRun();g.player.Teleport(World.Pos(15,10));g.player.AimAt(World.Pos(15,14)+Vector3.up*1.6f);yield return new WaitForSecondsRealtime(.4f);ScreenCapture.CaptureScreenshot(Path.Combine(output,"03-shogun-hud.png"));yield return new WaitForSecondsRealtime(.3f);
  g.SelectCharacter(0);yield return new WaitForSecondsRealtime(.3f);ScreenCapture.CaptureScreenshot(Path.Combine(output,"04-ninja-hud.png"));yield return new WaitForSecondsRealtime(.3f);
  g.map=true;yield return new WaitForSecondsRealtime(.3f);ScreenCapture.CaptureScreenshot(Path.Combine(output,"05-sector-map.png"));yield return new WaitForSecondsRealtime(.3f);g.map=false;
  Screen.SetResolution(1280,720,FullScreenMode.Windowed);yield return new WaitForSecondsRealtime(.5f);ScreenCapture.CaptureScreenshot(Path.Combine(output,"06-hud-720p.png"));yield return new WaitForSecondsRealtime(.3f);
  g.SetMode(Mode.Title);yield return new WaitForSecondsRealtime(.3f);ScreenCapture.CaptureScreenshot(Path.Combine(output,"07-menu-720p.png"));yield return new WaitForSecondsRealtime(.3f);
  Screen.SetResolution(1024,768,FullScreenMode.Windowed);yield return new WaitForSecondsRealtime(.5f);ScreenCapture.CaptureScreenshot(Path.Combine(output,"08-menu-4x3.png"));yield return new WaitForSecondsRealtime(.3f);
  File.WriteAllText(Path.Combine(output,"gallery.txt"),"Captured Ninja/Shogun selection, both HUDs, minimap, full map, 1440x900, 1280x720 and 1024x768. Inspect PNGs for visual validation.");Application.Quit();
 }
}
}
