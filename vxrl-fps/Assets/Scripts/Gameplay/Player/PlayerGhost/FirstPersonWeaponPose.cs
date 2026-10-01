using System.Collections.Generic;
using UnityEngine;

namespace Unity.MP_FPS
{
    // Runs after the legacy gun Animator, so the hands remain on the custom weapon.
    [DefaultExecutionOrder(10000)]
    public sealed class FirstPersonWeaponPose : MonoBehaviour
    {
        // Weapon-only viewmodel: show just the weapon, hide the robot arms/hands.
        private const bool k_WeaponOnly = true;

        private Transform m_Visuals, m_View, m_Weapon;
        private Camera m_Camera;
        private Vector3 m_OriginalPosition, m_OriginalScale;
        private Quaternion m_OriginalRotation;
        private float m_OriginalNearClip;
        private Arm m_Left, m_Right;
        private uint m_WeaponId;
        private float m_ActionTime = 10f;
        private readonly List<Finger> m_Fingers = new();
        private readonly List<SkinnedMeshRenderer> m_Skins = new();
        private readonly List<bool> m_SkinUpdates = new();

        private sealed class Arm
        {
            public Transform Upper, Lower, Hand;
            public bool Valid => Upper != null && Lower != null && Hand != null;
        }
        private struct Finger
        {
            public Transform Bone;
            public Vector3 Rest;
        }

        public Transform Weapon => m_Weapon;
        public float LeftGripError { get; private set; }
        public float RightGripError { get; private set; }

        public void Configure(Transform visuals, Camera camera, Transform weapon, uint weaponId)
        {
            if (m_Visuals == null)
            {
                m_Visuals = visuals;
                m_OriginalPosition = visuals.localPosition;
                m_OriginalRotation = visuals.localRotation;
                m_OriginalScale = visuals.localScale;
                m_Left = FindArm(visuals, "Left");
                m_Right = FindArm(visuals, "Right");
                foreach (var bone in visuals.GetComponentsInChildren<Transform>(true))
                    if (bone.name.EndsWith("Proximal") || bone.name.EndsWith("Intermediate") || bone.name.EndsWith("Distal"))
                        m_Fingers.Add(new Finger { Bone = bone, Rest = bone.localEulerAngles });
                foreach (var skin in visuals.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    m_Skins.Add(skin);
                    m_SkinUpdates.Add(skin.updateWhenOffscreen);
                    skin.updateWhenOffscreen = true;
                }
                m_Camera = camera;
                m_OriginalNearClip = camera.nearClipPlane;
            }
            m_View = camera.transform;
            m_Weapon = weapon;
            m_WeaponId = weaponId;
            m_ActionTime = 10f;
            weapon.SetParent(m_View, false);
            m_Camera.nearClipPlane = .04f;
            enabled = true;
            ApplyPose(0f);
        }

        public void Clear()
        {
            if (m_Visuals != null)
            {
                m_Visuals.localPosition = m_OriginalPosition;
                m_Visuals.localRotation = m_OriginalRotation;
                m_Visuals.localScale = m_OriginalScale;
            }
            for (int i = 0; i < m_Skins.Count; i++)
                if (m_Skins[i] != null)
                {
                    m_Skins[i].updateWhenOffscreen = m_SkinUpdates[i];
                    m_Skins[i].enabled = true; // restore arms if we leave the weapon-only viewmodel
                }
            if (m_Camera != null) m_Camera.nearClipPlane = m_OriginalNearClip;
            m_Visuals = m_Weapon = m_View = null;
            m_Fingers.Clear(); m_Skins.Clear(); m_SkinUpdates.Clear();
            enabled = false;
        }

        public void Attack() => m_ActionTime = 0f;

        private void LateUpdate()
        {
            if (m_Weapon == null || m_View == null) return;
            float duration = m_WeaponId == 5 ? .18f : m_WeaponId == 4 ? .3f : .34f;
            m_ActionTime += Time.deltaTime;
            float phase = Mathf.Clamp01(m_ActionTime / duration);
            ApplyPose(Mathf.Sin(phase * Mathf.PI));
        }

        public void ApplyPose(float action)
        {
            if (m_Weapon == null || m_Visuals == null) return;
            // The imported sample arms are doubled in size. Bring them back to hand scale.
            m_Visuals.localPosition = new Vector3(0f, -1.9f, .07f);
            m_Visuals.localRotation = Quaternion.identity;
            m_Visuals.localScale = Vector3.one * 1.2f;

            Vector3 position, rightGrip, leftGrip;
            Quaternion rotation;
            float scale;
            bool twoHanded = m_WeaponId == 2 || m_WeaponId == 5;
            switch (m_WeaponId)
            {
                case 5: // Imported barrel points +Y; +X is the underside of the stock.
                    position = new Vector3(.13f, -.04f, .34f);
                    rotation = Quaternion.LookRotation(Vector3.left, Vector3.forward);
                    scale = .85f;
                    rightGrip = new Vector3(.079f, -.055f, 0f);
                    leftGrip = new Vector3(.043f, .27f, 0f);
                    position += new Vector3(0f, -.006f, -.035f) * action;
                    rotation = Quaternion.Euler(-4f * action, 0f, 0f) * rotation;
                    break;
                case 3:
                    position = new Vector3(.16f, -.08f, .40f);
                    rotation = Quaternion.Euler(70f, 0f, 25f);
                    scale = 1.15f;
                    rightGrip = new Vector3(0f, -.005f, 0f);
                    leftGrip = Vector3.zero;
                    position += new Vector3(-.14f, .025f, .14f) * action;
                    rotation = Quaternion.Euler(25f * action, -35f * action, -25f * action) * rotation;
                    break;
                case 4:
                    position = new Vector3(.17f, -.02f, .43f);
                    rotation = Quaternion.Euler(5f, -25f, -15f);
                    scale = 1f;
                    rightGrip = new Vector3(0f, -.065f, 0f);
                    leftGrip = Vector3.zero;
                    position += new Vector3(-.12f, .035f, .16f) * action;
                    rotation *= Quaternion.Euler(0f, 0f, -65f * action);
                    break;
                default: // Two-handed katana guard: tip forward, slightly up and inward.
                    position = new Vector3(.11f, -.08f, .43f);
                    rotation = Quaternion.Euler(60f, 0f, 8f);
                    scale = 1f;
                    rightGrip = new Vector3(0f, .05f, 0f);
                    leftGrip = new Vector3(0f, -.068f, 0f);
                    position += new Vector3(-.19f, -.045f, .025f) * action;
                    rotation = Quaternion.Euler(48f * action, -35f * action, -24f * action) * rotation;
                    break;
            }
            m_Weapon.localPosition = position;
            m_Weapon.localRotation = rotation;
            m_Weapon.localScale = Vector3.one * scale;

            if (k_WeaponOnly)
            {
                // Weapon-only viewmodel: hide the arms/hands and skip the arm IK + finger posing.
                for (int i = 0; i < m_Skins.Count; i++)
                    if (m_Skins[i] != null) m_Skins[i].enabled = false;
                return;
            }

            Vector3 axis = m_WeaponId == 5 ? m_View.up : m_Weapon.up;
            Quaternion rightRotation = Quaternion.LookRotation(-axis, m_View.right);
            Quaternion leftRotation = Quaternion.LookRotation(m_Weapon.up, m_View.right);
            Vector3 rightPalm = m_Weapon.TransformPoint(rightGrip);
            Vector3 leftPalm = twoHanded ? m_Weapon.TransformPoint(leftGrip)
                : m_View.TransformPoint(new Vector3(-.13f, -.11f, .36f));
            if (!twoHanded)
                leftRotation = m_View.rotation * Quaternion.Euler(25f, 0f, -30f);

            // Palm centres are offset from the wrist bones; target the grip, not the wrist.
            Vector3 rightOffset = new Vector3(.026f, -.06f, 0f) * 1.2f;
            Vector3 leftOffset = new Vector3(-.026f, .06f, 0f) * 1.2f;
            SolveArm(m_Right, rightPalm - rightRotation * rightOffset, rightRotation,
                m_View.TransformPoint(new Vector3(.65f, -.6f, .08f)));
            SolveArm(m_Left, leftPalm - leftRotation * leftOffset, leftRotation,
                m_View.TransformPoint(new Vector3(-.6f, -.55f, .13f)));
            if (m_Right.Valid) RightGripError = Vector3.Distance(m_Right.Hand.position + rightRotation * rightOffset, rightPalm);
            if (m_Left.Valid) LeftGripError = Vector3.Distance(m_Left.Hand.position + leftRotation * leftOffset, leftPalm);
            CurlFingers(twoHanded);
        }

        private void CurlFingers(bool twoHanded)
        {
            foreach (var finger in m_Fingers)
            {
                string name = finger.Bone.name;
                if (name.Contains("Thumb")) continue;
                Vector3 euler = finger.Rest;
                bool guard = !twoHanded && name.StartsWith("Left_");
                bool pinch = m_WeaponId == 4 && name.StartsWith("Right_Index");
                // Tighter curls so the hand reads as a closed grip rather than splayed-open fingers.
                euler.x = name.EndsWith("Proximal") ? (guard ? 60f : pinch ? 40f : 78f)
                    : name.EndsWith("Intermediate") ? (guard ? 75f : pinch ? 50f : 92f)
                    : (guard ? 50f : pinch ? 42f : 58f); // Distal
                finger.Bone.localRotation = Quaternion.Euler(euler);
            }
        }

        private static Arm FindArm(Transform root, string side)
        {
            var arm = new Arm();
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == side + "_UpperArm") arm.Upper = t;
                if (t.name == side + "_LowerArm") arm.Lower = t;
                if (t.name == side + "_Hand") arm.Hand = t;
            }
            return arm;
        }

        private static void SolveArm(Arm arm, Vector3 wrist, Quaternion rotation, Vector3 hint)
        {
            if (arm == null || !arm.Valid) return;
            Vector3 shoulder = arm.Upper.position;
            float upperLength = Vector3.Distance(shoulder, arm.Lower.position);
            float lowerLength = Vector3.Distance(arm.Lower.position, arm.Hand.position);
            Vector3 direction = wrist - shoulder;
            float distance = Mathf.Clamp(direction.magnitude, .001f, upperLength + lowerLength - .001f);
            direction.Normalize();
            Vector3 bend = Vector3.ProjectOnPlane(hint - shoulder, direction).normalized;
            float along = (upperLength * upperLength - lowerLength * lowerLength + distance * distance) / (2f * distance);
            float height = Mathf.Sqrt(Mathf.Max(0f, upperLength * upperLength - along * along));
            Vector3 elbow = shoulder + direction * along + bend * height;
            arm.Upper.rotation = Quaternion.FromToRotation(arm.Lower.position - shoulder, elbow - shoulder) * arm.Upper.rotation;
            arm.Lower.rotation = Quaternion.FromToRotation(arm.Hand.position - arm.Lower.position,
                shoulder + direction * distance - arm.Lower.position) * arm.Lower.rotation;
            arm.Hand.rotation = rotation;
        }
    }
}
