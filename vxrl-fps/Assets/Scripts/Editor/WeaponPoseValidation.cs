using System;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.MP_FPS;

public static class WeaponPoseValidation
{
    [MenuItem("Tools/PvE/Validate and Render Weapon Poses")]
    public static void Validate()
    {
        if (EditorApplication.isPlaying) throw new Exception("Stop Play mode before rendering prefab pose checks.");
        string folder = "/private/tmp/fps-pose-previews";
        Directory.CreateDirectory(folder);
        var report = new StringBuilder();
        bool passed = true;
        foreach (string character in new[] { "Rifle", "Shotgun" })
        {
            var root = PrefabUtility.LoadPrefabContents($"Assets/Prefabs/PlayerGhosts/ArmaturePlayer_{character}.prefab");
            try
            {
                var player = root.GetComponent<PlayerGhost>();
                var owner = (GameObject)typeof(PlayerGhost).GetField("m_OwnerVisuals", BindingFlags.Instance|BindingFlags.NonPublic).GetValue(player);
                var other = (GameObject)typeof(PlayerGhost).GetField("m_OtherPlayerVisuals", BindingFlags.Instance|BindingFlags.NonPublic).GetValue(player);
                owner.SetActive(true); other.SetActive(false);
                foreach(var t in root.GetComponentsInChildren<Transform>(true))
                    if(t.name.StartsWith("Pfb_")) t.gameObject.SetActive(false);
                var cameraObject = new GameObject("PoseCheckCamera");
                SceneManager.MoveGameObjectToScene(cameraObject, root.scene);
                var camera = cameraObject.AddComponent<Camera>();
                camera.scene = root.scene;
                camera.transform.SetParent(player.CameraTarget, false);
                camera.fieldOfView = 40; camera.aspect = 16f/9f;
                camera.clearFlags = CameraClearFlags.SolidColor;camera.backgroundColor = new Color(.08f,.12f,.18f);
                foreach(var lightRotation in new[]{new Vector3(35,-30,0),new Vector3(20,130,0)})
                {
                    var go = new GameObject("PoseCheckLight");SceneManager.MoveGameObjectToScene(go,root.scene);
                    go.transform.SetParent(root.transform);var light = go.AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.5f;light.transform.rotation=Quaternion.Euler(lightRotation);
                }
                var pose=root.AddComponent<FirstPersonWeaponPose>();
                var animator=owner.GetComponent<Animator>();
                AnimationClip idle=null;
                foreach(var clip in animator.runtimeAnimatorController.animationClips)
                    if(clip.name == "Shoot_"+(character == "Rifle" ? "AssaultRifle" : "Shotgun")) { idle=clip; break; }
                var originalPosition=owner.transform.localPosition;var originalScale=owner.transform.localScale;
                string[] names={"Katana","Tanto","Shuriken","Yari"};
                for(uint id=2;id<=5;id++)
                {
                    idle?.SampleAnimation(owner,0f);
                    var weapon=UnityEngine.Object.Instantiate(Resources.Load<GameObject>("WeaponViewmodels/"+names[id-2]));
                    SceneManager.MoveGameObjectToScene(weapon,root.scene);
                    pose.Configure(owner.transform,camera,weapon.transform,id);
                    foreach(float action in new[]{0f,1f})
                    {
                        idle?.SampleAnimation(owner,0f);
                        pose.ApplyPose(action);
                        report.AppendLine($"{character} {names[id-2]} action={action} right_error={pose.RightGripError:F4} left_error={pose.LeftGripError:F4} barrel_forward={Vector3.Dot(weapon.transform.up,camera.transform.forward):F3}");
                        if(pose.RightGripError>.035f || pose.LeftGripError>.035f) passed=false;
                        if(id==5 && Vector3.Dot(weapon.transform.up,camera.transform.forward)<.98f) passed=false;
                        if(character=="Shotgun") Render(camera,Path.Combine(folder,$"{names[id-2]}_{action}.png"));
                    }
                    pose.Clear();UnityEngine.Object.DestroyImmediate(weapon);
                    if(owner.transform.localPosition!=originalPosition || owner.transform.localScale!=originalScale) passed=false;
                }
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        File.WriteAllText(Path.Combine(folder,"validation.txt"),report.ToString());
        if(!passed)throw new Exception("[WeaponPoseValidation] Pose checks failed; see /private/tmp/fps-pose-previews/validation.txt");
        Debug.Log("[WeaponPoseValidation] PASS: both characters, four weapons, idle/attack grip reach, launcher direction, original pose restoration.");
    }
    private static void Render(Camera camera,string path)
    {
        var previous=RenderTexture.active;var rt=new RenderTexture(1280,720,24);var image=new Texture2D(1280,720,TextureFormat.RGB24,false);
        var baked = new System.Collections.Generic.List<GameObject>();
        var meshes = new System.Collections.Generic.List<Mesh>();
        var skins = new System.Collections.Generic.List<SkinnedMeshRenderer>();
        foreach (var skin in camera.transform.root.GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            if (!skin.enabled) continue;
            var mesh = new Mesh(); skin.BakeMesh(mesh); meshes.Add(mesh);
            var go = new GameObject("BakedPosePreview"); go.transform.SetParent(skin.transform,false);
            go.AddComponent<MeshFilter>().sharedMesh=mesh;
            go.AddComponent<MeshRenderer>().sharedMaterials=skin.sharedMaterials;
            skin.enabled=false; skins.Add(skin); baked.Add(go);
        }
        try {camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());}
        finally{foreach(var go in baked)UnityEngine.Object.DestroyImmediate(go);foreach(var mesh in meshes)UnityEngine.Object.DestroyImmediate(mesh);foreach(var skin in skins)skin.enabled=true;camera.targetTexture=null;RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(image);}
    }
}
