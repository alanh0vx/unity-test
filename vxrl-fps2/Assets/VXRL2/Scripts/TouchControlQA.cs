using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
namespace VXRL2 {
public static class TouchControlQA {
 static void Touch(Touchscreen screen,int id,Vector2 gui,UnityEngine.InputSystem.TouchPhase phase){InputSystem.QueueStateEvent(screen,new TouchState{touchId=id,position=new Vector2(gui.x,Screen.height-gui.y),phase=phase});}
 public static IEnumerator Run(Action<bool,string> check){
  var g=Game.I;var p=g.player;var controls=TouchControls.I;var screen=InputSystem.AddDevice<Touchscreen>();controls.active=true;p.automation=false;
  foreach(var e in g.enemies)e.enabled=false;p.Teleport(World.Pos(g.Level.start));p.Select(1);p.ammo[1]=50;p.ResetCombat();
  var move=controls.MoveArea.center;var fire=controls.FireArea.center;var aim=new Vector2(Screen.width*.6f,Screen.height*.4f);
  Touch(screen,101,move,UnityEngine.InputSystem.TouchPhase.Began);Touch(screen,102,aim,UnityEngine.InputSystem.TouchPhase.Began);yield return null;yield return null;
  Touch(screen,101,move-Vector2.up*35,UnityEngine.InputSystem.TouchPhase.Moved);Touch(screen,102,aim+Vector2.right*45,UnityEngine.InputSystem.TouchPhase.Moved);Touch(screen,103,fire,UnityEngine.InputSystem.TouchPhase.Began);
  var before=p.transform.position;float yaw=p.transform.eulerAngles.y;int ammo=p.ammo[1];yield return null;yield return new WaitForSeconds(.1f);
  check(controls.move.y>.2f&&Vector3.Distance(before,p.transform.position)>.05f,"touch joystick moves player without keyboard input");
  check(Mathf.Abs(Mathf.DeltaAngle(yaw,p.transform.eulerAngles.y))>1,"independent touch drag turns camera");check(p.ammo[1]<ammo&&controls.firing,"third finger fires while moving and aiming");
  Touch(screen,101,move,UnityEngine.InputSystem.TouchPhase.Ended);Touch(screen,102,aim,UnityEngine.InputSystem.TouchPhase.Canceled);Touch(screen,103,fire,UnityEngine.InputSystem.TouchPhase.Canceled);yield return null;yield return null;
  check(controls.move==Vector2.zero&&!controls.firing,"release and cancellation clear held mobile controls");
  Touch(screen,104,controls.WeaponArea.center,UnityEngine.InputSystem.TouchPhase.Began);yield return null;yield return null;check(p.weapon==5,"touch weapon button cycles to owned blade");Touch(screen,104,controls.WeaponArea.center,UnityEngine.InputSystem.TouchPhase.Ended);yield return null;
  p.Select(1);p.ResetCombat();ammo=p.ammo[1];Touch(screen,105,aim,UnityEngine.InputSystem.TouchPhase.Began);yield return null;yield return null;Touch(screen,105,aim,UnityEngine.InputSystem.TouchPhase.Ended);yield return null;yield return null;check(p.ammo[1]==ammo-1,"short aim-area tap fires one shot");
  Touch(screen,106,controls.PauseArea.center,UnityEngine.InputSystem.TouchPhase.Began);yield return null;yield return null;check(g.mode==Mode.Paused&&controls.move==Vector2.zero&&!controls.firing,"touch pause releases gameplay inputs");
  Touch(screen,106,controls.PauseArea.center,UnityEngine.InputSystem.TouchPhase.Ended);yield return null;InputSystem.RemoveDevice(screen);controls.Clear();controls.active=Application.isMobilePlatform;g.SetMode(Mode.Playing);p.ResetCombat();
 }
}
}
