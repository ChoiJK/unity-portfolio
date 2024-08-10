using UnityEngine.Rendering.Universal;

public class VideoFeedBlurFeature : ScriptableRendererFeature
{
    public static VideoFeedBlurRenderPass VideoFeedBlurRenderPassInstance;

    public override void Create()
    {
        VideoFeedBlurRenderPassInstance = new VideoFeedBlurRenderPass();

        VideoFeedBlurRenderPassInstance.Initialize();

        VideoFeedBlurRenderPassInstance.renderPassEvent = RenderPassEvent.BeforeRendering;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(VideoFeedBlurRenderPassInstance);
    }
}
