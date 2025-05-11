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

        // Configuración de movimiento AGGRESIVA
        navAgent.stoppingDistance = 0.1f; // ¡Que se pegue al jugador!
        navAgent.radius = 0.2f; // Para colisiones más ajustadas
        navAgent.angularSpeed = 0f; // Rotación manual total
        navAgent.acceleration = 100f; // Aceleración de Fórmula 1

        // Stats brutales
        attackDamage = baseDamage * 1.5f; // 50% más de daño
        moveSpeed = baseSpeed * 2f; // El doble de rápido
        tiempoEntreAtaques = 0.2f; // Ataque cada 0.2 segundos

        Debug.Log("Goblin en modo DIOS - " +
                 $"Daño: {attackDamage} | " +
                 $"Velocidad: {moveSpeed} | " +
                 $"Parada: {navAgent.stoppingDistance}");
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

        // 1. Posicionamiento FORZADO
        if (navAgent != null && navAgent.enabled)
        {
            navAgent.SetDestination(target.position); // Renovación constante del destino
            navAgent.Move(transform.forward * 0.5f); // Empujón violento hacia adelante
        }

        // 2. Detección con parámetros EXTREMOS
        Collider[] objetivos = Physics.OverlapSphere(
            transform.position + transform.forward * 0.5f, // Offset frontal
            rangoAtaque * 2f, // Radio duplicado
            attackableLayerMask,
            QueryTriggerInteraction.Collide // Incluir triggers
        );

        // 3. Aplicación de daño SIN PIEDAD
        foreach (Collider col in objetivos)
        {
            if (col.CompareTag("Player"))
            {
                // Debug visual tipo "Mierda, me están matando"
                Debug.DrawLine(transform.position, col.transform.position, Color.red, 1f);
                col.GetComponent<IDamageable>()?.TakeDamage(attackDamage * 2); // Daño doble por si acaso

                // Empujón físico al jugador (opcional)
                Rigidbody rb = col.GetComponent<Rigidbody>();
                if (rb != null) rb.AddForce(transform.forward * 10f, ForceMode.Impulse);
            }
        }

        // 4. Animación y recuperación
        animator.Play("AtaqueViolento", 0, 0f); // Saltar directamente al ataque
        yield return new WaitForSeconds(0.1f); // ¡Casi instantáneo!
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