using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class LoboIA : MonoBehaviour
{
    public float rangoSalto = 3f;
    public float fuerzaSalto = 8f;
    public float tiempoEntreSaltos = 2f;

    private NavMeshAgent agente;
    private Rigidbody rb;
    private GameObject playerObject;
    private bool puedeSaltar = true;
    private bool estaVivo = true;
    public LoboSpawner spawner;

    void Start()
    {
        agente = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody>();

        StartCoroutine(EsperarNavMesh());
    }

    IEnumerator EsperarNavMesh()
    {
        while (!agente.isOnNavMesh)
        {
            yield return null; 
        }

        agente.SetDestination(playerObject.transform.position);
    }

    void Update()
    {
        if (!estaVivo) return;

        float distancia = Vector3.Distance(transform.position, playerObject.transform.position);

        if (agente.isOnNavMesh)
        {
            agente.SetDestination(playerObject.transform.position);
        }
        
        if (distancia <= rangoSalto && puedeSaltar)
        {
            StartCoroutine(Saltar());
        }
    }

    private IEnumerator Saltar()
    {
        puedeSaltar = false;
        agente.isStopped = true;

        Vector3 direccionSalto = (playerObject.transform.position - transform.position).normalized;
        direccionSalto.y = 1f;
        rb.AddForce(direccionSalto * fuerzaSalto, ForceMode.Impulse);

        yield return new WaitForSeconds(0.5f);
        agente.isStopped = false;

        yield return new WaitForSeconds(tiempoEntreSaltos);
        puedeSaltar = true;
    }

    public void Morir()
    {
        if (!estaVivo) return;

        estaVivo = false;
        agente.isStopped = true;
        agente.enabled = false;
        rb.isKinematic = true;

        if (spawner != null)
        {
            spawner.LoboEliminado(gameObject);
        }
    }

    public void SetPlayer(GameObject player)
    {
           playerObject = player;
    }
}
