using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class PostProcessingManager : MonoBehaviour
{
    [SerializeField] private Material speedLinesShaderMaterial;
    [SerializeField] private VolumeProfile postProcessingProfile;
    [SerializeField] private UniversalRendererData rendererData;

    private void Awake()
    {
        InitializeCustomRenderPass();
    }

    private void InitializeCustomRenderPass()
    {
        if (speedLinesShaderMaterial == null)
        {
            Debug.LogError("Speed Lines shader material is not assigned!");
            return;
        }

        if (rendererData == null)
        {
            Debug.LogError("Renderer Data is not assigned!");
            return;
        }

        // Find existing or add new CustomRenderPassFeature
        CustomRenderPassFeature customFeature = null;

        foreach (var feature in rendererData.rendererFeatures)
        {
            if (feature is CustomRenderPassFeature)
            {
                customFeature = feature as CustomRenderPassFeature;
                break;
            }
        }

        if (customFeature == null)
        {
            customFeature = ScriptableObject.CreateInstance<CustomRenderPassFeature>();
            rendererData.rendererFeatures.Add(customFeature);
        }

        // Initialize the feature settings
        customFeature.settings.material = speedLinesShaderMaterial;
        customFeature.settings.renderPassEvent = RenderPassEvent.AfterRenderingTransparents;

        // Make sure Create is called to initialize the feature
        customFeature.Create();

        // Set the initial state
        CustomRenderPassFeature.isActive = false;
        CustomRenderPassFeature.runtimeMaterial = speedLinesShaderMaterial;
    }

    private void OnApplicationQuit()
    {
        // Clean up if needed
        CustomRenderPassFeature.isActive = false;
    }
}