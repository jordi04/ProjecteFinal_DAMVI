using UnityEngine;
using System.Collections;

public class CharcoVeneno : MonoBehaviour
{
    public float da�oPorSegundo = 5f;
    public float duracion = 5f;

    void Start()
    {
        Destroy(gameObject, duracion);

        // Intentar convertir el MeshCollider en convex si existe
        MeshCollider meshCol = GetComponent<MeshCollider>();
        if (meshCol != null)
        {
            meshCol.convex = true;  // Convierte el MeshCollider a una forma convexa
            meshCol.isTrigger = true;  // Habilita el trigger
        }
    }


    void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            other.GetComponent<Jugador>().AplicarVeneno(da�oPorSegundo, 1f);
        }
    }
}
