using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class VideoFeedBlurRenderPass : ScriptableRenderPass
{
    private readonly List<TextureData> updateTextureDataPool = new();
    private Material _blurMaterial;
    private RenderTexture _renderTexture_DownSample;
    private RenderTargetIdentifier _renderTexture_DownSampleID;

    public void Initialize()
    {
        if (_renderTexture_DownSample == null)
        {
            _renderTexture_DownSample =
                new RenderTexture(TextureData.BlurTextureWidth, TextureData.BlurTextureHeight, GraphicsFormat.R8G8B8A8_SRGB, GraphicsFormat.None);
            _renderTexture_DownSample.wrapMode = TextureWrapMode.Repeat;
            _renderTexture_DownSample.filterMode = FilterMode.Bilinear;
            _renderTexture_DownSample.useMipMap = false;
            _renderTexture_DownSample.autoGenerateMips = false;
            _renderTexture_DownSample.Create();
            _renderTexture_DownSample.hideFlags = HideFlags.HideAndDontSave;

            _renderTexture_DownSampleID = new RenderTargetIdentifier(_renderTexture_DownSample);
        }

        if (_blurMaterial == null)
        {
            _blurMaterial = new Material(Shader.Find("VideoFeedGaussianBlur"));
        }
    }

    public void AppendTextureData(TextureData td)
    {
        if (!updateTextureDataPool.Contains(td))
        {
            updateTextureDataPool.Add(td);
        }
    }

    public void ClearTextureData()
    {
        updateTextureDataPool.Clear();
    }

    public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
    {
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        foreach (var textureData in updateTextureDataPool)
        {
            var cmd = CommandBufferPool.Get();
            cmd.name = "VideoFeed DownSampling & Blur";

            //DownSampling
            cmd.Blit(textureData.Texture2D, _renderTexture_DownSampleID);
            //blur
            _blurMaterial.SetFloat("_Brightness", 0.4f);
            cmd.Blit(_renderTexture_DownSampleID, textureData.RenderTargetID, _blurMaterial);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);

            textureData.UpdateTexture();
        }

        updateTextureDataPool.Clear();
    }

    public override void OnCameraCleanup(CommandBuffer cmd)
    {
    }
}
