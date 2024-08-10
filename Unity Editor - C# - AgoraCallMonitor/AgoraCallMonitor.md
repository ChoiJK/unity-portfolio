[![mainPage](https://img.shields.io/badge/Main%20Page-blue)](../README.md)

## AgoraCallMonitor
---
### 도입 이유
- 영상통화 솔루션 Agora를 사용하는 프로젝트로서 Agora 사용량은 요금과 직결됩니다.
- 너무 많은 채널을 사용하거나 너무 큰 해상도를 처리하고있다면 CPU/GPU의 성능에 영향을 줍니다.
- Agora 솔루션에서는 각 채널에 대한 정보만 제공합니다.
- 여러 채널을 관리하고, 요금을 예측하고 줄이기 위해 Monitor가 필요하여 개발하게 되었습니다.
---
### 전체 로직
- Agora로부터 각 채널의 정보를 취득합니다.
- 각 채널의 정보를 AgoraStats.AddStats, AddEvent를 통해 AgoraStats에 등록합니다.
    - 등록된 정보는 Update에서 1초에 한번씩 다시 정리됩니다.
- AgoraMonitor와 AgoraStatsWindow를 통해 에디터 화면에 렌더링됩니다.
- 테이블 형식으로 렌더링되며, 여러가지 key를 기준으로 정렬할 수 있습니다.
---
### 파일 설명
#### AgoraStats.cs
- 외부로부터 Agora 채널의 정보를 주입받아 관리하는 역할을 담당합니다.
#### AgoraMonitor.cs
- Editor 창의 요소들을 표시하는 역할을 담당합니다.
- DrawMaster, DrawChannel을 통해 EditorGUI를 표시합니다.
- DrawMaster
    - Agora를 통해 Sent/Recv하는 모든 데이터 크기
    - Agora가 사용하는 CPU, Memory 크기
- DrawChannel
    - 표 형식으로 현재 관리되고있는 모든 Agora Channel의 상세 정보
#### AgoraStatsWindow.cs
- 실제 Editor 창의 로직을 담당합니다.