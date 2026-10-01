#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Unity.MP_FPS.EditorTools
{
    /// <summary>
    /// Builds simple placeholder weapon models (katana, short blade, shuriken, spear-shotgun) out of
    /// Unity's built-in shapes, so the weapon system has something to show before the real Blender
    /// models arrive. Run: Tools ▸ PvE ▸ Build Placeholder Weapons.
    ///
    /// Each weapon is saved as a prefab in Assets/Prefabs/Weapons/Placeholder/. The grip/pivot sits at
    /// the origin and the weapon points along +Z (forward), so it attaches naturally to a hand bone.
    /// Re-running overwrites the prefabs in place (keeps their GUIDs, so references survive).
    /// </summary>
    public static class PlaceholderWeaponBuilder
    {
        private const string k_Folder = "Assets/Prefabs/Weapons/Placeholder";
        private const string k_MaterialPath = k_Folder + "/PlaceholderWeaponMat.mat";

        private static Material s_Material;

        [MenuItem("Tools/PvE/Build Placeholder Weapons")]
        public static void BuildAll()
        {
            try
            {
                Directory.CreateDirectory(k_Folder);
                s_Material = GetOrCreateMaterial();

                BuildKatana();
                BuildShortBlade();
                BuildShuriken();
                BuildSpearShotgun();

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log("<color=lime>[Placeholder Weapons] Built 4 weapons</color> in " + k_Folder +
                          " (WP_Katana, WP_ShortBlade, WP_Shuriken, WP_SpearShotgun). " +
                          "These are stand-ins — swap in your Blender models later.");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Placeholder Weapons] Build failed: {e}");
            }
        }

        private static Material GetOrCreateMaterial()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Material>(k_MaterialPath);
            if (existing != null)
            {
                return existing;
            }

            // URP Lit if available, otherwise the Standard shader — either renders fine (no magenta).
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var mat = new Material(shader) { color = new Color(0.62f, 0.63f, 0.66f) };
            AssetDatabase.CreateAsset(mat, k_MaterialPath);
            return mat;
        }

        // ------------------------------------------------------------------ builders

        private static void BuildKatana()
        {
            var root = new GameObject("WP_Katana");
            Prim(PrimitiveType.Cylinder, root.transform, "Handle",
                pos: new Vector3(0f, 0f, 0.11f), euler: new Vector3(90f, 0f, 0f),
                scale: new Vector3(0.036f, 0.11f, 0.036f));
            Prim(PrimitiveType.Cylinder, root.transform, "Guard",
                pos: new Vector3(0f, 0f, 0.225f), euler: new Vector3(90f, 0f, 0f),
                scale: new Vector3(0.11f, 0.006f, 0.11f));
            Prim(PrimitiveType.Cube, root.transform, "Blade",
                pos: new Vector3(0f, 0.004f, 0.6f), euler: Vector3.zero,
                scale: new Vector3(0.032f, 0.012f, 0.75f));
            Save(root, "WP_Katana");
        }

        private static void BuildShortBlade()
        {
            var root = new GameObject("WP_ShortBlade");
            Prim(PrimitiveType.Cylinder, root.transform, "Handle",
                pos: new Vector3(0f, 0f, 0.05f), euler: new Vector3(90f, 0f, 0f),
                scale: new Vector3(0.03f, 0.05f, 0.03f));
            Prim(PrimitiveType.Cube, root.transform, "Blade",
                pos: new Vector3(0f, 0f, 0.21f), euler: new Vector3(0f, 0f, 45f),
                scale: new Vector3(0.035f, 0.012f, 0.22f));
            Save(root, "WP_ShortBlade");
        }

        private static void BuildShuriken()
        {
            var root = new GameObject("WP_Shuriken");
            // Four points: two crossed flat bars in the XZ plane.
            Prim(PrimitiveType.Cube, root.transform, "BarX",
                pos: Vector3.zero, euler: Vector3.zero,
                scale: new Vector3(0.13f, 0.008f, 0.025f));
            Prim(PrimitiveType.Cube, root.transform, "BarZ",
                pos: Vector3.zero, euler: Vector3.zero,
                scale: new Vector3(0.025f, 0.008f, 0.13f));
            Prim(PrimitiveType.Cylinder, root.transform, "Hub",
                pos: Vector3.zero, euler: new Vector3(90f, 0f, 0f),
                scale: new Vector3(0.04f, 0.006f, 0.04f));
            Save(root, "WP_Shuriken");
        }

        private static void BuildSpearShotgun()
        {
            var root = new GameObject("WP_SpearShotgun");
            Prim(PrimitiveType.Cylinder, root.transform, "Shaft",
                pos: new Vector3(0f, 0f, 0.4f), euler: new Vector3(90f, 0f, 0f),
                scale: new Vector3(0.035f, 0.4f, 0.035f));
            Prim(PrimitiveType.Cube, root.transform, "SpearTip",
                pos: new Vector3(0f, 0f, 0.9f), euler: new Vector3(0f, 0f, 45f),
                scale: new Vector3(0.05f, 0.05f, 0.2f));
            Prim(PrimitiveType.Cube, root.transform, "ShotgunBody",
                pos: new Vector3(0f, 0.02f, 0.12f), euler: Vector3.zero,
                scale: new Vector3(0.08f, 0.1f, 0.26f));
            Prim(PrimitiveType.Cube, root.transform, "Grip",
                pos: new Vector3(0f, -0.09f, 0.1f), euler: new Vector3(15f, 0f, 0f),
                scale: new Vector3(0.05f, 0.13f, 0.06f));
            Save(root, "WP_SpearShotgun");
        }

        // ------------------------------------------------------------------ helpers

        private static void Prim(PrimitiveType type, Transform parent, string name,
            Vector3 pos, Vector3 euler, Vector3 scale)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            var col = go.GetComponent<Collider>();
            if (col != null)
            {
                Object.DestroyImmediate(col); // viewmodels don't need colliders
            }

            var renderer = go.GetComponent<Renderer>();
            if (renderer != null && s_Material != null)
            {
                renderer.sharedMaterial = s_Material;
            }

            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localEulerAngles = euler;
            go.transform.localScale = scale;
        }

        private static void Save(GameObject root, string fileName)
        {
            string path = $"{k_Folder}/{fileName}.prefab";
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            Debug.Log($"[Placeholder Weapons] Saved {path}");
        }
    }
}
#endif
