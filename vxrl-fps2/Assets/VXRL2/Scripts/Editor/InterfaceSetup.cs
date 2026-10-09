using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace VXRL2.Editor {
public static class InterfaceSetup {
 [MenuItem("VXRL2/Install Character and HUD Upgrade")]
 public static void Configure(){
  const string folder="Assets/VXRL2/Data/";
  foreach(string id in new[]{"ninja","shogun"}){var importer=(TextureImporter)AssetImporter.GetAtPath("Assets/SourceArt/Portraits/"+id+".png");importer.npotScale=TextureImporterNPOTScale.None;importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.filterMode=FilterMode.Bilinear;importer.SaveAndReimport();}
  var a=AssetDatabase.LoadAssetAtPath<GameAssets>(folder+"GameAssets.asset");
  a.displayFont=AssetDatabase.LoadAssetAtPath<Font>("Assets/VXRL2/Fonts/RussoOne-Regular.ttf");a.bodyFont=AssetDatabase.LoadAssetAtPath<Font>("Assets/VXRL2/Fonts/BarlowCondensed-SemiBold.ttf");
  if(!a.displayFont||!a.bodyFont)throw new Exception("Missing interface fonts");
  var tanto=AssetDatabase.LoadAssetAtPath<WeaponDefinition>(folder+"WeaponTanto.asset");
  if(!tanto){tanto=ScriptableObject.CreateInstance<WeaponDefinition>();AssetDatabase.CreateAsset(tanto,folder+"WeaponTanto.asset");
   var root=new GameObject("Tanto");var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/SourceArt/Weapons/Tanto/Tanto_Short_Blade.fbx");var model=UnityEngine.Object.Instantiate(source,root.transform);var rs=model.GetComponentsInChildren<Renderer>();var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);model.transform.localScale*=.55f/Mathf.Max(b.size.x,b.size.y,b.size.z);b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);model.transform.position-=b.center;
   foreach(var c in model.GetComponentsInChildren<Collider>())UnityEngine.Object.DestroyImmediate(c);
   foreach(var r in rs){var mats=r.sharedMaterials;for(int i=0;i<mats.Length;i++){var original=mats[i];var mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));mat.color=original&&original.HasProperty("_Color")?original.color:new Color(.3f,.36f,.4f);mat.SetFloat("_Metallic",.65f);mat.SetFloat("_Smoothness",.35f);string path="Assets/VXRL2/Materials/Tanto_"+r.name.Replace('/','_')+"_"+i+".mat";path=AssetDatabase.GenerateUniqueAssetPath(path);AssetDatabase.CreateAsset(mat,path);mats[i]=mat;}r.sharedMaterials=mats;}
   tanto.model=PrefabUtility.SaveAsPrefabAsset(root,"Assets/VXRL2/Prefabs/Tanto.prefab");UnityEngine.Object.DestroyImmediate(root);
   tanto.id="tanto";tanto.displayName="TANTO";tanto.fireMode=FireMode.Melee;tanto.damage=34;tanto.interval=.25f;tanto.range=2.3f;tanto.maxAmmo=0;tanto.fireSound=a.blade;tanto.color=Color.cyan;tanto.viewPosition=new Vector3(.31f,-.22f,.5f);tanto.viewRotation=new Vector3(0,0,-25);EditorUtility.SetDirty(tanto);
  }
  var campaign=a.campaign;var weapons=campaign.weapons.ToList();if(!weapons.Contains(tanto))weapons.Add(tanto);campaign.weapons=weapons.ToArray();
  var ninja=Character(folder+"Ninja.asset","ninja","NINJA","SPEED / PRECISION","Fast movement. Rapid shuriken fire.\nFinish close targets with the Tanto.",new Color(.2f,.86f,.9f),9.8f,15,a,1,tanto,90);
  var shogun=Character(folder+"Shogun.asset","shogun","SHOGUN","POWER / ARMOR","Explosive Yari spears. Heavy armor.\nCut through close targets with the Katana.",new Color(.95f,.34f,.17f),8.6f,45,a,3,weapons[0],18);
  campaign.characters=new[]{ninja,shogun};EditorUtility.SetDirty(campaign);EditorUtility.SetDirty(a);
  AssetDatabase.SaveAssets();Debug.Log("VXRL2_INTERFACE_CONTENT_OK");
 }
 static CharacterDefinition Character(string path,string id,string name,string tag,string description,Color accent,float speed,float armor,GameAssets a,int ranged,WeaponDefinition melee,int ammo){
  var c=AssetDatabase.LoadAssetAtPath<CharacterDefinition>(path);if(!c){c=ScriptableObject.CreateInstance<CharacterDefinition>();AssetDatabase.CreateAsset(c,path);}
  c.id=id;c.displayName=name;c.tagline=tag;c.description=description;c.accent=accent;c.movementSpeed=speed;c.startingArmor=armor;c.portrait=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/SourceArt/Portraits/"+id+".png");c.startingWeapons=new[]{new WeaponGrant{weapon=a.campaign.weapons[ranged],ammo=ammo},new WeaponGrant{weapon=melee,ammo=0}};c.inventoryOrder=new[]{a.campaign.weapons[ranged],melee,a.campaign.weapons[2],a.campaign.weapons[4]};EditorUtility.SetDirty(c);return c;
 }
 public static void Build(){Configure();CombatFeelSetup.Configure();ProjectBuilder.Build();}
}
}
