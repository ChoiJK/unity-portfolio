# Unity Portfolio 소개

> 전 직장의 개발과정에서 제가 직접 작업한 코드입니다.  
> 본 포트폴리오를 작성할 때는 프로젝트 실행이 불가능한 관계로  
> 해당 기능에 대한 캡쳐화면은 없습니다.  
> 이점 양해해주시기 바랍니다.
---
## Hi-Z Occlusion
[Hi-Z Occlusion 바로가기](Unity%20-%20C%23%20-%20Hi-Z%20Occlusion,%20Blur/OcclusionCulling/Hi-Z-Occusion.md)  
- Unity에서 제공해주는 Occlusion Culling은 Scene에 이미 배치된 Static Object에 대해서만 처리됩니다.
- 다양한 이유로 모든 Dynamic Object가 Occluder, Occludee역할을 수행할 수 있어야한다는 필요가 생겼습니다.
- GPU -> CPU 메모리 복사가 느려서 전체적인 occlusion성능은 Unity의 Occlusion Culling이 프레임당 0.5ms~1ms정도 이점이 있었습니다. (Android 기준, 나머지는 비슷함)
- 하지만 Scene에 배치된 모든 Object를 Dynamic으로 전환시킬 수 있다는 강점이 있어서 씬 커스터마이징을 고려하여 적용하였습니다.
---
## Blur Process
[Blur Process 바로가기](Unity%20-%20C%23%20-%20Hi-Z%20Occlusion,%20Blur/VideoFeedBlur/VideoFeedBlur.md)
- 다양한 이미지(영상통화, 미디어 플레이어, 화면공유 등)를 Blur처리해야하는 기능을 개발해야했습니다.
- 기존에 전역으로 사용하던 Blur방식은 CPU에서 처리하도록 작업되어 있었습니다.
- ComputeShader로 전환하려 시도해봤으나 Android에서 sRGB Texture 이슈로 정상적으로 렌더링되지 않았기에 RenderPass를 통해 블러처리하도록 고안하였습니다.
- 이를위해 Memory는 조금 더 사용했어야 했지만 CPU와 GPU의 사용량을 극단적으로 줄일 수 있었습니다.

---
## Agora Call Monitor (Editor)
[Agora Call Monitor 바로가기](Unity%20Editor%20-%20C%23%20-%20AgoraCallMonitor/AgoraCallMonitor.md)
- 영상통화 솔루션 Agora를 사용하는 프로젝트로서 Agora 사용량은 요금과 직결됩니다.
- 너무 많은 채널을 사용하거나 너무 큰 해상도를 처리하고있다면 CPU/GPU의 성능에 영향을 줍니다.
- Agora 솔루션에서는 각 채널에 대한 정보만 제공합니다.
- 여러 채널을 관리하고, 요금을 예측하고 줄이기 위해 Monitor가 필요하여 개발하게 되었습니다.
