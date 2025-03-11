using UnityEngine;
using System.Collections;

public class CharcoVeneno : MonoBehaviour
{
    public float dañoPorSegundo = 5f;
    public float duracion = 5f;
    static Coroutine poisonCorutine;

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
            AplicarVeneno(this, dañoPorSegundo, 1f);
        }
    }

    public static void AplicarVeneno(MonoBehaviour executor, float dañoPorSegundo, float duracion)
    {
        if (poisonCorutine != null)
        {
            executor.StopCoroutine(poisonCorutine);
        }
        poisonCorutine = executor.StartCoroutine(EfectoVeneno(dañoPorSegundo, duracion));
    }

    private static IEnumerator EfectoVeneno(float dañoPorSegundo, float duracion)
    {
        float tiempoTranscurrido = 0f;

        while (tiempoTranscurrido < duracion)
        {
            ManaSystem.instance.TakeDamage(dañoPorSegundo);
            tiempoTranscurrido += 1f;
            yield return new WaitForSeconds(1f);
        }
        poisonCorutine = null;
    }
}
