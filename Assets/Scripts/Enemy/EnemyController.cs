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

    [System.Serializable]
    public enum EnemyState
    {
        Idle,
        Patrolling,
        Chasing,
        Attacking,
        Retreating,
        Dead
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
    [SerializeField] protected float waypointSwitchDistance = 1f;

    [Header("Attack Configuration")]
    [SerializeField] protected AttackType attackType = AttackType.Ranged;
    [SerializeField] protected float attackRange = 10f;
    [SerializeField] protected float attackRate = 1f;
    [SerializeField] protected float attackDamage = 10f;
    [SerializeField] protected float chanceToAttack = 100f;
    [SerializeField] protected float attackAngle = 45f;
    [SerializeField] public Transform attackOrigin;
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
    [SerializeField] protected float retreatAfterAttackDistance = 3f;
    [SerializeField] protected float circlingDistance = 5f;
    [SerializeField] protected float circlingSpeed = 1f;

    [Header("Animation Settings")]
    [SerializeField] protected Animator animator;
    [SerializeField] protected string idleAnimTrigger = "Idle";
    [SerializeField] protected string moveAnimTrigger = "Move";
    [SerializeField] protected string attackAnimTrigger = "Attack";
    [SerializeField] protected string damageAnimTrigger = "Damage";
    [SerializeField] protected string deathAnimTrigger = "Death";
    [SerializeField] protected bool useAnimatorSpeed = true;

    [Header("Audio Settings")]
    [SerializeField] protected string attackSoundEvent = "event:/Enemies/Attack";
    [SerializeField] protected string damageSoundEvent = "event:/Enemies/Damage";
    [SerializeField] protected string deathSoundEvent = "event:/Enemies/Death";
    [SerializeField] protected string idleSoundEvent = "event:/Enemies/Idle";
    [SerializeField] protected string footstepSoundEvent = "event:/Enemies/Footstep";
    [SerializeField] protected float idleSoundInterval = 5f;
    [SerializeField] protected float idleSoundChance = 20f;

    // For attack sounds that need to be interrupted
    private FMOD.Studio.EventInstance attackEventInstance;

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
    [SerializeField] protected GameObject hotZoneObject;
    [SerializeField] protected GameObject triggerAreaObject;

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
    protected bool isSoundDisabled = false;
    protected bool playerInSightRange = false;
    protected bool playerInAttackRange = false;
    protected Transform target;
    protected Color originalColor;
    protected float nextAttackTime = 0f;
    protected float nextIdleSoundTime = 0f;
    protected IEnemyMovement movementStrategy;
    protected IEnemyAttack attackStrategy;
    protected Coroutine attackCoroutine;
    protected Coroutine meleeAttackCoroutine;
    protected Coroutine retreatCoroutine;
    protected EnemyState currentState = EnemyState.Idle;

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

        // Ensure NavMeshAgent is active
        if (navAgent != null)
        {
            navAgent.isStopped = false;
            navAgent.updatePosition = true;
            navAgent.updateRotation = true;

            // Check if the agent is on a NavMesh
            if (!navAgent.isOnNavMesh)
            {
                Debug.LogError($"Enemy {gameObject.name} is not on a NavMesh!");
            }
        }

        // Set initial state
        if (movementType == MovementType.Patrol)
        {
            currentState = EnemyState.Patrolling;
        }
        else
        {
            currentState = EnemyState.Idle;
        }

        // Setup hot zone if available
        if (hotZoneObject != null)
        {
            hotZoneObject.SetActive(false);
        }
    }

    protected virtual void OnEnable()
    {
        // Check patrol points if using patrol movement
        if (movementType == MovementType.Patrol)
        {
            if (patrolPoints == null || patrolPoints.Length == 0)
            {
                Debug.LogError("Patrol movement selected but no patrol points assigned!");
            }
            else
            {
                Debug.Log($"Enemy has {patrolPoints.Length} patrol points assigned");
                for (int i = 0; i < patrolPoints.Length; i++)
                {
                    if (patrolPoints[i] == null)
                        Debug.LogError($"Patrol point {i} is null!");
                }
            }
        }
    }

    protected virtual void Update()
    {
        if (isDead || !isActivated) return;

        // Check if player is in sight or attack range
        CheckPlayerRanges();

        // State machine for enemy behavior
        switch (currentState)
        {
            case EnemyState.Idle:
                HandleIdle();
                break;
            case EnemyState.Patrolling:
                HandlePatrolling();
                break;
            case EnemyState.Chasing:
                HandleChasing();
                break;
            case EnemyState.Attacking:
                HandleAttacking();
                break;
            case EnemyState.Retreating:
                HandleRetreating();
                break;
        }

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

    protected virtual void OnDrawGizmos()
    {
        if (navAgent != null && navAgent.enabled && Application.isPlaying)
        {
            // Draw path
            Gizmos.color = Color.yellow;
            Vector3[] corners = navAgent.path.corners;
            for (int i = 0; i < corners.Length - 1; i++)
            {
                Gizmos.DrawLine(corners[i], corners[i + 1]);
            }

            // Draw destination
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(navAgent.destination, 0.5f);
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

    #region State Machine Methods
    protected virtual void CheckPlayerRanges()
    {
        if (target == null) return;

        float distanceToTarget = Vector3.Distance(transform.position, target.position);
        playerInSightRange = distanceToTarget <= detectionRange;
        playerInAttackRange = distanceToTarget <= attackRange;

        // State transitions based on player ranges
        if (currentState != EnemyState.Dead && currentState != EnemyState.Retreating)
        {
            if (playerInAttackRange && currentState != EnemyState.Attacking)
            {
                currentState = EnemyState.Attacking;
            }
            else if (playerInSightRange && !playerInAttackRange && currentState != EnemyState.Chasing)
            {
                currentState = EnemyState.Chasing;
                if (hotZoneObject != null)
                {
                    hotZoneObject.SetActive(true);
                }
                if (triggerAreaObject != null)
                {
                    triggerAreaObject.SetActive(false);
                }
            }
            else if (!playerInSightRange && !playerInAttackRange &&
                    (currentState == EnemyState.Chasing || currentState == EnemyState.Attacking))
            {
                if (movementType == MovementType.Patrol)
                {
                    currentState = EnemyState.Patrolling;
                }
                else
                {
                    currentState = EnemyState.Idle;
                }

                if (hotZoneObject != null)
                {
                    hotZoneObject.SetActive(false);
                }
                if (triggerAreaObject != null)
                {
                    triggerAreaObject.SetActive(true);
                }
            }
        }
    }

    protected virtual void HandleIdle()
    {
        if (animator != null)
        {
            animator.SetTrigger(idleAnimTrigger);
            if (useAnimatorSpeed)
                animator.SetFloat("Speed", 0);
        }

        if (moveParticles != null && moveParticles.isPlaying)
        {
            moveParticles.Stop();
        }

        // Face target if in sight range
        if (playerInSightRange && faceTarget && target != null)
        {
            Vector3 direction = (target.position - transform.position).normalized;
            Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * rotationSpeed);
        }
    }

    protected virtual void HandlePatrolling()
    {
        if (isDead || isTakingDamage) return;

        if (movementStrategy != null && isMoving)
        {
            movementStrategy.Move();

            if (animator != null)
            {
                animator.SetTrigger(moveAnimTrigger);
                if (useAnimatorSpeed && animator.GetFloat("Speed") != moveSpeed / 5f)
                    animator.SetFloat("Speed", moveSpeed / 5f);
            }

            if (moveParticles != null && !moveParticles.isPlaying)
            {
                moveParticles.Play();
            }
        }
    }

    protected virtual void HandleChasing()
    {
        if (isDead || isTakingDamage) return;

        if (target != null && navAgent != null && navAgent.enabled)
        {
            navAgent.SetDestination(target.position);

            if (animator != null)
            {
                animator.SetTrigger(moveAnimTrigger);
                if (useAnimatorSpeed)
                    animator.SetFloat("Speed", moveSpeed / 5f);
            }

            if (moveParticles != null && !moveParticles.isPlaying)
            {
                moveParticles.Play();
            }
        }
    }

    protected virtual void HandleAttacking()
    {
        if (isDead || isTakingDamage) return;

        // Stop movement when attacking unless configured otherwise
        if (!canAttackWhileMoving && navAgent != null && navAgent.enabled)
        {
            navAgent.SetDestination(transform.position);
        }

        // Face the target
        if (target != null && faceTarget)
        {
            Vector3 direction = (target.position - transform.position).normalized;
            Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * rotationSpeed);
        }

        // Perform attack if cooldown is complete
        if (Time.time > nextAttackTime)
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

        // Stop movement particles
        if (moveParticles != null && moveParticles.isPlaying && !canAttackWhileMoving)
        {
            moveParticles.Stop();
        }
    }

    protected virtual void HandleRetreating()
    {
        if (isDead || isTakingDamage) return;

        // Retreat behavior is handled by the RetreatCoroutine
        if (!isRetreating)
        {
            // If retreat is complete, return to appropriate state
            if (playerInAttackRange)
            {
                currentState = EnemyState.Attacking;
            }
            else if (playerInSightRange)
            {
                currentState = EnemyState.Chasing;
            }
            else if (movementType == MovementType.Patrol)
            {
                currentState = EnemyState.Patrolling;
            }
            else
            {
                currentState = EnemyState.Idle;
            }
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

        if (attackOrigin == null)
            attackOrigin = transform;

        materialPropertyBlock = new MaterialPropertyBlock();

        if (enemyRenderer != null)
            originalColor = enemyRenderer.material.color;

        InitializeFMOD();
    }

    protected virtual void InitializeFMOD()
    {
        try
        {
            // Create event instances that need to be stopped/manipulated later
            if (!string.IsNullOrEmpty(attackSoundEvent))
            {
                attackEventInstance = FMODUnity.RuntimeManager.CreateInstance(attackSoundEvent);
                if (!attackEventInstance.isValid())
                {
                    Debug.LogWarning($"FMOD event {attackSoundEvent} not found. Sound will be disabled.");
                    isSoundDisabled = true;
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error initializing FMOD: {e.Message}. Sound will be disabled but movement should still work.");
            // Set a flag to disable sound functionality
            isSoundDisabled = true;
        }
    }

    protected virtual void InitializeStrategies()
    {
        if (targetPoint == null)
        {
            targetPoint = GameObject.FindGameObjectWithTag("Player")?.transform;
            Debug.Log("Found player target: " + (targetPoint != null ? targetPoint.name : "None"));
        }

        movementStrategy = CreateMovementStrategy();

        if (movementStrategy != null && targetPoint != null)
        {
            movementStrategy.Initialize(transform, targetPoint);
            Debug.Log("Movement strategy initialized with target: " + targetPoint.name);
        }
        else
        {
            Debug.LogError("Failed to initialize movement strategy or target is null");
        }

        attackStrategy = CreateAttackStrategy();
        if (attackStrategy != null && targetPoint != null)
        {
            attackStrategy.Initialize(transform, targetPoint);
            attackStrategy.SetDamageMultiplier(damageMultiplier);
        }

        if (navAgent != null)
        {
            // Make sure stopping distance is less than attack range for melee enemies
            if (attackType == AttackType.Melee)
            {
                // Set stopping distance to be slightly less than attack range
                navAgent.stoppingDistance = attackRange * 0.8f;
            }
            else
            {
                navAgent.stoppingDistance = stoppingDistance;
            }

            navAgent.speed = moveSpeed;
            navAgent.angularSpeed = rotationSpeed * 100;
            navAgent.isStopped = false;
            Debug.Log($"NavMeshAgent configured with speed: {moveSpeed}, stopping distance: {navAgent.stoppingDistance}");
        }
        else
        {
            Debug.LogError("NavMeshAgent component is missing!");
        }
    }

    protected virtual IEnemyMovement CreateMovementStrategy()
    {
        switch (movementType)
        {
            case MovementType.NavMesh:
                if (attackType == AttackType.Melee)
                    return new MeleeMovement(moveSpeed, stoppingDistance, faceTarget, avoidObstacles, retreatAfterAttackDistance, circlingDistance, circlingSpeed, attackRange);
                else
                    return new NavMeshMovement(moveSpeed, stoppingDistance, faceTarget, avoidObstacles);
            case MovementType.Direct:
                //return new DirectMovement(moveSpeed, stoppingDistance, rotationSpeed, faceTarget);
            case MovementType.Patrol:
                return new PatrolMovement(moveSpeed, stoppingDistance, patrolPoints, patrolWaitTime, rotationSpeed, detectionRange);
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
                    meleeUpwardForce,
                    attackOrigin
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
            Debug.LogWarning("Attempted to assign a null target");
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
    }

    public virtual void TakeDamage(float damageAmount)
    {
        if (isDead || isInvulnerable) return;

        currentHealth -= damageAmount;
        isTakingDamage = true;

        StartCoroutine(DamageFlash());
        PlaySound(damageSoundEvent);

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
            Debug.Log("Moving enemy: " + gameObject.name);
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
        else
        {
            if (movementStrategy == null)
                Debug.LogError("Movement strategy is null for: " + gameObject.name);
            if (!isMoving)
                Debug.Log("Enemy is not set to move: " + gameObject.name);
        }
    }

    protected virtual void HandleAttack()
    {
        if (isDead || isTakingDamage || attackStrategy == null || target == null) return;

        float distanceToTarget = Vector3.Distance(transform.position, target.position);

        // Debug the distance to help diagnose issues
        Debug.Log($"Distance to target: {distanceToTarget}, Attack Range: {attackRange}");

        if (Time.time > nextAttackTime && distanceToTarget <= attackRange)
        {
            if (Random.Range(0f, 100f) < chanceToAttack)
            {
                if (requireLineOfSight)
                {
                    if (HasLineOfSightToTarget())
                    {
                        Debug.Log("Performing attack - has line of sight");
                        PerformAttack();
                    }
                }
                else
                {
                    Debug.Log("Performing attack - no line of sight required");
                    PerformAttack();
                }
            }
            nextAttackTime = Time.time + 1f / attackRate;
        }
    }

    protected virtual void HandleIdleSounds()
    {
        if (isDead || isSoundDisabled) return;

        if (Time.time > nextIdleSoundTime)
        {
            if (Random.Range(0f, 100f) < idleSoundChance)
            {
                PlaySound(idleSoundEvent);
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

            // Retreat after attack for melee enemies
            if (attackType == AttackType.Melee && movementStrategy is MeleeMovement)
            {
                ((MeleeMovement)movementStrategy).RetreatAfterAttack();
            }

            isAttacking = false;
        }
    }

    protected virtual IEnumerator DelayedAttack()
    {
        yield return new WaitForSeconds(attackDelay);

        if (!isDead && attackStrategy != null && attackStrategy.CanAttack())
        {
            attackStrategy.Attack();

            // Retreat after attack for melee enemies
            if (attackType == AttackType.Melee && movementStrategy is MeleeMovement)
            {
                ((MeleeMovement)movementStrategy).RetreatAfterAttack();
            }
        }

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
        currentState = EnemyState.Retreating;
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
        StopAttackSound();

        if (isDead) return;
        isDead = true;
        isActivated = false;
        currentState = EnemyState.Dead;

        StopAllCoroutines();

        if (animator != null)
            animator.SetTrigger(deathAnimTrigger);

        PlaySound(deathSoundEvent);

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

    protected virtual void PlaySound(string eventPath)
    {
        if (isSoundDisabled || string.IsNullOrEmpty(eventPath)) return;

        try
        {
            // Play one-shot sound at the position of this enemy
            FMODUnity.RuntimeManager.PlayOneShot(eventPath, transform.position);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Error playing sound {eventPath}: {e.Message}");
            isSoundDisabled = true;
        }
    }

    protected virtual void PlayAttackSound()
    {
        if (isSoundDisabled || string.IsNullOrEmpty(attackSoundEvent)) return;

        try
        {
            // Stop any existing attack sound
            if (attackEventInstance.isValid())
            {
                attackEventInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
                attackEventInstance.release();
            }

            // Create and start a new instance
            attackEventInstance = FMODUnity.RuntimeManager.CreateInstance(attackSoundEvent);
            if (attackEventInstance.isValid())
            {
                attackEventInstance.set3DAttributes(FMODUnity.RuntimeUtils.To3DAttributes(transform.position));
                attackEventInstance.start();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Error playing attack sound: {e.Message}");
            isSoundDisabled = true;
        }
    }

    protected virtual void StopAttackSound()
    {
        if (isSoundDisabled) return;

        try
        {
            if (attackEventInstance.isValid())
            {
                attackEventInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Error stopping attack sound: {e.Message}");
            isSoundDisabled = true;
        }
    }

    public void PlayFootstepSound()
    {
        if (isSoundDisabled || string.IsNullOrEmpty(footstepSoundEvent)) return;

        try
        {
            // Play the footstep sound at the enemy's position
            FMODUnity.RuntimeManager.PlayOneShot(footstepSoundEvent, transform.position);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Error playing footstep sound: {e.Message}");
            isSoundDisabled = true;
        }
    }

    #endregion

    #region Movement Strategy Implementations
    public class NavMeshMovement : IEnemyMovement
    {
        protected NavMeshAgent navAgent;
        protected Transform enemyTransform;
        protected Transform targetTransform;
        protected float moveSpeed;
        protected float stoppingDistance;
        protected bool shouldFaceTarget;
        protected bool shouldAvoidObstacles;

        public NavMeshAgent Agent => navAgent;

        public NavMeshMovement(float speed, float stopDistance, bool faceTarget, bool avoidObstacles)
        {
            moveSpeed = speed;
            stoppingDistance = stopDistance;
            shouldFaceTarget = faceTarget;
            shouldAvoidObstacles = avoidObstacles;
        }

        public virtual void Initialize(Transform enemy, Transform target)
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
            navAgent.updateRotation = shouldFaceTarget;
            navAgent.isStopped = false; // Make sure it's not stopped initially

            // Check if the agent is on a NavMesh
            if (!navAgent.isOnNavMesh)
            {
                Debug.LogError($"Enemy {enemy.name} is not on a NavMesh!");
            }
        }

        public virtual void Move()
        {
            if (navAgent == null || !navAgent.enabled || targetTransform == null) return;

            Debug.Log($"NavMeshMovement: Setting destination to {targetTransform.position}");
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

    protected class MeleeMovement : NavMeshMovement
    {
        private float retreatAfterAttackDistance;
        private float circlingDistance;
        private float circlingSpeed;
        private bool isRetreatingAfterAttack = false;
        private float lastAttackTime = -1f;
        private float retreatDuration = 1.5f;
        private bool isCircling = false;
        private float nextPositionChangeTime = 0f;
        private Vector3 circlePoint;
        private float attackRange;

        public MeleeMovement(float speed, float stopDistance, bool faceTarget, bool avoidObstacles,
                            float retreatDistance, float circleDistance, float circleSpeed, float attackRange)
            : base(speed, stopDistance, faceTarget, avoidObstacles)
        {
            this.retreatAfterAttackDistance = retreatDistance;
            this.circlingDistance = circleDistance;
            this.circlingSpeed = circleSpeed;
            this.attackRange = attackRange;
        }

        public override void Move()
        {
            if (navAgent == null || !navAgent.enabled || targetTransform == null) return;

            float distanceToTarget = Vector3.Distance(enemyTransform.position, targetTransform.position);

            // If we're retreating after an attack
            if (isRetreatingAfterAttack)
            {
                if (Time.time > lastAttackTime + retreatDuration)
                {
                    isRetreatingAfterAttack = false;
                }
                return; // Don't change the retreat destination while retreating
            }

            // If we're very close to the target and not already retreating
            if (distanceToTarget <= stoppingDistance * 0.8f && !isRetreatingAfterAttack)
            {
                // Time to circle around the player instead of standing still
                if (!isCircling)
                {
                    isCircling = true;
                    CalculateCirclingPoint();
                }

                if (Time.time > nextPositionChangeTime)
                {
                    CalculateCirclingPoint();
                    nextPositionChangeTime = Time.time + Random.Range(1.5f, 3.0f);
                }

                navAgent.SetDestination(circlePoint);
            }
            else if (distanceToTarget <= attackRange * 0.9f && distanceToTarget > stoppingDistance)
            {
                // We're in attack range but not too close - approach normally
                isCircling = false;
                navAgent.SetDestination(targetTransform.position);
            }
            else
            {
                // We're far away - chase the target
                isCircling = false;
                navAgent.SetDestination(targetTransform.position);
            }
        }

        private void CalculateCirclingPoint()
        {
            // Calculate a point around the target to circle to
            Vector3 directionToTarget = targetTransform.position - enemyTransform.position;
            Vector3 perpendicular = Vector3.Cross(directionToTarget.normalized, Vector3.up);

            // Randomly decide whether to circle left or right
            if (Random.value > 0.5f)
                perpendicular = -perpendicular;

            // Calculate the circle point
            circlePoint = targetTransform.position + perpendicular.normalized * circlingDistance;

            // Make sure the point is on the navmesh
            NavMeshHit hit;
            if (NavMesh.SamplePosition(circlePoint, out hit, circlingDistance, NavMesh.AllAreas))
            {
                circlePoint = hit.position;
            }
        }

        public void RetreatAfterAttack()
        {
            if (navAgent == null || !navAgent.enabled || targetTransform == null) return;

            isRetreatingAfterAttack = true;
            lastAttackTime = Time.time;

            // Calculate retreat position (away from target)
            Vector3 retreatDirection = (enemyTransform.position - targetTransform.position).normalized;
            Vector3 retreatPosition = enemyTransform.position + retreatDirection * retreatAfterAttackDistance;

            // Ensure the retreat position is on the navmesh
            NavMeshHit hit;
            if (NavMesh.SamplePosition(retreatPosition, out hit, retreatAfterAttackDistance, NavMesh.AllAreas))
            {
                navAgent.SetDestination(hit.position);
            }
        }
    }

    //protected class DirectMovement : IEnemyMovement
    //{
    //    private Transform enemyTransform;
    //    private Transform targetTransform;
    //    private float moveSpeed;
    //    private float rotationSpeed;
    //    private float stoppingDistance;
    //    private bool shouldFaceTarget;

    //    public DirectMovement(float speed, float stopDistance, float rotSpeed, bool faceTarget)
    //    {
    //        moveSpeed = speed;
    //        stoppingDistance = stopDistance;
    //        rotationSpeed = rotSpeed;
    //        shouldFaceTarget = faceTarget;
    //    }

    //    public void Initialize(Transform enemy, Transform target)
    //    {
    //        enemyTransform = enemy;
    //        targetTransform = target;
    //    }

        //public virtual void Move()
        //{
        //    if (navAgent == null || !navAgent.enabled || targetTransform == null) return;
        //    navAgent.SetDestination(targetTransform.position);

        //    if (enemyTransform == null || targetTransform == null) return;

        //    Vector3 directionToTarget = targetTransform.position - enemyTransform.position;
        //    float distanceToTarget = directionToTarget.magnitude;

        //    if (distanceToTarget > stoppingDistance)
        //    {
        //        enemyTransform.position += directionToTarget.normalized * moveSpeed * Time.deltaTime;

        //        if (shouldFaceTarget)
        //        {
        //            Quaternion targetRotation = Quaternion.LookRotation(new Vector3(directionToTarget.x, 0, directionToTarget.z));
        //            enemyTransform.rotation = Quaternion.Slerp(enemyTransform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        //        }
        //    }
        //    else if (shouldFaceTarget)
        //    {
        //        Quaternion targetRotation = Quaternion.LookRotation(new Vector3(directionToTarget.x, 0, directionToTarget.z));
        //        enemyTransform.rotation = Quaternion.Slerp(enemyTransform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        //    }
        //}

    //    public void SetTarget(Transform target) => targetTransform = target;
    //    public void Stop() { }
    //    public void Resume() { }

    //    public bool IsInRange(float range)
    //    {
    //        if (targetTransform == null) return false;
    //        return Vector3.Distance(enemyTransform.position, targetTransform.position) <= range;
    //    }

    //    public float GetDistanceToTarget()
    //    {
    //        if (targetTransform == null) return float.MaxValue;
    //        return Vector3.Distance(enemyTransform.position, targetTransform.position);
    //    }
    //}

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

        public PatrolMovement(float speed, float stopDistance, Transform[] points, float waitTime, float rotSpeed, float detectionRange)
        {
            moveSpeed = speed;
            stoppingDistance = stopDistance;
            patrolPoints = points;
            patrolWaitTime = waitTime;
            rotationSpeed = rotSpeed;
            this.detectionRange = detectionRange;
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
                agent.isStopped = false;
            }

            hasTarget = target != null;

            // Add debug logging to check patrol points
            if (patrolPoints == null || patrolPoints.Length == 0)
            {
                Debug.LogWarning("No patrol points assigned to enemy: " + enemy.name);
            }
            else
            {
                for (int i = 0; i < patrolPoints.Length; i++)
                {
                    if (patrolPoints[i] == null)
                        Debug.LogWarning($"Patrol point {i} is null on enemy: {enemy.name}");
                }
                SetPatrolDestination();
            }
        }

        public void Move()
        {
            if (enemyTransform == null) return;

            // If we have a target and it's in range, chase it
            if (hasTarget && targetTransform != null &&
                Vector3.Distance(enemyTransform.position, targetTransform.position) <= detectionRange)
            {
                if (agent != null && agent.enabled)
                {
                    Debug.Log($"Moving to target: {targetTransform.name}");
                    agent.SetDestination(targetTransform.position);
                }
                return;
            }

            // Otherwise patrol
            if (patrolPoints == null || patrolPoints.Length == 0)
            {
                Debug.LogWarning("No patrol points available");
                return;
            }

            if (isWaiting)
            {
                waitTimer += Time.deltaTime;
                if (waitTimer >= patrolWaitTime)
                {
                    isWaiting = false;
                    MoveToNextPatrolPoint();
                    Debug.Log($"Moving to next patrol point: {currentPatrolIndex}");
                }
            }
            else if (agent != null && agent.enabled)
            {
                // Check if we've reached the destination
                if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
                {
                    Debug.Log($"Reached patrol point {currentPatrolIndex}, waiting...");
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
                Debug.Log($"Setting patrol destination to point {currentPatrolIndex}: {patrolPoints[currentPatrolIndex].position}");
                agent.SetDestination(patrolPoints[currentPatrolIndex].position);
            }
            else
            {
                Debug.LogError($"Invalid patrol point at index {currentPatrolIndex}");
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
        private Transform attackOriginTransform;
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
            float upwardForce,
            Transform attackOrigin)
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
            this.attackOriginTransform = attackOrigin;
            this.lastAttackTime = -attackRate;
        }

        public void Initialize(Transform enemy, Transform target)
        {
            this.enemy = enemy;
            this.target = target;

            // If attackOriginTransform wasn't set in constructor, try to find it
            if (this.attackOriginTransform == null)
            {
                // Try to get it from the EnemyController
                EnemyController controller = enemy.GetComponent<EnemyController>();
                if (controller != null && controller.attackOrigin != null)
                {
                    this.attackOriginTransform = controller.attackOrigin;
                }
                else
                {
                    // Fallback to using the enemy transform
                    this.attackOriginTransform = enemy;
                    Debug.LogWarning("No AttackOrigin found for enemy " + enemy.name + ". Using enemy transform instead.");
                }
            }
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

            // Use attackOriginTransform if available, otherwise fall back to enemy position
            Vector3 attackPosition = attackOriginTransform != null ? attackOriginTransform.position : enemy.position;

            Collider[] hitColliders = Physics.OverlapSphere(attackPosition, meleeRadius, attackableLayerMask);

            Debug.Log($"Melee attack at {attackPosition}, found {hitColliders.Length} potential targets");

            foreach (var hitCollider in hitColliders)
            {
                if (hitCollider.transform == enemy)
                    continue;

                Vector3 directionToTarget = (hitCollider.transform.position - attackPosition).normalized;
                float angle = Vector3.Angle(enemy.forward, directionToTarget);
                if (angle > attackAngle * 0.5f)
                    continue;

                IDamageable damageable = hitCollider.GetComponent<IDamageable>();
                if (damageable != null)
                {
                    Debug.Log($"Dealing {damage * damageMultiplier} damage to {hitCollider.name}");
                    damageable.TakeDamage(damage * damageMultiplier);
                }

                if (usePhysics)
                {
                    Rigidbody rb = hitCollider.GetComponent<Rigidbody>();
                    if (rb != null && !rb.isKinematic)
                    {
                        Vector3 forceDirection = (hitCollider.transform.position - attackPosition).normalized;
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

            Vector3 attackPosition = attackOriginTransform != null ? attackOriginTransform.position : enemy.position;
            Vector3 direction = target.position - attackPosition;

            RaycastHit hit;
            if (Physics.Raycast(attackPosition, direction.normalized, out hit, attackRange))
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
        [SerializeField] private float lifeTime = 5f;
        [SerializeField] private bool applyForce = false;
        [SerializeField] private float impactForce = 5f;
        [SerializeField] private float upwardForce = 0f;
        [SerializeField] private bool useGravity = false;
        [SerializeField] private float gravityDelay = 0.5f;
        [SerializeField] private float gravityScale = 1f;
        [SerializeField] private bool rotateTowardsVelocity = true;
        [SerializeField] private float rotationSpeed = 10f;
        [SerializeField] private TrailRenderer trailRenderer;
        [SerializeField] private ParticleSystem particleSystem;

        private GameObject owner;
        private Rigidbody rb;
        private float gravityTimer = 0f;
        private bool isGravityEnabled = false;
        private Vector3 lastVelocity;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.useGravity = false;
                lastVelocity = rb.velocity;
            }

            if (lifeTime > 0)
                Destroy(gameObject, lifeTime);
        }

        private void Update()
        {
            if (useGravity && !isGravityEnabled)
            {
                gravityTimer += Time.deltaTime;
                if (gravityTimer >= gravityDelay)
                {
                    isGravityEnabled = true;
                }
            }

            if (rb != null)
            {
                if (isGravityEnabled)
                {
                    rb.AddForce(Physics.gravity * gravityScale, ForceMode.Acceleration);
                }

                if (rotateTowardsVelocity && rb.velocity.sqrMagnitude > 0.1f)
                {
                    lastVelocity = rb.velocity;
                    Quaternion targetRotation = Quaternion.LookRotation(rb.velocity);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
                }
            }
        }

        public void SetDamage(float newDamage) => damage = newDamage;
        public void SetOwner(GameObject newOwner) => owner = newOwner;

        private void OnCollisionEnter(Collision collision)
        {
            HandleImpact(collision.gameObject, collision.contacts[0].point, collision.contacts[0].normal);
        }

        private void OnTriggerEnter(Collider other)
        {
            HandleImpact(other.gameObject, transform.position, -transform.forward);
        }

        private void HandleImpact(GameObject hitObject, Vector3 hitPoint, Vector3 hitNormal)
        {
            if (hitObject == owner)
                return;

            IDamageable damageable = hitObject.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(damage);
            }

            if (applyForce)
            {
                Rigidbody targetRb = hitObject.GetComponent<Rigidbody>();
                if (targetRb != null && !targetRb.isKinematic)
                {
                    Vector3 forceDirection = transform.forward;
                    Vector3 force = forceDirection * impactForce;
                    force.y += upwardForce;
                    targetRb.AddForce(force, ForceMode.Impulse);
                }
            }

            if (impactEffect != null)
            {
                GameObject effect = Instantiate(impactEffect, hitPoint, Quaternion.LookRotation(hitNormal));
                Destroy(effect, 2f);
            }

            if (impactSound != null)
            {
                AudioSource.PlayClipAtPoint(impactSound, hitPoint);
            }
            else
            {
                try
                {
                    // Try to play FMOD sound if available
                    FMODUnity.RuntimeManager.PlayOneShot("event:/Weapons/ProjectileImpact", hitPoint);
                }
                catch (System.Exception)
                {
                    // Silently fail if FMOD is not available
                }
            }

            if (destroyOnImpact)
            {
                // Detach trail renderer if it exists
                if (trailRenderer != null)
                {
                    trailRenderer.transform.SetParent(null);
                    trailRenderer.autodestruct = true;
                }

                // Detach particle system if it exists
                if (particleSystem != null)
                {
                    particleSystem.transform.SetParent(null);
                    particleSystem.Stop();
                    Destroy(particleSystem.gameObject, particleSystem.main.duration + particleSystem.main.startLifetime.constantMax);
                }

                Destroy(gameObject);
            }
        }
    }
    #endregion
    #region Getters
    public float GetAttackDamage() => attackDamage;
    public float GetAttackRate() => attackRate;
    public float GetAttackRange() => attackRange;
    public float GetMeleeRadius() => meleeRadius;
    public float GetAttackAngle() => attackAngle;
    public LayerMask GetAttackableLayerMask() => attackableLayerMask;
    public bool GetRequireLineOfSight() => requireLineOfSight;
    public bool GetUsePhysicsForMelee() => usePhysicsForMelee;
    public float GetMeleeForce() => meleeForce;
    public float GetMeleeUpwardForce() => meleeUpwardForce;
    public GameObject GetProjectilePrefab() => projectilePrefab;
    public Transform[] GetShootPoints() => shootPoints;
    public float GetProjectileSpeed() => projectileSpeed;
    public bool GetBurstFire() => burstFire;
    public int GetBurstCount() => burstCount;
    public float GetBurstDelay() => burstDelay;
    public bool GetUseRandomShootPoint() => useRandomShootPoint;
    public float GetProjectileSpread() => projectileSpread;
    public float GetProjectileLifetime() => projectileLifetime;

    #endregion
}
