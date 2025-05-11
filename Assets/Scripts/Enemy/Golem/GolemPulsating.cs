using System.Collections;
using UnityEngine;

public class EnemyPulsatingEmission : MonoBehaviour
{
    [Header("Material Settings")]
    [SerializeField] private string emissionColorProperty = "_EmissionColor";
    [SerializeField] private float emissionIntensity = 1.0f;

    [Header("Renderer Reference")]
    [SerializeField] private SkinnedMeshRenderer skinnedMeshRenderer;
    [SerializeField] private int materialIndex = 0; // In case the renderer uses multiple materials

    [Header("Pulsating Settings")]
    [SerializeField] private Gradient colorGradient;
    [SerializeField] private float pulseCycleDuration = 2.0f;
    [SerializeField] private AnimationCurve intensityCurve;
    [SerializeField] private bool useIntensityCurve = true;

    private Material targetMaterial;
    private Coroutine pulsatingCoroutine;

    private void Awake()
    {
        // If no renderer is assigned, try to get it from this GameObject
        if (skinnedMeshRenderer == null)
        {
            skinnedMeshRenderer = GetComponent<SkinnedMeshRenderer>();

            // If still not found, try to find it in children
            if (skinnedMeshRenderer == null)
            {
                skinnedMeshRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
            }
        }

        if (skinnedMeshRenderer != null)
        {
            // Get the material instance that's already being used by the renderer
            if (materialIndex < skinnedMeshRenderer.materials.Length)
            {
                targetMaterial = skinnedMeshRenderer.materials[materialIndex];
            }
            else
            {
                Debug.LogError("Material index out of range on " + gameObject.name);
            }
        }
        else
        {
            Debug.LogError("No SkinnedMeshRenderer found on " + gameObject.name);
        }
    }

    private void OnEnable()
    {
        if (targetMaterial != null)
        {
            // Make sure emission is enabled on the material
            targetMaterial.EnableKeyword("_EMISSION");

            // Start the pulsating effect
            StartPulsating();
        }
    }

    private void OnDisable()
    {
        StopPulsating();
    }

    public void StartPulsating()
    {
        if (targetMaterial == null)
            return;

        if (pulsatingCoroutine != null)
        {
            StopCoroutine(pulsatingCoroutine);
        }

        pulsatingCoroutine = StartCoroutine(PulsateEmission());
    }

    public void StopPulsating()
    {
        if (pulsatingCoroutine != null)
        {
            StopCoroutine(pulsatingCoroutine);
            pulsatingCoroutine = null;
        }
    }

    private IEnumerator PulsateEmission()
    {
        float timeElapsed = 0f;

        while (true)
        {
            // Calculate the normalized time within the cycle (0 to 1)
            float normalizedTime = (timeElapsed % pulseCycleDuration) / pulseCycleDuration;

            // Get the color from the gradient based on the current time
            Color baseColor = colorGradient.Evaluate(normalizedTime);

            // Apply intensity curve if enabled
            float currentIntensity = emissionIntensity;
            if (useIntensityCurve)
            {
                currentIntensity *= intensityCurve.Evaluate(normalizedTime);
            }

            // Apply the color with HDR intensity to the emission
            Color emissiveColor = baseColor * currentIntensity;
            targetMaterial.SetColor(emissionColorProperty, emissiveColor);

            // Increment time
            timeElapsed += Time.deltaTime;
            yield return null;
        }
    }

    // Optional: Method to change the gradient at runtime
    public void SetGradient(Gradient newGradient)
    {
        colorGradient = newGradient;
    }

    // Optional: Method to change the pulse duration at runtime
    public void SetPulseDuration(float duration)
    {
        pulseCycleDuration = Mathf.Max(0.1f, duration);
    }

    // Optional: Method to set a specific material from the renderer
    public void SetMaterialIndex(int index)
    {
        if (skinnedMeshRenderer != null && index >= 0 && index < skinnedMeshRenderer.materials.Length)
        {
            materialIndex = index;
            targetMaterial = skinnedMeshRenderer.materials[materialIndex];

            // Restart the effect with the new material
            if (isActiveAndEnabled)
            {
                StopPulsating();
                StartPulsating();
            }
        }
    }
}
