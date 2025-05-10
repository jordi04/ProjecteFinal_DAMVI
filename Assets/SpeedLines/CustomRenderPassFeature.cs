using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class CustomRenderPassFeature : ScriptableRendererFeature
{
    public static bool isActive = false;
    public static Material runtimeMaterial;
    
    [System.Serializable]
    public class CustomRenderPassSettings
    {
        public Material material;
        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
    }

    CustomRenderPass m_ScriptablePass;
    public CustomRenderPassSettings settings = new CustomRenderPassSettings();

    public override void Create()
    {
        m_ScriptablePass = new CustomRenderPass(settings);
                runtimeMaterial = settings.material;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (true)
        {
            renderer.EnqueuePass(m_ScriptablePass);
        }
    }

}


