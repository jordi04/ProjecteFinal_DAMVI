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

    [SerializeField] float stoppingDistancePersonalizada = 1.5f;
    private bool puedeAtacar = true;

    protected override void Awake()
    {
        base.Awake();

        navAgent.stoppingDistance = rangoAtaque * 0.25f;
        navAgent.angularSpeed = 720f;

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
        //animator.SetTrigger(attackAnimTrigger);

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