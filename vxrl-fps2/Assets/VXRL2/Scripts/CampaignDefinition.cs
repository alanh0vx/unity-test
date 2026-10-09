using UnityEngine;
namespace VXRL2 {
[CreateAssetMenu(menuName="VXRL2/Campaign")]
public sealed class CampaignDefinition:ScriptableObject {public CharacterDefinition[] characters;public string title="ROOTBREACH";public LevelDefinition[] levels;public WeaponDefinition[] weapons;}
}
