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

    public void TomarDa�o(float da�o)
    {
        vidaActual -= da�o;
        Debug.Log("Jugador recibi� da�o: " + da�o);
        VerificarMuerte();
    }

    public void AplicarVeneno(float da�oPorSegundo, float duracion)
    {
        if (venenoCoroutine != null)
        {
            StopCoroutine(venenoCoroutine);
        }
        venenoCoroutine = StartCoroutine(EfectoVeneno(da�oPorSegundo, duracion));
    }

    private IEnumerator EfectoVeneno(float da�oPorSegundo, float duracion)
    {
        float tiempoTranscurrido = 0f;

        while (tiempoTranscurrido < duracion)
        {
            TomarDa�o(da�oPorSegundo);
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