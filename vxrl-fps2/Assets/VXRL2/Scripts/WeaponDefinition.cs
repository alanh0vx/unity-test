using UnityEngine;
namespace VXRL2 {
[CreateAssetMenu(menuName="VXRL2/Weapon Definition")]
public sealed class WeaponDefinition:ScriptableObject {
 public string id, displayName; public GameObject model; public FireMode fireMode;
 public float damage=20, interval=.25f, range=100, projectileSpeed=28, splashRadius=4, spread=4;
 public int pellets=1, startAmmo=30, maxAmmo=120, pickupAmmo=20;
 public bool availableAtStart; public AudioClip fireSound; public Color color=Color.cyan;
 public Vector3 viewPosition=new(.26f,-.25f,.52f),viewRotation;public float viewScale=1;
 [Header("Blade / throwing presentation")]
 public bool throwingMotion;
 [Range(.1f,.8f)] public float contactFraction=.35f;
 public Vector3 windupPosition=new(.08f,.05f,-.08f),cutPosition=new(-.38f,-.14f,.08f);
 public Vector3 windupRotation=new(-25,0,-35),cutRotation=new(25,-25,90);
 public AudioClip cutSound,wallSound;
}

}
