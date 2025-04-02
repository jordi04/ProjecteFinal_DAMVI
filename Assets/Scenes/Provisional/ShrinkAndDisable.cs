using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlackHoleEffect : MonoBehaviour
{
    public float shrinkDuration = 2f;
    public float spiralStrength = 10f;
    public float attractionSpeed = 3f;
    public float finalAttractionMultiplier = 5f;
    public float finalSnapThreshold = 0.2f;

    private Transform[] children;
    private Dictionary<Transform, Vector3> originalScales = new Dictionary<Transform, Vector3>();

    public void StartBlackHoleEffect()
    {
        children = GetComponentsInChildren<Transform>();

        foreach (var child in children)
        {
            if (child != transform) // Evita afectar al padre
                originalScales[child] = child.localScale;
        }

        StartCoroutine(AbsorbObjects());
    }

    private IEnumerator AbsorbObjects()
    {
        float elapsedTime = 0f;

        if (!Application.isPlaying) yield break;

        while (elapsedTime < shrinkDuration)
        {
            float progress = elapsedTime / shrinkDuration; // Valor entre 0 y 1
            float scaleFactor = Mathf.Lerp(1f, 0f, progress);

            foreach (var child in children)
            {
                if (child != transform && originalScales.ContainsKey(child))
                {
                    // Reducir tamaño del objeto
                    child.localScale = originalScales[child] * scaleFactor;

                    // Dirección hacia el centro
                    Vector3 directionToCenter = (transform.position - child.position);
                    float distanceToCenter = directionToCenter.magnitude;
                    directionToCenter.Normalize();

                    // Espiral más pronunciada al principio y reducida cerca del centro
                    Vector3 perpendicular = Vector3.Cross(directionToCenter, Vector3.up) * (spiralStrength * (1f - progress));

                    // Movimiento curvo hacia el centro
                    float speedMultiplier = Mathf.Lerp(1f, finalAttractionMultiplier, progress);
                    Vector3 curvedPath = directionToCenter * speedMultiplier + perpendicular;

                    child.position += curvedPath * attractionSpeed * Time.deltaTime;

                    // Si el objeto está suficientemente cerca, lo forzamos al centro
                    if (distanceToCenter < finalSnapThreshold)
                    {
                        child.position = transform.position;
                    }

                    // Rotación para mayor efecto visual
                    child.Rotate(Vector3.up * (180 * Time.deltaTime), Space.World);
                }
            }

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Forzar todos los objetos al centro y desactivarlos
        foreach (var child in children)
        {
            if (child != transform)
            {
                child.position = transform.position;
                child.gameObject.SetActive(false);
            }
        }
    }
}
