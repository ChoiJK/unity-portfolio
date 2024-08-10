using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using UnityEngine;
using Random = UnityEngine.Random;

[Serializable]
[StructLayout(LayoutKind.Sequential)]
public struct OccludeeDataCSInput
{
    public Vector3 boundsCenter; // 3
    public Vector3 boundsSize; // 6

    public OccludeeDataCSInput(Vector3 center, Vector3 size)
    {
        boundsCenter = center;
        boundsSize = size;
    }
}

public class Hi_Z_OcclusionCulling : MonoBehaviour
{
    public static Hi_Z_OcclusionCulling instance;

    private readonly Stopwatch stopWatch = new();

    private bool _isUpdateComputeBuffer;


    #region Shader PropertyID

    private readonly int _ShouldFrustumCull = Shader.PropertyToID("_ShouldFrustumCull");
    private readonly int _ShouldOcclusionCull = Shader.PropertyToID("_ShouldOcclusionCull");
    private readonly int _ShouldDetailCull = Shader.PropertyToID("_ShouldDetailCull");
    private readonly int _ShadowDistance = Shader.PropertyToID("_ShadowDistance");
    private readonly int _DetailCullingScreenPercentage = Shader.PropertyToID("_DetailCullingScreenPercentage");
    private readonly int _HiZTextureSize = Shader.PropertyToID("_HiZTextureSize");
    private readonly int _OccludeeDataBuffer = Shader.PropertyToID("_OccludeeDataBuffer");
    private readonly int _IsVisibleBuffer = Shader.PropertyToID("_IsVisibleBuffer");
    private readonly int _HiZMap = Shader.PropertyToID("_HiZMap");

    private readonly int _UNITY_MATRIX_MVP = Shader.PropertyToID("_UNITY_MATRIX_MVP");
    private readonly int _CamPosition = Shader.PropertyToID("_CamPosition");

    #endregion

    #region Inspector Member

    public bool enableFrustumCulling = true;
    public bool enableOcclusionCulling = true;
    public bool enableDetailCulling = true;

    [Range(00.00f, 00.02f)] public float detailCullingPercentage = 0.005f;

    #endregion

    #region Occludee Member

    [SerializeField] public int OccludeeCount;
    public int CurrentViewOccludeeCount;
    public float OcclusionProcessTime;
    public float OcclusionApplyTime;
    private readonly List<IHiZOccludee> _HiZOccludees = new();
    private readonly List<IHiZOccludee> _HiZOccludees_AddBuffer = new();
    private readonly List<IHiZOccludee> _HiZOccludees_RemoveBuffer = new();

    #endregion

    #region ComputeShader

    private ComputeShader occlusionCS;
    private int kernelID = -1;
    private int _occlusionGroupX;
    private readonly string kernelName = "CSMain";

    private ComputeBuffer _occludeeDataBuffer;

    private ComputeBuffer _occludeeIsVisibleBuffer;

    private uint[] _isVisibleResultBuffer;
    private RenderTexture _hiZBuffer;
    private readonly int computeShaderInputSize = Marshal.SizeOf(typeof(OccludeeDataCSInput));

    #endregion

    #region Test Member

    private GameObject _testRootObj;
    private readonly List<GameObject> _OccludeeObjects = new();

    #endregion

    #region LifeCycle

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        Debug.LogWarning("GraphicsDeviceVersion : " + SystemInfo.graphicsDeviceVersion);
        Debug.LogWarning("GraphicsShaderLevel : " + SystemInfo.graphicsShaderLevel);

        UnityEngine.Camera.main.depthTextureMode |= DepthTextureMode.Depth;

        occlusionCS = Resources.Load<ComputeShader>("Shader/HiZ/HiZCullingCS");

        if (!occlusionCS.HasKernel(kernelName))
        {
            Debug.LogError(kernelName + " kernel not found in " + occlusionCS.name + "!");
            return;
        }

        kernelID = occlusionCS.FindKernel(kernelName);

        if (occlusionCS != null && occlusionCS.IsSupported(kernelID))
        {
            Debug.LogWarning("OcclusionCulling HiZCullingCS Success");
        }
        else
        {
            Debug.LogWarning("OcclusionCulling HiZCullingCS Fail");
        }


        occlusionCS.SetInt(_ShouldFrustumCull, enableFrustumCulling ? 1 : 0);
        occlusionCS.SetInt(_ShouldOcclusionCull, enableOcclusionCulling ? 1 : 0);
        occlusionCS.SetInt(_ShouldDetailCull, enableDetailCulling ? 1 : 0);
        occlusionCS.SetFloat(_ShadowDistance, QualitySettings.shadowDistance);
        occlusionCS.SetFloat(_DetailCullingScreenPercentage, detailCullingPercentage);
    }

    private void LateUpdate()
    {
        stopWatch.Restart();

        updateOccludeeList();

        if (_HiZOccludees.Count == 0)
        {
            return;
        }

        OccludeeCount = _HiZOccludees.Count;

        updateComputeBuffer();
        updateOccludeeDataBuffer();

        occlusionCS.SetFloat(_ShadowDistance, QualitySettings.shadowDistance);
        var mainCamera = UnityEngine.Camera.main;
        if (mainCamera != default)
        {
            processOcclusionCulling(mainCamera);
            stopWatch.Stop();
            OcclusionProcessTime = (float)stopWatch.Elapsed.TotalMilliseconds;

            stopWatch.Restart();
            applyResult();
            stopWatch.Stop();
            OcclusionApplyTime = (float)stopWatch.Elapsed.TotalMilliseconds;
        }

        _isUpdateComputeBuffer = false;
    }

    private void OnDestroy()
    {
        if (instance != this)
        {
            return;
        }

        _HiZOccludees.Clear();
        _HiZOccludees_AddBuffer.Clear();
        _HiZOccludees_RemoveBuffer.Clear();

        instance = default;
    }
    #endregion

    #region Hi-Z Occlusion

    private void updateOccludeeList()
    {
        // Remove
        foreach (var removeOccludee in _HiZOccludees_RemoveBuffer)
        {
            _HiZOccludees.Remove(removeOccludee);
        }

        // Add
        _HiZOccludees.AddRange(_HiZOccludees_AddBuffer);

        _HiZOccludees_AddBuffer.Clear();
        _HiZOccludees_RemoveBuffer.Clear();

        var occludeeCount = _HiZOccludees.Count;
        var iter = _HiZOccludees.GetEnumerator();
        for (var i = 0; i < occludeeCount; ++i)
        {
            if (iter.MoveNext())
            {
                iter.Current.Index = i;
            }
        }
    }

    private void updateComputeBuffer()
    {
        if (_occludeeDataBuffer != default && _occludeeDataBuffer.count >= _HiZOccludees.Count)
        {
            return;
        }

        if (_occludeeDataBuffer != default)
        {
            _occludeeDataBuffer.Release();
            _occludeeIsVisibleBuffer.Release();
        }

        _occludeeDataBuffer = new ComputeBuffer(_HiZOccludees.Count, computeShaderInputSize, ComputeBufferType.Raw,
            ComputeBufferMode.SubUpdates);

        var intBufferCount = (_HiZOccludees.Count + sizeof(uint)) / sizeof(uint);
        _occludeeIsVisibleBuffer = new ComputeBuffer(intBufferCount, sizeof(uint), ComputeBufferType.Raw);
        _isVisibleResultBuffer = new uint[intBufferCount];

        occlusionCS.SetBuffer(kernelID, _OccludeeDataBuffer, _occludeeDataBuffer);
        occlusionCS.SetBuffer(kernelID, _IsVisibleBuffer, _occludeeIsVisibleBuffer);
        _isUpdateComputeBuffer = true;
    }

    private void updateOccludeeDataBuffer()
    {
        _occlusionGroupX = Mathf.Max(1, _occludeeDataBuffer.count / 32);

        var writeBuffer = _occludeeDataBuffer.BeginWrite<OccludeeDataCSInput>(0, _HiZOccludees.Count);
        var occludeeCount = _HiZOccludees.Count;
        var iter = _HiZOccludees.GetEnumerator();
        for (var i = 0; i < occludeeCount; ++i)
        {
            if (iter.MoveNext())
            {
                writeBuffer[i] = iter.Current.GetOccludeeData();
            }
        }

        _occludeeDataBuffer.EndWrite<OccludeeDataCSInput>(_HiZOccludees.Count);
    }

    private void processOcclusionCulling(UnityEngine.Camera mainCamera)
    {
        _hiZBuffer = Hi_Z_DepthTexMaker_RenderPass.HiZDepthTexture;
        occlusionCS.SetTexture(kernelID, _HiZMap, _hiZBuffer);
        occlusionCS.SetVector(_HiZTextureSize, new Vector2(_hiZBuffer.width, _hiZBuffer.height));
        occlusionCS.SetMatrix(_UNITY_MATRIX_MVP, mainCamera.projectionMatrix * mainCamera.worldToCameraMatrix);
        occlusionCS.SetVector(_CamPosition, mainCamera.transform.position);

        // Dispatch
        occlusionCS.Dispatch(kernelID, _occlusionGroupX, 1, 1);
    }

    private void applyResult()
    {
        // IsVisibleBuffer -> _HiZOccludees
        CurrentViewOccludeeCount = 0;
        var intBufferCount = (_HiZOccludees.Count + sizeof(uint)) / sizeof(uint);
        _occludeeIsVisibleBuffer.GetData(_isVisibleResultBuffer);

        var occludeeCount = _HiZOccludees.Count;
        var iter = _HiZOccludees.GetEnumerator();
        for (var i = 0; i < intBufferCount; ++i)
        {
            var returnVal = _isVisibleResultBuffer[i];
            for (var j = 0; j < 4; ++j)
            {
                var currentVal = (returnVal >> (j * 8)) & 0xFF;
                if (currentVal == 0)
                {
                    CurrentViewOccludeeCount++;
                }

                if (iter.MoveNext())
                {
                    iter.Current.SetOcclusionResult(currentVal);
                }
                else
                {
                    break;
                }
            }
        }
    }

    #endregion

    #region Occludee Managing

    public void AddOccludee(IHiZOccludee occludee)
    {
        _HiZOccludees_AddBuffer.Add(occludee);
    }

    public void RemoveOccludee(IHiZOccludee occludee)
    {
        _HiZOccludees_RemoveBuffer.Add(occludee);
    }

    #region Testing

    public void CreateOccludeeObjcet(int count)
    {
        if (_testRootObj == default)
        {
            _testRootObj = new GameObject("Occlusion Test Root");
        }

        for (var i = 0; i < count; ++i)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.transform.parent = _testRootObj.transform;
            go.transform.position = new Vector3(Random.Range(-300f, 300f), 0.5f,
                Random.Range(-300f, 300f));
            var randomScale = Random.Range(0.5f, 5f);
            go.transform.localScale = new Vector3(randomScale, randomScale, randomScale);
            go.AddComponent<HiZOccludee>();

            _OccludeeObjects.Add(go);
        }
    }

    public void RemoveOccludeeObject(int count)
    {
        for (var i = 0; i < count; ++i)
        {
            if (_OccludeeObjects.Count == 0)
            {
                return;
            }

            Destroy(_OccludeeObjects[_OccludeeObjects.Count - 1]);
            _OccludeeObjects.RemoveAt(_OccludeeObjects.Count - 1);
        }
    }

    #endregion

    #endregion
}
