# 곡예 프로토타입

Unity 6000.3.14f1에서 `CircusGame/Assets/Prototype/Scenes/CircusPrototype.unity`를 열고 Play를 누릅니다.

## 현재 시연 범위

- 아트팀의 서커스장, 무인 캐릭터, 방울 모델 활용
- 시작 / 설정 / 종료 메뉴
- WASD 이동, Space 점프, 마우스 시점, 좌클릭 방울 음파 투사체 발사, 우클릭 방울 근접 공격
- 기본 3인칭 시점, V 키로 1인칭 / 3인칭 전환. 1인칭에서는 전용 방울 모델 표시
- 추적형 근접 랑과 고정형 원거리 랑을 스포너로 소환. 동시에 최대 6마리
- 제1막: 근거리 8마리. 제2막: 근거리 8마리와 원거리 4마리
- 근거리 랑: 플레이어 추적 후 근접 공격. 원거리 랑: 예고선 후 투사체 발사
- 플레이어와 적 체력바 / 남은 랑 / 무대 위 랑 / 점수 / 라운드 HUD
- 체력 10% 미만에서 감염 상태와 녹색 체력바, 감염 시 공격력 증가
- ESC 또는 HUD의 정지 버튼으로 일시정지, 계속하기, 메인 화면 복귀
- 패배 / 두 막 완료 결과와 재시작
- 실제 방울 효과음과 음량 설정, Windows 빌드의 전체 화면 설정

## 팀 작업 시 참고

원본 `Assets/Scenes/Game Scenes.unity`, `UI Scenes.unity`와 아트 파일은 보존되어 있습니다. 원본 UI 씬에는 카메라와 조명만 있어, 메뉴와 HUD는 프로토타입 씬에 추가했습니다.

에너미의 `Temporary Voxel Model`은 임시 모델입니다. 아트팀 에너미가 나오면 `Assets/Prototype/Prefabs/CloseRang.prefab`, `RangedRang.prefab`의 해당 자식과 `model` 참조를 교체합니다. 콜라이더와 `CircusEnemy`는 루트에 유지합니다.

감염 기준은 상세기획서 플레이 방식의 10% 미만을 사용했습니다. 과제 안내의 투사체 공격과 적 AI 2종 조건에 맞춰 방울 음파 발사와 근접 랑 추적을 추가했습니다. 원거리 랑은 고정형입니다. 인원수는 시연용 8+12로 설정했으며, `Show Director`에서 변경할 수 있습니다. 거대 관중, 상세 내부 공간, 최종 에너미 모델은 이번 기본 프로토타입 범위에 포함되지 않습니다.

`Assets/Prototype/Scripts`에 기능별 런타임 코드가 있습니다. `Assets/Prototype/Editor`의 검증 코드는 이동·점프·공격·피격·감염·두 라운드·UI·재시작을 실제 Play Mode에서 점검합니다. 테스트 보고서는 Unity 프로젝트의 `Temp/circus-verification.txt`에 저장됩니다.

Windows 실행 파일은 `CircusGame/Builds/CircusPrototype/CircusPrototype.exe`입니다. 다른 PC에 전달할 때는 실행 파일만 보내지 말고 `CircusPrototype` 폴더 전체를 압축해 전달합니다. 빌드 폴더는 Git에서 제외됩니다.

`com.unity.pipeline`은 로컬 Unity 편집·검증 도구이며, Player 빌드에서는 비활성화됩니다. 편집 도구의 임시 eval 어셈블리 때문에 Input System 링크 단계에서 문자열 변환 오류가 발생하면, 씬을 저장하고 Unity를 재시작한 뒤 빌드합니다. 게임 코드나 아트 파일을 바꿀 필요는 없습니다.

글꼴은 SIL Open Font License의 나눔고딕입니다. 라이선스는 `Assets/Prototype/Fonts/OFL.txt`에 포함되어 있습니다.


