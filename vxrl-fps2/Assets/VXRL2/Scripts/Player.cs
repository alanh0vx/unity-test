using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
namespace VXRL2 {
public sealed class Player:MonoBehaviour {
 public bool automation,autoFire;public Vector3 autoMove;
 public Camera cam;public CharacterController controller;public float health=100,armor=25;
 public int[] ammo; public bool[] owned;public int weapon=1;
 public WeaponDefinition[] Definitions=>Game.I.assets.campaign.weapons;
 public WeaponDefinition Current=>Definitions[weapon];
 public string prompt=""; public Interactable target;
 public Vector3 velocity;float pitch,cooldown,recoil,stepTimer,bobClock,invulnerable;
 float attackTime=-1;bool contactPending;WeaponDefinition attacking;AudioSource weaponAudio;
 Transform weaponRoot;GameObject visual;Light muzzle;GameAssets assets;
 public void Build(GameAssets a){
  assets=a;weaponAudio=gameObject.AddComponent<AudioSource>();weaponAudio.playOnAwake=false;weaponAudio.spatialBlend=0;gameObject.layer=9;controller=gameObject.AddComponent<CharacterController>();controller.height=1.8f;controller.radius=.32f;controller.center=new(0,.9f,0);controller.stepOffset=.35f;controller.slopeLimit=48;controller.skinWidth=.025f;
  var cameraGO=new GameObject("Player camera");cameraGO.transform.SetParent(transform,false);cameraGO.transform.localPosition=new(0,1.6f,0);cam=cameraGO.AddComponent<Camera>();cam.tag="MainCamera";cam.nearClipPlane=.05f;cam.farClipPlane=220;cam.fieldOfView=Game.I.fov;cam.backgroundColor=new(.02f,.03f,.045f);cam.clearFlags=CameraClearFlags.SolidColor;cam.cullingMask=~(1<<8);cameraGO.AddComponent<AudioListener>();
  var viewGO=new GameObject("Viewmodel camera");viewGO.transform.SetParent(cam.transform,false);var view=viewGO.AddComponent<Camera>();view.cullingMask=1<<8;view.nearClipPlane=.01f;view.farClipPlane=3;view.fieldOfView=65;view.GetUniversalAdditionalCameraData().renderType=CameraRenderType.Overlay;cam.GetUniversalAdditionalCameraData().cameraStack.Add(view);view.GetUniversalAdditionalCameraData().renderPostProcessing=true;
  weaponRoot=new GameObject("Viewmodel").transform;weaponRoot.SetParent(cam.transform,false);
  muzzle=World.LightAt(Vector3.zero,new Color(1,.6f,.2f),0,5);muzzle.transform.SetParent(cam.transform,false);muzzle.transform.localPosition=new(.3f,-.1f,1);
  var lamp=World.LightAt(Vector3.zero,new Color(.75f,.88f,1),.8f,9);lamp.transform.SetParent(cam.transform,false);lamp.transform.localPosition=new(0,0,.5f);
  ApplyCharacter();
 }
 public void ApplyCharacter(){
  ammo=new int[Definitions.Length];owned=new bool[Definitions.Length];health=100;armor=Game.I.Character.startingArmor;
  foreach(var grant in Game.I.Character.startingWeapons){int index=System.Array.IndexOf(Definitions,grant.weapon);if(index<0)continue;owned[index]=true;ammo[index]=Mathf.Clamp(grant.ammo,0,Definitions[index].maxAmmo);}
  SelectSlot(0);ResetCombat();
 }
 public int[] InventorySlots {get{var order=Game.I.Character.inventoryOrder;var result=new int[order.Length];for(int i=0;i<order.Length;i++)result[i]=System.Array.IndexOf(Definitions,order[i]);return result;}}
 public bool CanUse(int index)=>index>=0&&index<Definitions.Length&&System.Array.IndexOf(Game.I.Character.inventoryOrder,Definitions[index])>=0;
 public int StartingRangedIndex=>System.Array.IndexOf(Definitions,Game.I.Character.startingWeapons[0].weapon);
 public void SelectSlot(int slot){var slots=InventorySlots;if(slot>=0&&slot<slots.Length)Select(slots[slot]);}

 public void SelectIfChanged(int index){if(index!=weapon)Select(index);}
 public void Select(int index){if(index<0||index>=Definitions.Length||!owned[index]||!CanUse(index))return;attackTime=-1;contactPending=false;weapon=index;if(visual)Destroy(visual);visual=Instantiate(Current.model,weaponRoot);visual.transform.localPosition=Vector3.zero;visual.transform.localRotation=Quaternion.identity;visual.transform.localScale=Vector3.one*Current.viewScale;foreach(var t in visual.GetComponentsInChildren<Transform>())t.gameObject.layer=8;foreach(var c in visual.GetComponentsInChildren<Collider>())Destroy(c);weaponRoot.localPosition=Current.viewPosition;weaponRoot.localRotation=Quaternion.Euler(Current.viewRotation);recoil=.08f;}
 void Update(){
  if(Game.I.mode!=Mode.Playing)return;float dt=Time.deltaTime;var k=Keyboard.current;var m=Mouse.current;
  var touch=TouchControls.I;bool mobile=TouchControls.Active;
  Vector2 delta=automation?Vector2.zero:mobile?touch.look:(m!=null?m.delta.ReadValue()*Game.I.sensitivity:Vector2.zero);
  transform.Rotate(0,delta.x,0);pitch=Mathf.Clamp(pitch-delta.y,-80,80);
  float x=(k!=null&&k.dKey.isPressed?1:0)-(k!=null&&k.aKey.isPressed?1:0),z=(k!=null&&k.wKey.isPressed?1:0)-(k!=null&&k.sKey.isPressed?1:0);
  if(mobile){x=touch.move.x;z=touch.move.y;}
  Vector3 input=automation?transform.InverseTransformDirection(autoMove):Vector3.ClampMagnitude(new Vector3(x,0,z),1);Vector3 desired=transform.TransformDirection(input)*Game.I.Character.movementSpeed;
  velocity.x=Mathf.MoveTowards(velocity.x,desired.x,55*dt);velocity.z=Mathf.MoveTowards(velocity.z,desired.z,55*dt);velocity.y=controller.isGrounded?-2:velocity.y-25*dt;
  controller.Move(velocity*dt);if(transform.position.y< -10){TakeDamage(1000);}
  float speed=new Vector2(velocity.x,velocity.z).magnitude;bobClock+=dt*speed*1.4f;
  cam.transform.localPosition=new Vector3(0,1.6f+(Game.I.bob?Mathf.Sin(bobClock)*.025f*Mathf.Clamp01(speed):0),0);cam.transform.localRotation=Quaternion.Euler(pitch-recoil*8,0,0);cam.fieldOfView=Game.I.fov;
  if(speed>2 && controller.isGrounded){stepTimer-=dt;if(stepTimer<=0){Game.I.Sound(assets.step,.16f,Random.Range(.9f,1.1f));stepTimer=.34f;}}
  if(k!=null&&k.digit1Key.wasPressedThisFrame)SelectSlot(0);if(k!=null&&k.digit2Key.wasPressedThisFrame)SelectSlot(1);if(k!=null&&k.digit3Key.wasPressedThisFrame)SelectSlot(2);if(k!=null&&k.digit4Key.wasPressedThisFrame)SelectSlot(3);if(k!=null&&k.digit5Key.wasPressedThisFrame)SelectSlot(4);
  if(!mobile&&m!=null&&m.scroll.ReadValue().y!=0){int direction=m.scroll.ReadValue().y>0?1:-1;var slots=InventorySlots;int current=System.Array.IndexOf(slots,weapon);for(int n=1;n<=slots.Length;n++){int w=slots[(current+n*direction+slots.Length*2)%slots.Length];if(w>=0&&owned[w]){Select(w);break;}}}
  cooldown-=dt;invulnerable-=dt;
  AdvanceAttack(dt);
  if(automation?autoFire:mobile?(touch.firing||touch.fireTap):(m!=null&&m.leftButton.isPressed))Fire();
  recoil=Mathf.MoveTowards(recoil,0,dt*2.5f);muzzle.intensity=Mathf.MoveTowards(muzzle.intensity,0,dt*65);
  weaponRoot.localPosition=Current.viewPosition+new Vector3(Game.I.bob?Mathf.Sin(bobClock*.5f)*.014f:0,Game.I.bob?Mathf.Cos(bobClock)*.012f:0,-recoil*.25f);
  weaponRoot.localRotation=Quaternion.Euler(Current.viewRotation+new Vector3(-recoil*18,Current.fireMode==FireMode.Melee?recoil*80:0,0));
  PresentAttack();
  FindInteractable();if(((k!=null&&k.eKey.wasPressedThisFrame)||(mobile&&touch.use)) && target)target.Use();
 }
 public void FindInteractable(){target=null;prompt="";if(Physics.Raycast(cam.transform.position,cam.transform.forward,out var hit,4.5f,~((1<<8)|(1<<9)),QueryTriggerInteraction.Ignore)){target=hit.collider.GetComponentInParent<Interactable>();if(target)prompt=target.Prompt;}}
 public bool Fire(){
  if(cooldown>0||Game.I.mode!=Mode.Playing)return false;var d=Current;
  if(d.fireMode!=FireMode.Melee && ammo[weapon]<=0){cooldown=.25f;Game.I.Say("OUT OF AMMO • SWITCH WEAPON",1);return false;}
  if(d.fireMode!=FireMode.Melee)ammo[weapon]--;cooldown=d.interval;bool blade=d.fireMode==FireMode.Melee;recoil=blade||d.throwingMotion?0:d.fireMode==FireMode.Scatter?.65f:.4f;muzzle.intensity=blade||d.throwingMotion?0:5;
  if(blade||d.throwingMotion){attacking=d;attackTime=0;contactPending=blade;}
  weaponAudio.pitch=Random.Range(.96f,1.04f);weaponAudio.PlayOneShot(d.fireSound?d.fireSound:assets.shot,.65f);
  var start=cam.transform.position;var dir=cam.transform.forward;
  if(d.fireMode==FireMode.Melee){
   // Contact is resolved once during the visible cutting stroke.
  }else if(d.fireMode==FireMode.Scatter||d.fireMode==FireMode.Automatic){
   for(int i=0;i<d.pellets;i++){Vector2 spread=Random.insideUnitCircle*Mathf.Tan(d.spread*Mathf.Deg2Rad);var direction=(dir+cam.transform.right*spread.x+cam.transform.up*spread.y).normalized;Vector3 end=start+direction*d.range;
    if(Physics.Raycast(start,direction,out var hit,d.range,~((1<<8)|(1<<9)),QueryTriggerInteraction.Ignore)){end=hit.point;var e=hit.collider.GetComponentInParent<Enemy>();if(e)e.Damage(d.damage,hit.point);Effects.Impact(hit.point,d.color,.13f);}Effects.Tracer(start+cam.transform.right*.2f-cam.transform.up*.17f,end,d.color);
   }
  }else Projectile.Spawn(start+dir*.08f,dir,d.projectileSpeed,d.damage,false,d.color,d.fireMode==FireMode.Explosive?d.splashRadius:0);
  return true;
 }
 public void AdvanceAttack(float dt){
  if(attackTime<0||Game.I.mode!=Mode.Playing)return;
  attackTime+=dt;
  if(contactPending&&attackTime>=attacking.interval*attacking.contactFraction){
   contactPending=false;
   if(Physics.SphereCast(cam.transform.position,.28f,cam.transform.forward,out var hit,attacking.range,~((1<<8)|(1<<9)|(1<<2)),QueryTriggerInteraction.Ignore)){
    var enemy=hit.collider.GetComponentInParent<Enemy>();
    if(enemy)enemy.Damage(attacking.damage,hit.point);
    var clip=enemy?attacking.cutSound:attacking.wallSound;if(clip)weaponAudio.PlayOneShot(clip,enemy?.8f:.45f);
    Effects.Impact(hit.point,attacking.color,.12f);
   }
  }
  if(attackTime>=attacking.interval)attackTime=-1;
 }
 void PresentAttack(){
  if(attackTime<0||!attacking)return;
  float t=Mathf.Clamp01(attackTime/attacking.interval);Vector3 pos,rot;
  float wind=attacking.contactFraction*.55f,follow=Mathf.Min(.78f,attacking.contactFraction+.18f);
  if(t<wind){float f=Mathf.SmoothStep(0,1,t/wind);pos=attacking.windupPosition*f;rot=attacking.windupRotation*f;}
  else if(t<follow){float f=Mathf.SmoothStep(0,1,(t-wind)/(follow-wind));pos=Vector3.Lerp(attacking.windupPosition,attacking.cutPosition,f);rot=Vector3.Lerp(attacking.windupRotation,attacking.cutRotation,f);}
  else{float f=1-Mathf.SmoothStep(0,1,(t-follow)/(1-follow));pos=attacking.cutPosition*f;rot=attacking.cutRotation*f;}
  weaponRoot.localPosition=attacking.viewPosition+pos;weaponRoot.localRotation=Quaternion.Euler(attacking.viewRotation+rot);
 }
 public void TakeDamage(float amount){if(Game.I.mode!=Mode.Playing||health<=0||invulnerable>0)return;float scale=Game.I.difficulty==0?.65f:Game.I.difficulty==2?1.4f:1;amount*=scale;float absorbed=Mathf.Min(armor,amount*.5f);armor-=absorbed;health=Mathf.Max(0,health-(amount-absorbed));Game.I.damageFlash=.6f;Game.I.Sound(assets.hurt,.7f);invulnerable=.16f;if(health<=0){Game.I.Sound(assets.death);Game.I.SetMode(Mode.Dead);}}
 public void Teleport(Vector3 p,float yaw=0){controller.enabled=false;transform.position=p+Vector3.up*.06f;transform.rotation=Quaternion.Euler(0,yaw,0);controller.enabled=true;velocity=Vector3.zero;pitch=0;}
 public void ResetCombat(){attackTime=-1;contactPending=false;cooldown=0;recoil=0;invulnerable=.5f;}
 public void AimAt(Vector3 p){var dir=p-cam.transform.position;transform.rotation=Quaternion.Euler(0,Mathf.Atan2(dir.x,dir.z)*Mathf.Rad2Deg,0);pitch=-Mathf.Asin(dir.normalized.y)*Mathf.Rad2Deg;cam.transform.localRotation=Quaternion.Euler(pitch,0,0);}
}
public static class Effects {
 static Material material;
 public static Material Glow {get{if(!material){material=Game.I.assets.effects;}return material;}}
 public static void Impact(Vector3 point,Color color,float size){var go=GameObject.CreatePrimitive(PrimitiveType.Sphere);Object.Destroy(go.GetComponent<Collider>());go.layer=2;go.transform.position=point;go.transform.localScale=Vector3.one*size;go.GetComponent<Renderer>().sharedMaterial=Glow;var mp=new MaterialPropertyBlock();mp.SetColor("_BaseColor",color*2);go.GetComponent<Renderer>().SetPropertyBlock(mp);go.AddComponent<Transient>().life=.15f;}
 public static void Tracer(Vector3 start,Vector3 end,Color color){var go=new GameObject("Tracer");var line=go.AddComponent<LineRenderer>();line.sharedMaterial=Glow;line.positionCount=2;line.SetPosition(0,start);line.SetPosition(1,end);line.startWidth=.025f;line.endWidth=.008f;line.startColor=color;line.endColor=color;go.AddComponent<Transient>().life=.055f;}
}
public sealed class Projectile:MonoBehaviour {
 public Vector3 direction;public float speed,damage,radius;public bool hostile;public Color color;float life=7;
 public static Projectile Spawn(Vector3 pos,Vector3 direction,float speed,float damage,bool hostile,Color color,float radius=0){
  var go=GameObject.CreatePrimitive(PrimitiveType.Sphere);Object.Destroy(go.GetComponent<Collider>());go.layer=2;go.transform.position=pos;go.transform.localScale=Vector3.one*(hostile?.3f:radius>0?.22f:.12f);go.GetComponent<Renderer>().sharedMaterial=Effects.Glow;var mp=new MaterialPropertyBlock();mp.SetColor("_BaseColor",color*3);go.GetComponent<Renderer>().SetPropertyBlock(mp);
  var p=go.AddComponent<Projectile>();p.direction=direction.normalized;p.speed=speed;p.damage=damage;p.hostile=hostile;p.color=color;p.radius=radius;
  var trail=go.AddComponent<TrailRenderer>();trail.sharedMaterial=Effects.Glow;trail.startWidth=.13f;trail.endWidth=0;trail.time=.12f;trail.startColor=color;trail.endColor=new Color(color.r,color.g,color.b,0);return p;
 }
 void Update(){float travel=speed*Time.deltaTime;int mask=~((1<<8)|(1<<2)|(hostile?1<<10:1<<9));if(Physics.SphereCast(transform.position,.1f,direction,out var hit,travel,mask,QueryTriggerInteraction.Ignore)){
   transform.position=hit.point-direction*.05f;
   if(radius>0)Explode();else if(hostile){var p=hit.collider.GetComponentInParent<Player>();if(p)p.TakeDamage(damage);}else{var e=hit.collider.GetComponentInParent<Enemy>();if(e)e.Damage(damage,hit.point);}
   Effects.Impact(hit.point,color,radius>0?1.8f:.35f);Destroy(gameObject);return;
  }transform.position+=direction*travel;life-=Time.deltaTime;if(life<=0)Destroy(gameObject);
 }
 void Explode(){Game.I.Sound(Game.I.assets.blast,.8f);foreach(var e in Game.I.enemies){if(e.dead)continue;float d=Vector3.Distance(e.transform.position+Vector3.up,transform.position);if(d<radius&&!Physics.Linecast(transform.position,e.transform.position+Vector3.up,1<<0,QueryTriggerInteraction.Ignore))e.Damage(damage*(1-d/radius),transform.position);}
  float pd=Vector3.Distance(Game.I.player.transform.position+Vector3.up,transform.position);if(pd<radius&&!Physics.Linecast(transform.position,Game.I.player.transform.position+Vector3.up,1<<0,QueryTriggerInteraction.Ignore))Game.I.player.TakeDamage(damage*.45f*(1-pd/radius));
 }
}
}
