using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class DuendeMeleeAI : EnemyController
{
    [Header("Duende Settings")]
    [SerializeField] float chargeSpeedMultiplier = 2f; // Velocidad durante el ataque
    [SerializeField] float chargeDuration = 0.5f; // Duración del ataque rápido
    [SerializeField] float attackWindup = 0.2f; // Tiempo de preparación antes del ataque

    private float baseSpeed; // Para almacenar la velocidad normal

    // En DuendeMeleeAI.cs - Modificar el Awake
    protected override void Awake()
    {
        base.Awake();

        // Configurar NavMeshAgent
        navAgent.angularSpeed = 720f; // Aumentar velocidad de rotación
        navAgent.acceleration = 50f; // Aceleración rápida
        navAgent.stoppingDistance = 0.1f; // Distancia mínima
        navAgent.autoBraking = false; // Evitar frenadas bruscas
        navAgent.updateRotation = false; // Control manual de rotación

        enemyRigidbody.constraints = RigidbodyConstraints.FreezeRotation; // Evitar rotación física
    }

    protected override void InitializeStrategies()
    {
        // Movimiento personalizado con capacidad de carga
        movementStrategy = new DuendeMovement(
            moveSpeed,
            stoppingDistance,
            faceTarget,
            avoidObstacles,
            chargeSpeedMultiplier,
            chargeDuration
        );

        // Ataque rápido con detección mejorada
        attackStrategy = new RapidMeleeAttack(
            attackDamage,
            attackRate,
            attackRange,
            attackWindup
        );
    }

    protected override void Update()
    {
        base.Update();

        if (isDead || !isActivated) return;

        // Lógica específica del duende
        HandleSpeedBoost();
        FixedUpdate();
    }
    // Añadir en Update para control manual de rotación
    void FixedUpdate()
    {
        if (target != null && !isDead)
        {
            Vector3 lookDirection = (target.position - transform.position).normalized;
            lookDirection.y = 0;
            Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                Time.deltaTime * 20f // Aumentar velocidad de rotación
            );
        }
    }
    void HandleSpeedBoost()
    {
        // Aumentar velocidad cuando está cerca del jugador
        if (movementStrategy.GetDistanceToTarget() <= attackRange * 1.5f)
        {
            navAgent.speed = baseSpeed * chargeSpeedMultiplier;
        }
        else
        {
            navAgent.speed = baseSpeed;
        }
    }

    // Implementación de movimiento especializado
    protected class DuendeMovement : NavMeshMovement
    {
        private float chargeMultiplier;
        private float chargeTime;

        public DuendeMovement(float speed, float stopDistance, bool faceTarget,
                            bool avoidObstacles, float chargeMult, float chargeDur)
            : base(speed, stopDistance, faceTarget, avoidObstacles)
        {
            chargeMultiplier = chargeMult;
            chargeTime = chargeDur;
        }

        public override void Move()
        {
            base.Move();

            // Lógica de carga agresiva
            if (Agent != null && Agent.remainingDistance <= stoppingDistance * 2f)
            {
                Agent.speed *= chargeMultiplier;
            }
        }
    }

    // Implementación de ataque rápido
    protected class RapidMeleeAttack : IEnemyAttack
    {
        private Transform enemy;
        private Transform target;
        private float damage;
        private float attackCooldown;
        private float attackRange;
        private float windupTime;
        private float lastAttackTime;
        private float damageMultiplier = 1f;

        public RapidMeleeAttack(float dmg, float cooldown, float range, float windup)
        {
            damage = dmg;
            attackCooldown = cooldown;
            attackRange = range;
            windupTime = windup;
        }

        public void Initialize(Transform enemy, Transform target)
        {
            this.enemy = enemy;
            this.target = target;
        }

        public bool CanAttack()
        {
            return Time.time > lastAttackTime + attackCooldown &&
                   Vector3.Distance(enemy.position, target.position) <= attackRange;
        }

        public void Attack()
        {
            enemy.GetComponent<MonoBehaviour>().StartCoroutine(PerformAttack());
        }

        private IEnumerator PerformAttack()
        {
            lastAttackTime = Time.time;
            yield return new WaitForSeconds(windupTime);

            // Añadir debug visual
            Debug.DrawRay(enemy.position, enemy.forward * attackRange, Color.red, 2f);

            // Modificar detección con parámetros correctos
            Collider[] hits = Physics.OverlapSphere(
                enemy.position + enemy.forward * 0.5f, // Offset frontal
                attackRange * 1.2f,
                LayerMask.GetMask("Player"), // Capa explícita
                QueryTriggerInteraction.Ignore // Ignorar triggers
            );

            foreach (var hit in hits)
            {
                if (hit.TryGetComponent<IDamageable>(out var damageable))
                {
                    damageable.TakeDamage(damage * damageMultiplier);
                    Debug.Log($"Daño aplicado a {hit.name}");
                }
            }
        }

        public void SetTarget(Transform target) => this.target = target;
        public void SetDamageMultiplier(float multiplier) => damageMultiplier = multiplier;
    }

    // Configuración de muerte específica
    protected override IEnumerator DeathSequence()
    {
        navAgent.speed = 0; // Congelar movimiento
        yield return base.DeathSequence();
    }
}