using System.Collections;
using UnityEngine;

public class EmissivePulsating : MonoBehaviour
{
    [Header("Material Settings")]
    [SerializeField] private Material targetMaterial;
    [SerializeField] private string emissionColorProperty = "_EmissionColor";
    [SerializeField] private float emissionIntensity = 1.0f;

    [Header("Pulsating Settings")]
    [SerializeField] private Gradient colorGradient;
    [SerializeField] private float pulseCycleDuration = 2.0f;
    [SerializeField] private AnimationCurve intensityCurve;
    [SerializeField] private bool useIntensityCurve = true;

    private Coroutine pulsatingCoroutine;
    private Color originalEmissionColor;
    private bool hasOriginalColor = false;

    private void Awake()
    {
        // Try to get material from renderers if not assigned
        if (targetMaterial == null)
        {
            // Try MeshRenderer first
            MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
            if (meshRenderer != null && meshRenderer.material != null)
            {
                targetMaterial = meshRenderer.material;
            }
            else
            {
                // Try SkinnedMeshRenderer next
                SkinnedMeshRenderer skinnedMeshRenderer = GetComponent<SkinnedMeshRenderer>();
                if (skinnedMeshRenderer != null && skinnedMeshRenderer.material != null)
                {
                    targetMaterial = skinnedMeshRenderer.material;
                }
                else
                {
                    // Try Particle System Renderer
                    ParticleSystemRenderer particleRenderer = GetComponent<ParticleSystemRenderer>();
                    if (particleRenderer != null && particleRenderer.material != null)
                    {
                        targetMaterial = particleRenderer.material;
                    }
                }
            }
        }

        if (targetMaterial != null)
        {
            // Store the original emission color
            if (targetMaterial.HasProperty(emissionColorProperty))
            {
                originalEmissionColor = targetMaterial.GetColor(emissionColorProperty);
                hasOriginalColor = true;
            }
        }
        else
        {
            Debug.LogError("No material found or assigned to " + gameObject.name);
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

        // Restore original emission color when disabled
        if (targetMaterial != null && hasOriginalColor)
        {
            targetMaterial.SetColor(emissionColorProperty, originalEmissionColor);
        }
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

    // Method to set a new target material
    public void SetTargetMaterial(Material newMaterial)
    {
        if (newMaterial != null)
        {
            // Store original color of current material if needed
            if (targetMaterial != null && hasOriginalColor)
            {
                targetMaterial.SetColor(emissionColorProperty, originalEmissionColor);
            }

            // Set new material
            targetMaterial = newMaterial;

            // Store original color of new material
            if (targetMaterial.HasProperty(emissionColorProperty))
            {
                originalEmissionColor = targetMaterial.GetColor(emissionColorProperty);
                hasOriginalColor = true;
            }
            else
            {
                hasOriginalColor = false;
            }

            // Restart pulsating if enabled
            if (isActiveAndEnabled)
            {
                StopPulsating();
                StartPulsating();
            }
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
}
