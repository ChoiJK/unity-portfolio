[![mainPage](https://img.shields.io/badge/Main%20Page-blue)](../../README.md)
## Hi-Z Occlusion
---
### 도입 이유
- Unity에서 제공해주는 Occlusion Culling은 Scene의 Static Object에 대해서만 처리됩니다.
- 프로젝트에서 문, 회의실 등의 객체를 사용자가 Dynamic Object로 추가 할 수 있도록 기능을 제공하고자 했습니다.
- 이 오브젝트들은 미리 씬의 Occlusion Data에 포함되어있지 않기에 오브젝트 뒤의 오브젝트를 가리지못합니다.
- 모든 Dynamic Object가 Occluder, Occludee역할을 수행 할 수 있도록 DepthMap을 기준으로 처리되는 Hi-Z Occlusion Culling을 선택하여 개발했습니다.  
---
### 전체 로직
#### Occludee  
- Scene에 생성되면 Hi_Z_OcclusionCulling에 AddOccludee로 자신을 추가합니다.
- Scene에서 제거될 때 Hi_Z_OcclusionCulling에 RemoveOccludee로 자신을 제거합니다.
- Hi_Z_OcclusionCulling에서 연산 결과가 발생하면 SetOcclusionResult를 통해 Occlusion상태를 입력받아 처리합니다.
#### Hi-Z DepthMap
- urp의 DepthTexture를 복사해옵니다.
- 이전 Mip-level의 4개 텍셀 중 가장 화면에서 멀리 떨어진 텍셀을 선택하여 다음 Mip-level의 1개의 텍셀로 매핑합니다.
#### Hi-Z Occlusion Culling
- 입력받은 Occludee와 Hi-Z DepthMap을 통해 Occlusion 연산을 실행합니다.
- Occludee가 ScreenSpace에 렌더링 될 바운드 영역 크기를 구한 후 어떤 Mip-level을 사용할지 결정합니다.
    - Compute Shader를 통해 처리
- 결정된 Mip-level의 텍셀 4개를 선택하여 가장 멀리있는 텍셀의 Depth값과 Occludee 바운드에서 카메라와 가장 가까운 Depth값을 비교하여 Occlusion되었는지 체크합니다.
    - Compute Shader를 통해 처리
- Occlusion Culling 결과를 각 Occludee에 통보합니다.   
---
### 파일 및 코드 설명
#### HiZOcludee.cs
- Occludee를 정의하고 Occlusion되었을 때의 행동양식을 결정합니다.
- IHiZOccludee를 상속합니다.
- 필요시 IHiZOccludee를 상속받아 새로운 행동양식의 Occludee를 정의 할 수 있습니다.
#### Hi_Z_OcclusionCulling.cs
- Hi-Z Occlusion 알고리즘을 실행합니다.
- Unity의 CullingGroup을 대체할 목적으로 설계되었습니다.
- 모든 Occludee는 AddOccludee, RemoveOccludee를 통해 자신을 Occlusion대상에 포함할지 결정합니다.
- ComputeShader를 통해 Occlusion 연산을 수행합니다.
- Hi-Z Occlusion 연산을 수행하려면 Mipmap처리된 DepthMap이 필요한데 이는 RenderPass를 통해 연산합니다.
    - RenderPass는 LateUpdate 이후에 실행되고, OcclusionCulling은 LateUpdate에서 수행되므로 OcclusionCulling에서 바라보는 DepthMap은 n-1프레임의 DepthMap입니다.
    - 1프레임의 차이가 발생 할 수 있는데 Occludee나 카메라가 매우 빠르게 움직이는 경우가 아니라면 보정은 필요하지 않습니다.
- ComputeShader로 Occlusion연산을 처리하고 GetData를 통해 결과를 취득한 후 결과를 Occludee에게 전달합니다.
    - GetData는 무겁습니다. GPU메모리에서 CPU메모리로 값을 가져오는 건 항상 무겁습니다.
    - 5000개 오브젝트 기준으로 안드로이드 구형기기에서도 약 1~1.5ms의 시간이 소요됩니다.
    - 좋은 대안으로는 GPU instancing과 연계하여 Occlusion처리 결과를 CPU메모로 복사하지 않고, Rendering on/off만 처리하도록 할 수 있습니다.
    - 현 프로젝트에서는 Occlusion처리 결과를 통해 CPU 연산을 크게 줄이고 있기에 CPU 메모리로 값을 가져오는 것이 이득이었습니다.
#### Hi_Z_DepthTexMaker_*.cs
- Mipmap처리된 DepthMap을 연산하는 RenderPass입니다.
- HiZDepthBuild의 Blit Shader를 통해 urp의 depthTexture를 Copy해오고 Reduce Shader를 통해 Mipmap 연산을 합니다.
#### Hi_Z_DepthDebugger.cs
- DepthMap의 Mipmap이 정상적으로 생성되었는지 확인하기 위한 기능입니다.
#### Shader Code
- HiZDepthBuild.shader
    - DepthMap의 Mipmap처리를 위한 shader입니다. ShaderInclude_HiZ.cginc를 사용합니다.
    - Blit를 통해 Unity의 DepthMap에서 복사해오고 Reduce를 통해 Mipmap 연산을 합니다.
- HiZCullingCS.compute
    - Hi-Z Occlusion 연산을 처리합니다.
    - Frustum Culling 연산을 진행합니다.
    - Frustum Culling을 통과한 Occludee에 대해 Hi-Z Map을 통해 Occlusion 연산을 진행합니다
    - Occlusion을 통과한 Occludee가 화면의 0.005%이상 차지하지 않는다면 Occlusion되었다 판별합니다.