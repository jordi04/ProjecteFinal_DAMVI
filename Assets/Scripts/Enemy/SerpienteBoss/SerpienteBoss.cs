using UnityEngine;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine.SceneManagement;

public class SerpienteBoss : MonoBehaviour
{
    [SerializeField] Transform snakePosition;
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
        objetivo = ManaSystem.instance.gameObject.transform;
    }

    void Update()
    {
        float distancia = Vector3.Distance(snakePosition.position, objetivo.position);

        estaActiva = distancia <= rangoActivacion;

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

    void FixedUpdate()
    {
        if (!estaActiva || objetivo == null) return;

        snakePosition.transform.LookAt(objetivo);

        snakePosition.transform.Rotate(0, 180, 0);
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
            puntoDisparo.position,
            Quaternion.identity);

        veneno.GetComponent<ProyectilVeneno>().IniciarVeneno(
            (objetivo.position - puntoDisparo.position).normalized,
            dañoVeneno,
            duracionVeneno,
            charcoVenenoPrefab,
            tiempoCharcoVeneno
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

    private void OnDrawGizmosSelected()
    {
        // Rango de activación (ataque a distancia)
        Gizmos.color = new Color(0, 1, 1, 0.25f); // Cyan transparente
        Gizmos.DrawWireSphere(snakePosition.position, rangoActivacion);

        // Rango de mordida (ataque cercano)
        Gizmos.color = new Color(1, 0, 0, 0.5f); // Rojo semitransparente
        Gizmos.DrawWireSphere(snakePosition.position, rangoMordida);
    }
}
