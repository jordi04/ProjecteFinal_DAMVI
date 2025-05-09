using UnityEngine;
using System.Collections;

public class CharcoVeneno : MonoBehaviour
{
    public float danoPorSegundo = 5f;
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
            AplicarVeneno(this, danoPorSegundo, 1f);
        }
    }

    public static void AplicarVeneno(MonoBehaviour executor, float daņoPorSegundo, float duracion)
    {
        if (poisonCorutine != null)
        {
            executor.StopCoroutine(poisonCorutine);
        }
        poisonCorutine = executor.StartCoroutine(EfectoVeneno(daņoPorSegundo, duracion));
    }

    private static IEnumerator EfectoVeneno(float daņoPorSegundo, float duracion)
    {
        float tiempoTranscurrido = 0f;

        while (tiempoTranscurrido < duracion)
        {
            ManaSystem.instance.TakeDamage(daņoPorSegundo);
            tiempoTranscurrido += 1f;
            yield return new WaitForSeconds(1f);
        }
        poisonCorutine = null;
    }
}
