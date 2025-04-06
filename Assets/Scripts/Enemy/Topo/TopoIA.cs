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
    [SerializeField] private float tiempoEntreAtaques = 1f;

    protected override void Awake()
    {
        movementType = MovementType.NavMesh;
        attackType = AttackType.Melee;

        base.Awake();
        originalSpeed = moveSpeed;
    }

    protected override void InitializeStrategies()
    {
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

    protected override void Start()
    {
        base.Start();
        StartCoroutine(CicloComportamiento());
        ToggleComponents(false);
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
        ((TopoMovement)movementStrategy).SetSpeed(originalSpeed);

        float timer = 0f;
        while (timer < tiempoPersecucion)
        {
            movementStrategy.Move();
            timer += Time.deltaTime;
            yield return null;
        }
    }

    IEnumerator FaseSumergido()
    {
        ToggleComponents(false);
        estaSumergido = true;
        ((TopoMovement)movementStrategy).SetSpeed(velocidadSumergido);
        burrowParticles.Play();

        yield return StartCoroutine(MoveToTarget());

        burrowParticles.Stop();
        yield return new WaitForSeconds(tiempoEsperaEmergido);
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

    public override void TakeDamage(float damageAmount)
    {
        if (estaSumergido) return;
        base.TakeDamage(damageAmount);
    }

    protected override IEnumerator DeathSequence()
    {
        Deactivate();
        ToggleComponents(false);
        burrowParticles.Stop();

        yield return base.DeathSequence();

        if (spawner != null) spawner.EnemyEliminated(gameObject);
        Destroy(gameObject);
    }

    protected class TopoMovement : IEnemyMovement
    {
        private NavMeshAgent agent;
        private Transform enemyTransform;
        private Transform targetTransform;
        private float moveSpeed;
        private float stoppingDistance;
        private bool faceTarget;
        private bool avoidObstacles;

        public TopoMovement(float speed, float stopDistance, bool faceTarget, bool avoidObstacles)
        {
            this.moveSpeed = speed;
            this.stoppingDistance = stopDistance;
            this.faceTarget = faceTarget;
            this.avoidObstacles = avoidObstacles;
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
                agent.avoidancePriority = avoidObstacles ? 50 : 99;
            }
        }

        public void Move()
        {
            if (agent != null && agent.enabled && targetTransform != null)
            {
                agent.SetDestination(targetTransform.position);
            }
        }

        public void SetSpeed(float newSpeed)
        {
            if (agent != null) agent.speed = newSpeed;
        }

        public void SetTarget(Transform target) => targetTransform = target;
        public void Stop() { if (agent != null) agent.isStopped = true; }
        public void Resume() { if (agent != null) agent.isStopped = false; }
        public bool IsInRange(float range) => GetDistanceToTarget() <= range;
        public float GetDistanceToTarget() => Vector3.Distance(enemyTransform.position, targetTransform.position);
    }

    protected class MeleeContactAttack : IEnemyAttack
    {
        private float damage;
        private float attackCooldown;
        private float attackDistance;
        private float lastAttackTime;
        private float damageMultiplier = 1f;

        public MeleeContactAttack(float damage, float cooldown, float distance)
        {
            this.damage = damage;
            this.attackCooldown = cooldown;
            this.attackDistance = distance;
        }

        public void Initialize(Transform enemy, Transform target) { }
        public void SetTarget(Transform target) { }
        public bool CanAttack() => Time.time > lastAttackTime + attackCooldown;

        public void Attack()
        {
            lastAttackTime = Time.time;
        }

        public void SetDamageMultiplier(float multiplier) => damageMultiplier = multiplier;
    }
}