using UnityEngine;
using UnityEngine.AI;
using System.Collections;

// Hereda de EnemyController
public class DuendeMeleeAI : EnemyController
{
    [Header("Configuraci�n Espec�fica")]
    [SerializeField] float rangoAtaque = 0.25f;
    [SerializeField] float tiempoEntreAtaques = 0.5f;

    [Header("Referencias")]
    [SerializeField] Transform puntoAtaque;

    private bool puedeAtacar = true;

    protected override void Start()
    {
        base.Start(); // Llama al Start de EnemyController
        ConfigurarComponentesAdicionales();
    }

    void ConfigurarComponentesAdicionales()
    {
        // Configuraci�n espec�fica del NavMeshAgent
        navAgent.stoppingDistance = rangoAtaque;
        navAgent.angularSpeed = 720f;
    }

    protected override void Update()
    {
        base.Update(); // Llama al Update base

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
        animator.SetTrigger(attackAnimTrigger); // Usa la variable del padre

        yield return new WaitForSeconds(0.3f);

        Collider[] objetivos = Physics.OverlapSphere(
            puntoAtaque.position,
            rangoAtaque,
            attackableLayerMask // Usa la m�scara de capa del padre
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

    // Implementaci�n de IDamageable heredada
    public override void TakeDamage(float cantidad)
    {
        if (IsDead()) return;

        base.TakeDamage(cantidad); // Llama a la l�gica base
        StartCoroutine(EfectoDanoPersonalizado());
    }

    IEnumerator EfectoDanoPersonalizado()
    {
        // Usa el sistema de da�o del padre pero con color personalizado
        SetColor(Color.cyan);
        yield return new WaitForSeconds(flashDuration);
        if (!IsDead()) ResetColor();
    }

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected(); // Mantiene los gizmos base
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(puntoAtaque.position, rangoAtaque);
    }
}