using UnityEngine;
using System.Collections.Generic;
namespace VXRL2 {
[CreateAssetMenu(menuName="VXRL2/Level Definition")]
public sealed class LevelDefinition:ScriptableObject {
 public string id="quarantine",displayName="QUARANTINE GATE",subtitle="SECTOR 01 / ROOTBREACH";
 public Vector2Int start=new(15,2),exit=new(15,36),checkpoint=new(15,25);
 public List<RoomDefinition> rooms=new(); public List<EnemySpawn> enemies=new();public List<PickupSpawn> pickups=new();public List<DoorSpawn> doors=new();
 public List<Vector2Int> pillars=new();public List<SignSpawn> signs=new();
 public bool requiresBoss=true;
 public string entryObjective="FIND BLUE ACCESS IN THE FOUNDRY",blueObjective="ENTER THE REACTOR • FIND RED ACCESS",redObjective="BREACH THE RED GATE • DESTROY WARDEN",exitObjective="REACH THE PURGE TERMINAL";
}

}
