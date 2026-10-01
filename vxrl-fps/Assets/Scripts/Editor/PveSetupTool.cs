#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.MP_FPS;
using Unity.NetCode;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;

namespace Unity.MP_FPS.EditorTools
{
    /// <summary>
    /// One-click setup for the PvE virus enemies. Runs in the Unity Editor only.
    ///
    /// Usage (top menu):
    ///   Tools ▸ PvE ▸ 1 · Build & Register All Enemies   (builds 5 enemy prefabs from the imported models,
    ///                                                      auto-scaled, networked, and registered)
    ///   Tools ▸ PvE ▸ 2 · Add Enemy Spawners To Open Scene (drops a ring of spawners into the open scene)
    ///
    /// Everything uses Unity's own APIs, so it won't corrupt your scenes. Each step logs to the Console.
    /// Re-running rebuilds the prefabs in place.
    /// </summary>
    public static class PveSetupTool
    {
        private const string k_EnemyFolder = "Assets/Prefabs/Enemies";
        private const string k_GhostResourcesScenePath = "Assets/Scenes/GameResourcesSubScene.unity";

        private struct EnemyDef
        {
            public string Name;         // prefab + address name
            public string ModelPath;    // imported FBX
            public int Type;            // VirusEnemy.VirusType index
            public float MaxHealth;
            public float MoveSpeed;
            public float AttackDamage;
            public float TargetHeight;  // the model is auto-scaled to roughly this height (metres)
            public bool BasicSpawn;     // included in the Step-2 spawner mix (bosses are placed by hand)
        }

        private static readonly EnemyDef[] k_Enemies =
        {
            new EnemyDef { Name = "VirusEnemy_Scout",   ModelPath = "Assets/Art/Enemies/Virus_Scout/Virus_Scout.fbx",         Type = 0, MaxHealth = 40f,  MoveSpeed = 3.5f, AttackDamage = 5f,  TargetHeight = 1.0f, BasicSpawn = true },
            new EnemyDef { Name = "VirusEnemy_Brawler", ModelPath = "Assets/Art/Enemies/Malware_Brawler/Malware_Brawler.fbx", Type = 1, MaxHealth = 90f,  MoveSpeed = 2.8f, AttackDamage = 10f, TargetHeight = 1.6f, BasicSpawn = true },
            new EnemyDef { Name = "VirusEnemy_Worm",    ModelPath = "Assets/Art/Enemies/Worm_Burrower/Worm_Burrower.fbx",     Type = 2, MaxHealth = 60f,  MoveSpeed = 3.2f, AttackDamage = 7f,  TargetHeight = 1.0f, BasicSpawn = true },
            new EnemyDef { Name = "VirusBoss_Warden",   ModelPath = "Assets/Art/Enemies/Ransomware_Warden/Ransomware_Warden.fbx", Type = 3, MaxHealth = 400f, MoveSpeed = 1.8f, AttackDamage = 18f, TargetHeight = 2.4f, BasicSpawn = false },
            new EnemyDef { Name = "VirusBoss_Overlord", ModelPath = "Assets/Art/Enemies/Rootkit_Overlord/Rootkit_Overlord.fbx",   Type = 4, MaxHealth = 600f, MoveSpeed = 1.6f, AttackDamage = 22f, TargetHeight = 3.0f, BasicSpawn = false },
        };

        // ---------------------------------------------------------------------------------------------
        // STEP 1 — build all enemy prefabs from the models, mark Addressable, register in GlobalGhostPrefabs.
        // ---------------------------------------------------------------------------------------------
        [MenuItem("Tools/PvE/1 · Build & Register All Enemies")]
        public static void BuildAndRegisterEnemies()
        {
            try
            {
                Directory.CreateDirectory(k_EnemyFolder);

                var guids = new List<string>();
                foreach (var def in k_Enemies)
                {
                    var prefab = BuildEnemyPrefab(def);
                    if (prefab != null)
                    {
                        string guid = AssetDatabase.AssetPathToGUID($"{k_EnemyFolder}/{def.Name}.prefab");
                        MakeAddressable(guid, def.Name);
                        guids.Add(guid);
                    }
                }

                RegisterInGhostPrefabs(guids);

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log($"<color=lime>[PvE Setup] Step 1 complete.</color> Built {guids.Count} enemy prefabs, " +
                          "marked Addressable, and registered them in GlobalGhostPrefabs. " +
                          "Next: open GameScene and run Tools ▸ PvE ▸ 2.");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[PvE Setup] Step 1 failed: {e}");
            }
        }

        private static GameObject BuildEnemyPrefab(EnemyDef def)
        {
            int serverEnemyLayer = LayerMask.NameToLayer("ServerEnemy");
            if (serverEnemyLayer < 0)
            {
                Debug.LogError("[PvE Setup] Layer 'ServerEnemy' not found (should be slot 9). Aborting.");
                return null;
            }

            string prefabPath = $"{k_EnemyFolder}/{def.Name}.prefab";
            var root = new GameObject(def.Name) { layer = serverEnemyLayer };

            // --- Visual from the imported model (auto-scaled to target height, feet at y=0) ---
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(def.ModelPath);
            if (model != null)
            {
                var visual = (GameObject)PrefabUtility.InstantiatePrefab(model);
                visual.name = "Visual";
                visual.transform.SetParent(root.transform, false);
                ScaleAndGroundVisual(visual, def.TargetHeight);
            }
            else
            {
                Debug.LogWarning($"[PvE Setup] Model not found at {def.ModelPath}; using a capsule for {def.Name}.");
                var cap = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                Object.DestroyImmediate(cap.GetComponent<Collider>());
                cap.name = "Visual";
                cap.transform.SetParent(root.transform, false);
                cap.transform.localScale = Vector3.one * (def.TargetHeight * 0.5f);
                cap.transform.localPosition = new Vector3(0f, def.TargetHeight * 0.5f, 0f);
            }

            // --- Gameplay collider on the root (this is what weapons hit) ---
            var col = root.AddComponent<CapsuleCollider>();
            col.height = def.TargetHeight;
            col.radius = def.TargetHeight * 0.3f;
            col.center = new Vector3(0f, def.TargetHeight * 0.5f, 0f);

            // --- Networking + AI ---
            var ghostGo = root.AddComponent<GhostGameObject>();
            // Enemies move on both server + client (like projectiles), so they do NOT use transform sync.
            ghostGo.RequireTransformSync = false;
            root.AddComponent<GhostAuthoringComponent>();
            var enemy = root.AddComponent<VirusEnemy>();

            var so = new SerializedObject(enemy);
            SetFloat(so, "_maxHealth", def.MaxHealth);
            SetFloat(so, "_moveSpeed", def.MoveSpeed);
            SetFloat(so, "_attackDamage", def.AttackDamage);
            SetFloat(so, "_attackCooldown", 1.5f);  // slower attacks, so the player survives longer
            SetFloat(so, "_detectionRange", 28f);   // only nearby enemies give chase (less swarming)
            SetEnum(so, "_type", def.Type);

            // Hit/kill feedback sound (reuse the sample's target-hit SFX).
            var hitSfx = AssetDatabase.LoadAssetAtPath<SoundDef>(
                "Assets/Audio/Sounddefs/TargetHit/SoundDef_TargetHit.asset");
            var hitProp = so.FindProperty("_hitSfx");
            if (hitProp != null && hitSfx != null) hitProp.objectReferenceValue = hitSfx;

            so.ApplyModifiedPropertiesWithoutUndo();

            var saved = PrefabUtility.SaveAsPrefabAsset(root, prefabPath, out bool ok);
            Object.DestroyImmediate(root);

            if (!ok || saved == null)
            {
                Debug.LogError($"[PvE Setup] Failed to save {prefabPath}.");
                return null;
            }

            Debug.Log($"[PvE Setup] Built {prefabPath}");
            return saved;
        }

        private static void ScaleAndGroundVisual(GameObject visual, float targetHeight)
        {
            var rends = visual.GetComponentsInChildren<Renderer>();
            if (rends.Length == 0)
            {
                return;
            }

            Bounds b = rends[0].bounds;
            for (int i = 1; i < rends.Length; i++)
            {
                b.Encapsulate(rends[i].bounds);
            }

            if (b.size.y > 0.0001f)
            {
                float scale = targetHeight / b.size.y;
                visual.transform.localScale = visual.transform.localScale * scale;
            }

            // Recompute bounds after scaling and drop the model so its feet sit at the root origin.
            rends = visual.GetComponentsInChildren<Renderer>();
            Bounds b2 = rends[0].bounds;
            for (int i = 1; i < rends.Length; i++)
            {
                b2.Encapsulate(rends[i].bounds);
            }

            float bottomOffset = b2.min.y - visual.transform.root.position.y;
            visual.transform.localPosition -= new Vector3(0f, bottomOffset, 0f);
        }

        private static void SetFloat(SerializedObject so, string field, float value)
        {
            var p = so.FindProperty(field);
            if (p != null)
            {
                p.floatValue = value;
            }
            else
            {
                Debug.LogWarning($"[PvE Setup] VirusEnemy field '{field}' not found (stat left at default).");
            }
        }

        private static void SetEnum(SerializedObject so, string field, int enumIndex)
        {
            var p = so.FindProperty(field);
            if (p != null)
            {
                p.enumValueIndex = enumIndex;
            }
        }

        private static void MakeAddressable(string guid, string address)
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                Debug.LogError("[PvE Setup] Addressable settings not found. Open " +
                               "Window ▸ Asset Management ▸ Addressables ▸ Groups once, then re-run.");
                return;
            }

            var entry = settings.CreateOrMoveEntry(guid, settings.DefaultGroup);
            entry.address = address;
            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryModified, entry, true);
        }

        private static void RegisterInGhostPrefabs(List<string> guids)
        {
            var scene = EditorSceneManager.OpenScene(k_GhostResourcesScenePath, OpenSceneMode.Additive);
            try
            {
                var authoring = Object.FindObjectsByType<GhostPrefabsAuthoring>(FindObjectsSortMode.None)
                    .FirstOrDefault();
                if (authoring == null || authoring.GhostPrefabs == null)
                {
                    Debug.LogError("[PvE Setup] GhostPrefabsAuthoring (GlobalGhostPrefabs) not found / list is null " +
                                   "in " + k_GhostResourcesScenePath + ". Add the enemy prefabs to its list manually.");
                    return;
                }

                int added = 0;
                foreach (var guid in guids)
                {
                    if (authoring.GhostPrefabs.Any(r => r != null && r.AssetGUID == guid))
                    {
                        continue;
                    }

                    authoring.GhostPrefabs.Add(new AssetReferenceGameObject(guid));
                    added++;
                }

                if (added > 0)
                {
                    EditorUtility.SetDirty(authoring);
                    EditorSceneManager.MarkSceneDirty(authoring.gameObject.scene);
                    EditorSceneManager.SaveScene(authoring.gameObject.scene);
                }

                Debug.Log($"[PvE Setup] Registered {added} new enemy prefab(s) in GlobalGhostPrefabs.");
            }
            finally
            {
                if (scene.IsValid() && scene.path == k_GhostResourcesScenePath)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }

        // ---------------------------------------------------------------------------------------------
        // STEP 2 — drop a ring of spawners (mix of the basic enemies) into the open scene.
        // ---------------------------------------------------------------------------------------------
        [MenuItem("Tools/PvE/2 · Add Enemy Spawners To Open Scene")]
        public static void AddEnemySpawnersToOpenScene()
        {
            try
            {
                var basics = k_Enemies.Where(e => e.BasicSpawn)
                    .Select(e => AssetDatabase.AssetPathToGUID($"{k_EnemyFolder}/{e.Name}.prefab"))
                    .Where(g => !string.IsNullOrEmpty(g))
                    .ToList();

                if (basics.Count == 0)
                {
                    Debug.LogError("[PvE Setup] No enemy prefabs found. Run Step 1 first.");
                    return;
                }

                var activeScene = SceneManager.GetActiveScene();
                if (!activeScene.IsValid() || !activeScene.isLoaded)
                {
                    Debug.LogError("[PvE Setup] No scene is open. Open your GameScene first, then re-run.");
                    return;
                }

                // Clean up a previous spawner group so re-running doesn't stack them.
                var existing = GameObject.Find("PvE_EnemySpawners");
                if (existing != null)
                {
                    Object.DestroyImmediate(existing);
                }

                var parent = new GameObject("PvE_EnemySpawners");
                const int count = 10;
                const float radius = 20f;
                for (int i = 0; i < count; i++)
                {
                    float angle = (i / (float)count) * Mathf.PI * 2f;
                    var pos = new Vector3(Mathf.Cos(angle) * radius, 1f, Mathf.Sin(angle) * radius);

                    var go = new GameObject($"VirusSpawner_{i}");
                    go.transform.SetParent(parent.transform, false);
                    go.transform.position = pos;

                    var spawner = go.AddComponent<GhostSpawner>();
                    string guid = basics[i % basics.Count]; // cycle scout/brawler/worm
                    spawner.GhostPrefabReference.SetAssetReference(new AssetReferenceGameObject(guid));
                }

                EditorSceneManager.MarkSceneDirty(activeScene);
                Debug.Log($"<color=lime>[PvE Setup] Added {count} enemy spawners</color> (mix of scout/brawler/worm) " +
                          $"to scene '{activeScene.name}'. Save (Cmd/Ctrl+S), then Play ▸ Direct ▸ Start Host.");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[PvE Setup] Step 2 failed: {e}");
            }
        }
    }
}
#endif
