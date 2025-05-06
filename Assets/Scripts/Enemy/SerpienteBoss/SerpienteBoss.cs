using UnityEngine;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine.SceneManagement;

public class SerpienteBoss : MonoBehaviour
{
    public Transform objetivo;
    public GameObject proyectilVenenoPrefab;
    public Transform puntoDisparo;
    public float rangoActivacion = 40f;
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
    private bool estaActiva = false;

    private void Start()
    {
        originalColor = snakeRenderer.material.color;
        materialPropertyBlock = new MaterialPropertyBlock();
    }

    void Update()
    {
        if (objetivo == null) return;
        float distancia = Vector3.Distance(transform.position, objetivo.position);

        if (!estaActiva && distancia <= rangoActivacion)
        {
            estaActiva = true;
            Debug.Log("¡La serpiente se ha activado!");
        }

        // Si la serpiente está activa, atacar al jugador
        if (estaActiva && puedeAtacar)
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
        materialPropertyBlock.SetColor("_Color", color);
        snakeRenderer.SetPropertyBlock(materialPropertyBlock);
    }

    private void ResetColor()
    {
        SetColor(originalColor);
    }
}
