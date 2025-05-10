using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class DuendeMeleeAI : EnemyController
{
    [Header("Configuración Específica")]
    [SerializeField] float rangoAtaque = 0.25f;
    [SerializeField] float tiempoEntreAtaques = 0.5f;

    [Header("Referencias")]
    [SerializeField] Transform puntoAtaque;

    [Header("Valores Base")]
    [SerializeField] float baseDamage = 10f;
    [SerializeField] float baseHealth = 100f;
    [SerializeField] float baseSpeed = 3.5f;

    [SerializeField] float stoppingDistancePersonalizada = 1.5f;
    private bool puedeAtacar = true;

    protected override void Awake()
    {
        // 1. Ejecutar Awake() del EnemyController
        base.Awake();

        // 2. Configurar valores específicos del goblin
        navAgent.stoppingDistance = rangoAtaque;
        navAgent.angularSpeed = 720f;

        // 3. Inicializar con valores base del prefab
        attackDamage = baseDamage;
        maxHealth = baseHealth;
        moveSpeed = baseSpeed;

        Debug.Log("DuendeMeleeAI inicializado - " +
                 $"Daño: {attackDamage}, " +
                 $"Salud: {maxHealth}, " +
                 $"Velocidad: {moveSpeed}");
    }

    protected override void Start()
    {
        base.Start(); 
    }

    void ConfigurarComponentesAdicionales()
    {
        navAgent.stoppingDistance = rangoAtaque;
        navAgent.angularSpeed = 720f;
        navAgent.speed = moveSpeed;
    }

    protected override void Update()
    {
        base.Update();

        if (IsDead() || target == null) return;

        GestionarAtaque();
    }

    void GestionarAtaque()
    {
        if (Vector3.Distance(transform.position, target.position) <= rangoAtaque && puedeAtacar)
        {
            StartCoroutine(AtaqueMelee());
        }
    }

    IEnumerator AtaqueMelee()
    {
        puedeAtacar = false;
        animator.SetTrigger(attackAnimTrigger);

        yield return new WaitForSeconds(0.3f);

        Collider[] objetivos = Physics.OverlapSphere(
            puntoAtaque.position,
            rangoAtaque,
            attackableLayerMask
        );

        foreach (Collider col in objetivos)
        {
            if (col.CompareTag("Player"))
            {
                col.GetComponent<IDamageable>()?.TakeDamage(attackDamage);
            }
        }

        yield return new WaitForSeconds(tiempoEntreAtaques);
        puedeAtacar = true;
    }

    public override void TakeDamage(float cantidad)
    {
        if (IsDead()) return;

        base.TakeDamage(cantidad);
        StartCoroutine(EfectoDanoPersonalizado());
    }

    IEnumerator EfectoDanoPersonalizado()
    {
        SetColor(Color.cyan);
        yield return new WaitForSeconds(flashDuration);
        if (!IsDead()) ResetColor();
    }

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(puntoAtaque.position, rangoAtaque);
    }

    // Sobrescribir métodos de multiplicación
    public override void SetDamageMultiplier(float multiplier)
    {
        base.SetDamageMultiplier(multiplier);
        attackDamage = baseDamage * multiplier;
    }

    public override void SetHealthMultiplier(float multiplier)
    {
        base.SetHealthMultiplier(multiplier);
        maxHealth = baseHealth * multiplier;
        currentHealth = maxHealth;
    }

    public override void SetSpeedMultiplier(float multiplier)
    {
        base.SetSpeedMultiplier(multiplier);
        moveSpeed = baseSpeed * multiplier;
        if (navAgent != null)
        {
            navAgent.speed = moveSpeed;
        }
    }
}