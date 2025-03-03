using UnityEngine;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine.SceneManagement;

public class SerpienteBoss : MonoBehaviour
{
    public Transform objetivo;
    public GameObject proyectilVenenoPrefab;
    public Transform puntoDisparo;
    public float tiempoEntreAtaques = 5f;
    public float rangoMordida = 5f;
    public float dañoMordida = 20f;
    public float dañoVeneno = 5f;
    public float duracionVeneno = 5f;
    public GameObject charcoVenenoPrefab;
    public float tiempoCharcoVeneno = 5f;
    
    [SerializeField] float life = 50f;
    [SerializeField] private float flashDuration = 0.2f;
    [SerializeField] private Renderer snakeRenderer;
    private MaterialPropertyBlock materialPropertyBlock;
    private Color originalColor;

    private bool isDead = false;
    private bool puedeAtacar = true;

    void Update()
    {
        if (objetivo == null) return;
        float distancia = Vector3.Distance(transform.position, objetivo.position);

        if (puedeAtacar)
        {
            if (distancia <= rangoMordida)
            {
                StartCoroutine(AtaqueMordida());
            }
            else
            {
                StartCoroutine(AtaqueEscupitajo());
            }
        }
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

            // Flash on every hit
            StartCoroutine(DamageFlash());

            if (life <= 0)
            {
                isDead = true;
                StartCoroutine(DeathSequence());
            }
        }
    }

    IEnumerator AtaqueMordida()
    {
        puedeAtacar = false;
        Debug.Log("La serpiente ataca con una mordida!");
        ManaSystem.instance.TakeDamage(dañoMordida);
        yield return new WaitForSeconds(tiempoEntreAtaques);
        puedeAtacar = true;
    }

    IEnumerator AtaqueEscupitajo()
    {
        puedeAtacar = false;
        Debug.Log("La serpiente escupe veneno!");

        // Instanciamos el proyectil adelantado para evitar colisión con el lanzador
        GameObject veneno = Instantiate(proyectilVenenoPrefab,
            puntoDisparo.position + puntoDisparo.forward * 1f,
            Quaternion.identity);

        veneno.GetComponent<ProyectilVeneno>().IniciarVeneno(
            (objetivo.position - puntoDisparo.position).normalized,
            dañoVeneno,
            duracionVeneno,
            charcoVenenoPrefab,
            tiempoCharcoVeneno,
            puntoDisparo.GetComponent<Collider>() // Pasamos el collider para ignorar colisión
        );

        yield return new WaitForSeconds(tiempoEntreAtaques);
        puedeAtacar = true;
    }

    public void TakeDamage(float damageAmount)
    {
        if (isDead) return;

        life -= damageAmount;
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
        SceneManager.LoadScene("Win");
    }

    private void SetColor(Color color)
    {
        snakeRenderer.GetPropertyBlock(materialPropertyBlock);
        materialPropertyBlock.SetColor("_BaseColor", color);
        snakeRenderer.SetPropertyBlock(materialPropertyBlock);
    }

    private void ResetColor()
    {
        SetColor(originalColor);
    }
}
