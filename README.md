# 곡예 (Circus Game)

게임엔진응용 미니 팀 프로젝트. 동양풍 도깨비와 서양식 서커스를 결합한 3D 좀비 소탕 게임입니다.

## 프로젝트 열기

- Unity Editor: **6000.3.14f1** (팀원 모두 동일 버전 사용)
- 템플릿: **3D URP** (Universal Render Pipeline)
- Unity Hub에서 **Add > Add project from disk**를 선택하고 이 저장소의 **CircusGame** 폴더를 추가합니다.
- 기본 씬: `CircusGame/Assets/Scenes/SampleScene.unity`
- 초기 프로젝트이며 게임 로직은 이후 구현합니다.

## 기획 기준

- WASD 이동, 마우스 좌클릭 공격, 스페이스바 점프.
- 근거리형 및 원거리형 좀비. 원거리형은 도깨비 불씨를 발사합니다.
- 1라운드: 근거리형 100마리. 2라운드: 근거리형 50마리 + 원거리형 50마리.
- 점수, 체력, 감염 상태, 라운드, 남은 적 수 표시.
- 엔딩: 클리어 / 사망 / 좀비화 사망.
- 감염 시작 체력 기준은 기획서의 20% 이하와 10% 미만 중 추후 확정합니다.

## 협업

- `Assets`, `Packages`, `ProjectSettings` 및 `.meta` 파일을 함께 관리합니다.
- `Library`, `Temp`, `Logs`, `UserSettings` 등 자동 생성 폴더는 커밋하지 않습니다.
- 프로그래밍 1: UI, 점수, HP, 라운드 및 남은 적 수.
- 프로그래밍 2: 플레이어와 무기, 적 소환·이동·공격, 엔딩.
- 아트: 모델링, 무기, 배경, UI 이미지와 효과.
