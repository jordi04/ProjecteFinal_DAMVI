using UnityEngine;
using System.Collections;

public class ProyectilVeneno : MonoBehaviour
{
    public float velocidad = 10f;
    private float dañoVeneno;
    private float duracionVeneno;
    private Vector3 direccion;
    private GameObject charcoVenenoPrefab;
    private float tiempoCharcoVeneno;
    private bool impactoRealizado = false;

    public void IniciarVeneno(Vector3 direccion, float daño, float duracion, GameObject charcoPrefab, float tiempoCharco)
    {
        this.direccion = direccion;
        this.dañoVeneno = daño;
        this.duracionVeneno = duracion;
        this.charcoVenenoPrefab = charcoPrefab;
        this.tiempoCharcoVeneno = tiempoCharco;

        Invoke("DestruirProyectil", 5f);
    }

    void Update()
    {
        transform.position += direccion * velocidad * Time.deltaTime;
    }


    private void OnTriggerEnter(Collider other)
    {
        if (impactoRealizado) return;

        if (!other.gameObject.CompareTag("Enemy"))
            DestruirProyectil();

        if (other.gameObject.layer == 7)
            Destroy(this.gameObject);
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
                charcoComponent.danoPorSegundo = dañoVeneno;
                charcoComponent.duracionDPS = duracionVeneno;
            }

            // Ignorar colisión entre el proyectil y el charco
            Collider proyectilCol = GetComponent<Collider>();
            Collider charcoCol = charco.GetComponent<Collider>();
            Destroy(charco, tiempoCharcoVeneno);

            if (proyectilCol != null && charcoCol != null)
            {
                Physics.IgnoreCollision(proyectilCol, charcoCol);
            }
        }
    }
}
