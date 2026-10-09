using UnityEngine;
namespace VXRL2 {
[CreateAssetMenu(menuName="VXRL2/Game Assets")]
public sealed class GameAssets : ScriptableObject {
 public CampaignDefinition campaign;
 public Font displayFont,bodyFont;
 public Shader worldTextShader;
 public Material effects, floor, wall, trim, dark, metal, cyan, amber, red, green;
 public AudioClip shot, blast, blade, hurt, pickup, death, step;
 public Texture2D portrait;
}
}
