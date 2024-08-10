using UnityEngine.Rendering.Universal;

public class Hi_Z_DepthTexMaker_RenderPassFeature : ScriptableRendererFeature
{
    private Hi_Z_DepthTexMaker_RenderPass m_ProcessPass;

    public override void Create()
    {
        m_ProcessPass = new Hi_Z_DepthTexMaker_RenderPass(RenderPassEvent.AfterRenderingOpaques);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(m_ProcessPass);
    }
}
