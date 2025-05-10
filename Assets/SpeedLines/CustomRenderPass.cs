using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class CustomRenderPass : ScriptableRenderPass
{
    private Material material;
    private RenderTargetIdentifier source;
    private RenderTargetHandle tempTexture;
    private string profilerTag = "Custom Post Processing";

    public CustomRenderPass(CustomRenderPassFeature.CustomRenderPassSettings settings)
    {
        this.material = settings.material;
        this.renderPassEvent = settings.renderPassEvent;
        tempTexture.Init("_TempTargetCustomEffect");
    }

    public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
    {
        source = renderingData.cameraData.renderer.cameraColorTarget;
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        if (material == null)
        {
            Debug.LogError("Material not assigned for CustomRenderPass");
            return;
        }

        CommandBuffer cmd = CommandBufferPool.Get(profilerTag);

        // Get camera descriptor for rendering
        RenderTextureDescriptor cameraTextureDescriptor = renderingData.cameraData.cameraTargetDescriptor;
        cameraTextureDescriptor.depthBufferBits = 0;

        // Create temporary render texture
        cmd.GetTemporaryRT(tempTexture.id, cameraTextureDescriptor, FilterMode.Bilinear);

        // Blit from source to temp texture with the material
        Blit(cmd, source, tempTexture.Identifier(), material, 0);

        // Blit from temp texture back to source
        Blit(cmd, tempTexture.Identifier(), source);

        // Release the temporary render texture
        cmd.ReleaseTemporaryRT(tempTexture.id);

        // Execute the command buffer
        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }

    public override void OnCameraCleanup(CommandBuffer cmd)
    {
        if (cmd == null) return;

        // Release the temporary render texture if it hasn't been released yet
        if (tempTexture != RenderTargetHandle.CameraTarget)
        {
            cmd.ReleaseTemporaryRT(tempTexture.id);
        }
    }
}