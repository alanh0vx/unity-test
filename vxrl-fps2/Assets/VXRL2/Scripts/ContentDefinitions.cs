using System;
using System.Collections.Generic;
using UnityEngine;
namespace VXRL2 {
public enum FireMode { Melee, Bolt, Scatter, Explosive, Automatic }
public enum EnemyRole { Ranged, Charger, Ambusher, Boss }
[Serializable] public class RoomDefinition {public string name;public RectInt cells; public Color accent=Color.cyan;public bool decorate=true;}
[Serializable] public class SignSpawn {public string text;public Vector2Int cell;public float yaw;public Color color=Color.cyan;}
[Serializable] public class EnemySpawn {public EnemyDefinition definition;public Vector2Int cell;}
[Serializable] public class PickupSpawn {public PickupType type;public Vector2Int cell;public int amount=25,weaponIndex=2;}
[Serializable] public class DoorSpawn {public Vector2Int cell;public bool alongX;public bool secret;public int key; public string label;}
}
