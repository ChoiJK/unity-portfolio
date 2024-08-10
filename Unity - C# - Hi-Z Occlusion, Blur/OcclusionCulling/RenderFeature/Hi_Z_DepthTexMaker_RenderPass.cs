using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

internal class Hi_Z_DepthTexMaker_RenderPass : ScriptableRenderPass
{
    // Consts
    private const int MAXIMUM_BUFFER_SIZE = 1024;
    public static int LODCount;

    private readonly Material _copyDepthMaterial;

    #region Profiling

    private readonly ProfilingSampler _executeSampler = new("HiZDepthMaker_Excute");

    #endregion

    private RenderTargetIdentifier _renderTaretId;
    private int _targetSize;
    private int[] _temporaries;

    public string[] _temporaryStrings;

    private Vector2 _textureSize;

    public Hi_Z_DepthTexMaker_RenderPass(RenderPassEvent evt)
    {
        renderPassEvent = evt;
        var shader = Shader.Find("HiZ/HiZDepthBuild");
        _copyDepthMaterial = new Material(shader);
        HiZDepthTexture = default;
    }

    public static RenderTexture HiZDepthTexture { get; private set; }

    public void RenderTextureUpdate(Camera mainCamera)
    {
        if (HiZDepthTexture != default)
        {
            HiZDepthTexture.Release();
        }

        var size = (int)Mathf.Max(mainCamera.pixelWidth, (float)mainCamera.pixelHeight);
        size = (int)Mathf.Min(Mathf.NextPowerOfTwo(size), (float)MAXIMUM_BUFFER_SIZE);
        _textureSize.x = size;
        _textureSize.y = size;
        LODCount = (int)Mathf.Floor(Mathf.Log(size, 2f));
        _temporaries = new int[LODCount];
        _temporaryStrings = new string[LODCount];
        for (var i = 0; i < LODCount; i++)
        {
            _temporaryStrings[i] = $"_temporaries_{i}"; //
        }


        HiZDepthTexture = new RenderTexture(size, size, 0, RenderTextureFormat.RGHalf);
        HiZDepthTexture.filterMode = FilterMode.Point;
        HiZDepthTexture.useMipMap = true;
        HiZDepthTexture.autoGenerateMips = false;
        HiZDepthTexture.Create();
        HiZDepthTexture.hideFlags = HideFlags.HideAndDontSave;

        _renderTaretId = new RenderTargetIdentifier(HiZDepthTexture);
    }

    public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
    {
        if (!renderingData.cameraData.camera.CompareTag("MainCamera"))
        {
            return;
        }

        if (_copyDepthMaterial == default)
        {
            return;
        }

        var mainCamera = renderingData.cameraData.camera;
        _targetSize = (int)Mathf.Max(mainCamera.pixelWidth, (float)mainCamera.pixelHeight);
        _targetSize = (int)Mathf.Min(Mathf.NextPowerOfTwo(_targetSize), (float)MAXIMUM_BUFFER_SIZE);
        _textureSize.x = _targetSize;
        _textureSize.y = _targetSize;
        LODCount = (int)Mathf.Floor(Mathf.Log(_targetSize, 2f));

        if (HiZDepthTexture == null ||
            HiZDepthTexture.width != _targetSize || HiZDepthTexture.height != _targetSize)
        {
            RenderTextureUpdate(mainCamera);
        }


        ConfigureTarget(new RenderTargetIdentifier(_renderTaretId, 0, CubemapFace.Unknown, -1), _renderTaretId);
        ConfigureClear(ClearFlag.Color, Color.black);
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        if (!renderingData.cameraData.camera.CompareTag("MainCamera"))
        {
            return;
        }

        if (_copyDepthMaterial == default)
        {
            return;
        }

        if (LODCount == 0 || _temporaries == default)
        {
            return;
        }

        var cmd = CommandBufferPool.Get();
        using (new ProfilingScope(cmd, _executeSampler))
        {
            cmd.name = "Hi_Z_DepthTexMaker Process";

            cmd.Blit(renderingData.cameraData.targetTexture, _renderTaretId, _copyDepthMaterial, (int)Pass.Blit);

            for (var i = 0; i < LODCount; ++i)
            {
                _temporaries[i] = Shader.PropertyToID(_temporaryStrings[i]);
                _targetSize >>= 1;
                _targetSize = Mathf.Max(_targetSize, 1);

                cmd.GetTemporaryRT(_temporaries[i], _targetSize, _targetSize, 0, FilterMode.Point,
                    RenderTextureFormat.RGHalf, RenderTextureReadWrite.Linear);

                if (i == 0)
                {
                    cmd.Blit(_renderTaretId, _temporaries[0], _copyDepthMaterial, (int)Pass.Reduce);
                }
                else
                {
                    cmd.Blit(_temporaries[i - 1], _temporaries[i], _copyDepthMaterial, (int)Pass.Reduce);
                }

                cmd.CopyTexture(_temporaries[i], 0, 0, _renderTaretId, 0, i + 1);

                if (i >= 1)
                {
                    cmd.ReleaseTemporaryRT(_temporaries[i - 1]);
                }
            }

            cmd.ReleaseTemporaryRT(_temporaries[LODCount - 1]);
        }

        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }

    public override void OnCameraCleanup(CommandBuffer cmd)
    {
        if (_copyDepthMaterial == default)
        {
        }
    }

    // Enums
    private enum Pass
    {
        Blit,
        Reduce
    }
}
