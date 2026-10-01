using System.Collections.Generic;
using System;
using Gameplay.Leaderboard;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace Unity.MP_FPS
{
    public class Projectile : GhostMonoBehaviour, IUpdateServer, IUpdateClient
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            PredictedProjectiles = new List<PredictedProjectileInfo>();
        }

        public static List<PredictedProjectileInfo> PredictedProjectiles = new List<PredictedProjectileInfo>();
        private int _hitLayerMask;

        // Holds the projectile's networked state
        public struct ProjectileData : IComponentData
        {
            [GhostField] public int OwnerNetworkId;
            [GhostField] public uint SpawnTick;
            [GhostField] public uint WeaponID;
        }

        public class PredictedProjectileInfo
        {
            public GameObject Instance;
            public uint SpawnTick;
        }

        private void Awake()
        {
            // PvE: projectiles hit virus enemies and geometry — never other players.
            _hitLayerMask = LayerMask.GetMask("ServerEnemy", "Ground", "Default");
        }

        private void Update()
        {
            if (GhostGameObject == null || !GhostGameObject.IsGhostLinked())
            {
                var weaponData = WeaponManager.Instance.WeaponRegistry.GetWeaponData(_weaponId);
                Move(Time.deltaTime, weaponData.ProjectileSpeed);
            }
        }

        public void SetWeaponId(uint weaponId)
        {
            _weaponId = weaponId;
        }

        private float _localTime;
        private uint _weaponId;

        public override void OnGhostLinked()
        {
            var projectileData = GhostGameObject.ReadGhostComponentData<ProjectileData>();
            _weaponId = projectileData.WeaponID;
        }

        public void UpdateServer(float deltaTime)
        {
            var weaponData = WeaponManager.Instance.WeaponRegistry.GetWeaponData(_weaponId);
            Move(deltaTime, weaponData.ProjectileSpeed);

            _localTime += deltaTime;
            if (_localTime > 5f)
            {
                GhostGameObject.DestroyEntity();
                return;
            }

            CheckForCollision(weaponData, deltaTime);
        }

        private void Move(float deltaTime, float speed)
        {
            transform.position += transform.forward * (speed * deltaTime);
        }

        public void UpdateClient(float deltaTime)
        {
            var weaponData = WeaponManager.Instance.WeaponRegistry.GetWeaponData(_weaponId);
            Move(deltaTime, weaponData.ProjectileSpeed);
        }

        private void CheckForCollision(WeaponData weaponData, float deltaTime)
        {
            float projectileRadius = 0.2f;
            float distanceThisFrame = weaponData.ProjectileSpeed * deltaTime;

            if (UnityEngine.Physics.SphereCast(transform.position, projectileRadius, transform.forward,
                    out RaycastHit hitInfo, distanceThisFrame, _hitLayerMask, QueryTriggerInteraction.Ignore))
            {
                GameObject hitObject = hitInfo.collider.gameObject;
                var projectileData = GhostGameObject.ReadGhostComponentData<ProjectileData>();

                // PvE: projectiles damage virus enemies; everything else is treated as geometry.
                if (hitObject.layer == LayerMask.NameToLayer("ServerEnemy"))
                {
                    HandleEnemyImpact(hitObject, hitInfo.point, weaponData, projectileData);
                }
                else
                {
                    HandleGeometryImpact(hitInfo.point);
                }

                if (weaponData.ProjectileHitVfxPrefab != null && weaponData.ProjectileHitVfxPrefab.GhostGuid.IsValid)
                {
                    GhostSpawner.SpawnGhostPrefab(weaponData.ProjectileHitVfxPrefab, hitInfo.point,
                        Quaternion.LookRotation(hitInfo.normal), GhostGameObject.GenerateRandomHash());
                }
            }
        }

        private void HandleGeometryImpact(Vector3 impactPosition)
        {
            DrawGizmoAtPosition(impactPosition);
            GhostGameObject.DestroyEntity();
        }

        private void HandleEnemyImpact(GameObject hitObject, Vector3 impactPosition, WeaponData weaponData,
            ProjectileData projectileData)
        {
            int shooterNetworkId = projectileData.OwnerNetworkId;

            if (weaponData.Behavior == ProjectileBehavior.AreaOfEffect)
            {
                // Splash: damage every enemy within the blast radius.
                var enemiesInRadius = UnityEngine.Physics.OverlapSphere(impactPosition, weaponData.AoeRadius,
                    LayerMask.GetMask("ServerEnemy"), QueryTriggerInteraction.Ignore);
                foreach (var enemyCollider in enemiesInRadius)
                {
                    DamageEnemy(enemyCollider.gameObject, weaponData.Damage, shooterNetworkId);
                }
            }
            else // DirectDamage — just the enemy we hit.
            {
                DamageEnemy(hitObject, weaponData.Damage, shooterNetworkId);
            }

            // Destroy the projectile once (whether it hit one enemy or several).
            GhostGameObject.DestroyEntity();
        }

        private void DamageEnemy(GameObject enemyObject, float damage, int shooterNetworkId)
        {
            if (GhostGameObject.TryFindGhostGameObject(enemyObject, out var enemyGhostObject))
            {
                var enemy = enemyGhostObject.GetComponentInChildren<VirusEnemy>();
                if (enemy != null)
                {
                    enemy.ServerTakeDamage(damage, shooterNetworkId);
                }
            }
        }

        private static void DrawGizmoAtPosition(Vector3 position)
        {
            var color = Color.yellow;
            const float duration = 4.0f; // How long the gizmo will be visible in seconds
            const float size = 0.25f; // The length of the lines for the cross marker

            Debug.DrawRay(position - Vector3.up * size, Vector3.up * size * 2, color, duration);
            Debug.DrawRay(position - Vector3.right * size, Vector3.right * size * 2, color, duration);
            Debug.DrawRay(position - Vector3.forward * size, Vector3.forward * size * 2, color, duration);
        }
    }
}