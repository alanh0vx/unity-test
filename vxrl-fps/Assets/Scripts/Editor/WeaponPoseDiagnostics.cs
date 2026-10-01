using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using Unity.MP_FPS;

public static class WeaponPoseDiagnostics
{
    [MenuItem("Tools/PvE/Inspect Weapon Poses")]
    public static void Inspect()
    {
        var report = new StringBuilder();
        foreach (string name in new[] { "ArmaturePlayer_Rifle", "ArmaturePlayer_Shotgun" })
        {
            var root = PrefabUtility.LoadPrefabContents($"Assets/Prefabs/PlayerGhosts/{name}.prefab");
            try
            {
                report.AppendLine("PLAYER " + name);
                foreach (var a in root.GetComponentsInChildren<Animator>(true))
                {
                    report.AppendLine($"ANIMATOR {a.name} human={a.isHuman} controller={a.runtimeAnimatorController}");
                    if (a.runtimeAnimatorController != null)
                        foreach (var clip in a.runtimeAnimatorController.animationClips)
                            report.AppendLine($"CLIP {clip.name} {AssetDatabase.GetAssetPath(clip)}");
                }
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                {
                    string path=AnimationUtility.CalculateTransformPath(t, root.transform);
                    if (path.Contains("3P")) continue;
                    report.AppendLine($"{path} | localPos={t.localPosition:F4} rot={t.localEulerAngles:F2} world={t.position:F4} worldRot={t.eulerAngles:F2} scale={t.lossyScale:F3}");
                }
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        foreach (string name in new[] { "Katana", "Tanto", "Shuriken", "Yari" })
        {
            var root = PrefabUtility.LoadPrefabContents($"Assets/Resources/WeaponViewmodels/{name}.prefab");
            try
            {
                report.AppendLine("WEAPON " + name);
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                    report.AppendLine($"{t.name} pos={t.localPosition:F4} rot={t.localEulerAngles:F2} scale={t.localScale:F3}");
                foreach(var r in root.GetComponentsInChildren<Renderer>(true))
                    report.AppendLine($"BOUNDS {r.name} {r.bounds}");
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        File.WriteAllText("/private/tmp/fps-weapon-poses.txt",report.ToString());
        Debug.Log("[WeaponPoseDiagnostics] Written /private/tmp/fps-weapon-poses.txt");
    }
}
