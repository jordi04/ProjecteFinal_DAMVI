using System.Collections;
using UnityEngine;
using static EnemyController;

public class BossGolemEnemyController : EnemyController
{
    [Header("Boss Golem Settings")]
    [SerializeField] private float rockThrowCooldown = 5f;
    [SerializeField] private float enrageHealthThreshold = 0.3f; // 30% health
    [SerializeField] private float enragedDamageMultiplier = 1.5f;
    [SerializeField] private float enragedSpeedMultiplier = 1.3f;
    [SerializeField] private GameObject enrageEffect;
    [SerializeField] private float specialAttackCooldown = 15f;

    private bool isEnraged = false;
    private float lastRockThrowTime = -100f;
    private float lastSpecialAttackTime = -100f;
    private bool isPerformingSpecialAttack = false;

    protected override void Awake()
    {
        base.Awake();
        // Additional initialization for Boss Golem
    }

    protected override void Update()
    {
        base.Update();

        // Check for enrage state
        if (!isEnraged && currentHealth <= maxHealth * enrageHealthThreshold)
        {
            EnterEnragedState();
        }

        // Update animation states based on current behavior
        UpdateAnimationState();
    }

    protected override void InitializeStrategies()
    {
        // Override to setup both ranged and melee attacks
        if (targetPoint == null)
        {
            targetPoint = GameObject.FindGameObjectWithTag("Player")?.transform;
        }

        // Setup movement strategy as NavMesh for boss
        movementStrategy = CreateMovementStrategy();

        if (movementStrategy != null && targetPoint != null)
        {
            movementStrategy.Initialize(transform, targetPoint);
        }

        // Setup both melee and ranged attacks
        attackStrategy = new BossGolemAttack(targetPoint, this);

        if (navAgent != null)
        {
            navAgent.speed = moveSpeed;
            navAgent.stoppingDistance = stoppingDistance;
            navAgent.angularSpeed = rotationSpeed * 100;
            navAgent.isStopped = false;
        }
    }

    protected override IEnemyMovement CreateMovementStrategy()
    {
        // Create a specialized movement strategy for the golem boss
        return new GolemMovementStrategy(
            moveSpeed,
            stoppingDistance,
            faceTarget,
            avoidObstacles,
            detectionRange,
            meleeRadius,
            attackRange
        );
    }

    protected override void HandleAttack()
    {
        if (isDead || isTakingDamage || attackStrategy == null || target == null) return;

        float distanceToTarget = Vector3.Distance(transform.position, target.position);

        // Debug distance information
        Debug.Log($"Golem distance to target: {distanceToTarget}, Melee radius: {meleeRadius}, Attack range: {attackRange}");

        // Handle special attacks based on state and cooldowns
        if (isEnraged && Time.time > lastSpecialAttackTime + specialAttackCooldown)
        {
            PerformSpecialAttack();
            return;
        }

        // If in melee range, prioritize melee attack
        if (distanceToTarget <= meleeRadius && Time.time > nextAttackTime)
        {
            if (Random.Range(0f, 100f) < chanceToAttack)
            {
                Debug.Log("Performing melee attack");
                PerformMeleeAttack();
            }
            nextAttackTime = Time.time + 1f / attackRate;
        }
        // If outside melee range but within attack range, use ranged attack
        else if (distanceToTarget <= attackRange && distanceToTarget > meleeRadius &&
                Time.time > lastRockThrowTime + rockThrowCooldown)
        {
            Debug.Log("Performing ranged attack");
            PerformRockThrow();
        }
    }

    protected override void PerformAttack()
    {
        if (isDead || isAttacking) return;

        isAttacking = true;

        if (animator != null)
        {
            // Decide which attack animation to play based on distance
            float distance = movementStrategy.GetDistanceToTarget();
            if (distance <= meleeRadius)
            {
                animator.SetBool("CloseAttack", true);
                animator.SetBool("RangeAttack", false);
            }
            else
            {
                animator.SetBool("CloseAttack", false);
                animator.SetBool("RangeAttack", true);
                lastRockThrowTime = Time.time;
            }
        }

        PlayAttackSound();

        if (attackChargeEffect != null)
            StartCoroutine(ShowAttackEffect());

        if (attackDelay > 0)
        {
            attackCoroutine = StartCoroutine(DelayedAttack());
        }
        else
        {
            if (attackStrategy != null && attackStrategy.CanAttack())
                attackStrategy.Attack();
            isAttacking = false;
        }
    }

    private void PerformMeleeAttack()
    {
        if (isDead || isAttacking) return;

        isAttacking = true;

        if (animator != null)
        {
            animator.SetBool("CloseAttack", true);
            animator.SetBool("RangeAttack", false);
        }

        PlayAttackSound();

        if (attackChargeEffect != null)
            StartCoroutine(ShowAttackEffect());

        if (attackDelay > 0)
        {
            attackCoroutine = StartCoroutine(DelayedMeleeAttack());
        }
        else
        {
            if (attackStrategy != null && attackStrategy is BossGolemAttack bossAttack)
            {
                bossAttack.MeleeAttackOnly();
            }
            isAttacking = false;
        }
    }

    private void PerformRockThrow()
    {
        if (isDead || isAttacking) return;

        isAttacking = true;
        lastRockThrowTime = Time.time;

        if (animator != null)
        {
            animator.SetBool("CloseAttack", false);
            animator.SetBool("RangeAttack", true);
        }

        PlayAttackSound();

        if (attackDelay > 0)
        {
            attackCoroutine = StartCoroutine(DelayedRangedAttack());
        }
        else
        {
            if (attackStrategy != null && attackStrategy is BossGolemAttack bossAttack)
            {
                bossAttack.RangedAttackOnly();
            }
            isAttacking = false;
        }
    }

    private void PerformSpecialAttack()
    {
        if (isDead || isAttacking) return;

        isAttacking = true;
        isPerformingSpecialAttack = true;
        lastSpecialAttackTime = Time.time;

        // Special attack animation and logic
        StartCoroutine(SpecialAttackSequence());
    }

    private IEnumerator DelayedMeleeAttack()
    {
        yield return new WaitForSeconds(attackDelay);

        if (!isDead && attackStrategy != null && attackStrategy is BossGolemAttack bossAttack)
        {
            bossAttack.MeleeAttackOnly();
        }

        isAttacking = false;

        if (animator != null)
        {
            animator.SetBool("CloseAttack", false);
        }
    }

    private IEnumerator SpecialAttackSequence()
    {
        // Stop movement during special attack
        if (navAgent != null)
            navAgent.isStopped = true;

        // Play special attack animation/effects
        if (animator != null)
        {
            animator.SetBool("CloseAttack", true);
            animator.SetBool("RangeAttack", true); // Both true for special attack
        }

        yield return new WaitForSeconds(1.5f); // Wind-up time

        // Perform area damage around the boss
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, attackRange, attackableLayerMask);
        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.transform == transform) continue;

            IDamageable damageable = hitCollider.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(attackDamage * 2 * damageMultiplier);
            }

            // Add force to push away
            Rigidbody rb = hitCollider.GetComponent<Rigidbody>();
            if (rb != null && !rb.isKinematic)
            {
                Vector3 direction = (hitCollider.transform.position - transform.position).normalized;
                rb.AddForce(direction * meleeForce * 2 + Vector3.up * meleeUpwardForce, ForceMode.Impulse);
            }
        }

        yield return new WaitForSeconds(1f); // Recovery time

        // Resume normal behavior
        if (navAgent != null)
            navAgent.isStopped = false;

        isAttacking = false;
        isPerformingSpecialAttack = false;

        // Reset animation bools
        if (animator != null)
        {
            animator.SetBool("CloseAttack", false);
            animator.SetBool("RangeAttack", false);
        }
    }

    protected override IEnumerator DelayedAttack()
    {
        yield return new WaitForSeconds(attackDelay);

        if (!isDead && attackStrategy != null && attackStrategy.CanAttack())
            attackStrategy.Attack();

        isAttacking = false;
    }

    private IEnumerator DelayedRangedAttack()
    {
        yield return new WaitForSeconds(attackDelay);

        if (!isDead && attackStrategy != null && attackStrategy is BossGolemAttack bossAttack)
        {
            bossAttack.RangedAttackOnly();
        }

        isAttacking = false;
    }

    private void EnterEnragedState()
    {
        isEnraged = true;
        damageMultiplier *= enragedDamageMultiplier;

        if (navAgent != null)
        {
            navAgent.speed = moveSpeed * enragedSpeedMultiplier;
        }

        if (enrageEffect != null)
        {
            enrageEffect.SetActive(true);
        }

        // Update attack strategy damage multiplier
        if (attackStrategy != null)
        {
            attackStrategy.SetDamageMultiplier(damageMultiplier);
        }
    }

    private void UpdateAnimationState()
    {
        if (animator == null) return;

        if (isDead)
        {
            animator.SetBool("Death", true);
            animator.SetBool("Idle", false);
            animator.SetBool("LoopRunning", false);
            animator.SetBool("FullRun", false);
            return;
        }

        float distanceToTarget = target != null ? movementStrategy.GetDistanceToTarget() : float.MaxValue;

        // Check if navAgent is valid before using it
        bool isMoving = false;
        if (navAgent != null && navAgent.enabled && navAgent.isOnNavMesh)
        {
            isMoving = !navAgent.isStopped && navAgent.velocity.magnitude > 0.1f;
        }

        // Set running animations based on speed and state
        if (isMoving)
        {
            animator.SetBool("Idle", false);

            // Full run when enraged or far from target
            if (isEnraged || distanceToTarget > attackRange * 1.5f)
            {
                animator.SetBool("FullRun", true);
                animator.SetBool("LoopRunning", false);
            }
            else
            {
                animator.SetBool("LoopRunning", true);
                animator.SetBool("FullRun", false);
            }
        }
        else
        {
            animator.SetBool("Idle", true);
            animator.SetBool("LoopRunning", false);
            animator.SetBool("FullRun", false);
        }
    }


    protected override void Die()
    {
        if (isDead) return;

        // Set death animation
        if (animator != null)
        {
            animator.SetBool("Death", true);
            animator.SetBool("CloseAttack", false);
            animator.SetBool("RangeAttack", false);
            animator.SetBool("LoopRunning", false);
            animator.SetBool("FullRun", false);
            animator.SetBool("Idle", false);
        }

        base.Die();
    }
}

// Custom movement strategy for the Golem Boss
public class GolemMovementStrategy : NavMeshMovement
{
    private float detectionRange;
    private float meleeRange;
    private float attackRange;
    private bool isChasing = false;

    public GolemMovementStrategy(float speed, float stopDistance, bool faceTarget, bool avoidObstacles,
                               float detectionRange, float meleeRange, float attackRange)
        : base(speed, stopDistance, faceTarget, avoidObstacles)
    {
        this.detectionRange = detectionRange;
        this.meleeRange = meleeRange;
        this.attackRange = attackRange;
    }

    public override void Move()
    {
        if (navAgent == null || !navAgent.enabled || !navAgent.isOnNavMesh || targetTransform == null) return;

        float distanceToTarget = Vector3.Distance(enemyTransform.position, targetTransform.position);

        // If player is within detection range, start chasing
        if (distanceToTarget <= detectionRange)
        {
            isChasing = true;
        }
        // If player moves too far away, stop chasing
        else if (distanceToTarget > detectionRange * 1.5f)
        {
            isChasing = false;
            if (navAgent.isOnNavMesh)
                navAgent.isStopped = true;
            return;
        }

        if (isChasing)
        {
            // If within melee range, stop closer to the target
            if (distanceToTarget <= meleeRange * 1.2f)
            {
                navAgent.stoppingDistance = meleeRange * 0.8f;
            }
            // If within attack range but outside melee range, stop at a medium distance
            else if (distanceToTarget <= attackRange)
            {
                navAgent.stoppingDistance = attackRange * 0.7f;
            }
            // If outside attack range, use a smaller stopping distance to get closer
            else
            {
                navAgent.stoppingDistance = stoppingDistance;
            }

            // Set destination to target position
            navAgent.SetDestination(targetTransform.position);
            navAgent.isStopped = false;
        }
    }
}

// Custom attack class for Boss Golem
public class BossGolemAttack : IEnemyAttack
{
    private Transform target;
    private EnemyController owner;
    private float lastAttackTime = -100f;

    private MeleeAttack meleeAttack;
    private RangedAttack rangedAttack;

    public BossGolemAttack(Transform target, EnemyController owner)
    {
        this.target = target;
        this.owner = owner;

        meleeAttack = new MeleeAttack(
            owner.GetAttackDamage(),
            owner.GetAttackRate(),
            owner.GetAttackRange(),
            owner.GetMeleeRadius(),
            owner.GetAttackAngle(),
            owner.GetAttackableLayerMask(),
            owner.GetRequireLineOfSight(),
            owner.GetUsePhysicsForMelee(),
            owner.GetMeleeForce(),
            owner.GetMeleeUpwardForce(),
            owner.attackOrigin
        );

        rangedAttack = new RangedAttack(
            owner.GetProjectilePrefab(),
            owner.GetShootPoints(),
            owner.GetProjectileSpeed(),
            owner.GetAttackDamage(),
            owner.GetAttackRate(),
            owner.GetAttackRange(),
            owner.GetBurstFire(),
            owner.GetBurstCount(),
            owner.GetBurstDelay(),
            owner.GetUseRandomShootPoint(),
            owner.GetProjectileSpread(),
            owner.GetProjectileLifetime()
        );

        meleeAttack.Initialize(owner.transform, target);
        rangedAttack.Initialize(owner.transform, target);
    }

    public void Initialize(Transform enemy, Transform target)
    {
        this.target = target;
        meleeAttack.Initialize(enemy, target);
        rangedAttack.Initialize(enemy, target);
    }

    public void SetTarget(Transform target)
    {
        this.target = target;
        meleeAttack.SetTarget(target);
        rangedAttack.SetTarget(target);
    }

    public bool CanAttack()
    {
        // Boss can attack if either melee or ranged can attack
        return meleeAttack.CanAttack() || rangedAttack.CanAttack();
    }

    public void Attack()
    {
        float distance = Vector3.Distance(owner.transform.position, target.position);
        if (distance <= owner.GetMeleeRadius())
        {
            if (meleeAttack.CanAttack())
                meleeAttack.Attack();
        }
        else
        {
            if (rangedAttack.CanAttack())
                rangedAttack.Attack();
        }
    }

    public void MeleeAttackOnly()
    {
        if (meleeAttack.CanAttack())
            meleeAttack.Attack();
    }

    public void RangedAttackOnly()
    {
        if (rangedAttack.CanAttack())
            rangedAttack.Attack();
    }

    public void SetDamageMultiplier(float multiplier)
    {
        meleeAttack.SetDamageMultiplier(multiplier);
        rangedAttack.SetDamageMultiplier(multiplier);
    }
}
