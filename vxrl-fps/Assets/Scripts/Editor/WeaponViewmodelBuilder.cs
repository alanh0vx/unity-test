#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Unity.MP_FPS.EditorTools
{
    /// <summary>
    /// Builds "viewmodel" prefabs from the imported weapon FBXs and puts them in a Resources folder so
    /// they can be loaded at runtime by name (Resources.Load). PlayerGhost attaches the matching one to
    /// the player's hand and hides the placeholder gun.
    ///
    /// Run: Tools ▸ PvE ▸ Build Weapon Viewmodels. Re-running overwrites them in place.
    /// The prefab names here MUST match the ids used in PlayerGhost.WeaponViewmodelResourceName().
    /// </summary>
    public static class WeaponViewmodelBuilder
    {
        private const string k_Folder = "Assets/Resources/WeaponViewmodels";

        private struct Entry
        {
            public string PrefabName; // Resources name
            public string FbxPath;
        }

        private static readonly Entry[] k_Entries =
        {
            new Entry { PrefabName = "Katana",   FbxPath = "Assets/Art/Weapons/Katana/Katana_FPS.fbx" },
            new Entry { PrefabName = "Tanto",    FbxPath = "Assets/Art/Weapons/Tanto/Tanto_Short_Blade.fbx" },
            new Entry { PrefabName = "Shuriken", FbxPath = "Assets/Art/Weapons/Shuriken/Shuriken.fbx" },
            new Entry { PrefabName = "Yari",     FbxPath = "Assets/Art/Weapons/YariSpearLauncher/Yari_Spear_Launcher.fbx" },
        };

        [MenuItem("Tools/PvE/Build Weapon Viewmodels")]
        public static void BuildViewmodels()
        {
            try
            {
                Directory.CreateDirectory(k_Folder);
                int built = 0;

                foreach (var e in k_Entries)
                {
                    var model = AssetDatabase.LoadAssetAtPath<GameObject>(e.FbxPath);
                    if (model == null)
                    {
                        Debug.LogWarning($"[Viewmodels] Model not found: {e.FbxPath} (skipping {e.PrefabName}).");
                        continue;
                    }

                    // Wrap the model in a simple root so PlayerGhost can position/rotate the whole thing.
                    var root = new GameObject(e.PrefabName);
                    var visual = (GameObject)PrefabUtility.InstantiatePrefab(model);
                    visual.name = "Model";
                    visual.transform.SetParent(root.transform, false);

                    // Strip any colliders — a viewmodel is purely visual.
                    foreach (var col in root.GetComponentsInChildren<Collider>(true))
                    {
                        Object.DestroyImmediate(col);
                    }

                    string path = $"{k_Folder}/{e.PrefabName}.prefab";
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    Object.DestroyImmediate(root);
                    built++;
                    Debug.Log($"[Viewmodels] Built {path}");
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log($"<color=lime>[Viewmodels] Built {built} weapon viewmodels</color> in {k_Folder}. " +
                          "Now Play ▸ Start Host — the weapon should appear in your hand.");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Viewmodels] Build failed: {ex}");
            }
        }
    }
}
#endif
