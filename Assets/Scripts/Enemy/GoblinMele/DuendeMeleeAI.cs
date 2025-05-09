using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class DuendeMeleeAI : EnemyController
{
    [Header("Duende Settings")]
    [SerializeField] float chargeSpeedMultiplier = 2f; // Velocidad durante el ataque
    [SerializeField] float chargeDuration = 0.5f; // Duraci�n del ataque r�pido
    [SerializeField] float attackWindup = 0.2f; // Tiempo de preparaci�n antes del ataque

    private float baseSpeed; // Para almacenar la velocidad normal

    protected override void Awake()
    {
        base.Awake();

        // Configuraci�n mejorada
        navAgent.speed = 8f;
        navAgent.angularSpeed = 720f;
        navAgent.acceleration = 40f;
        navAgent.stoppingDistance = 0.3f;
        navAgent.avoidancePriority = 50;

        enemyRigidbody.interpolation = RigidbodyInterpolation.Interpolate;
        enemyRigidbody.constraints = RigidbodyConstraints.FreezeRotation;
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

        // Ataque r�pido con detecci�n mejorada
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

        // L�gica espec�fica del duende
        HandleSpeedBoost();
        FixedUpdate();
    }
    // A�adir en Update para control manual de rotaci�n
    void FixedUpdate()
    {
        if (target != null && !isDead)
        {
            // Rotaci�n suave mejorada
            Vector3 direction = (target.position - transform.position).normalized;
            direction.y = 0;

            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    Time.deltaTime * 25f
                );
            }
        }
    }
    void HandleSpeedBoost()
    {
        // Aumentar velocidad cuando est� cerca del jugador
        if (movementStrategy.GetDistanceToTarget() <= attackRange * 1.5f)
        {
            navAgent.speed = baseSpeed * chargeSpeedMultiplier;
        }
        else
        {
            navAgent.speed = baseSpeed;
        }
    }

    // Implementaci�n de movimiento especializado
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

            // L�gica de carga agresiva
            if (Agent != null && Agent.remainingDistance <= stoppingDistance * 2f)
            {
                Agent.speed *= chargeMultiplier;
            }
        }
    }

    // Implementaci�n de ataque r�pido
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

            // A�adir debug visual
            Debug.DrawRay(enemy.position, enemy.forward * attackRange, Color.red, 2f);

            // Modificar detecci�n con par�metros correctos
            Collider[] hits = Physics.OverlapSphere(
                enemy.position + enemy.forward * 0.5f, // Offset frontal
                attackRange * 1.2f,
                LayerMask.GetMask("Player"), // Capa expl�cita
                QueryTriggerInteraction.Ignore // Ignorar triggers
            );

            foreach (var hit in hits)
            {
                if (hit.TryGetComponent<IDamageable>(out var damageable))
                {
                    damageable.TakeDamage(damage * damageMultiplier);
                    Debug.Log($"Da�o aplicado a {hit.name}");
                }
            }
        }

        public void SetTarget(Transform target) => this.target = target;
        public void SetDamageMultiplier(float multiplier) => damageMultiplier = multiplier;
    }

    // Configuraci�n de muerte espec�fica
    protected override IEnumerator DeathSequence()
    {
        navAgent.speed = 0; // Congelar movimiento
        yield return base.DeathSequence();
    }
}