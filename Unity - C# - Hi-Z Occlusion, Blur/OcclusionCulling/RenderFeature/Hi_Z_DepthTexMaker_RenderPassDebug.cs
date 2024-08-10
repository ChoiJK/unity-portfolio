using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

internal class Hi_Z_DepthTexMaker_RenderPassDebug : ScriptableRenderPass
{
    // Consts
    private const int MAXIMUM_BUFFER_SIZE = 1024;
    private readonly Material _debugMaterial;


#region Profiling

    private readonly ProfilingSampler _debugSampler = new("HiZDepthMaker_Debug");

#endregion

    private readonly Hi_Z_DepthTexMaker_RenderPass _processPass;

    public Hi_Z_DepthTexMaker_RenderPassDebug(RenderPassEvent evt, Hi_Z_DepthTexMaker_RenderPass processPass)
    {
        renderPassEvent = evt;
        _processPass = processPass;
        _debugMaterial = new Material(Shader.Find("HiZ/HiZDepthDebug"));
    }

    public int MipLevel { get; set; }

    public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
    {
        if (renderingData.cameraData.camera.tag != "MainCamera")
        {
        }
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        if (renderingData.cameraData.camera.tag != "MainCamera")
        {
            return;
        }

        if (_debugMaterial == default)
        {
            return;
        }

        if (_processPass == default || Hi_Z_DepthTexMaker_RenderPass.HiZDepthTexture == default)
        {
            return;
        }

        var cmd = CommandBufferPool.Get();
        using (new ProfilingScope(cmd, _debugSampler))
        {
            cmd.name = "Hi_Z_DepthTexMaker Debug";

            cmd.SetViewport(new Rect(0.0f, 0.5f, 0.5f, 0.5f));
            _debugMaterial.SetInt("_NUM", 0);
            _debugMaterial.SetInt("_LOD", MipLevel);
            cmd.Blit(Hi_Z_DepthTexMaker_RenderPass.HiZDepthTexture, renderingData.cameraData.targetTexture,
                _debugMaterial);
        }

        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }

    public override void OnCameraCleanup(CommandBuffer cmd)
    {
    }
}
