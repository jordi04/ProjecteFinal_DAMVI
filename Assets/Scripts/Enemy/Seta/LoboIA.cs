using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using UnityEngine.SceneManagement;

public class LoboIA : MonoBehaviour
{
    public float rangoSalto = 3f;
    public float fuerzaSalto = 8f;
    public float tiempoEntreSaltos = 2f;
    [SerializeField] float damage = 10f;

    private NavMeshAgent agente;
    private Rigidbody rb;
    private GameObject playerObject;
    private bool puedeSaltar = true;
    public LoboSpawner spawner;


    [SerializeField] float life = 10f;
    [SerializeField] private float flashDuration = 0.2f;
    [SerializeField] private Renderer mushroomRenderer;
    private MaterialPropertyBlock materialPropertyBlock;
    private Color originalColor;

    private bool isDead = false;

    void Start()
    {
        agente = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody>();

        StartCoroutine(EsperarNavMesh());
        materialPropertyBlock = new MaterialPropertyBlock();
        originalColor = mushroomRenderer.material.color;
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
        if (isDead) return;

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


    private void OnTriggerEnter(Collider other)
    {
        if (isDead) return;

        if (other.CompareTag("FireBall"))
        {
            // Try to get the damage amount from the fireball
            FireballPrefabScript fireball = other.GetComponent<FireballPrefabScript>();

            // If we can't get the fireball script, use default damage
            if (fireball == null)
            {
                life -= 10f;
            }

            // The actual damage is now handled by the TakeDamage method
            // which the fireball script will call directly

            StartCoroutine(DamageFlash());

            if (life <= 0)
            {
                isDead = true;
                StartCoroutine(DeathSequence());
            }
        }
        if (other.CompareTag("Player"))
        {
            ManaSystem.instance.TakeDamage(damage);
        }
    }

    public void TakeDamage(float damageAmount)
    {
        if (isDead) return;

        life -= damageAmount;
        Debug.Log(life);
        // Flash on every hit
        StartCoroutine(DamageFlash());

        if (life <= 0)
        {
            isDead = true;
            StartCoroutine(DeathSequence());
        }
    }

    private IEnumerator DamageFlash()
    {
        SetColor(Color.red);
        yield return new WaitForSeconds(flashDuration);
        if (!isDead) ResetColor();
    }

    private IEnumerator DeathSequence()
    {
        yield return new WaitForSeconds(flashDuration);
        if (spawner != null)
        {
            spawner.LoboEliminado(gameObject);
            Destroy(gameObject);
        }
    }

    private void SetColor(Color color)
    {
        mushroomRenderer.GetPropertyBlock(materialPropertyBlock);
        materialPropertyBlock.SetColor("_BaseColor", color);
        mushroomRenderer.SetPropertyBlock(materialPropertyBlock);
    }

    private void ResetColor()
    {
        SetColor(originalColor);
    }

    public void SetPlayer(GameObject player)
    {
           playerObject = player;
    }
}
