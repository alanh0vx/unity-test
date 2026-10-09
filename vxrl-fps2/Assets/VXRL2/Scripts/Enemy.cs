using UnityEngine;
using UnityEngine.AI;
namespace VXRL2 {
public sealed class Enemy:MonoBehaviour {
 public EnemyDefinition definition;public float health;public bool dead;public NavMeshAgent agent;
 Transform visual;Renderer[] renderers;Collider body;float attackTimer,windup,flash,think;bool alerted;Vector3 attackDirection;MaterialPropertyBlock block;
 public void Build(EnemyDefinition def,Vector3 position){
  definition=def;health=def.health;transform.position=position;gameObject.layer=10;
  var model=Instantiate(def.model,transform);visual=model.transform;renderers=model.GetComponentsInChildren<Renderer>();block=new MaterialPropertyBlock();
  var col=gameObject.AddComponent<CapsuleCollider>();col.height=def.height;col.radius=def.height*.28f;col.center=Vector3.up*def.height*.5f;body=col;
  agent=gameObject.AddComponent<NavMeshAgent>();agent.height=def.height;agent.radius=Mathf.Min(.7f,def.height*.25f);agent.speed=def.speed;agent.acceleration=20;agent.angularSpeed=540;agent.stoppingDistance=def.role==EnemyRole.Ranged?8:1.7f;
  if(NavMesh.SamplePosition(position,out var hit,3,NavMesh.AllAreas))agent.Warp(hit.position);
  attackTimer=Random.Range(.5f,1.8f);
 }
 void Update(){if(dead||Game.I.mode!=Mode.Playing)return;var player=Game.I.player;float dt=Time.deltaTime;var delta=player.transform.position-transform.position;float distance=delta.magnitude;var eye=transform.position+Vector3.up*definition.height*.65f;bool see=!Physics.Linecast(eye,player.cam.transform.position,1<<0,QueryTriggerInteraction.Ignore);
  if(distance<definition.noticeRange&&see)alerted=true;
  flash=Mathf.MoveTowards(flash,0,dt*5);
  if(!alerted)return;
  attackTimer-=dt;think-=dt;
  if(agent.enabled&&agent.isOnNavMesh){
   if(think<=0){agent.SetDestination(player.transform.position);think=.2f;}
   agent.isStopped=windup>0 || (definition.role==EnemyRole.Ranged&&see&&distance<12);
  }
  float wobble=Mathf.Sin(Time.time*8+transform.position.x)*.04f;
  visual.localPosition=Vector3.up*(definition.role==EnemyRole.Ambusher?wobble*.3f:wobble);
  visual.localScale=Vector3.one*(1+windup*.09f);
  if(windup>0){windup-=dt;if(windup<=0)Attack(eye);}
  else if(attackTimer<=0&&see&&distance<(definition.role==EnemyRole.Charger||definition.role==EnemyRole.Ambusher?3:24)){
   windup=definition.role==EnemyRole.Boss?.7f:.45f;attackDirection=(player.cam.transform.position-eye).normalized;attackTimer=definition.attackInterval;Effects.Impact(eye,definition.color,.5f);
  }
  if(distance>1){var flat=new Vector3(delta.x,0,delta.z);transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(flat),dt*7);}
  foreach(var r in renderers){block.SetColor("_EmissionColor",definition.color*(flash*5+windup*2));r.SetPropertyBlock(block);}
 }
 void Attack(Vector3 eye){var p=Game.I.player;
  switch(definition.role){
   case EnemyRole.Charger:case EnemyRole.Ambusher:if(Vector3.Distance(p.transform.position,transform.position)<3.3f&&!Physics.Linecast(eye,p.cam.transform.position,1<<0))p.TakeDamage(definition.damage);break;
   case EnemyRole.Ranged:Projectile.Spawn(eye,attackDirection,definition.projectileSpeed,definition.damage,true,definition.color);break;
   case EnemyRole.Boss:for(int i=-2;i<=2;i++)Projectile.Spawn(eye,Quaternion.Euler(0,i*12,0)*attackDirection,definition.projectileSpeed,definition.damage,true,definition.color);break;
  }
 }
 public void Damage(float damage,Vector3 point){if(dead)return;health-=damage;alerted=true;flash=1;Game.I.hitMarker=1;
  if(health<=0){dead=true;health=0;agent.enabled=false;body.enabled=false;Game.I.Kill(this);Effects.Impact(transform.position+Vector3.up,definition.color,1.1f);visual.localScale=new Vector3(1,.18f,1);visual.localPosition=Vector3.up*.06f;foreach(var r in renderers){block.SetColor("_BaseColor",new Color(.07f,.08f,.09f));block.SetColor("_EmissionColor",Color.black);r.SetPropertyBlock(block);} }
  else if(damage>25){windup=0;attackTimer=Mathf.Max(.5f,attackTimer);}
 }
 public void Restore(EnemyState s){dead=s.dead;health=s.health;agent.enabled=false;transform.position=s.position;body.enabled=!dead;visual.localScale=dead?new Vector3(1,.18f,1):Vector3.one;visual.localPosition=Vector3.zero;foreach(var r in renderers)r.SetPropertyBlock(null);agent.enabled=!dead;if(!dead&&NavMesh.SamplePosition(s.position,out var hit,3,NavMesh.AllAreas))agent.Warp(hit.position);alerted=false;windup=0;attackTimer=1;}
}
}
