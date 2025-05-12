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

        // Configurar parámetros específicos del goblin
        attackType = AttackType.Melee;
        movementType = MovementType.NavMesh;
        attackRange = rangoAtaque;
        attackRate = 1f / tiempoEntreAtaques;

        navAgent = GetComponent<NavMeshAgent>();
        jugador = GameObject.FindGameObjectWithTag("Player")?.transform;

        if (navAgent != null)
        {
            navAgent.stoppingDistance = rangoAtaque * 0.8f;
            navAgent.angularSpeed = 720f;
            navAgent.autoBraking = false;
            navAgent.acceleration = 50f;
            navAgent.updateRotation = false; // Desactivamos rotación automática
        }
    }

    protected override void Update()
    {
        base.Update(); // Importante mantener la lógica base

        if (isDead || jugador == null) return;

        ActualizarDestino();
        GestionarMovimientoAnimacion();
        RotarHaciaJugador();

        if (puedeAtacar && Vector3.Distance(transform.position, jugador.position) <= rangoAtaque)
        {
            StartCoroutine(AtaqueMelee());
        }
    }

    void ActualizarDestino()
    {
        if (Time.time >= tiempoSiguienteActualizacion)
        {
            navAgent?.SetDestination(jugador.position);
            tiempoSiguienteActualizacion = Time.time + actualizarDestinoIntervalo;
        }
    }

    void GestionarMovimientoAnimacion()
    {
        if (animator != null)
        {
            bool isMoving = navAgent.velocity.magnitude > 0.1f;
            animator.SetBool("IsWalking", isMoving);
        }
    }

    void RotarHaciaJugador()
    {
        if (jugador == null) return;

        Vector3 direccion = (jugador.position - transform.position).normalized;
        direccion.y = 0f;

        if (direccion.sqrMagnitude > 0.001f)
        {
            Quaternion rotacionDeseada = Quaternion.LookRotation(direccion);
            transform.rotation = Quaternion.Slerp(transform.rotation, rotacionDeseada, Time.deltaTime * 10f);
        }
    }

    IEnumerator AtaqueMelee()
    {
        puedeAtacar = false;

        if (animator != null)
            animator.SetTrigger("Attack");

        if (navAgent != null)
        {
            navAgent.isStopped = true;
            navAgent.velocity = Vector3.zero;
        }

        Vector3 direccion = (jugador.position - transform.position).normalized;
        direccion.y = 0;
        transform.rotation = Quaternion.LookRotation(direccion);

        yield return new WaitForSeconds(0.2f);

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

        if (navAgent != null)
        {
            navAgent.isStopped = false;
            navAgent.SetDestination(jugador.position);
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
