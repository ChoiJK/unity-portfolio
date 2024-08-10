[![mainPage](https://img.shields.io/badge/Main%20Page-blue)](../../README.md)
## Blur Process
---
### 도입 이유
- 영상통화를 이용하는 프로젝트이고 다양한 경로를 통해 Image가 유입 될 수 있습니다.
    - ex) 영상통화, 주변 사람의 상태 이미지, 미디어 플레이어, 화면공유 등
- 플레이어의 상태(자리비움 등)에 따라 해당 플레이어가 제공해주는 이미지의 Blur처리가 필요했습니다.
    - 기존 코드는 cpu에서 처리되어 있었습니다. 이로 인해 CPU사용량이 너무 컸습니다.
- 아래의 제약사항이 있습니다.
    - 한번 입력받은 이미지는  3~15프레임가량 유지 될 수 있습니다. ( 영상통화의 fps는 3~15일 수 있습니다.)
    - ComputeShader에서 처리된 Texture2D는 Android에서 정상적으로 표현되지 않을 수 있습니다. (sRGB 이슈)
- 위 제약사항을 통해 작업 방향을 결정하였습니다.
    - 입력받은 이미지는 블러처리한 후 사용합니다. (1회만 연산)
    - ComputeShader가 아닌 RenderPass를 통해 블러처리 합니다.
- 이점과 단점
    - 이점
        - CPU, GPU의 사용량을 극단적으로 줄일 수 있었습니다.
            - 최악의경우 1프레임에  40장의 이미지를 블러처리 해야 할 수도 있습니다.
            - 이 경우에도 성능이 떨어지지 않았습니다.
    - 단점
        - 이미지 취득 후 블러완료 Texture 적용까지 딜레이가 발생할 수 있습니다.
            - 이미지 취득 전용 Thread에서 이미지 취득
            - Unity Thread Update에서 Texture생성 및 GPU로 업데이트
            - RenderPass에서 블러처리 - before Rendering
            - NextFrame - Unity Thread Update에서 Blur Texture적용(Blur 첫 프레임일 떄)
        - 블러처리를 위해 downSample Texture가 각 Texture별로 한장씩 더 필요합니다.
---
### 전체 로직
- 영상통화를 통해 이미지를 취득합니다. (100 * 100, 320 * 320, 1920 * 1080)
    - 이미지 취득 전용 Thread
- 이미지를 통해 TextureData(Texture2D의 MultiThread Wrapper)를 생성합니다.
- 이미지가 Blur처리되어야 할 플레이어의 이미지라면 VideoFeedBlurRenderPass에 Blur처리 요청합니다.
- VideoFeedBlurRenderPass에서 Blur처리합니다.
    - Blur 연산의 최적화를 위해 대상 Texture를 downSample합니다.
    - downSample된 Texture를 Blur처리합니다.
- TextureData를 Upate합니다.
- Update된 TextureData는 다음 프레임 Unity Thread에서 처리됩니다.
---
### 파일 설명
#### VideoFeedBlur*.cs
- AppendTextureData로 입력된 TextureData를 downSample, 가우시안블러 처리한 후에 apply해줍니다.
#### ShaderCode
- GaussianBlur.shader
    - 가장 간단한 형태의 가우시안블러입니다.