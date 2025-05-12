using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class GolemBossEnemyController : EnemyController
{
    [Header("Golem Boss Settings")]
    [SerializeField] private float rockThrowCooldown = 5f;
    [SerializeField] private float enrageHealthThreshold = 0.3f; // 30% health
    [SerializeField] private float enragedDamageMultiplier = 1.5f;
    [SerializeField] private float enragedSpeedMultiplier = 1.3f;
    [SerializeField] private GameObject enrageEffect;
    [SerializeField] private float specialAttackCooldown = 15f;

    [Header("Rock Attack Settings")]
    [SerializeField] private List<GameObject> availableRocks = new List<GameObject>(); // List of rocks in the scene
    [SerializeField] private Transform rightHandAttachPoint;
    [SerializeField] private Transform leftHandAttachPoint;
    [SerializeField] private float rockDamage = 25f;
    [SerializeField] private float rockThrowForce = 20f;
    [SerializeField] private float rockUpwardForce = 2f;
    [SerializeField] private float rangedAttackMinDistance = 5f;
    [SerializeField] private float rangedAttackMaxDistance = 15f;
    [SerializeField] private float rockPickupRange = 3f;
    [SerializeField] private float telekinesisMoveSpeed = 5f; // Speed at which rocks move to hand
    [SerializeField] private GameObject telekinesisEffectPrefab; // Optional particle effect

    [Header("Energy Ball Attack Settings")]
    [SerializeField] private GameObject energyBallPrefab;
    [SerializeField] private float energyBallDamage = 15f;
    [SerializeField] private float energyBallThrowForce = 15f;
    [SerializeField] private float meleeAttackRadius = 3f;

    [Header("Death Instanced Objects")]
    [SerializeField] private Transform coreSpawnPoint;
    [SerializeField] private GameObject coreInstance;
    [SerializeField] private GameObject tooltipEndGame;

    [Header("Rotation Settings")]
    [SerializeField] private float rotationSpeed = 5f; // Controls how quickly the golem rotates
    [SerializeField] private float minRotationAngle = 5f; // Minimum angle difference to consider rotation complete

    private bool isRotatingTowardsTarget = false;
    private Quaternion targetRotation;


    private bool isEnraged = false;
    private float lastRockThrowTime = -100f;
    private float lastSpecialAttackTime = -100f;
    private bool isPerformingSpecialAttack = false;
    private GameObject currentProjectile = null;
    private AttackType currentAttackType = AttackType.None;
    private GameObject targetRock = null;
    private bool isMovingToRock = false;
    private bool isTelekinesisActive = false;
    private GameObject telekinesisEffect = null;

    protected override void Awake()
    {
        base.Awake();

        // Ensure we have the required components
        if (rightHandAttachPoint == null)
        {
            Debug.LogError("Right hand attach point is missing! Please assign it in the inspector.");
        }

        // Initialize rock list if empty by finding all rocks with appropriate tag
        if (availableRocks.Count == 0)
        {
            //no funciona
            GameObject[] sceneRocks = GameObject.FindGameObjectsWithTag("GolemRock");
            availableRocks.AddRange(sceneRocks);
            Debug.Log($"Found {availableRocks.Count} rocks in the scene");
        }

        // Initialize wandering
        wanderPoint = transform.position;
        nextWanderTime = Time.time + Random.Range(minWanderWaitTime, maxWanderWaitTime);
    }
    protected override void CheckPlayerRanges()
    {
        if (target == null) return;

        // Don't change states if currently attacking or using telekinesis
        if (isAttacking || isTelekinesisActive) return;

        float distanceToTarget = Vector3.Distance(transform.position, target.position);

        // Update detection flags
        playerInSightRange = distanceToTarget <= playerDetectionRadius;

        bool inMeleeRange = distanceToTarget <= meleeAttackRadius;
        bool inRangedRange = distanceToTarget > rangedAttackMinDistance &&
                             distanceToTarget <= rangedAttackMaxDistance &&
                             availableRocks.Count > 0 &&
                             Time.time > lastRockThrowTime + rockThrowCooldown;

        // Golem can attack if in melee range OR in ranged range with rocks available
        playerInAttackRange = inMeleeRange || inRangedRange;

        // Only change states if not currently in a special action
        if (currentState != EnemyState.Dead && currentState != EnemyState.Retreating && !isMovingToRock)
        {
            if (playerInAttackRange && currentState != EnemyState.Attacking && !isAttacking)
            {
                currentState = EnemyState.Attacking;
                Debug.Log("Golem entered ATTACKING state");
            }
            else if (playerInSightRange && !playerInAttackRange && currentState != EnemyState.Chasing)
            {
                currentState = EnemyState.Chasing;
                Debug.Log("Golem entered CHASING state");
            }
            else if (!playerInSightRange && !playerInAttackRange)
            {
                // Return to appropriate idle state when player leaves detection radius
                if (movementType == MovementType.Patrol)
                {
                    currentState = EnemyState.Patrolling;
                }
                else
                {
                    currentState = EnemyState.Idle;
                }
                //Debug.Log("Golem returned to IDLE/PATROL state");
            }
        }
    }


    // Override the state machine to handle the golem's unique behaviors
    protected void StateMachine()
    {
        if (isDead) return;

        switch (currentState)
        {
            case EnemyState.Idle:
                HandleWandering();
                break;

            case EnemyState.Patrolling:
                // For golem, patrolling is just wandering
                HandleWandering();
                break;

            case EnemyState.Chasing:
                // Only chase if not performing any special action
                if (!isMovingToRock && !isTelekinesisActive && !isAttacking)
                {
                    if (navAgent != null && navAgent.enabled && navAgent.isOnNavMesh && target != null)
                    {
                        // Check if path is valid before setting destination
                        NavMeshPath path = new NavMeshPath();
                        if (navAgent.CalculatePath(target.position, path) && path.status != NavMeshPathStatus.PathInvalid)
                        {
                            navAgent.SetDestination(target.position);
                            navAgent.isStopped = false; // Ensure agent is not stopped
                        }
                        else
                        {
                            // Path is invalid, try to find a valid position nearby
                            NavMeshHit hit;
                            if (NavMesh.SamplePosition(target.position, out hit, 10f, NavMesh.AllAreas))
                            {
                                navAgent.SetDestination(hit.position);
                                navAgent.isStopped = false;
                            }
                        }
                    }
                }
                break;

            case EnemyState.Attacking:
                // Handle the golem's attack logic
                HandleAttack();
                break;

            case EnemyState.Retreating:
                // Golem doesn't retreat, but just in case
                HandleWandering();
                break;

            case EnemyState.Dead:
                // Nothing to do when dead
                break;
        }

        // Always check player ranges to update state
        CheckPlayerRanges();
    }

    // Override Update to ensure our state machine is called properly
    protected override void Update()
    {
        if (isDead) return;

        // Handle telekinesis rock movement if active
        if (isTelekinesisActive && targetRock != null)
        {
            UpdateTelekinesisMovement();
        }

        // Only run state machine if not currently rotating towards a target
        if (!isRotatingTowardsTarget)
        {
            // Run our custom state machine logic
            StateMachine();
        }

        // Update animation states based on current behavior
        UpdateAnimationState();
    }


    // This method is now primarily for visual effects before OnGrabRock is called from animation
    private void UpdateTelekinesisMovement()
    {
        if (targetRock == null || rightHandAttachPoint == null) return;

        // Move rock toward hand using telekinesis - this is mainly for visual effect
        // before the animation event grabs the rock
        float step = telekinesisMoveSpeed * Time.deltaTime;
        targetRock.transform.position = Vector3.MoveTowards(
            targetRock.transform.position,
            rightHandAttachPoint.position,
            step);

        // If rock reaches hand position, we can optionally complete pickup
        // but the animation event should handle this
        if (Vector3.Distance(targetRock.transform.position, rightHandAttachPoint.position) < 0.1f)
        {
            // We don't need to call CompleteRockPickup() here anymore
            // as the animation event will handle grabbing the rock

            // Just clean up the telekinesis effect
            if (telekinesisEffect != null)
            {
                Destroy(telekinesisEffect);
                telekinesisEffect = null;
            }

            isTelekinesisActive = false;
        }
    }

    private void CompleteRockPickup()
    {
        // The rock is now moved by the telekinesis system, but we'll keep this method
        // in case we need to manually complete the pickup

        if (targetRock == null) return;

        // Note: We don't need to manually attach the rock here anymore
        // because the OnGrabRock animation event will handle it

        // Remove telekinesis effect if it exists
        if (telekinesisEffect != null)
        {
            Destroy(telekinesisEffect);
            telekinesisEffect = null;
        }

        currentProjectile = targetRock;
        isTelekinesisActive = false;

        // Note: We don't need to trigger RangeAttack again here
        // as it's already triggered in StartTelekinesis

        PlayAttackSound();
    }

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();

        // Draw melee attack radius
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, meleeAttackRadius);

        // Draw ranged attack min/max distance
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, rangedAttackMinDistance);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, rangedAttackMaxDistance);

        // Draw rock pickup range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, rockPickupRange);

        //player detection radius
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, playerDetectionRadius);
    }

    [Header("Wandering Settings")]
    [SerializeField] private float wanderRadius = 15f;
    [SerializeField] private float minWanderWaitTime = 3f;
    [SerializeField] private float maxWanderWaitTime = 8f;
    [SerializeField] private float playerDetectionRadius = 20f;

    private Vector3 wanderPoint;
    private float nextWanderTime;
    private bool isWandering = false;
    private bool isRotating;

    protected override void HandleAttack()
    {
        if (isDead) return;

        // Check if player is in sight range
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        // If no player or player is too far away, wander
        if (player == null || (Vector3.Distance(transform.position, player.transform.position) > playerDetectionRadius))
        {
            // If we've lost our target, clear it
            if (target != null)
            {
                target = null;
                // If we were moving to a rock, cancel that action
                if (isMovingToRock)
                {
                    isMovingToRock = false;
                    //potser dona errors !!!!
                    targetRock = null;
                }
            }

            // Handle wandering behavior
            HandleWandering();
            return;
        }
        else
        {
            // Player found, set as target
            target = player.transform;
            isWandering = false;
        }

        float distanceToTarget = Vector3.Distance(transform.position, target.position);

        // If already attacking or using telekinesis, don't start a new attack
        if (isAttacking || isTelekinesisActive) return;

        // If moving to a rock, check if we've reached it
        if (isMovingToRock && targetRock != null)
        {
            float distanceToRock = Vector3.Distance(transform.position, targetRock.transform.position);

            // If we're close enough to the rock, grab it with telekinesis
            if (distanceToRock <= rockPickupRange)
            {
                Debug.Log("Close enough to grab rock with telekinesis");
                StartTelekinesis();
                isMovingToRock = false;
                return;
            }

            // Otherwise keep moving to the rock
            if (navAgent != null && navAgent.enabled && navAgent.isOnNavMesh)
            {
                navAgent.SetDestination(targetRock.transform.position);
            }
            return;
        }

        // Check if we should perform a special attack (only when enraged)
        if (isEnraged && Time.time > lastSpecialAttackTime + specialAttackCooldown)
        {
            PerformSpecialAttack();
            return;
        }

        // Prioritize ranged attacks if rocks are available and we're at appropriate distance
        if (availableRocks.Count > 0 &&
            distanceToTarget > rangedAttackMinDistance &&
            distanceToTarget <= rangedAttackMaxDistance &&
            Time.time > lastRockThrowTime + rockThrowCooldown)
        {
            FindNearestRock();
            return;
        }
        // If in melee range and melee attack is off cooldown
        else if (distanceToTarget <= meleeAttackRadius && Time.time > nextAttackTime)
        {
            // Higher chance to attack when player is close
            float attackChance = isEnraged ? chanceToAttack * 1.5f : chanceToAttack;
            if (Random.Range(0f, 100f) < attackChance)
            {
                Debug.Log("Performing energy ball attack");
                PerformEnergyBallAttack();
            }
            nextAttackTime = Time.time + 1f / attackRate;
        }
        // If we have no rocks and are out of melee range, move toward the target
        else if (navAgent != null && navAgent.enabled && navAgent.isOnNavMesh && distanceToTarget > meleeAttackRadius)
        {
            navAgent.SetDestination(target.position);
        }
    }

    private void HandleWandering()
    {
        // If already in an action, don't wander
        if (isAttacking || isTelekinesisActive) return;

        // Don't wander if we're already attacking or wandering
        if (isWandering)
        {
            // Check if we've reached the wander point
            if (navAgent != null && navAgent.enabled && navAgent.isOnNavMesh)
            {
                if (!navAgent.pathPending && navAgent.remainingDistance <= navAgent.stoppingDistance)
                {
                    // Reached destination, wait before wandering again
                    if (Time.time >= nextWanderTime)
                    {
                        isWandering = false;
                    }
                }
            }
            return;
        }

        // Start a new wander if time has elapsed
        if (Time.time >= nextWanderTime)
        {
            // Find a random point to wander to
            Vector3 randomDirection = Random.insideUnitSphere * wanderRadius;
            randomDirection += transform.position;

            NavMeshHit navHit;
            if (NavMesh.SamplePosition(randomDirection, out navHit, wanderRadius, NavMesh.AllAreas))
            {
                wanderPoint = navHit.position;

                // Set destination
                if (navAgent != null && navAgent.enabled && navAgent.isOnNavMesh)
                {
                    navAgent.SetDestination(wanderPoint);
                    isWandering = true;

                    // Set next wander time
                    nextWanderTime = Time.time + Random.Range(minWanderWaitTime, maxWanderWaitTime);

                    // Trigger move animation
                    if (animator != null)
                    {
                        animator.SetTrigger("Move");
                    }

                    Debug.Log("Golem is wandering to: " + wanderPoint);
                }
            }
        }
    }

    private void FindNearestRock()
    {
        if (availableRocks.Count == 0) return;

        // Remove any null references (destroyed rocks)
        availableRocks.RemoveAll(rock => rock == null);

        if (availableRocks.Count == 0) return;

        // Find the nearest rock
        GameObject nearestRock = null;
        float nearestDistance = float.MaxValue;

        foreach (GameObject rock in availableRocks)
        {
            float distance = Vector3.Distance(transform.position, rock.transform.position);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestRock = rock;
            }
        }

        if (nearestRock != null)
        {
            // Set target rock and move toward it
            targetRock = nearestRock;
            isMovingToRock = true;

            if (navAgent != null && navAgent.enabled && navAgent.isOnNavMesh)
            {
                navAgent.SetDestination(targetRock.transform.position);
                Debug.Log($"Moving to rock at {targetRock.transform.position}");
            }
        }
    }
    private void StartTelekinesis()
    {
        if (targetRock == null) return;

        Debug.Log("Starting telekinesis with rock: " + targetRock.name);

        // Remove the rock from available rocks
        availableRocks.Remove(targetRock);

        // First rotate towards the rock, then start telekinesis
        StartCoroutine(RotateTowardsTargetCoroutine(targetRock.transform.position, () => {
            // This code executes after rotation is complete

            // Start telekinesis
            isTelekinesisActive = true;

            // Disable physics on rock for telekinesis movement
            Rigidbody rockRb = targetRock.GetComponent<Rigidbody>();
            if (rockRb != null)
            {
                rockRb.isKinematic = true;
                rockRb.useGravity = false;
            }

            // Disable collider during telekinesis
            Collider rockCollider = targetRock.GetComponent<Collider>();
            if (rockCollider != null)
            {
                rockCollider.enabled = false;
            }

            // Create telekinesis effect if prefab is assigned
            if (telekinesisEffectPrefab != null)
            {
                telekinesisEffect = Instantiate(telekinesisEffectPrefab, targetRock.transform.position, Quaternion.identity);
                telekinesisEffect.transform.SetParent(targetRock.transform);
            }

            // Stop movement while using telekinesis
            if (navAgent != null)
            {
                navAgent.isStopped = true;
            }

            // Start the RangeAttack animation - this should trigger the animation events
            if (animator != null)
            {
                animator.SetTrigger("RangeAttack");
            }

            // Prepare for ranged attack
            isAttacking = true;
            currentAttackType = AttackType.Ranged;
            lastRockThrowTime = Time.time;

            Debug.Log("Started telekinesis on rock and triggered RangeAttack animation");
        }));
    }



    private void PerformEnergyBallAttack()
    {
        if (isDead || isAttacking) return;

        isAttacking = true;
        currentAttackType = AttackType.Melee;

        if (navAgent != null)
        {
            navAgent.isStopped = true;
        }

        // First rotate towards the target, then perform the attack
        if (target != null)
        {
            StartCoroutine(RotateTowardsTargetCoroutine(target.position, () => {
                // This code executes after rotation is complete
                if (animator != null)
                {
                    animator.SetTrigger("CloseAttack");
                    Debug.Log("Triggered CloseAttack animation after smooth rotation");
                }
                PlayAttackSound();
            }));
        }
        else
        {
            // No target, just play animation
            if (animator != null)
            {
                animator.SetTrigger("CloseAttack");
            }
            PlayAttackSound();
        }
    }


    private bool RotateSmooth(Vector3 lookDirection)
    {
        // Calculate the target rotation
        targetRotation = Quaternion.LookRotation(lookDirection);

        // Smoothly rotate towards the target
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

        // Calculate the angle difference between current and target rotation
        float angleDifference = Quaternion.Angle(transform.rotation, targetRotation);

        // Return true if rotation is complete (angle difference is small enough)
        return angleDifference <= minRotationAngle;
    }

    private void PerformSpecialAttack()
    {
        if (isDead || isAttacking) return;

        isAttacking = true;
        isPerformingSpecialAttack = true;
        lastSpecialAttackTime = Time.time;
        currentAttackType = AttackType.Special;

        if (navAgent != null)
        {
            navAgent.isStopped = true;
        }

        // Special attack animation and logic
        StartCoroutine(SpecialAttackSequence());
    }

    // Animation event method - called from RangeAttack animation
    // This should be connected to the first event emitter in the RangeAttack animation
    public void OnGrabRock()
    {

        Debug.Log("OnGrabRock animation event fired");
        if (targetRock == null)
        {
            Debug.LogError("targetRock is null in OnGrabRock!");
            return;
        }

        // Move rock to hand
        targetRock.transform.SetParent(rightHandAttachPoint);
        targetRock.transform.localPosition = Vector3.zero;
        targetRock.transform.localRotation = Quaternion.identity;

        // Disable physics
        Rigidbody rockRb = targetRock.GetComponent<Rigidbody>();
        if (rockRb != null)
        {
            rockRb.isKinematic = true;
        }

        // Disable collider
        Collider rockCollider = targetRock.GetComponent<Collider>();
        if (rockCollider != null)
        {
            rockCollider.enabled = false;
        }

        currentProjectile = targetRock;

        // Rotate towards player if target exists
        if (target != null)
        {
            StartCoroutine(RotateTowardsTargetCoroutine(target.position));
        }

        Debug.Log("Rock grabbed and attached to hand (animation event)");
    }


    // Animation event method - called from RangeAttack animation
    public void OnThrowRock()
    {
        Debug.Log("OnThrowRock animation event fired");
        if (currentProjectile == null)
        {
            Debug.LogError("currentProjectile is null in OnThrowRock!");
            return;
        }

        ThrowProjectile(rockDamage, rockThrowForce);
    }

    // Animation event method - called from CloseAttack animation
    public void OnCreateEnergyBall()
    {
        if (energyBallPrefab == null) return;
        //Trobam posicio intermitja entre la mà dreta i esquerra
        Vector3 instantiatePos = Vector3.Lerp(rightHandAttachPoint.position, leftHandAttachPoint.position, 0.5f);
        // Create energy ball and attach to hand
        currentProjectile = Instantiate(energyBallPrefab, instantiatePos, rightHandAttachPoint.rotation);
        currentProjectile.transform.SetParent(rightHandAttachPoint);

        Debug.Log("Energy ball created and attached to hand");
    }

    // Fix for the CS0428 error in the OnThrowEnergyBall method
    public void OnThrowEnergyBall()
    {
        if (currentProjectile == null) return;

        // Corrected the syntax for accessing the Rigidbody component
        Rigidbody energyBallRb = currentProjectile.GetComponent<Rigidbody>();
        if (energyBallRb != null)
        {
            energyBallRb.isKinematic = false;
        }

        // Corrected the call to GetComponent and Initialize
        GolemEnergyBallProjectile energyBallInstance = currentProjectile.GetComponent<GolemEnergyBallProjectile>();
        if (energyBallInstance != null)
        {
            energyBallInstance.OnThrow(energyBallThrowForce, transform.forward);
        }

        Debug.Log("Energy ball thrown.");
    }

    private void ThrowProjectile(float damage, float force)
    {
        if (currentProjectile == null) return;

        // Store player position at the moment of throwing
        Vector3 throwTargetPosition = target != null ? target.position : transform.position + transform.forward * 10f;

        // Unparent the projectile
        currentProjectile.transform.SetParent(null);

        // Enable physics
        Rigidbody projectileRb = currentProjectile.GetComponent<Rigidbody>();
        if (projectileRb != null)
        {
            projectileRb.isKinematic = false;
            projectileRb.useGravity = true;

            // Calculate throw direction
            Vector3 throwDirection = (throwTargetPosition - currentProjectile.transform.position).normalized;

            // Apply force
            projectileRb.AddForce(throwDirection * force + Vector3.up * rockUpwardForce, ForceMode.Impulse);
        }

        // Enable collider
        Collider projectileCollider = currentProjectile.GetComponent<Collider>();
        if (projectileCollider != null)
        {
            projectileCollider.enabled = true;
        }

        // Add damage component to projectile
        GolemProjectile projectileScript = currentProjectile.GetComponent<GolemProjectile>();
        if (projectileScript == null)
        {
            projectileScript = currentProjectile.AddComponent<GolemProjectile>();
        }

        // Apply enrage multiplier if needed
        if (isEnraged)
        {
            damage *= enragedDamageMultiplier;
        }

        projectileScript.Initialize(damage, gameObject);

        // Reset references
        currentProjectile = null;
        targetRock = null;
    }

    // Animation event method - called when attack animation ends
    public void OnAttackAnimationEnd() //Té errors !!!!
    {
        isAttacking = false;

        // Resume movement if needed
        if (navAgent != null && currentState == EnemyState.Chasing)
        {
            navAgent.isStopped = false;
        }
        // If we still have a projectile attached (attack interrupted), destroy it if it's an energy ball
        if (currentProjectile != null)
        {
            if (currentAttackType == AttackType.Melee) 
            {
                // Energy ball can be destroyed
                Destroy(currentProjectile);
            }
            else if (currentAttackType == AttackType.Ranged)
            {
                // Return rock to available rocks
                if (!availableRocks.Contains(currentProjectile))
                {
                    availableRocks.Add(currentProjectile);
                }
                currentProjectile.transform.SetParent(null);
            }
            //no entenc currentProjectile
            currentProjectile = null;
        }

        targetRock = null;
    }

    //This is the melee attack with the energy ball
    private IEnumerator SpecialAttackSequence()
    {
        

        // Stop movement during special attack
        if (navAgent != null)
            navAgent.isStopped = true;

        // Play special attack animation/effects
        if (animator != null)
        {
            animator.SetTrigger("CloseAttack");
        }

        // Create multiple energy balls
        int energyBallCount = 5;
        GameObject[] energyBalls = new GameObject[energyBallCount];

        yield return new WaitForSeconds(1f); // Wind-up time

        for (int i = 0; i < energyBallCount; i++)
        {
            energyBalls[i] = Instantiate(energyBallPrefab, transform.position + Vector3.up * 2f, Quaternion.identity);
        }

        yield return new WaitForSeconds(0.5f);

        // Throw all energy balls in a circular!!!!! pattern it should only throw one!!!!
        for (int i = 0; i < energyBallCount; i++)
        {
            if (energyBalls[i] != null)
            {
                // Calculate throw direction in a circular pattern
                float angle = i * (360f / energyBallCount);
                Vector3 direction = Quaternion.Euler(0, angle, 0) * transform.forward;

                Rigidbody energyBallRb = energyBalls[i].GetComponent<Rigidbody>();
                if (energyBallRb != null)
                {
                    energyBallRb.isKinematic = false;
                    energyBalls[i].GetComponent<GolemEnergyBallProjectile>().OnThrow(energyBallThrowForce, direction);
                    Debug.Log("Energy ball thrown in direction: " + direction);

                    //energyBallRb.AddForce(direction * energyBallThrowForce * 1.5f + Vector3.up * rockUpwardForce, ForceMode.Impulse);
                }

                Collider energyBallCollider = energyBalls[i].GetComponent<Collider>();
                if (energyBallCollider != null)
                {
                    energyBallCollider.enabled = true;
                }

                GolemProjectile projectileScript = energyBalls[i].GetComponent<GolemProjectile>();
                if (projectileScript == null)
                {
                    projectileScript = energyBalls[i].AddComponent<GolemProjectile>();
                }

                projectileScript.Initialize(energyBallDamage * 1.5f * (isEnraged ? enragedDamageMultiplier : 1f), gameObject);
            }

            // Small delay between throws for visual effect
            yield return new WaitForSeconds(0.1f);
        }

        // Perform area damage around the boss
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, attackRange, attackableLayerMask);
        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.transform == transform) continue;

            IDamageable damageable = hitCollider.GetComponent<IDamageable>();
            if (damageable != null)
            {
                //si no es player fer mal als colliders hit
                if (!hitCollider.CompareTag("Player"))
                {
                    damageable.TakeDamage(attackDamage * 2 * damageMultiplier);
                }
            }
            //si es player fer-li mal
            if (hitCollider.CompareTag("Player"))
            {
                ManaSystem.instance.TakeDamage(attackDamage * 2 * damageMultiplier);
            }

            // Add force to push away
            Rigidbody rb = hitCollider.GetComponent<Rigidbody>();
            if (rb != null && !rb.isKinematic)
            {
                Vector3 direction = (hitCollider.transform.position - transform.position).normalized;
                rb.AddForce(direction * meleeForce * 2 + Vector3.up * 2f, ForceMode.Impulse);
            }
        }

        yield return new WaitForSeconds(1f); // Recovery time

        // Resume normal behavior
        if (navAgent != null)
            navAgent.isStopped = false;

        isAttacking = false;
        isPerformingSpecialAttack = false;
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

        // Trigger a visual indication of enrage (could be animation trigger)
        if (animator != null)
        {
            animator.SetTrigger("Enrage");
        }

        Debug.Log("Golem boss entered enraged state!");
    }

    private void UpdateAnimationState()
    {
        if (animator == null) return;

        if (isDead)
        {
            animator.SetBool("Death", true);
            return;
        }

        // Check if navAgent is valid before using it
        bool isMoving = false;
        if (navAgent != null && navAgent.enabled && navAgent.isOnNavMesh)
        {
            isMoving = !navAgent.isStopped && navAgent.velocity.magnitude > 0.1f;
        }

        // If attacking, always set Move to 0 to prevent animation conflicts
        if (isAttacking)
        {
            //animator.SetTrigger("Move");
        }
        else
        {
            // Only set movement parameter when not attacking
            float moveValue = 0;
            if (isMoving)
            {
                moveValue = (isEnraged || isWandering) ? 2f : 1f;
            }
            animator.SetTrigger("Move");
        }
    }
    //Així rotarà bé entre animacions
    private IEnumerator RotateTowardsTargetCoroutine(Vector3 targetPosition, System.Action onComplete = null)
    {
        isRotatingTowardsTarget = true;

        // Calculate direction to look at (ignoring Y axis for level rotation)
        Vector3 lookDirection = targetPosition - transform.position;
        lookDirection.y = 0; // Keep rotation level

        if (lookDirection == Vector3.zero)
        {
            isRotatingTowardsTarget = false;
            if (onComplete != null) onComplete();
            yield break;
        }

        // Rotate until we're facing the target
        bool rotationComplete = false;
        while (!rotationComplete && !isDead)
        {
            rotationComplete = RotateSmooth(lookDirection);
            yield return null;
        }

        isRotatingTowardsTarget = false;

        // Call the completion callback if provided
        if (onComplete != null) onComplete();
    }


    protected override void Die()
    {
        if (isDead) return;

        // Clean up any telekinesis effects
        if (telekinesisEffect != null)
        {
            Destroy(telekinesisEffect);
        }

        // Set death animation
        if (animator != null)
        {
            animator.SetBool("Death", true);
        }
        //Comencar a instanciar objectes on death
        //Spawn tooltip per explicar 
        if (tooltipEndGame != null)
        {
            tooltipEndGame.SetActive(true);
        }
        if (coreInstance != null)
        {
            coreInstance.SetActive(true);
            coreInstance.transform.position = coreSpawnPoint.position;
        }
        base.Die();
    }
}

// Enum for attack types
public enum AttackType
{
    None,
    Melee,
    Ranged,
    Special
}