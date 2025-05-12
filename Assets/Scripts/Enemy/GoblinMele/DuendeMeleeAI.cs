using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class DuendeMeleeAI : EnemyController
{
    [Header("Configuración Melee")]
    [SerializeField] float rangoAtaque = 0.5f;
    [SerializeField] float tiempoEntreAtaques = 1f;
    [SerializeField] Transform puntoAtaque;

    [Header("Movimiento")]
    [SerializeField] float actualizarDestinoIntervalo = 0.5f;

    private Transform jugador;
    private NavMeshAgent navAgent;
    private bool puedeAtacar = true;
    private float tiempoSiguienteActualizacion;

    protected override void Awake()
    {
        base.Awake();

        navAgent = GetComponent<NavMeshAgent>();
        jugador = GameObject.FindGameObjectWithTag("Player")?.transform; // Null-check seguro

        if (navAgent != null)
        {
            navAgent.stoppingDistance = rangoAtaque * 0.8f;
            navAgent.angularSpeed = 720f;
            navAgent.autoBraking = false;
            navAgent.acceleration = 50f;
        }

        Debug.Log("Goblin inicializado - Velocidad: " + navAgent?.speed);
    }

    void Update()
    {
        if (isDead || jugador == null) return;

        ActualizarDestino();
        GestionarAtaque();
    }

    void ActualizarDestino()
    {
        if (Time.time >= tiempoSiguienteActualizacion)
        {
            navAgent?.SetDestination(jugador.position);
            tiempoSiguienteActualizacion = Time.time + actualizarDestinoIntervalo;
        }
    }

    void GestionarAtaque()
    {
        if (puedeAtacar && Vector3.Distance(transform.position, jugador.position) <= rangoAtaque)
        {
            StartCoroutine(AtaqueMelee());
        }
    }

    IEnumerator AtaqueMelee()
    {
        puedeAtacar = false;

        // Bloqueo de movimiento
        if (navAgent != null)
        {
            navAgent.isStopped = true;
            navAgent.velocity = Vector3.zero;
            navAgent.updatePosition = false; // Previene actualizaciones de posición
        }

        // Rotación hacia el jugador
        Vector3 direccion = (jugador.position - transform.position).normalized;
        direccion.y = 0;
        transform.rotation = Quaternion.LookRotation(direccion);

        yield return new WaitForSeconds(0.2f);

        // Detección de daño con verificación de componentes
        if (puntoAtaque != null)
        {
            Collider[] objetivos = Physics.OverlapSphere(
                puntoAtaque.position,
                rangoAtaque,
                attackableLayerMask
            );

            foreach (Collider col in objetivos)
            {
                if (col != null && col.CompareTag("Player"))
                {
                    IDamageable damageable = col.GetComponent<IDamageable>();
                    damageable?.TakeDamage(attackDamage);
                }
            }
        }

        yield return new WaitForSeconds(tiempoEntreAtaques);

        // Reactivación de movimiento
        if (navAgent != null)
        {
            navAgent.isStopped = false;
            navAgent.updatePosition = true;
            navAgent.SetDestination(jugador.position); // Actualizar destino
        }

        puedeAtacar = true;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        if (puntoAtaque != null)
        {
            Gizmos.DrawWireSphere(puntoAtaque.position, rangoAtaque);
        }
    }
}