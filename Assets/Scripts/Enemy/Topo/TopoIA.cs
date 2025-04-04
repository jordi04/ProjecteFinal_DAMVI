using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class TopoIA : EnemyController
{
    [Header("Topo Settings")]
    [SerializeField] float tiempoPersecucion = 3f;
    [SerializeField] float tiempoEsperaEmergido = 0.5f;
    [SerializeField] float velocidadSumergido = 6f;
    [SerializeField] ParticleSystem burrowParticles;

    [Header("Component References")]
    [SerializeField] Collider damageCollider;
    [SerializeField] Renderer bodyRenderer;

    private bool estaSumergido = false;
    private float originalSpeed;

    protected override void Awake()
    {
        // Configurar tipo de enemigo
        movementType = MovementType.NavMesh;
        attackType = AttackType.Melee;

        base.Awake();

        originalSpeed = moveSpeed;
        if (navAgent != null) navAgent.speed = originalSpeed;
    }

    protected override void Start()
    {
        base.Start();
        StartCoroutine(CicloComportamiento());
        ToggleComponents(false);
    }

    protected override void InitializeStrategies()
    {
        // Usar movimiento NavMesh personalizado
        movementStrategy = new TopoMovement(
            moveSpeed,
            stoppingDistance,
            faceTarget,
            avoidObstacles
        );

        attackStrategy = new MeleeContactAttack(
            attackDamage,
            tiempoEntreAtaques,
            attackRange
        );
    }

    IEnumerator CicloComportamiento()
    {
        while (!isDead)
        {
            yield return StartCoroutine(FasePersecucion());
            yield return StartCoroutine(FaseSumergido());
        }
    }

    IEnumerator FasePersecucion()
    {
        ToggleComponents(true);
        estaSumergido = false;
        moveSpeed = originalSpeed;
        UpdateMovementSpeed();

        float timer = 0f;
        while (timer < tiempoPersecucion)
        {
            if (movementStrategy != null) movementStrategy.Move();
            timer += Time.deltaTime;
            yield return null;
        }
    }

    IEnumerator FaseSumergido()
    {
        ToggleComponents(false);
        estaSumergido = true;
        moveSpeed = velocidadSumergido;
        UpdateMovementSpeed();
        burrowParticles.Play();

        if (movementStrategy != null)
        {
            movementStrategy.SetTarget(target);
            yield return StartCoroutine(MoveToTarget());
        }

        burrowParticles.Stop();
        yield return new WaitForSeconds(tiempoEsperaEmergido);
    }

    void UpdateMovementSpeed()
    {
        if (movementStrategy is TopoMovement topoMovement)
        {
            topoMovement.SetSpeed(moveSpeed);
        }
    }

    IEnumerator MoveToTarget()
    {
        while (movementStrategy.GetDistanceToTarget() > attackRange)
        {
            movementStrategy.Move();
            yield return null;
        }
    }

    void ToggleComponents(bool state)
    {
        damageCollider.enabled = state;
        bodyRenderer.enabled = state;
    }

    // Implementación de movimiento especializado
    protected class TopoMovement : NavMeshMovement
    {
        public TopoMovement(float speed, float stopDistance, bool faceTarget, bool avoidObstacles)
            : base(speed, stopDistance, faceTarget, avoidObstacles) { }

        public void SetSpeed(float newSpeed)
        {
            if (agent != null) agent.speed = newSpeed;
        }
    }

    // Implementación de ataque por contacto
    protected class MeleeContactAttack : IEnemyAttack
    {
        private float damage;
        private float attackCooldown;
        private float attackDistance;
        private float lastAttackTime;

        public MeleeContactAttack(float damage, float cooldown, float distance)
        {
            this.damage = damage;
            this.attackCooldown = cooldown;
            this.attackDistance = distance;
        }

        public void Initialize(Transform enemy, Transform target) { }
        public void SetTarget(Transform target) { }

        public bool CanAttack()
        {
            return Time.time > lastAttackTime + attackCooldown;
        }

        public void Attack()
        {
            lastAttackTime = Time.time;
        }

        public void SetDamageMultiplier(float multiplier)
        {
            damage *= multiplier;
        }
    }

    // Métodos heredados
    public override void TakeDamage(float damageAmount)
    {
        if (estaSumergido) return; // Inmune cuando está sumergido

        base.TakeDamage(damageAmount);

        if (currentHealth <= 0)
        {
            StartCoroutine(DeathSequence());
        }
    }

    protected override IEnumerator DeathSequence()
    {
        Deactivate();
        ToggleComponents(false);
        burrowParticles.Stop();

        yield return base.DeathSequence();

        if (spawner != null)
        {
            spawner.EnemyEliminated(gameObject);
        }
        Destroy(gameObject);
    }
}