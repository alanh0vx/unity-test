using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Unity.MP_FPS;

// Regression check against the actual player prefabs, without modifying their assets.
public static class WeaponAttachmentValidation
{
    [MenuItem("Tools/PvE/Validate Player Weapon Attachments")]
    public static void Validate()
    {
        foreach (string name in new[] { "ArmaturePlayer_Rifle", "ArmaturePlayer_Shotgun" })
        {
            string path = $"Assets/Prefabs/PlayerGhosts/{name}.prefab";
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                root.name = "[Client] " + name + " [regression]";
                var player = root.GetComponent<PlayerGhost>();
                var method = typeof(PlayerGhost).GetMethod("FindPlaceholderWeaponRoots",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                var guns = (List<Transform>)method.Invoke(player, null);
                if (guns.Count != 2)
                    throw new Exception($"{name}: expected first/third-person gun roots; found {guns.Count}.");

                // A similarly named LOD child must not produce another replacement.
                var nested = new GameObject("Pfb_shotgun_LOD_regression");
                nested.transform.SetParent(guns[0], false);
                guns = (List<Transform>)method.Invoke(player, null);
                if (guns.Count != 2)
                    throw new Exception($"{name}: nested weapon meshes were selected twice.");

                foreach (var gun in guns)
                {
                    if (gun == root.transform || player.CameraTarget.IsChildOf(gun))
                        throw new Exception($"{name}: selection would disable the player/camera.");
                    gun.gameObject.SetActive(false);
                }
                if (!root.activeSelf || !player.CameraTarget.gameObject.activeInHierarchy)
                    throw new Exception($"{name}: swapping weapons disabled the player/camera target.");
                foreach (var gun in guns)
                    gun.gameObject.SetActive(true);
                Debug.Log($"[WeaponAttachmentValidation] PASS {name}: two weapon roots; player/camera preserved; nested LOD ignored.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
        Debug.Log("[WeaponAttachmentValidation] All checks passed.");
    }
}
