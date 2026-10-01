using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.Entities;
using Unity.NetCode;

namespace Unity.MP_FPS
{
    [RequireComponent(typeof(UIDocument))]
    public class InGameHUD : MonoBehaviour
    {
        [Header("Reticle Configuration")] [SerializeField]
        private float reticleBaseSize = 80f;

        // UI Element references
        private VisualElement m_RootElement;
        private ProgressBar m_HealthBar;
        private VisualElement m_PlayerHealthBarFill;
        private ProgressBar m_AmmoBar;
        private Label m_AmmoLabel;
        private VisualElement m_AmmoContainer;
        private Label m_WeaponNameLabel;
        private Label m_ReloadingLabel;
        private VisualElement m_Reticle;

        // Character portrait (bottom-right, above the ammo)
        private VisualElement m_CharacterPortrait;
        private Texture2D m_NinjaPortrait;
        private Texture2D m_ShogunPortrait;
        private int m_CurrentPortraitCharacter = -1;

        // Victory overlay
        private VisualElement m_WinOverlay;
        private bool m_EnemiesSeen;
        private bool m_WinTriggered;
        private float m_WinTimer;

        // Minimap (top-right): player at centre, enemies as dots.
        private VisualElement m_Minimap;
        private VisualElement m_MinimapPlayer;
        private readonly List<VisualElement> m_MinimapDots = new();
        private const float k_MinimapRadius = 75f;      // px, half of the 150px minimap
        private const float k_MinimapRadarRange = 40f;  // world metres mapped to the minimap edge
        private const float k_MinimapDotSize = 9f;

        // UI-side timer to ensure shot feedback is visible for a minimum duration.
        private float m_shotFeedbackTimer = 0f;

        // The reticle will stay white for at least 100ms after a shot.
        private const float k_ShotFeedbackDuration = 0.1f;
        private static readonly Color k_HealthBarColor = new Color(0.29f, 0.83f, 0.43f, 0.75f);
        private const string k_CrossReticleClass = "kits-reticle-cross";
        private const string k_TCrossReticleClass = "kits-reticle-tcross";
        private const string k_CircularCrossClass = "kits-reticle-circularcross";
        private const string k_OpenCircularReticleClass = "kits-reticle-opencircular";

        // ECS query fields
        private World m_ClientWorld;
        private EntityManager m_EntityManager;
        private EntityQuery m_LocalPlayerQuery;

        void OnEnable()
        {
            m_RootElement = GetComponent<UIDocument>().rootVisualElement;

            // Find the UI elements by name
            m_HealthBar = m_RootElement.Q<ProgressBar>("player-health-bar");
            if (m_HealthBar != null)
            {
                m_PlayerHealthBarFill = m_HealthBar.Q<VisualElement>(null, "unity-progress-bar__progress");
            }

            m_AmmoLabel = m_RootElement.Q<Label>("ammo-label");
            m_AmmoBar = m_RootElement.Q<ProgressBar>("player-ammo-bar");
            m_AmmoContainer = m_RootElement.Q<VisualElement>("ammo-container");
            m_WeaponNameLabel = m_RootElement.Q<Label>("weapon-type-label");
            m_ReloadingLabel = m_RootElement.Q<Label>("reloading-label");
            m_Reticle = m_RootElement.Q<VisualElement>("player-reticle");

            // Minimap
            m_Minimap = m_RootElement.Q<VisualElement>("minimap");
            m_MinimapPlayer = m_RootElement.Q<VisualElement>("minimap-player");

            // Victory overlay
            m_WinOverlay = m_RootElement.Q<VisualElement>("win-overlay");

            // Character portrait setup
            m_CharacterPortrait = m_RootElement.Q<VisualElement>("character-portrait");
            m_NinjaPortrait = Resources.Load<Texture2D>("Characters/ninja");
            m_ShogunPortrait = Resources.Load<Texture2D>("Characters/shogun");
            m_CurrentPortraitCharacter = -1;
        }

        private void UpdateCharacterPortrait(int characterIndex)
        {
            if (m_CharacterPortrait == null || characterIndex == m_CurrentPortraitCharacter)
            {
                return; // only update when the character changes
            }

            Texture2D tex = characterIndex == 1 ? m_ShogunPortrait : m_NinjaPortrait;
            if (tex != null)
            {
                m_CharacterPortrait.style.backgroundImage = new StyleBackground(tex);
                m_CharacterPortrait.style.display = DisplayStyle.Flex;
                m_CurrentPortraitCharacter = characterIndex;
            }
            else
            {
                m_CharacterPortrait.style.display = DisplayStyle.None;
            }
        }

        private void InitializeEcs()
        {
            // Find the active client world
            foreach (var world in World.All)
            {
                if (world.IsClient())
                {
                    m_ClientWorld = world;
                    m_EntityManager = world.EntityManager;
                    break;
                }
            }

            if (m_EntityManager != null)
            {
                // This query finds the single entity that is both a predicted player ghost
                // and is owned by the local client.
                m_LocalPlayerQuery = m_EntityManager.CreateEntityQuery(
                    ComponentType.ReadOnly<PredictedPlayerGhost>(),
                    ComponentType.ReadOnly<GhostOwnerIsLocal>()
                );
            }
        }

        void LateUpdate()
        {
            // Not valid outside play / before the runtime singletons exist.
            if (GameSettings.Instance == null || m_RootElement == null)
            {
                return;
            }

            // Toggle HUD visibility based on the overall game state
            bool isInGame = GameSettings.Instance.GameState == GlobalGameState.InGame;
            if (m_RootElement.style.display != (isInGame ? DisplayStyle.Flex : DisplayStyle.None))
            {
                m_RootElement.style.display = isInGame ? DisplayStyle.Flex : DisplayStyle.None;
            }

            if (!isInGame) return;

            // Victory: once enemies have appeared and then all are gone.
            CheckVictory();

            // If the world was destroyed (e.g., returned to main menu), try to re-initialize
            if (m_ClientWorld == null || !m_ClientWorld.IsCreated)
            {
                InitializeEcs();
            }

            // Ensure the ECS systems are ready
            if (m_EntityManager == null || !m_LocalPlayerQuery.HasSingleton<PredictedPlayerGhost>())
            {
                // No local player entity found, hide the HUD. This occurs when the player is dead.
                m_RootElement.style.display = DisplayStyle.None;
                return;
            }

            // Get the player's data directly from the ECS component
            PredictedPlayerGhost playerData = m_LocalPlayerQuery.GetSingleton<PredictedPlayerGhost>();

            // Update Character Portrait (Ninja / Shogun)
            UpdateCharacterPortrait(playerData.CharacterIndex);

            // Update Health Bar
            if (m_HealthBar != null)
            {
                m_HealthBar.highValue = playerData.MaxHealth > 0 ? playerData.MaxHealth : 100;
                m_HealthBar.value = playerData.CurrentHealth;

                float healthPercent = playerData.CurrentHealth / 100f;
                Color healthColor = Color.Lerp(Color.red, k_HealthBarColor, healthPercent);

                // Apply the calculated color to the fill element's background
                m_PlayerHealthBarFill.style.backgroundColor = healthColor;
            }

            // Update Ammo Label
            if (m_AmmoLabel != null)
            {
                var weaponData = WeaponManager.Instance.WeaponRegistry.GetWeaponData(playerData.EquippedWeaponID);
                bool isMelee = weaponData != null && weaponData.Type == WeaponType.Melee;

                // Show the current weapon's name above the ammo bar.
                if (m_WeaponNameLabel != null)
                {
                    m_WeaponNameLabel.text = weaponData != null ? weaponData.WeaponName : string.Empty;
                    m_WeaponNameLabel.style.display = DisplayStyle.Flex;
                }

                // Melee weapons (katana, blade) have no ammo — hide the whole ammo readout.
                if (m_AmmoContainer != null)
                {
                    m_AmmoContainer.style.display = isMelee ? DisplayStyle.None : DisplayStyle.Flex;
                }

                if (!isMelee)
                {
                    int magazineSize = weaponData != null ? weaponData.MagazineSize : 0;

                    // Update Ammo Text and Color
                    m_AmmoLabel.text = $"{playerData.CurrentAmmo.ToString()} / {magazineSize.ToString()}";
                    if (playerData.CurrentAmmo == 0) m_AmmoLabel.style.color = Color.red;
                    else if (playerData.CurrentAmmo <= magazineSize * 0.3f) m_AmmoLabel.style.color = Color.yellow;
                    else m_AmmoLabel.style.color = Color.white;

                    m_AmmoBar.highValue = magazineSize;
                    m_AmmoBar.value = playerData.CurrentAmmo;
                }
            }

            // Update Reloading Indicator
            if (m_ReloadingLabel != null)
            {
                m_ReloadingLabel.style.display =
                    playerData.ControllerState.IsReloadingState ? DisplayStyle.Flex : DisplayStyle.None;
            }

            // Update Reticle
            UpdateReticleVisual(playerData);

            // Update Minimap
            UpdateMinimap();
        }

        private void CheckVictory()
        {
            if (m_WinOverlay == null) return;

            int enemies = VirusEnemy.ActiveEnemies.Count;
            if (enemies > 0) m_EnemiesSeen = true;

            if (!m_WinTriggered && m_EnemiesSeen && enemies == 0)
            {
                m_WinTriggered = true;
                m_WinTimer = 5f;
                m_WinOverlay.style.display = DisplayStyle.Flex;
                Utils.SetCursorVisible(true);
            }

            if (m_WinTriggered)
            {
                m_WinTimer -= Time.deltaTime;
                if (m_WinTimer <= 0f)
                {
                    m_WinTriggered = false;
                    m_EnemiesSeen = false;
                    m_WinOverlay.style.display = DisplayStyle.None;
                    if (GameManager.Instance != null) GameManager.Instance.ReturnToMainMenuAsync();
                }
            }
        }

        private void UpdateMinimap()
        {
            if (m_Minimap == null || m_MinimapPlayer == null)
            {
                return;
            }

            var cam = Camera.main;
            if (cam == null)
            {
                return;
            }

            Vector3 playerPos = cam.transform.position;
            Vector3 fwd = cam.transform.forward;
            fwd.y = 0f;
            if (fwd.sqrMagnitude < 1e-4f)
            {
                return;
            }
            fwd.Normalize();
            Vector3 right = new Vector3(fwd.z, 0f, -fwd.x); // camera right, flattened

            // Player marker at the centre.
            m_MinimapPlayer.style.left = k_MinimapRadius - 6f;
            m_MinimapPlayer.style.top = k_MinimapRadius - 6f;

            var enemies = VirusEnemy.ActiveEnemies;
            float usableR = k_MinimapRadius - 8f;

            // Grow the dot pool as needed.
            while (m_MinimapDots.Count < enemies.Count)
            {
                var dot = new VisualElement { pickingMode = PickingMode.Ignore };
                dot.style.position = Position.Absolute;
                dot.style.width = k_MinimapDotSize;
                dot.style.height = k_MinimapDotSize;
                float r = k_MinimapDotSize / 2f;
                dot.style.borderTopLeftRadius = r;
                dot.style.borderTopRightRadius = r;
                dot.style.borderBottomLeftRadius = r;
                dot.style.borderBottomRightRadius = r;
                dot.style.backgroundColor = new Color(0.95f, 0.3f, 0.3f);
                m_Minimap.Add(dot);
                m_MinimapDots.Add(dot);
            }

            for (int i = 0; i < m_MinimapDots.Count; i++)
            {
                var dot = m_MinimapDots[i];
                if (i >= enemies.Count || enemies[i] == null)
                {
                    dot.style.display = DisplayStyle.None;
                    continue;
                }

                Vector3 delta = enemies[i].position - playerPos;
                float px = Vector3.Dot(delta, right) / k_MinimapRadarRange * usableR;
                float py = -Vector3.Dot(delta, fwd) / k_MinimapRadarRange * usableR; // forward -> up

                float d = Mathf.Sqrt(px * px + py * py);
                if (d > usableR)
                {
                    px = px / d * usableR;
                    py = py / d * usableR;
                }

                dot.style.display = DisplayStyle.Flex;
                dot.style.left = k_MinimapRadius + px - k_MinimapDotSize / 2f;
                dot.style.top = k_MinimapRadius + py - k_MinimapDotSize / 2f;
            }
        }

        private void UpdateReticleVisual(PredictedPlayerGhost playerData)
        {
            if (m_Reticle == null)
                return;

            if (m_shotFeedbackTimer > 0)
            {
                m_shotFeedbackTimer -= Time.deltaTime;
            }

            var weaponData = WeaponManager.Instance.WeaponRegistry.GetWeaponData(playerData.EquippedWeaponID);

            if (weaponData == null)
            {
                m_Reticle.style.display = DisplayStyle.None;
                return;
            }

            // Update Reticle Type
            string desiredClass = GetReticleClassName(weaponData.ReticleType);

            // Remove all reticle classes that are NOT the desired one.
            if (desiredClass != k_CrossReticleClass) m_Reticle.RemoveFromClassList(k_CrossReticleClass);
            if (desiredClass != k_TCrossReticleClass) m_Reticle.RemoveFromClassList(k_TCrossReticleClass);
            if (desiredClass != k_OpenCircularReticleClass) m_Reticle.RemoveFromClassList(k_OpenCircularReticleClass);
            if (desiredClass != k_CircularCrossClass) m_Reticle.RemoveFromClassList(k_CircularCrossClass);

            // Now, add the correct class if it's not already present.
            if (!m_Reticle.ClassListContains(desiredClass))
            {
                m_Reticle.AddToClassList(desiredClass);
            }

            // Update Reticle Visibility
            if (m_Reticle.style.display == DisplayStyle.None)
            {
                if (m_Reticle.style.position != StyleKeyword.Initial)
                {
                    m_Reticle.style.position = StyleKeyword.Initial;
                    m_Reticle.style.left = StyleKeyword.Initial;
                    m_Reticle.style.top = StyleKeyword.Initial;
                }

                m_Reticle.style.display = DisplayStyle.Flex;
            }

            m_Reticle.style.width = reticleBaseSize;
            m_Reticle.style.height = reticleBaseSize;

            // Determine if the player can shoot
            bool isReloading = playerData.ControllerState.IsReloadingState;
            bool justFired = playerData.ControllerState.Shoot;
            bool isWeaponOnCooldown = playerData.WeaponCooldown < weaponData.CooldownInMs;

            if (justFired)
            {
                m_shotFeedbackTimer = k_ShotFeedbackDuration;
            }

            bool greyReticleVisual = false;

            // Weapon reloading
            if (isReloading)
            {
                greyReticleVisual = true;
                m_shotFeedbackTimer = 0f;
            }
            // Weapon is on cooldown AND we are not in the shot feedback window.
            else if (isWeaponOnCooldown && m_shotFeedbackTimer <= 0)
            {
                greyReticleVisual = true;
            }

            // Update Reticle Color
            m_Reticle.style.unityBackgroundImageTintColor = greyReticleVisual
                ? new StyleColor(Color.grey)
                : new StyleColor(Color.white);
        }

        private string GetReticleClassName(ReticleType reticleType)
        {
            switch (reticleType)
            {
                case ReticleType.Cross: return k_CrossReticleClass;
                case ReticleType.TCross: return k_TCrossReticleClass;
                case ReticleType.OpenCircular: return k_OpenCircularReticleClass;
                case ReticleType.CircularCross: return k_CircularCrossClass;
                default: return "";
            }
        }
    }
}