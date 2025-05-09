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

    public void IniciarVeneno(Vector3 direccion, float da�o, float duracion, GameObject charcoPrefab, float tiempoCharco)
    {
        this.direccion = direccion;
        this.da�oVeneno = da�o;
        this.duracionVeneno = duracion;
        this.charcoVenenoPrefab = charcoPrefab;
        this.tiempoCharcoVeneno = tiempoCharco;

        Invoke("DestruirProyectil", 5f);
    }

    void Update()
    {
        transform.position += direccion * velocidad * Time.deltaTime;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (impactoRealizado) return;

        if (collision.gameObject.CompareTag("Player"))
        {
            CharcoVeneno.AplicarVeneno(this, da�oVeneno, duracionVeneno);
        }
        else if (collision.gameObject.CompareTag("Terrain"))
        {
            //Debug.Log("Proyectil impact� con el terreno.");
        }

        if (!collision.gameObject.CompareTag("Enemy")) 
            DestruirProyectil();
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
                charcoComponent.danoPorSegundo = da�oVeneno;
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
