using UnityEngine;
namespace VXRL2 {
[CreateAssetMenu(menuName="VXRL2/Enemy Definition")]
public sealed class EnemyDefinition:ScriptableObject {
 public string id,displayName;public GameObject model;public EnemyRole role;
 public float health=50,speed=3.5f,damage=10,attackInterval=1.8f,projectileSpeed=12,noticeRange=25,height=1.5f;
 public Color color=Color.green;
}

}
