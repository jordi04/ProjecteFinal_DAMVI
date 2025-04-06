using UnityEngine;
using UnityEngine.AI;

public class GoblinRangedAI : EnemyController
{
    [Header("Ranged Attack Configuration")]
    [SerializeField] private GameObject rockPrefab;
    [SerializeField] private Transform throwPoint;
    [SerializeField] private float arcHeight = 2f;

    protected override void Awake()
    {
        movementType = MovementType.NavMesh;
        attackType = AttackType.Ranged;

        base.Awake();

        enemyRigidbody.isKinematic = true;
        enemyRigidbody.useGravity = false;
    }

    protected override void InitializeStrategies()
    {
        movementStrategy = new NavMeshMovement(
            moveSpeed,
            stoppingDistance,
            faceTarget,
            avoidObstacles
        );

        attackStrategy = new LobbedProjectileAttack(
            rockPrefab, // projectilePrefab
            new[] { throwPoint }, // shootPoints
            projectileSpeed,
            attackDamage,
            attackRate,
            attackRange,
            arcHeight,
            projectileSpread,
            projectileLifetime
        );

        base.InitializeStrategies();
    }

    protected class LobbedProjectileAttack : RangedAttack
    {
        private float arcHeight;

        public LobbedProjectileAttack(
            GameObject projectilePrefab,
            Transform[] shootPoints,
            float projectileSpeed,
            float damage,
            float attackRate,
            float attackRange,
            float arcHeight,
            float projectileSpread,
            float projectileLifetime)
            : base(
                projectilePrefab,
                shootPoints,
                projectileSpeed,
                damage,
                attackRate,
                attackRange,
                false, // burstFire
                1, // burstCount
                0, // burstDelay
                false, // useRandomShootPoint
                projectileSpread,
                projectileLifetime)
        {
            this.arcHeight = arcHeight;
        }

        protected override void FireSingleShot()
        {
            Transform shootPoint = GetShootPoint();
            if (shootPoint == null || target == null) return;

            Vector3 targetPosition = target.position;
            Vector3 startPosition = shootPoint.position;

            GameObject projectile = Instantiate(
                projectilePrefab,
                startPosition,
                Quaternion.identity
            );

            RockProjectile rock = projectile.GetComponent<RockProjectile>();
            if (rock != null)
            {
                rock.InitializeLobbedTrajectory(
                    startPosition,
                    targetPosition,
                    arcHeight,
                    projectileSpeed
                );
                rock.SetDamage(damage);
                rock.SetOwner(enemy.gameObject);
            }

            if (projectileLifetime > 0)
            {
                Destroy(projectile, projectileLifetime);
            }
        }
    }
}