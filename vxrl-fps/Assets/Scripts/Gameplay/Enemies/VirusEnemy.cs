using System.Collections.Generic;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace Unity.MP_FPS
{
    /// <summary>
    /// A server-authoritative PvE enemy ("computer virus"). Modeled on <see cref="Projectile"/>:
    /// it is a <see cref="GhostMonoBehaviour"/> whose networked state lives in the nested
    /// <see cref="EnemyData"/> component (auto-added to the ghost entity by GhostGameObjectBaker).
    ///
    /// Server runs the AI (chase + attack) and applies damage; clients just see the replicated
    /// transform move. Weapons damage the enemy by calling <see cref="ServerTakeDamage"/>; the enemy
    /// destroys itself in its own <see cref="UpdateServer"/> so that <c>DestroyEntity</c> is called
    /// from a valid IUpdateServer context (it asserts this).
    ///
    /// Increment 1 of the PvE vertical slice: uses simple direct movement (no NavMesh) so it is
    /// testable without baking a NavMesh. Upgrade to NavMeshAgent pathfinding in a later pass.
    /// </summary>
    public class VirusEnemy : GhostMonoBehaviour, IUpdateServer, IUpdateClient
    {
        /// <summary>Client-side list of on-screen enemies, used by the HUD minimap.</summary>
        public static readonly List<Transform> ActiveEnemies = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => ActiveEnemies.Clear();

        public enum VirusType
        {
            Scout,    // Virus Scout — fast, fragile
            Brawler,  // Malware Brawler — tanky melee
            Worm,     // Worm Burrower — ground ambusher
            Warden,   // Ransomware Warden — boss
            Overlord  // Rootkit Overlord — boss
        }

        /// <summary>Networked enemy state. Auto-added to the ghost entity by reflection in the baker.</summary>
        public struct EnemyData : IComponentData
        {
            [GhostField] public float Health;
            [GhostField] public float MaxHealth;
            [GhostField] public int Type;
            [GhostField] public uint LastHitTick; // bumped when hit, so clients can flash/react
        }

        [Header("Identity")]
        [SerializeField] private VirusType _type = VirusType.Worm;
        [SerializeField] private float _maxHealth = 50f;

        [Header("AI — Movement")]
        [Tooltip("How far the enemy can sense players to start chasing them.")]
        [SerializeField] private float _detectionRange = 40f;
        [SerializeField] private float _moveSpeed = 4f;

        [Header("AI — Attack")]
        [SerializeField] private float _attackRange = 2.0f;
        [SerializeField] private float _attackDamage = 10f;
        [SerializeField] private float _attackCooldown = 1.0f;

        private float _attackTimer;
        private bool _serverInitialized;
        private GameObject _cachedTarget;
        private float _retargetTimer;
        private const float k_RetargetInterval = 0.3f; // only re-scan for a target a few times a second

        [Header("Feedback")]
        [SerializeField] private SoundDef _hitSfx; // optional; plays when the enemy is hit (assign later)

        // Client-side hit flash
        private Renderer[] _renderers;
        private MaterialPropertyBlock _mpb;
        private float _flashTimer;
        private uint _lastSeenHitTick;
        private static readonly int s_BaseColorId = Shader.PropertyToID("_BaseColor");
        private const float k_FlashDuration = 0.12f;

        // Client-side world-space health bar above the enemy
        private Transform _hpBarRoot;
        private Transform _hpFill;
        private static Material s_HpBgMat;
        private static Material s_HpFillMat;
        private const float k_HpBarWidth = 1.2f;
        private const float k_HpBarThickness = 0.16f;
        private const float k_HpBarHeightOffset = 0.4f;

        public override void OnGhostLinked()
        {
            // Seed the replicated health on the server only. Clients receive it via snapshots.
            if (Role == MultiplayerRole.Server && !_serverInitialized)
            {
                WriteGhostComponentData(new EnemyData
                {
                    Health = _maxHealth,
                    MaxHealth = _maxHealth,
                    Type = (int)_type,
                    LastHitTick = 0
                });
                _serverInitialized = true;
            }

            // Client-side: cache renderers for the hit flash + register for the minimap.
            if (Role != MultiplayerRole.Server)
            {
                _renderers = GetComponentsInChildren<Renderer>(true);
                _mpb = new MaterialPropertyBlock();
                _lastSeenHitTick = ReadGhostComponentData<EnemyData>().LastHitTick;
                if (!ActiveEnemies.Contains(transform)) ActiveEnemies.Add(transform);
                CreateHealthBar();
            }
        }

        private void CreateHealthBar()
        {
            if (s_HpBgMat == null) s_HpBgMat = MakeUnlit(new Color(0.08f, 0.04f, 0.04f, 1f));
            if (s_HpFillMat == null) s_HpFillMat = MakeUnlit(new Color(0.3f, 0.9f, 0.42f, 1f));

            _hpBarRoot = new GameObject("HealthBar").transform;
            _hpBarRoot.SetParent(transform, false);

            float top = k_HpBarHeightOffset;
            var rends = GetComponentsInChildren<Renderer>();
            if (rends.Length > 0)
            {
                var b = rends[0].bounds;
                for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
                top = (b.max.y - transform.position.y) + k_HpBarHeightOffset;
            }
            _hpBarRoot.localPosition = new Vector3(0f, top, 0f);

            MakeQuad("HpBg", s_HpBgMat, _hpBarRoot, new Vector3(k_HpBarWidth, k_HpBarThickness, 1f), Vector3.zero);
            _hpFill = MakeQuad("HpFill", s_HpFillMat, _hpBarRoot,
                new Vector3(k_HpBarWidth, k_HpBarThickness * 0.72f, 1f), new Vector3(0f, 0f, -0.01f));
        }

        private static Transform MakeQuad(string name, Material mat, Transform parent, Vector3 scale, Vector3 localPos)
        {
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            q.name = name;
            Destroy(q.GetComponent<Collider>());
            q.GetComponent<Renderer>().sharedMaterial = mat;
            q.transform.SetParent(parent, false);
            q.transform.localScale = scale;
            q.transform.localPosition = localPos;
            return q.transform;
        }

        private static Material MakeUnlit(Color c)
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
            var m = new Material(shader);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Cull")) m.SetFloat("_Cull", 0f); // double-sided so billboarding is robust
            // Draw on top of world geometry so every enemy's bar is visible (not hidden behind walls/models).
            if (m.HasProperty("_ZTest")) m.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);
            m.renderQueue = 4000; // overlay
            m.color = c;
            return m;
        }

        private void UpdateHealthBar()
        {
            if (_hpBarRoot == null) return;
            if (Camera.main != null) _hpBarRoot.rotation = Camera.main.transform.rotation; // billboard

            var data = ReadGhostComponentData<EnemyData>();
            float frac = data.MaxHealth > 0f ? Mathf.Clamp01(data.Health / data.MaxHealth) : 0f;
            if (_hpFill != null)
            {
                _hpFill.localScale = new Vector3(k_HpBarWidth * frac, k_HpBarThickness * 0.72f, 1f);
                _hpFill.localPosition = new Vector3(-(k_HpBarWidth * (1f - frac)) * 0.5f, 0f, -0.01f);
            }
        }

        public override void OnGhostPreDestroy() => ActiveEnemies.Remove(transform);
        private void OnDestroy() => ActiveEnemies.Remove(transform);

        /// <summary>
        /// Called by weapon code (server-side) when the enemy is hit. Only reduces health here —
        /// the actual entity destruction happens in <see cref="UpdateServer"/> so DestroyEntity runs
        /// in a valid IUpdateServer context.
        /// </summary>
        public void ServerTakeDamage(float amount, int attackerNetworkId)
        {
            if (Role != MultiplayerRole.Server)
            {
                return;
            }

            if (!GhostGameObject.IsGhostLinked())
            {
                return;
            }

            var data = ReadGhostComponentData<EnemyData>();
            if (data.Health <= 0f)
            {
                return; // already dead, pending cleanup this tick
            }

            data.Health -= amount;
            data.LastHitTick = GhostGameObject.GetCurrentTick();
            WriteGhostComponentData(data);
            // Death/cleanup is handled in UpdateServer (needs a valid update ECB for DestroyEntity).
            // TODO(campaign): award score to attackerNetworkId and spawn a death VFX here.
        }

        public void UpdateServer(float deltaTime)
        {
            var data = ReadGhostComponentData<EnemyData>();
            if (data.Health <= 0f)
            {
                GhostGameObject.DestroyEntity();
                return;
            }

            if (_attackTimer > 0f)
            {
                _attackTimer -= deltaTime;
            }

            var target = ChaseAndGetTarget(deltaTime, out float distance);
            if (target != null && distance <= _attackRange && _attackTimer <= 0f)
            {
                AttackPlayer(target);
                _attackTimer = _attackCooldown;
            }
        }

        /// <summary>
        /// Moves the enemy toward the nearest player. Runs on BOTH server and client (like projectiles),
        /// so the on-screen enemy always moves without relying on transform replication.
        /// </summary>
        private GameObject ChaseAndGetTarget(float deltaTime, out float distance)
        {
            distance = float.MaxValue;

            // Re-scan only a few times a second; reuse the cached target otherwise.
            _retargetTimer -= deltaTime;
            if (_retargetTimer <= 0f || _cachedTarget == null)
            {
                _retargetTimer = k_RetargetInterval;
                _cachedTarget = FindNearestPlayer();
            }

            var target = _cachedTarget;
            if (target == null)
            {
                return null;
            }

            Vector3 toTarget = target.transform.position - transform.position;
            distance = toTarget.magnitude;

            Vector3 flatDir = new Vector3(toTarget.x, 0f, toTarget.z);
            if (flatDir.sqrMagnitude > 0.0001f)
            {
                Vector3 dir = flatDir.normalized;
                transform.rotation = Quaternion.LookRotation(dir);
                if (distance > _attackRange)
                {
                    transform.position += dir * (_moveSpeed * deltaTime);
                }
            }

            return target;
        }

        public void UpdateClient(float deltaTime)
        {
            // Move the on-screen enemy the same way the server does (both chase the local player),
            // so it visibly walks toward you instead of freezing at its spawn point.
            ChaseAndGetTarget(deltaTime, out _);

            UpdateHealthBar();

            // Flash the enemy white whenever it takes damage (server bumps LastHitTick).
            uint hitTick = ReadGhostComponentData<EnemyData>().LastHitTick;
            if (hitTick != _lastSeenHitTick)
            {
                _lastSeenHitTick = hitTick;
                if (hitTick != 0)
                {
                    _flashTimer = k_FlashDuration;
                    SetFlash(true);
                    if (_hitSfx != null && GameManager.Instance != null)
                    {
                        GameManager.Instance.SoundSystem.CreateEmitter(_hitSfx, transform);
                    }
                }
            }

            if (_flashTimer > 0f)
            {
                _flashTimer -= deltaTime;
                if (_flashTimer <= 0f)
                {
                    SetFlash(false);
                }
            }
        }

        private void SetFlash(bool on)
        {
            if (_renderers == null || _mpb == null)
            {
                return;
            }

            foreach (var r in _renderers)
            {
                if (r == null)
                {
                    continue;
                }

                if (on)
                {
                    r.GetPropertyBlock(_mpb);
                    _mpb.SetColor(s_BaseColorId, Color.white);
                    r.SetPropertyBlock(_mpb);
                }
                else
                {
                    r.SetPropertyBlock(null); // restore the material's own color
                }
            }
        }

        /// <summary>
        /// Finds the closest player via the PlayerGhostManager (works on both server and client, and
        /// doesn't depend on physics-layer collider setup). Uses the role-appropriate player list.
        /// </summary>
        private GameObject FindNearestPlayer()
        {
            var searchRole = Role == MultiplayerRole.Server
                ? MultiplayerRole.Server
                : MultiplayerRole.ClientAll;

            if (!PlayerGhostManager.TryGetInstanceByRole(searchRole, out var manager)
                || !manager.TryGetPlayersByRole(searchRole, out var players))
            {
                return null;
            }

            GameObject nearest = null;
            float nearestSqr = _detectionRange * _detectionRange;
            foreach (var player in players)
            {
                if (player == null)
                {
                    continue;
                }

                float sqr = (player.transform.position - transform.position).sqrMagnitude;
                if (sqr < nearestSqr)
                {
                    nearestSqr = sqr;
                    nearest = player.gameObject;
                }
            }

            return nearest;
        }

        /// <summary>Applies melee damage to a player's replicated health via the server ghost lookup.</summary>
        private void AttackPlayer(GameObject playerObject)
        {
            if (!GhostGameObject.TryFindGhostGameObject(playerObject, out var playerGhostObject))
            {
                return;
            }

            var world = GhostGameObject.World;
            var playerGhostLookup = world.GetExistingSystemManaged<ServerPlayerMovementSystem>()
                .GetComponentLookup<PredictedPlayerGhost>();

            if (!playerGhostLookup.HasComponent(playerGhostObject.LinkedEntity))
            {
                return;
            }

            var targetPlayer = playerGhostLookup.GetRefRW(playerGhostObject.LinkedEntity);
            if (targetPlayer.ValueRO.CurrentHealth <= 0f)
            {
                return;
            }

            targetPlayer.ValueRW.CurrentHealth -= _attackDamage;
            targetPlayer.ValueRW.ControllerState.IsHit = true;
            targetPlayer.ValueRW.LastDamageAmount = _attackDamage;
            targetPlayer.ValueRW.LastHitTick = GhostGameObject.GetCurrentTick();
        }
    }
}
