using UnityEditor;
using UnityEngine;
namespace VXRL2.Editor {
public static class CombatFeelSetup {
 public static void Configure(){
  Set("Weapon1","ShurikenThrow",true,new(.03f,.01f,-.08f),new(-.08f,.03f,.12f),new(-12,0,-20),new(18,0,35));
  Set("WeaponTanto","TantoSlash",false,new(.06f,.06f,-.06f),new(-.36f,.16f,.10f),new(-20,0,-30),new(30,-25,85));
  Set("Weapon0","KatanaChop",false,new(.02f,.12f,-.10f),new(-.48f,.20f,.08f),new(-35,10,-40),new(35,-30,105));
  AssetDatabase.SaveAssets();
 }
 static AudioClip Clip(string name)=>AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/VXRL2/Audio/"+name+".wav");
 static void Set(string asset,string sound,bool throwing,Vector3 wind,Vector3 cut,Vector3 wr,Vector3 cr){
  var w=AssetDatabase.LoadAssetAtPath<WeaponDefinition>("Assets/VXRL2/Data/"+asset+".asset");if(!w)return;
  w.fireSound=Clip(sound);w.throwingMotion=throwing;w.contactFraction=.35f;w.windupPosition=wind;w.cutPosition=cut;w.windupRotation=wr;w.cutRotation=cr;w.cutSound=Clip("BladeCut");w.wallSound=Clip("BladeWall");EditorUtility.SetDirty(w);
 }
 public static void Build(){Configure();ProjectBuilder.Build();}
}
}
