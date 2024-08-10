using UnityEngine;

namespace Assets.Scripts.OcclusionCulling
{
    public class Hi_Z_DepthDebugger : MonoBehaviour
    {
        [SerializeField] [Range(1, 11)]
        private int MipLevel;

        private RenderTexture _depthRT;
        private Material _material;
        private Renderer _renderer;

        // Use this for initialization
        private void Start()
        {
            _renderer = GetComponent<Renderer>();
            _material = new Material(Shader.Find("HiZ/HiZDepthDebug"));
            _renderer.material = _material;
        }

        // Update is called once per frame
        private void Update()
        {
            if (_depthRT == default || _depthRT != Hi_Z_DepthTexMaker_RenderPass.HiZDepthTexture)
            {
                _depthRT = Hi_Z_DepthTexMaker_RenderPass.HiZDepthTexture;
                _material.SetTexture("_MainTex", _depthRT);
            }

            _material.SetInt("_NUM", 0);
            _material.SetInt("_LOD", MipLevel);
        }
    }
}
