using UnityEngine;
using System.Collections;

public class Jugador : MonoBehaviour
{
    public float vidaMaxima = 100f;
    private float vidaActual;
    private Coroutine venenoCoroutine;

    void Start()
    {
        vidaActual = vidaMaxima;
    }

    public void TomarDaño(float daño)
    {
        vidaActual -= daño;
        Debug.Log("Jugador recibió daño: " + daño);
        VerificarMuerte();
    }

    public void AplicarVeneno(float dañoPorSegundo, float duracion)
    {
        if (venenoCoroutine != null)
        {
            StopCoroutine(venenoCoroutine);
        }
        venenoCoroutine = StartCoroutine(EfectoVeneno(dañoPorSegundo, duracion));
    }

    private IEnumerator EfectoVeneno(float dañoPorSegundo, float duracion)
    {
        float tiempoTranscurrido = 0f;

        while (tiempoTranscurrido < duracion)
        {
            TomarDaño(dañoPorSegundo);
            tiempoTranscurrido += 1f;
            yield return new WaitForSeconds(1f);
        }
        venenoCoroutine = null;
    }

    private void VerificarMuerte()
    {
        if (vidaActual <= 0)
        {
            Debug.Log("Jugador ha muerto!");
        }
    }
}