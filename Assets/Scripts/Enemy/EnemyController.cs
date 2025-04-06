using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.AI;

public interface IDamageable
{
    void TakeDamage(float damageAmount);
    float GetCurrentHealth();
    float GetMaxHealth();
    bool IsDead();
}

public interface IEnemyMovement
{
    void Initialize(Transform enemy, Transform target);
    void Move();
    void SetTarget(Transform target);
    void Stop();
    void Resume();
    bool IsInRange(float range);
    float GetDistanceToTarget();
}

public interface IEnemyAttack
{
    void Initialize(Transform enemy, Transform target);
    void Attack();
    void SetTarget(Transform target);
    bool CanAttack();
    void SetDamageMultiplier(float multiplier);
}

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyController : MonoBehaviour, IDamageable
{
    [System.Serializable]
    public enum AttackType
    {
        None,
        Melee,
        Ranged,
        Special
    }

    [System.Serializable]
    public enum MovementType
    {
        None,
        NavMesh,
        Direct,
        Patrol,
        Stationary
    }

    [Header("Core Settings")]
    [SerializeField] protected GameObject enemyRoot;
    [SerializeField] protected Transform targetPoint;
    [SerializeField] protected Renderer enemyRenderer;

    [Header("Health & Damage")]
    [SerializeField] protected float maxHealth = 100f;
    [SerializeField] protected float currentHealth;
    [SerializeField] protected float damageMultiplier = 1f;
    [SerializeField] protected float flashDuration = 0.2f;
    [SerializeField] protected bool invulnerableDuringFlash = false;
    [SerializeField] protected List<string> damageSourceTags = new List<string> { "FireBall", "Weapon" };

    [Header("Death Settings")]
    [SerializeField] protected float destroyDelay = 3f;
    [SerializeField] protected GameObject deathEffect;
    [SerializeField] protected bool disableColliderOnDeath = true;
    [SerializeField] protected bool disableRendererOnDeath = false;

    [Header("Movement Configuration")]
    [SerializeField] protected MovementType movementType = MovementType.NavMesh;
    [SerializeField] protected float moveSpeed = 3.5f;
    [SerializeField] protected float rotationSpeed = 5f;
    [SerializeField] protected float stoppingDistance = 2f;
    [SerializeField] protected bool faceTarget = true;
    [SerializeField] protected bool avoidObstacles = true;
    [SerializeField] protected Transform[] patrolPoints;
    [SerializeField] protected float patrolWaitTime = 1f;

    [Header("Attack Configuration")]
    [SerializeField] protected AttackType attackType = AttackType.Ranged;
    [SerializeField] protected float attackRange = 10f;
    [SerializeField] protected float attackRate = 1f;
    [SerializeField] protected float attackDamage = 10f;
    [SerializeField] protected float chanceToAttack = 100f;
    [SerializeField] protected float attackAngle = 45f;
    [SerializeField] protected Transform attackOrigin;
    [SerializeField] protected LayerMask attackableLayerMask;
    [SerializeField] protected bool requireLineOfSight = true;
    [SerializeField] protected float attackDelay = 0.2f;
    [SerializeField] protected bool canAttackWhileMoving = false;

    [Header("Ranged Attack Settings")]
    [SerializeField] protected GameObject projectilePrefab;
    [SerializeField] protected float projectileSpeed = 10f;
    [SerializeField] protected float projectileLifetime = 5f;
    [SerializeField] protected Transform[] shootPoints;
    [SerializeField] protected bool useRandomShootPoint = false;
    [SerializeField] protected bool burstFire = false;
    [SerializeField] protected int burstCount = 3;
    [SerializeField] protected float burstDelay = 0.1f;
    [SerializeField] protected float projectileSpread = 5f;

    [Header("Melee Attack Settings")]
    [SerializeField] protected float meleeRadius = 1.5f;
    [SerializeField] protected bool usePhysicsForMelee = false;
    [SerializeField] protected float meleeForce = 5f;
    [SerializeField] protected float meleeUpwardForce = 0f;
    [SerializeField] protected GameObject meleeEffectPrefab;

    [Header("Animation Settings")]
    [SerializeField] protected Animator animator;
    [SerializeField] protected string idleAnimTrigger = "Idle";
    [SerializeField] protected string moveAnimTrigger = "Move";
    [SerializeField] protected string attackAnimTrigger = "Attack";
    [SerializeField] protected string damageAnimTrigger = "Damage";
    [SerializeField] protected string deathAnimTrigger = "Death";
    [SerializeField] protected bool useAnimatorSpeed = true;

    [Header("Audio Settings")]
    [SerializeField] protected AudioSource audioSource;
    [SerializeField] protected AudioClip[] attackSounds;
    [SerializeField] protected AudioClip[] damageSounds;
    [SerializeField] protected AudioClip[] deathSounds;
    [SerializeField] protected AudioClip[] idleSounds;
    [SerializeField] protected float idleSoundInterval = 5f;
    [SerializeField] protected float idleSoundChance = 20f;

    [Header("Visual Effects")]
    [SerializeField] protected Color damageFlashColor = Color.red;
    [SerializeField] protected GameObject attackChargeEffect;
    [SerializeField] protected ParticleSystem moveParticles;

    [Header("AI Settings")]
    [SerializeField] protected float detectionRange = 20f;
    [SerializeField] protected bool activateOnPlayerDetection = true;
    [SerializeField] protected float hearingRange = 10f;
    [SerializeField] protected LayerMask obstacleLayerMask;
    [SerializeField] protected bool shouldRetreat = false;
    [SerializeField] protected float retreatHealthThreshold = 25f;
    [SerializeField] protected float retreatDistance = 5f;

    protected Collider enemyCollider;
    protected Rigidbody enemyRigidbody;
    protected NavMeshAgent navAgent;
    protected MaterialPropertyBlock materialPropertyBlock;

    protected bool isDead = false;
    protected bool isAttacking = false;
    protected bool isMoving = true;
    protected bool isActivated = true;
    protected bool isTakingDamage = false;
    protected bool isRetreating = false;
    protected bool isInvulnerable = false;
    protected Transform target;
    protected Color originalColor;
    protected float nextAttackTime = 0f;
    protected float nextIdleSoundTime = 0f;
    protected IEnemyMovement movementStrategy;
    protected IEnemyAttack attackStrategy;
    protected Coroutine attackCoroutine;
    protected Coroutine meleeAttackCoroutine;
    protected Coroutine retreatCoroutine;

    public EnemySpawner spawner;

    #region Unity Lifecycle Methods
    protected virtual void Awake()
    {
        InitializeComponents();
        currentHealth = maxHealth;
    }

    protected virtual void Start()
    {
        InitializeStrategies();
        if (targetPoint != null)
        {
            SetTarget(targetPoint);
        }
        nextIdleSoundTime = Time.time + Random.Range(0f, idleSoundInterval);
    }

    protected virtual void Update()
    {
        if (isDead || !isActivated) return;

        HandleMovement();
        HandleAttack();
        HandleIdleSounds();
    }

    protected virtual void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        if (attackType == AttackType.Melee)
        {
            Gizmos.color = Color.blue;
            Vector3 meleeOrigin = attackOrigin != null ? attackOrigin.position : transform.position;
            Gizmos.DrawWireSphere(meleeOrigin, meleeRadius);
        }

        if (movementType == MovementType.Patrol && patrolPoints != null && patrolPoints.Length > 0)
        {
            Gizmos.color = Color.green;
            for (int i = 0; i < patrolPoints.Length; i++)
            {
                if (patrolPoints[i] != null)
                {
                    Gizmos.DrawSphere(patrolPoints[i].position, 0.3f);
                    if (i < patrolPoints.Length - 1 && patrolPoints[i + 1] != null)
                    {
                        Gizmos.DrawLine(patrolPoints[i].position, patrolPoints[i + 1].position);
                    }
                    else if (patrolPoints[0] != null)
                    {
                        Gizmos.DrawLine(patrolPoints[i].position, patrolPoints[0].position);
                    }
                }
            }
        }
    }

    protected virtual void OnDestroy()
    {
        if (spawner != null)
        {
            spawner.EnemyEliminated(gameObject);
        }
    }
    #endregion

    #region Initialization
    protected virtual void InitializeComponents()
    {
        if (enemyRoot == null)
            enemyRoot = gameObject;

        enemyCollider = GetComponent<Collider>();
        enemyRigidbody = GetComponent<Rigidbody>();
        navAgent = GetComponent<NavMeshAgent>();

        if (enemyRenderer == null)
            enemyRenderer = GetComponentInChildren<Renderer>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (attackOrigin == null)
            attackOrigin = transform;

        materialPropertyBlock = new MaterialPropertyBlock();

        if (enemyRenderer != null)
            originalColor = enemyRenderer.material.color;
    }

    protected virtual void InitializeStrategies()
    {
        movementStrategy = CreateMovementStrategy();

        if (targetPoint == null)
        {
            targetPoint = GameObject.FindGameObjectWithTag("Player")?.transform;
        }

        movementStrategy = CreateMovementStrategy();

        if (movementStrategy != null && targetPoint != null)
        {
            movementStrategy.Initialize(transform, targetPoint);
        }
        else
        {
            Debug.LogError("Fallo al inicializar movimiento");
        }

        attackStrategy = CreateAttackStrategy();
        if (attackStrategy != null && targetPoint != null)
        {
            attackStrategy.Initialize(transform, targetPoint);
            attackStrategy.SetDamageMultiplier(damageMultiplier);
        }

        if (navAgent != null)
        {
            navAgent.speed = moveSpeed;
            navAgent.stoppingDistance = stoppingDistance;
            navAgent.angularSpeed = rotationSpeed * 100;
        }
    }

    protected virtual IEnemyMovement CreateMovementStrategy()
    {
        switch (movementType)
        {
            case MovementType.NavMesh:
                return new NavMeshMovement(moveSpeed, stoppingDistance, faceTarget, avoidObstacles);
            case MovementType.Direct:
                return new DirectMovement(moveSpeed, stoppingDistance, rotationSpeed, faceTarget);
            case MovementType.Patrol:
                return new PatrolMovement(moveSpeed, stoppingDistance, patrolPoints, patrolWaitTime, rotationSpeed);
            default:
                return null;
        }
    }

    protected virtual IEnemyAttack CreateAttackStrategy()
    {
        switch (attackType)
        {
            case AttackType.Melee:
                return new MeleeAttack(
                    attackDamage,
                    attackRate,
                    attackRange,
                    meleeRadius,
                    attackAngle,
                    attackableLayerMask,
                    requireLineOfSight,
                    usePhysicsForMelee,
                    meleeForce,
                    meleeUpwardForce
                );
            case AttackType.Ranged:
                return new RangedAttack(
                    projectilePrefab,
                    shootPoints,
                    projectileSpeed,
                    attackDamage,
                    attackRate,
                    attackRange,
                    burstFire,
                    burstCount,
                    burstDelay,
                    useRandomShootPoint,
                    projectileSpread,
                    projectileLifetime
                );
            case AttackType.Special:
                return null;
            default:
                return null;
        }
    }
    #endregion

    #region Public Methods
    public virtual void SetTarget(Transform newTarget)
    {
        if (newTarget == null)
        {
            Debug.LogWarning("Se intentó asignar un target nulo");
            return;
        }

        target = newTarget;
        targetPoint = newTarget;

        if (movementStrategy != null)
        {
            movementStrategy.SetTarget(newTarget);
            movementStrategy.Initialize(transform, newTarget);
        }

        if (attackStrategy != null)
        {
            attackStrategy.SetTarget(newTarget);
        }

    public virtual void TakeDamage(float damageAmount)
    {
        if (isDead || isInvulnerable) return;

        currentHealth -= damageAmount;
        isTakingDamage = true;

        StartCoroutine(DamageFlash());
        PlayRandomSound(damageSounds);

        if (animator != null)
            animator.SetTrigger(damageAnimTrigger);

        if (shouldRetreat && currentHealth <= maxHealth * (retreatHealthThreshold / 100) && !isRetreating)
        {
            StartRetreat();
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public float GetCurrentHealth() => currentHealth;
    public float GetMaxHealth() => maxHealth;
    public bool IsDead() => isDead;

    public virtual void Activate() => isActivated = true;
    public virtual void Deactivate() => isActivated = false;

    public virtual void ForceAttack()
    {
        if (!isDead && attackStrategy != null)
        {
            PerformAttack();
        }
    }
    #endregion

    #region Protected Methods
    protected virtual void HandleMovement()
    {
        if (isDead || isTakingDamage || (isAttacking && !canAttackWhileMoving)) return;

        if (movementStrategy != null && isMoving)
        {
            movementStrategy.Move();

            if (animator != null && movementStrategy.GetDistanceToTarget() > stoppingDistance)
            {
                animator.SetTrigger(moveAnimTrigger);
                if (useAnimatorSpeed && animator.GetFloat("Speed") != moveSpeed / 5f)
                    animator.SetFloat("Speed", moveSpeed / 5f);
            }
            else if (animator != null)
            {
                animator.SetTrigger(idleAnimTrigger);
                if (useAnimatorSpeed)
                    animator.SetFloat("Speed", 0);
            }

            if (moveParticles != null)
            {
                if (movementStrategy.GetDistanceToTarget() > stoppingDistance)
                    moveParticles.Play();
                else
                    moveParticles.Stop();
            }
        }
    }

    protected virtual void HandleAttack()
    {
        if (isDead || isTakingDamage || attackStrategy == null || target == null) return;

        if (Time.time > nextAttackTime && movementStrategy.IsInRange(attackRange))
        {
            if (Random.Range(0f, 100f) < chanceToAttack)
            {
                if (requireLineOfSight)
                {
                    if (HasLineOfSightToTarget())
                    {
                        PerformAttack();
                    }
                }
                else
                {
                    PerformAttack();
                }
            }
            nextAttackTime = Time.time + 1f / attackRate;
        }
    }

    protected virtual void HandleIdleSounds()
    {
        if (isDead || audioSource == null || idleSounds == null || idleSounds.Length == 0) return;

        if (Time.time > nextIdleSoundTime)
        {
            if (Random.Range(0f, 100f) < idleSoundChance)
            {
                PlayRandomSound(idleSounds);
            }
            nextIdleSoundTime = Time.time + idleSoundInterval;
        }
    }

    protected virtual void PerformAttack()
    {
        if (isDead || isAttacking) return;

        isAttacking = true;

        if (animator != null)
            animator.SetTrigger(attackAnimTrigger);

        PlayRandomSound(attackSounds);

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

    protected virtual IEnumerator DelayedAttack()
    {
        yield return new WaitForSeconds(attackDelay);

        if (!isDead && attackStrategy != null && attackStrategy.CanAttack())
            attackStrategy.Attack();

        isAttacking = false;
    }

    protected virtual IEnumerator ShowAttackEffect()
    {
        if (attackChargeEffect != null)
        {
            attackChargeEffect.SetActive(true);
            yield return new WaitForSeconds(attackDelay);
            attackChargeEffect.SetActive(false);
        }
    }

    protected virtual void StartRetreat()
    {
        isRetreating = true;
        if (retreatCoroutine != null)
            StopCoroutine(retreatCoroutine);
        retreatCoroutine = StartCoroutine(RetreatCoroutine());
    }

    protected virtual IEnumerator RetreatCoroutine()
    {
        if (target != null && navAgent != null)
        {
            Vector3 retreatDirection = (transform.position - target.position).normalized;
            Vector3 retreatPosition = transform.position + retreatDirection * retreatDistance;

            NavMeshHit hit;
            if (NavMesh.SamplePosition(retreatPosition, out hit, retreatDistance, NavMesh.AllAreas))
            {
                navAgent.SetDestination(hit.position);
                yield return new WaitForSeconds(2f);
                isRetreating = false;
            }
        }
        isRetreating = false;
    }

    protected virtual void Die()
    {
        if (isDead) return;
        isDead = true;
        isActivated = false;

        StopAllCoroutines();

        if (animator != null)
            animator.SetTrigger(deathAnimTrigger);

        PlayRandomSound(deathSounds);

        if (deathEffect != null)
            Instantiate(deathEffect, transform.position, Quaternion.identity);

        if (navAgent != null)
            navAgent.enabled = false;

        if (disableColliderOnDeath && enemyCollider != null)
            enemyCollider.enabled = false;

        if (disableRendererOnDeath && enemyRenderer != null)
            enemyRenderer.enabled = false;

        StartCoroutine(DeathSequence());
    }

    protected virtual IEnumerator DamageFlash()
    {
        isInvulnerable = invulnerableDuringFlash;
        SetColor(damageFlashColor);
        yield return new WaitForSeconds(flashDuration);
        isTakingDamage = false;
        isInvulnerable = false;
        if (!isDead) ResetColor();
    }

    protected virtual IEnumerator DeathSequence()
    {
        yield return new WaitForSeconds(destroyDelay);

        if (spawner != null)
        {
            spawner.EnemyEliminated(gameObject);
        }

        Destroy(enemyRoot);
    }

    protected virtual bool HasLineOfSightToTarget()
    {
        if (target == null) return false;

        Vector3 directionToTarget = target.position - transform.position;
        float distanceToTarget = directionToTarget.magnitude;

        RaycastHit hit;
        if (Physics.Raycast(transform.position, directionToTarget.normalized, out hit, distanceToTarget, obstacleLayerMask))
        {
            return false;
        }

        return true;
    }

    protected virtual void SetColor(Color color)
    {
        if (enemyRenderer == null) return;

        enemyRenderer.GetPropertyBlock(materialPropertyBlock);
        materialPropertyBlock.SetColor("_BaseColor", color);
        materialPropertyBlock.SetColor("_Color", color);
        enemyRenderer.SetPropertyBlock(materialPropertyBlock);
    }

    protected virtual void ResetColor() => SetColor(originalColor);

    protected virtual void PlayRandomSound(AudioClip[] sounds)
    {
        if (audioSource == null || sounds == null || sounds.Length == 0) return;

        AudioClip soundToPlay = sounds[Random.Range(0, sounds.Length)];
        if (soundToPlay != null)
        {
            audioSource.PlayOneShot(soundToPlay);
        }
    }
    #endregion

    #region Movement Strategy Implementations
    protected class NavMeshMovement : IEnemyMovement
    {
        protected NavMeshAgent navAgent;
        private Transform enemyTransform;
        private Transform targetTransform;
        private float moveSpeed;
        private float stoppingDistance;
        private bool shouldFaceTarget;
        private bool shouldAvoidObstacles;

        public NavMeshAgent Agent => navAgent;

        public NavMeshMovement(float speed, float stopDistance, bool faceTarget, bool avoidObstacles)
        {
            moveSpeed = speed;
            stoppingDistance = stopDistance;
            shouldFaceTarget = faceTarget;
            shouldAvoidObstacles = avoidObstacles;
        }

        public void Initialize(Transform enemy, Transform target)
        {
            enemyTransform = enemy;
            targetTransform = target;
            navAgent = enemy.GetComponent<NavMeshAgent>();

            if (navAgent == null)
            {
                Debug.LogError($"NavMeshAgent not found on {enemy.name}");
                return;
            }

            navAgent.speed = moveSpeed;
            navAgent.stoppingDistance = stoppingDistance;
            navAgent.avoidancePriority = shouldAvoidObstacles ? 50 : 99;
        }

        public void Move()
        {
            if (navAgent == null || !navAgent.enabled || targetTransform == null) return;
            navAgent.SetDestination(targetTransform.position);
        }

        public void SetTarget(Transform target) => targetTransform = target;

        public void Stop()
        {
            if (navAgent != null && navAgent.enabled)
                navAgent.isStopped = true;
        }

        public void Resume()
        {
            if (navAgent != null && navAgent.enabled)
                navAgent.isStopped = false;
        }

        public bool IsInRange(float range)
        {
            if (targetTransform == null) return false;
            return Vector3.Distance(enemyTransform.position, targetTransform.position) <= range;
        }

        public float GetDistanceToTarget()
        {
            if (targetTransform == null || enemyTransform == null)
            {
                return float.MaxValue;
            }
            return Vector3.Distance(enemyTransform.position, targetTransform.position);
        }
    }

    protected class DirectMovement : IEnemyMovement
    {
        private Transform enemyTransform;
        private Transform targetTransform;
        private float moveSpeed;
        private float rotationSpeed;
        private float stoppingDistance;
        private bool shouldFaceTarget;

        public DirectMovement(float speed, float stopDistance, float rotSpeed, bool faceTarget)
        {
            moveSpeed = speed;
            stoppingDistance = stopDistance;
            rotationSpeed = rotSpeed;
            shouldFaceTarget = faceTarget;
        }

        public void Initialize(Transform enemy, Transform target)
        {
            enemyTransform = enemy;
            targetTransform = target;
        }

        public void Move()
        {
            if (enemyTransform == null || targetTransform == null) return;

            Vector3 directionToTarget = targetTransform.position - enemyTransform.position;
            float distanceToTarget = directionToTarget.magnitude;

            if (distanceToTarget > stoppingDistance)
            {
                enemyTransform.position += directionToTarget.normalized * moveSpeed * Time.deltaTime;

                if (shouldFaceTarget)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(new Vector3(directionToTarget.x, 0, directionToTarget.z));
                    enemyTransform.rotation = Quaternion.Slerp(enemyTransform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
                }
            }
            else if (shouldFaceTarget)
            {
                Quaternion targetRotation = Quaternion.LookRotation(new Vector3(directionToTarget.x, 0, directionToTarget.z));
                enemyTransform.rotation = Quaternion.Slerp(enemyTransform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
        }

        public void SetTarget(Transform target) => targetTransform = target;
        public void Stop() { }
        public void Resume() { }

        public bool IsInRange(float range)
        {
            if (targetTransform == null) return false;
            return Vector3.Distance(enemyTransform.position, targetTransform.position) <= range;
        }

        public float GetDistanceToTarget()
        {
            if (targetTransform == null) return float.MaxValue;
            return Vector3.Distance(enemyTransform.position, targetTransform.position);
        }
    }

    protected class PatrolMovement : IEnemyMovement
    {
        private Transform enemyTransform;
        private Transform targetTransform;
        private NavMeshAgent agent;
        private Transform[] patrolPoints;
        private float patrolWaitTime;
        private float moveSpeed;
        private float rotationSpeed;
        private float stoppingDistance;
        private int currentPatrolIndex = 0;
        private bool isWaiting = false;
        private float waitTimer = 0f;
        private bool hasTarget = false;
        private float detectionRange = 10f;

        public PatrolMovement(float speed, float stopDistance, Transform[] points, float waitTime, float rotSpeed)
        {
            moveSpeed = speed;
            stoppingDistance = stopDistance;
            patrolPoints = points;
            patrolWaitTime = waitTime;
            rotationSpeed = rotSpeed;
        }

        public void Initialize(Transform enemy, Transform target)
        {
            enemyTransform = enemy;
            targetTransform = target;
            agent = enemy.GetComponent<NavMeshAgent>();

            if (agent != null)
            {
                agent.speed = moveSpeed;
                agent.stoppingDistance = stoppingDistance;
            }

            hasTarget = target != null;

            if (patrolPoints != null && patrolPoints.Length > 0 && patrolPoints[0] != null)
            {
                SetPatrolDestination();
            }
        }

        public void Move()
        {
            if (enemyTransform == null) return;

            if (hasTarget && targetTransform != null &&
                Vector3.Distance(enemyTransform.position, targetTransform.position) <= detectionRange)
            {
                if (agent != null && agent.enabled)
                {
                    agent.SetDestination(targetTransform.position);
                }
                return;
            }

            if (patrolPoints == null || patrolPoints.Length == 0)
                return;

            if (isWaiting)
            {
                waitTimer += Time.deltaTime;
                if (waitTimer >= patrolWaitTime)
                {
                    isWaiting = false;
                    MoveToNextPatrolPoint();
                }
            }
            else if (agent != null && agent.enabled)
            {
                if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
                {
                    isWaiting = true;
                    waitTimer = 0f;
                }
            }
        }

        private void SetPatrolDestination()
        {
            if (agent == null || !agent.enabled) return;

            if (patrolPoints != null && patrolPoints.Length > 0 &&
                currentPatrolIndex < patrolPoints.Length && patrolPoints[currentPatrolIndex] != null)
            {
                agent.SetDestination(patrolPoints[currentPatrolIndex].position);
            }
        }

        private void MoveToNextPatrolPoint()
        {
            if (patrolPoints == null || patrolPoints.Length == 0) return;

            currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
            SetPatrolDestination();
        }

        public void SetTarget(Transform target)
        {
            targetTransform = target;
            hasTarget = target != null;
        }

        public void Stop()
        {
            if (agent != null && agent.enabled)
                agent.isStopped = true;
        }

        public void Resume()
        {
            if (agent != null && agent.enabled)
                agent.isStopped = false;
        }

        public bool IsInRange(float range)
        {
            if (targetTransform == null || enemyTransform == null)
            {
                return false;
            }
            return Vector3.Distance(enemyTransform.position, targetTransform.position) <= range;
        }

        public float GetDistanceToTarget()
        {
            if (targetTransform == null) return float.MaxValue;
            return Vector3.Distance(enemyTransform.position, targetTransform.position);
        }
    }
    #endregion

    #region Attack Types Implementations
    public class MeleeAttack : IEnemyAttack
    {
        private Transform enemy;
        private Transform target;
        private float damage;
        private float attackRate;
        private float attackRange;
        private float meleeRadius;
        private float attackAngle;
        private LayerMask attackableLayerMask;
        private bool requireLineOfSight;
        private bool usePhysics;
        private float meleeForce;
        private float upwardForce;
        private float damageMultiplier = 1f;
        private float lastAttackTime;

        public MeleeAttack(
            float damage,
            float attackRate,
            float attackRange,
            float meleeRadius,
            float attackAngle,
            LayerMask attackableLayerMask,
            bool requireLineOfSight,
            bool usePhysics,
            float meleeForce,
            float upwardForce)
        {
            this.damage = damage;
            this.attackRate = attackRate;
            this.attackRange = attackRange;
            this.meleeRadius = meleeRadius;
            this.attackAngle = attackAngle;
            this.attackableLayerMask = attackableLayerMask;
            this.requireLineOfSight = requireLineOfSight;
            this.usePhysics = usePhysics;
            this.meleeForce = meleeForce;
            this.upwardForce = upwardForce;
            this.lastAttackTime = -attackRate;
        }

        public void Initialize(Transform enemy, Transform target)
        {
            this.enemy = enemy;
            this.target = target;
        }

        public void SetTarget(Transform target) => this.target = target;

        public bool CanAttack()
        {
            if (target == null || enemy == null)
                return false;

            if (Time.time < lastAttackTime + attackRate)
                return false;

            float distanceToTarget = Vector3.Distance(enemy.position, target.position);
            if (distanceToTarget > attackRange)
                return false;

            if (attackAngle < 360f)
            {
                Vector3 directionToTarget = (target.position - enemy.position).normalized;
                float angle = Vector3.Angle(enemy.forward, directionToTarget);
                if (angle > attackAngle * 0.5f)
                    return false;
            }

            if (requireLineOfSight)
            {
                if (!HasLineOfSight())
                    return false;
            }

            return true;
        }

        public void Attack()
        {
            if (!CanAttack()) return;

            lastAttackTime = Time.time;

            Collider[] hitColliders = Physics.OverlapSphere(enemy.position, meleeRadius, attackableLayerMask);

            foreach (var hitCollider in hitColliders)
            {
                if (hitCollider.transform == enemy)
                    continue;

                Vector3 directionToTarget = (hitCollider.transform.position - enemy.position).normalized;
                float angle = Vector3.Angle(enemy.forward, directionToTarget);
                if (angle > attackAngle * 0.5f)
                    continue;

                IDamageable damageable = hitCollider.GetComponent<IDamageable>();
                if (damageable != null)
                {
                    damageable.TakeDamage(damage * damageMultiplier);
                }

                if (usePhysics)
                {
                    Rigidbody rb = hitCollider.GetComponent<Rigidbody>();
                    if (rb != null && !rb.isKinematic)
                    {
                        Vector3 forceDirection = (hitCollider.transform.position - enemy.position).normalized;
                        Vector3 force = forceDirection * meleeForce;
                        force.y += upwardForce;
                        rb.AddForce(force, ForceMode.Impulse);
                    }
                }
            }
        }

        public void SetDamageMultiplier(float multiplier) => damageMultiplier = multiplier;

        private bool HasLineOfSight()
        {
            if (target == null || enemy == null)
                return false;

            RaycastHit hit;
            Vector3 direction = target.position - enemy.position;
            if (Physics.Raycast(enemy.position, direction.normalized, out hit, attackRange))
            {
                return hit.transform == target || hit.transform.IsChildOf(target);
            }
            return false;
        }
    }

    public class RangedAttack : IEnemyAttack
    {
        protected Transform enemy;
        protected Transform target;
        protected GameObject projectilePrefab;
        protected Transform[] shootPoints;
        protected float projectileSpeed;
        protected float damage;
        protected float attackRate;
        protected float attackRange;
        protected bool burstFire;
        protected int burstCount;
        protected float burstDelay;
        protected bool useRandomShootPoint;
        protected float projectileSpread;
        protected float projectileLifetime;
        protected float damageMultiplier = 1f;
        protected float lastAttackTime;

        public RangedAttack(
            GameObject projectilePrefab,
            Transform[] shootPoints,
            float projectileSpeed,
            float damage,
            float attackRate,
            float attackRange,
            bool burstFire,
            int burstCount,
            float burstDelay,
            bool useRandomShootPoint,
            float projectileSpread,
            float projectileLifetime)
        {
            this.projectilePrefab = projectilePrefab;
            this.shootPoints = shootPoints;
            this.projectileSpeed = projectileSpeed;
            this.damage = damage;
            this.attackRate = attackRate;
            this.attackRange = attackRange;
            this.burstFire = burstFire;
            this.burstCount = burstCount;
            this.burstDelay = burstDelay;
            this.useRandomShootPoint = useRandomShootPoint;
            this.projectileSpread = projectileSpread;
            this.projectileLifetime = projectileLifetime;
            this.lastAttackTime = -attackRate;
        }

        public void Initialize(Transform enemy, Transform target)
        {
            this.enemy = enemy;
            this.target = target;
        }

        public void SetTarget(Transform target) => this.target = target;

        public bool CanAttack()
        {
            if (target == null || enemy == null || projectilePrefab == null || shootPoints == null || shootPoints.Length == 0)
                return false;

            if (Time.time < lastAttackTime + attackRate)
                return false;

            float distanceToTarget = Vector3.Distance(enemy.position, target.position);
            return distanceToTarget <= attackRange;
        }

        public void Attack()
        {
            if (!CanAttack()) return;

            lastAttackTime = Time.time;

            if (burstFire)
            {
                MonoBehaviour mb = enemy.GetComponent<MonoBehaviour>();
                if (mb != null)
                {
                    mb.StartCoroutine(FireBurst());
                }
                else
                {
                    FireSingleShot();
                }
            }
            else
            {
                FireSingleShot();
            }
        }

        private IEnumerator FireBurst()
        {
            for (int i = 0; i < burstCount; i++)
            {
                FireSingleShot();
                yield return new WaitForSeconds(burstDelay);
            }
        }

         protected virtual void FireSingleShot()
        {
            Transform shootPoint = GetShootPoint();
            if (shootPoint == null) return;

            Vector3 direction = CalculateFireDirection(shootPoint);

            GameObject projectile = GameObject.Instantiate(
                projectilePrefab,
                shootPoint.position,
                Quaternion.LookRotation(direction),
                shootPoint
            );

            projectile.SetActive(true);

            Rigidbody projectileRb = projectile.GetComponent<Rigidbody>();
            if (projectileRb == null)
            {
                projectileRb = projectile.AddComponent<Rigidbody>();
                projectileRb.useGravity = false;
                projectileRb.interpolation = RigidbodyInterpolation.Interpolate;
                projectileRb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            }

            projectileRb.velocity = direction * projectileSpeed;

            Projectile projectileScript = projectile.GetComponent<Projectile>();
            if (projectileScript == null)
            {
                projectileScript = projectile.AddComponent<Projectile>();
            }

            if (projectile.GetComponent<Collider>() == null)
            {
                SphereCollider collider = projectile.AddComponent<SphereCollider>();
                collider.radius = 0.25f;
                collider.isTrigger = true;
            }

            if (projectileScript == null)
            {
                projectileScript = projectile.AddComponent<Projectile>();
            }

            projectileScript.SetDamage(damage * damageMultiplier);
            projectileScript.SetOwner(enemy.gameObject);

            if (projectileLifetime > 0)
            {
                GameObject.Destroy(projectile, projectileLifetime);
            }
        }

        protected virtual Transform GetShootPoint()
        {
            if (shootPoints == null || shootPoints.Length == 0)
                return null;

            return useRandomShootPoint ?
                shootPoints[Random.Range(0, shootPoints.Length)] :
                shootPoints[0];
        }

        private Vector3 CalculateFireDirection(Transform shootPoint)
        {
            if (target == null)
                return shootPoint.forward;

            Vector3 targetDirection = (target.position - shootPoint.position).normalized;

            if (projectileSpread > 0)
            {
                float spreadX = Random.Range(-projectileSpread, projectileSpread);
                float spreadY = Random.Range(-projectileSpread, projectileSpread);
                Quaternion spreadRotation = Quaternion.Euler(spreadY, spreadX, 0);
                targetDirection = spreadRotation * targetDirection;
            }

            return targetDirection;
        }

        public void SetDamageMultiplier(float multiplier) => damageMultiplier = multiplier;
    }

    public class Projectile : MonoBehaviour
    {
        [SerializeField] private float damage = 10f;
        [SerializeField] private bool destroyOnImpact = true;
        [SerializeField] private GameObject impactEffect;
        [SerializeField] private AudioClip impactSound;

        private GameObject owner;

        public void SetDamage(float newDamage) => damage = newDamage;
        public void SetOwner(GameObject newOwner) => owner = newOwner;

        private void OnCollisionEnter(Collision collision)
        {
            HandleImpact(collision.gameObject, collision.contacts[0].point);
        }

        private void OnTriggerEnter(Collider other)
        {
            HandleImpact(other.gameObject, transform.position);
        }

        private void HandleImpact(GameObject hitObject, Vector3 hitPoint)
        {
            if (hitObject == owner)
                return;

            IDamageable damageable = hitObject.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(damage);
            }

            if (impactEffect != null)
            {
                GameObject effect = Instantiate(impactEffect, hitPoint, Quaternion.identity);
                Destroy(effect, 2f);
            }

            if (impactSound != null)
            {
                AudioSource.PlayClipAtPoint(impactSound, hitPoint);
            }

            if (destroyOnImpact)
            {
                Destroy(gameObject);
            }
        }
    }
    #endregion
}