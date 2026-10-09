using System;
using UnityEngine;
namespace VXRL2 {
[Serializable] public class WeaponGrant {public WeaponDefinition weapon;public int ammo;}
[CreateAssetMenu(menuName="VXRL2/Character Definition")]
public sealed class CharacterDefinition:ScriptableObject {
 public string id,displayName,tagline,description;
 public Texture2D portrait;
 public Color accent=Color.cyan;
 public float movementSpeed=9,startingArmor=25;
 public WeaponGrant[] startingWeapons;
 public WeaponDefinition[] inventoryOrder;
}
}
