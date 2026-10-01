using Unity.Cinemachine;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Serialization;
using static FirstPersonController;

namespace Unity.MP_FPS
{
    public partial class PlayerGhost : GhostMonoBehaviour, IUpdateClient, IUpdateServer
    {
        [field: SerializeField] public AssetReferenceGameObject ProjectilePrefabAR { get; private set; }
        [SerializeField] private Vector3 m_CameraRotation;
        [SerializeField] private GameObject m_OwnerVisuals;
        [SerializeField] private GameObject m_OtherPlayerVisuals;
        [SerializeField] private SoundDef m_SpawnSFX;
        [field: SerializeField] public Transform CameraTarget { get; private set; }
        [field: SerializeField] public Transform ReticlePoint { get; private set; }
        [field: SerializeField] public Transform ShotOrigin { get; private set; }
        [field: FormerlySerializedAs("<VisualShotOrigin>k__BackingField")] [field: SerializeField] 
        public Transform VisualShotOrigin1P { get; private set; }
        [field: SerializeField] public Transform VisualShotOrigin3P { get; private set; }

        [Header("Manual Aiming Setup")]
        [SerializeField] private Animator m_Animator3P;
        private static readonly int AimPitchHash = Animator.StringToHash("AimPitch");
        
        public int PlayerIndex { get; private set; }
        public int InputUserId { get; set; } = -1;
        public PlayerInput ServerMovementInput { get; set; }
        public ControllerConsts ControllerConsts { get; private set; }

        #region Cached Player Components
        private FirstPersonController m_Controller;
        public FirstPersonController Controller => m_Controller;

        #endregion

        [field: SerializeField] public GameObject MainCameraPrefab { get; private set; }

        private Camera m_PlayerCamera;
        private FirstPersonWeaponPose m_FirstPersonWeaponPose;
        private Animator _animatorCharacter;
        private Vector3 m_ReticleVector;

        private CinemachineTargetGroup m_TargetGroup;
        private CinemachinePositionComposer m_PositionComposer;
        private CinemachineCamera m_CinemachineCamera;

        // Procedural weapon swing (the "chop") — no animation asset needed.
        private readonly System.Collections.Generic.List<Transform> m_WeaponViewmodels = new();
        private readonly System.Collections.Generic.List<Quaternion> m_WeaponBaseRot = new();
        private readonly System.Collections.Generic.List<GameObject> m_HiddenGuns = new();
        private bool m_CurrentWeaponIsMelee;
        private uint m_LastSeenShotTick;
        private uint m_LastEquippedWeaponId = uint.MaxValue;
        private float m_SwingTimer;
        private const float k_SwingDuration = 0.25f;
        private const float k_SwingAngle = 80f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Init()
        {
            s_NextPredictionId = 1;
        }
        
        private static uint s_NextPredictionId = 1;

        public static uint GetNextPredictionID()
        {
            return s_NextPredictionId++;
        }

        public Camera GetPlayerCamera()
        {
            return m_PlayerCamera;
        }

        public CinemachineCamera GetPlayerCinemachineCamera()
        {
            return m_CinemachineCamera;
        }

        public CinemachinePositionComposer GetPositionComposer()
        {
            return m_PositionComposer;
        }

        public struct PlayerData : IComponentData
        {
            [GhostField] public FixedString128Bytes Name;
            public Entity ViewEntity;
            public Entity ControlledEntity;
        }

        public void Awake()
        {
            GetRequiredComponent(out m_Controller);
            m_ReticleVector = ReticlePoint.localPosition;
        }
        
        private void LateUpdate()
        {
            // This logic is for visual clients only
            if ((Role != MultiplayerRole.ClientProxy && Role != MultiplayerRole.ClientOwned) || m_Animator3P == null)
            {
                return;
            }

            var predictedPlayerGhost = ReadGhostComponentData<PredictedPlayerGhost>();
            var controllerState = predictedPlayerGhost.ControllerState;
            
            // matches vertical angle value in ClientInputReaderSystem
            float normalizedPitch = controllerState.PitchDegrees / 85f;
            
            m_Animator3P.SetFloat(AimPitchHash, normalizedPitch);
        }

        public override void OnGhostLinked()
        {
            bool isClientOwned = (Role == MultiplayerRole.ClientOwned);
            m_OwnerVisuals.SetActive(isClientOwned);
            m_OtherPlayerVisuals.SetActive(!isClientOwned);

            if (Role != MultiplayerRole.Server)
            {
                _animatorCharacter = GetComponent<Animator>();
                // spawn SFX
                if (m_SpawnSFX != null)
                {
                    GameManager.Instance.SoundSystem.CreateEmitter(m_SpawnSFX, transform.position);
                }
            }

            if (isClientOwned)
            {
                var predictedPlayer = ReadGhostComponentData<PredictedPlayerGhost>();
                PlayerIndex = predictedPlayer.InputIndex;
                // create camera
                CreateClientCamera();

                // Add AudioListener to client position and ensure all other AudioListeners are disabled
                var audioListeners = Resources.FindObjectsOfTypeAll<AudioListener>();
                foreach (var a in audioListeners)
                {
                    a.enabled = false;
                }
                m_OwnerVisuals.AddComponent<AudioListener>();
                
                // Attach the listener to the player model rather than the camera
                GameManager.Instance.SoundSystem.SetListenerTransform(m_OwnerVisuals.transform);    
            }
            else if (Role == MultiplayerRole.ClientProxy)
            {
                // no physics required
                Controller.CharacterController.enabled = false;
            }

            gameObject.layer = (Role == MultiplayerRole.Server)
                ? (int)LayerIndex.ServerPlayer
                : (int)LayerIndex.ClientPlayer;

            // The weapon viewmodel is attached in UpdateClient (which also handles weapon swaps).

            PlayerGhostManager.TryGetInstanceByRole(Role, out var playerManager);
            playerManager.Register(this);
        }

        public override void OnGhostPreDestroy()
        {
            if (PlayerGhostManager.TryGetInstanceByRole(Role, out var playerManager))
            {
                playerManager.Unregister(this);
            }
        }

        void AttachPlayerViewCamera()
        {
            var playerCamera = GameObject.FindAnyObjectByType<Camera>();
            if (playerCamera != null)
            {
                playerCamera.transform.parent = transform.Find("ViewPoint");
                playerCamera.transform.localPosition = Vector3.zero;
                playerCamera.transform.localRotation = Quaternion.identity;
            }
        }

        private void CreateClientCamera()
        {
            //Disable current camera
            var existingCamera = FindAnyObjectByType<Camera>();
            if (existingCamera != null)
            {
                existingCamera.enabled = false;
            }

            // spawn the camera
            var mainCameraInstance = Instantiate(MainCameraPrefab, CameraTarget.transform);
            mainCameraInstance.transform.localPosition = Vector3.zero;
            mainCameraInstance.name = $"MainCamera_{PlayerIndex}";

            m_PlayerCamera = mainCameraInstance.GetComponent<Camera>();
            if (m_PlayerCamera != null)
            {
                m_PlayerCamera.enabled = true; // make sure the player camera actually renders
            }

            var audioListener = mainCameraInstance.GetComponent<AudioListener>();
            if (audioListener != null)
            {
                GameManager.Instance.SoundSystem.SetListenerTransform(audioListener.transform);
            }

            Utils.SetCursorVisible(false);
        }

        public void UpdateServer(float deltaTime)
        {
            // Server does not need to do anything for now regarding PlayerGhost
        }

        public void UpdateClient(float deltaTime)
        {
            // Rebuild the weapon model whenever the equipped weapon changes (initial attach + swaps).
            uint equipped = ReadGhostComponentData<PredictedPlayerGhost>().EquippedWeaponID;
            if (equipped != m_LastEquippedWeaponId)
            {
                m_LastEquippedWeaponId = equipped;
                RebuildWeaponViewmodel(equipped);
            }

            UpdateWeaponSwing(deltaTime);

            var predictedPlayerGhost = ReadGhostComponentData<PredictedPlayerGhost>();
            var controllerState = predictedPlayerGhost.ControllerState;
            if (Role == MultiplayerRole.ClientOwned && CameraTarget != null)
            {
                // Use the player's own camera (created in CreateClientCamera); fall back to Camera.main.
                var cam = m_PlayerCamera != null ? m_PlayerCamera : Camera.main;
                if (cam != null)
                {
                    CameraTarget.transform.rotation = Quaternion.Euler(controllerState.PitchDegrees,
                        cam.transform.rotation.eulerAngles.y,
                        cam.transform.rotation.eulerAngles.z);
                }
            }

            var rot = Quaternion.Euler(controllerState.PitchDegrees, 0.0f, 0.0f);
            ReticlePoint.localPosition = rot * m_ReticleVector;

            //TODO: The following is a temporary fix for animation root moves (Robot Jump for example)
            m_OtherPlayerVisuals.transform.localPosition = Vector3.zero;
            m_OtherPlayerVisuals.transform.localRotation = Quaternion.identity;
        }

        public bool SetPlayerPositionFromRPC(float3 rpcPosition, float positionErrorSq)
        {
            var predictedPlayerGhost = ReadGhostComponentData<PredictedPlayerGhost>();
            var controllerState = predictedPlayerGhost.ControllerState;

            float positionError = math.distancesq(controllerState.CurrentPosition, rpcPosition);

            //allow the current player position to be altered by the client but only within a certain tolerance
            //(this is to avoid sliding during some position locked animations caused by the player predicting ahead of the server)
            if (positionError <= (positionErrorSq))
            {
                controllerState.CurrentPosition = rpcPosition;
                predictedPlayerGhost.ControllerState = controllerState;
                WriteGhostComponentData(predictedPlayerGhost);

                return true;
            }

            return false;
        }

        /// <summary>
        /// Client-side: hides the placeholder gun on the character and attaches the equipped weapon's
        /// model to the same hand. Weapons without a custom model (ids 0/1) keep the placeholder gun.
        /// </summary>
        private void RebuildWeaponViewmodel(uint weaponId)
        {
            m_FirstPersonWeaponPose?.Clear();
            // Remove the previous weapon model and restore any guns we hid.
            foreach (var vm in m_WeaponViewmodels)
            {
                if (vm != null) Destroy(vm.gameObject);
            }
            m_WeaponViewmodels.Clear();
            m_WeaponBaseRot.Clear();

            foreach (var gun in m_HiddenGuns)
            {
                if (gun != null) gun.SetActive(true);
            }
            m_HiddenGuns.Clear();

            var wd = WeaponManager.Instance.WeaponRegistry.GetWeaponData(weaponId);
            m_CurrentWeaponIsMelee = wd != null && wd.Type == WeaponType.Melee;
            m_LastSeenShotTick = ReadGhostComponentData<PredictedPlayerGhost>().LastShotTick;

            string resName = WeaponViewmodelResourceName(weaponId);
            if (resName == null)
            {
                return; // rifle/shotgun placeholder — leave the existing gun visible.
            }

            var prefab = Resources.Load<GameObject>("WeaponViewmodels/" + resName);
            if (prefab == null)
            {
                Debug.LogWarning($"[PlayerGhost] Viewmodel 'WeaponViewmodels/{resName}' not found. " +
                                 "Run Tools ▸ PvE ▸ Build Weapon Viewmodels.");
                return;
            }

            // Search only the visual hierarchies. Searching the entire player also matches
            // ArmaturePlayer_Shotgun, deactivating the player and its camera on weapon swap.
            var guns = FindPlaceholderWeaponRoots();

            if (guns.Count > 0)
            {
                foreach (var gun in guns)
                {
                    gun.gameObject.SetActive(false); // hide the gun
                    m_HiddenGuns.Add(gun.gameObject);
                    var inst = Instantiate(prefab, gun.parent);
                    bool firstPerson = Role == MultiplayerRole.ClientOwned
                        && gun.IsChildOf(m_OwnerVisuals.transform);
                    if (firstPerson)
                    {
                        if (m_FirstPersonWeaponPose == null)
                            m_FirstPersonWeaponPose = gameObject.AddComponent<FirstPersonWeaponPose>();
                        m_FirstPersonWeaponPose.Configure(m_OwnerVisuals.transform, m_PlayerCamera,
                            inst.transform, weaponId);
                    }
                    else
                    {
                        inst.transform.localPosition = gun.localPosition;
                        // Custom FBXs point +Y; the original hand socket expects +X.
                        inst.transform.localRotation = gun.localRotation
                            * Quaternion.Euler(0f, 0f, weaponId == 5 ? -90f : -60f);
                        inst.transform.localScale = Vector3.one / Mathf.Max(.001f, gun.parent.lossyScale.x);
                    }
                    RegisterWeaponViewmodel(inst.transform);
                }
            }
            else
            {
                // Fallback: no gun found — hang the weapon off the shot-origin transforms.
                Debug.LogWarning("[PlayerGhost] No placeholder gun found; attaching weapon to VisualShotOrigin.");
                if (VisualShotOrigin1P != null) RegisterWeaponViewmodel(Instantiate(prefab, VisualShotOrigin1P).transform);
                if (VisualShotOrigin3P != null) RegisterWeaponViewmodel(Instantiate(prefab, VisualShotOrigin3P).transform);
            }
        }

        private System.Collections.Generic.List<Transform> FindPlaceholderWeaponRoots()
        {
            var guns = new System.Collections.Generic.List<Transform>();
            if (m_OwnerVisuals != null)
                CollectPlaceholderWeaponRoots(m_OwnerVisuals.transform, guns);
            if (m_OtherPlayerVisuals != null)
                CollectPlaceholderWeaponRoots(m_OtherPlayerVisuals.transform, guns);
            return guns;
        }

        private static void CollectPlaceholderWeaponRoots(Transform visualRoot,
            System.Collections.Generic.List<Transform> guns)
        {
            // Visit children, never the visual container itself. Once a weapon root is
            // found, don't descend into its meshes/LODs and spawn duplicate viewmodels.
            foreach (Transform child in visualRoot)
            {
                if (child.name.StartsWith("Pfb_assaultRifle", System.StringComparison.OrdinalIgnoreCase)
                    || child.name.StartsWith("Pfb_shotgun", System.StringComparison.OrdinalIgnoreCase))
                {
                    if (!guns.Contains(child))
                        guns.Add(child);
                    continue;
                }

                CollectPlaceholderWeaponRoots(child, guns);
            }
        }

        private void RegisterWeaponViewmodel(Transform viewmodel)
        {
            m_WeaponViewmodels.Add(viewmodel);
            m_WeaponBaseRot.Add(viewmodel.localRotation);
        }

        /// <summary>Procedural chop: rotates the weapon down and back whenever the player attacks.</summary>
        private void UpdateWeaponSwing(float deltaTime)
        {
            if (m_WeaponViewmodels.Count == 0)
            {
                return;
            }

            uint shotTick = ReadGhostComponentData<PredictedPlayerGhost>().LastShotTick;
            if (shotTick != m_LastSeenShotTick)
            {
                m_LastSeenShotTick = shotTick;
                m_FirstPersonWeaponPose?.Attack();
                if (m_CurrentWeaponIsMelee)
                {
                    m_SwingTimer = k_SwingDuration;
                }
            }

            float angle = 0f;
            if (m_SwingTimer > 0f)
            {
                m_SwingTimer -= deltaTime;
                float phase = Mathf.Clamp01(1f - m_SwingTimer / k_SwingDuration); // 0 -> 1 over the swing
                angle = Mathf.Sin(phase * Mathf.PI) * k_SwingAngle;               // down and back up
            }

            for (int i = 0; i < m_WeaponViewmodels.Count; i++)
            {
                if (m_WeaponViewmodels[i] != null
                    && (m_FirstPersonWeaponPose == null || m_WeaponViewmodels[i] != m_FirstPersonWeaponPose.Weapon))
                {
                    // Rotate about the weapon's local Y axis for a downward (forward) chop.
                    m_WeaponViewmodels[i].localRotation = m_WeaponBaseRot[i] * Quaternion.Euler(0f, angle, 0f);
                }
            }
        }

        private static string WeaponViewmodelResourceName(uint weaponId)
        {
            switch (weaponId)
            {
                case 2: return "Katana";
                case 3: return "Tanto";
                case 4: return "Shuriken";
                case 5: return "Yari";
                default: return null; // 0 = rifle, 1 = shotgun → keep placeholder gun
            }
        }
    }
}