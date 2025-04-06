using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class MushroomAI : EnemyController
{
    [Header("Jump Settings")]
    [SerializeField] float jumpRange = 3f;
    [SerializeField] float jumpForce = 8f;
    [SerializeField] float jumpCooldown = 2f;

    [Header("Visual Settings")]
    [SerializeField] Renderer mushroomRenderer;

    protected override void Awake()
    {
        movementType = MovementType.NavMesh;
        attackType = AttackType.Melee;

        base.Awake();

        enemyRigidbody.isKinematic = false;
        enemyRigidbody.interpolation = RigidbodyInterpolation.Interpolate;

        if (enemyRenderer == null)
            enemyRenderer = mushroomRenderer;
    }

    protected override void InitializeStrategies()
    {
        movementStrategy = new JumpingMovement(
            moveSpeed,
            stoppingDistance,
            faceTarget,
            avoidObstacles,
            jumpRange,
            jumpForce,
            jumpCooldown,
            this
        );

        attackStrategy = new MeleeContactAttack(
            attackDamage,
            attackRate,
            attackRange
        );
    }

    protected override void Update()
    {
        base.Update();
        if (isDead) return;

        ((JumpingMovement)movementStrategy).CheckJump(enemyRigidbody, target);
    }

    protected override IEnumerator DeathSequence()
    {
        Deactivate();

        if (spawner != null && spawner is EnemySpawner enemySpawner)
            enemySpawner.EnemyEliminated(gameObject);

        yield return base.DeathSequence();
        Destroy(gameObject);
    }

    protected class JumpingMovement : NavMeshMovement
    {
        private float jumpRange;
        private float jumpForce;
        private float jumpCooldown;
        private bool canJump = true;
        private MonoBehaviour owner;

        public JumpingMovement(float speed, float stopDistance, bool faceTarget, bool avoidObstacles,
                             float jumpRange, float jumpForce, float jumpCooldown, MonoBehaviour owner)
            : base(speed, stopDistance, faceTarget, avoidObstacles)
        {
            this.jumpRange = jumpRange;
            this.jumpForce = jumpForce;
            this.jumpCooldown = jumpCooldown;
            this.owner = owner;
        }

        public void CheckJump(Rigidbody rb, Transform target)
        {
            if (canJump && GetDistanceToTarget() <= jumpRange)
                owner.StartCoroutine(PerformJump(rb, target));
        }

        private IEnumerator PerformJump(Rigidbody rb, Transform target)
        {
            canJump = false;
            if (Agent != null) Agent.isStopped = true;

            Vector3 jumpDirection = (target.position - rb.position).normalized;
            jumpDirection.y = 1f;
            rb.AddForce(jumpDirection * jumpForce, ForceMode.Impulse);

            yield return new WaitForSeconds(0.5f);
            if (Agent != null) Agent.isStopped = false;

            yield return new WaitForSeconds(jumpCooldown);
            canJump = true;
        }
    }

    protected class MeleeContactAttack : IEnemyAttack
    {
        private float damage;
        private float attackCooldown;
        private float attackRange;
        private float lastAttackTime;
        private float damageMultiplier = 1f;

        public MeleeContactAttack(float damage, float cooldown, float distance)
        {
            this.damage = damage;
            this.attackCooldown = cooldown;
            this.attackRange = distance;
        }

        public void Initialize(Transform enemy, Transform target) { }
        public void SetTarget(Transform target) { }

        public bool CanAttack() => Time.time > lastAttackTime + attackCooldown;
        public void Attack() => lastAttackTime = Time.time;
        public void SetDamageMultiplier(float multiplier) => damageMultiplier = multiplier;
    }
}