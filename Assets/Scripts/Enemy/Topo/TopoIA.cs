using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class TopoIA : MonoBehaviour
{
    public float tiempoPersecucion = 3f; 
    public float tiempoEsperaEmergido = 0.5f; 
    public float damage = 10f;
    public float velocidadMovimiento = 3.5f;
    public float velocidadSumergido = 6f;
    public Transform objetivo;
    public float salud = 50f; 
    public float tiempoEntreGolpes = 1f; 

    private NavMeshAgent agente;
    private Collider hitbox;
    private MeshRenderer modelo;
    private bool estaSumergido = false;
    private bool puedeHacerDa�o = true;

    private ParticleSystem  particles;

    void Start()
    {
        agente = GetComponent<NavMeshAgent>();
        hitbox = GetComponent<Collider>();
        modelo = GetComponentInChildren<MeshRenderer>();
        particles = GetComponent<ParticleSystem>();
        particles.Stop();
        StartCoroutine(PerseguirJugador());
    }

    IEnumerator PerseguirJugador()
    {
        float tiempoRestante = tiempoPersecucion;
        while (tiempoRestante > 0)
        {
            agente.SetDestination(objetivo.position);
            tiempoRestante -= Time.deltaTime;
            yield return null;
        }
        Sumergirse();
    }

    void Sumergirse()
    {
        estaSumergido = true;
        hitbox.enabled = false;
        modelo.enabled = false;
        agente.isStopped = true;
        agente.speed = velocidadSumergido;
        particles.Play();
        StartCoroutine(MoverseHaciaObjetivo());
    }

    IEnumerator MoverseHaciaObjetivo()
    {
        agente.isStopped = false;
        agente.SetDestination(objetivo.position);
        while (agente.pathPending || agente.remainingDistance > 0.5f)
        {
            yield return null;
        }
        yield return new WaitForSeconds(tiempoEsperaEmergido);
        Emerger();
    }

    void Emerger()
    {
        estaSumergido = false;
        hitbox.enabled = true;
        modelo.enabled = true;
        agente.speed = velocidadMovimiento;
        StartCoroutine(PerseguirJugador());
    }

    void OnTriggerEnter(Collider other)
    {
        if (!estaSumergido && other.CompareTag("Player"))
        {
            AplicarDa�oJugador(other);
        }
    }

    void OnTriggerStay(Collider other)
    {
        if (!estaSumergido && other.CompareTag("Player"))
        {
            AplicarDa�oJugador(other);
        }
    }

    void AplicarDa�oJugador(Collider jugador)
    {
        if (puedeHacerDa�o)
        {
            ManaSystem.instance.TakeDamage(damage);
            StartCoroutine(EsperarProximoGolpe());
        }
    }

    IEnumerator EsperarProximoGolpe()
    {
        puedeHacerDa�o = false;
        yield return new WaitForSeconds(tiempoEntreGolpes);
        puedeHacerDa�o = true;
    }

    public void TomarDa�o(float da�o)
    {
        salud -= da�o;
        if (salud <= 0)
        {
            Morir();
        }
    }

    void Morir()
    {
        Destroy(gameObject);
    }
}
