using UnityEngine;
using System.Collections;

public class ProyectilVeneno : MonoBehaviour
{
    public float velocidad = 10f;
    private float da�oVeneno;
    private float duracionVeneno;
    private Vector3 direccion;
    private GameObject charcoVenenoPrefab;
    private float tiempoCharcoVeneno;
    private bool impactoRealizado = false;

    public void IniciarVeneno(Vector3 direccion, float da�o, float duracion, GameObject charcoPrefab, float tiempoCharco, Collider lanzador)
    {
        this.direccion = direccion.normalized;
        this.da�oVeneno = da�o;
        this.duracionVeneno = duracion;
        this.charcoVenenoPrefab = charcoPrefab;
        this.tiempoCharcoVeneno = tiempoCharco;

        // Ignorar colisi�n con el lanzador (serpiente)
        Collider col = GetComponent<Collider>();
        if (col != null && lanzador != null)
        {
            Physics.IgnoreCollision(col, lanzador);
        }

        Invoke("DestruirProyectil", 5f);
    }

    void Update()
    {
        transform.position += direccion * velocidad * Time.deltaTime;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (impactoRealizado) return;
        impactoRealizado = true;

        if (collision.gameObject.CompareTag("Player"))
        {
            collision.gameObject.GetComponent<Jugador>().AplicarVeneno(da�oVeneno, duracionVeneno);
        }
        else if (collision.gameObject.CompareTag("Terrain"))
        {
            Debug.Log("Proyectil impact� con el terreno.");
        }

        CrearCharco();
        Destroy(gameObject);
    }

    void DestruirProyectil()
    {
        if (!impactoRealizado)
        {
            CrearCharco();
            impactoRealizado = true;
        }
        Destroy(gameObject);
    }

    void CrearCharco()
    {
        if (charcoVenenoPrefab != null)
        {
            GameObject charco = Instantiate(charcoVenenoPrefab, transform.position, Quaternion.identity);
            CharcoVeneno charcoComponent = charco.GetComponent<CharcoVeneno>();

            if (charcoComponent != null)
            {
                charcoComponent.da�oPorSegundo = da�oVeneno;
                charcoComponent.duracion = tiempoCharcoVeneno;
            }

            // Ignorar colisi�n entre el proyectil y el charco
            Collider proyectilCol = GetComponent<Collider>();
            Collider charcoCol = charco.GetComponent<Collider>();

            if (proyectilCol != null && charcoCol != null)
            {
                Physics.IgnoreCollision(proyectilCol, charcoCol);
            }
        }
    }
}
