# 최신 전투 개선

2026-10-23 최신 기획 정리는 [CIRCUS_SHOW_UPDATE.md](CIRCUS_SHOW_UPDATE.md)입니다. **하나의 큰 서커스 무대에서 1막4웨이브·2막5웨이브를 진행**하고 이후 보스방이 열립니다. 이전 던전형 방/통로는 비활성 보관 상태입니다. **Q 대시(3.5m/0.2초/쿨타임1.5초)**와 Space로 넘는 보스 지면 충격파가 추가됐습니다. Shift 달리기는 유지합니다.

조작: WASD 이동, **왼쪽/오른쪽 Shift를 누른 채 이동하면 달리기**(기본 6m/s → 9.6m/s), Space 점프, V 시점 변경. Muin Player의 CircusPlayer / Sprint Multiplier에서 달리기 배율을 조정할 수 있습니다. 달리기는 이동 속도에만 적용하며 공격 쿨타임과 점프 높이는 유지합니다.

2026-10-23 연결형 맵/막 진행 확장은 [STAGE_EXPANSION_UPDATE.md](STAGE_EXPANSION_UPDATE.md)에 정리했습니다. 시작→A/B→C/D/FINAL→보스 통로→기존 보스전으로 이어집니다. 최신 연결형 진행에는 아래의 과거 단일 경기장 적 수 설정을 사용하지 않습니다.

2026-10-22 최신 설정은 [ARENA_BOSS_UPDATE.md](ARENA_BOSS_UPDATE.md)입니다. 기존 9개 패턴에 연속탄·연속 돌진·부유탄·안전지대·필살기를 확장했습니다. BGM과 기존 조용한 적중/내려찍기/등장 소리는 유지하고, 별도 음량을 가진 작은 보스 패턴 전용 소리를 추가했습니다.

보스 낙하 등장·카메라·포효·공격 준비 동작의 이전 작업 기록은 [BOSS_ENTRANCE_UPDATE.md](BOSS_ENTRANCE_UPDATE.md)를 참고하세요.

세 공격의 모션·타격감 작업은 [COMBAT_PRESENTATION_UPDATE.md](COMBAT_PRESENTATION_UPDATE.md), 이전 3페이즈 구성은 [FINAL_BOSS_UPDATE.md](FINAL_BOSS_UPDATE.md)에 기록돼 있습니다. 아래는 당시 설정에 대한 과거 기록이며 최신 밸런스/소리 연결은 ARENA_BOSS_UPDATE를 우선하세요.

> 현재 사운드는 이전 합성음 19종으로 되돌렸으며 보스 장판 패턴을 추가했습니다. [GROUND_PATTERN_UPDATE.md](GROUND_PATTERN_UPDATE.md)를 확인하세요.

> UI 여백 수정과 무료 CC0 효과음 20종 개선 사항은 [POLISH_UPDATE.md](POLISH_UPDATE.md)를 확인하세요.

> 2026-10-20: 최신 팀 버전 기반의 3막·보스전·사운드 확장이 적용되었습니다.
> 현재 동작·파일 목록·Inspector·검증 안내는 [THREE_ACT_UPDATE.md](THREE_ACT_UPDATE.md)를 확인하세요.

# 곡예 프로토타입

Unity 6000.3.14f1에서 `CircusGame/Assets/Prototype/Scenes/CircusPrototype.unity`를 열고 Play를 누릅니다.

## 현재 시연 범위

- 아트팀의 서커스장, 무인 캐릭터, 방울 모델 활용
- 시작 / 설정 / 종료 메뉴
- WASD 이동, Space 점프, 마우스 시점, 좌클릭 방울 음파 투사체 발사, 우클릭 방울 근접 공격
- 기본 3인칭 시점, V 키로 1인칭 / 3인칭 전환. 1인칭에서는 전용 방울 모델 표시
- 추적형 근접 랑과 고정형 원거리 랑을 스포너로 소환. 동시에 최대 6마리
- 제1막: 근거리 8마리. 제2막: 강화된 근거리 8마리와 원거리 4마리. 제3막: 보스 1마리
- 근거리 랑: 플레이어 추적 후 근접 공격. 원거리 랑: 예고선 후 투사체 발사
- 플레이어와 적 체력바 / 남은 랑 / 무대 위 랑 / 점수 / 라운드 HUD
- 체력 10% 미만에서 감염 상태와 녹색 체력바, 감염 시 공격력 증가
- ESC 또는 HUD의 정지 버튼으로 일시정지, 계속하기, 메인 화면 복귀
- 패배 / 보스 처치 후 세 막 완료 결과와 재시작
- 직접 합성한 19종 효과음과 음량 설정, Windows 빌드의 전체 화면 설정

## 팀 작업 시 참고

원본 `Assets/Scenes/Game Scenes.unity`, `UI Scenes.unity`와 아트 파일은 보존되어 있습니다. 원본 UI 씬에는 카메라와 조명만 있어, 메뉴와 HUD는 프로토타입 씬에 추가했습니다.

에너미의 `Temporary Voxel Model`은 임시 모델입니다. 아트팀 에너미가 나오면 `Assets/Prototype/Prefabs/CloseRang.prefab`, `RangedRang.prefab`의 해당 자식과 `model` 참조를 교체합니다. 콜라이더와 `CircusEnemy`는 루트에 유지합니다.

감염 기준은 상세기획서 플레이 방식의 10% 미만을 사용했습니다. 과제 안내의 투사체 공격과 적 AI 2종 조건에 맞춰 방울 음파 발사와 근접 랑 추적을 추가했습니다. 원거리 랑은 고정형입니다. 인원수는 시연용 8+12로 설정했으며, `Show Director`에서 변경할 수 있습니다. 거대 관중, 상세 내부 공간, 최종 에너미 모델은 이번 기본 프로토타입 범위에 포함되지 않습니다.

`Assets/Prototype/Scripts`에 기능별 런타임 코드가 있습니다. `Assets/Prototype/Editor`의 검증 코드는 이동·점프·공격·피격·감염·두 라운드·UI·재시작을 실제 Play Mode에서 점검합니다. 3막 테스트 보고서는 Unity 프로젝트의 `Temp/three-act-verification.txt`에 저장됩니다.

Windows 실행 파일은 `CircusGame/Builds/CircusPrototype/CircusPrototype.exe`입니다. 다른 PC에 전달할 때는 실행 파일만 보내지 말고 `CircusPrototype` 폴더 전체를 압축해 전달합니다. 빌드 폴더는 Git에서 제외됩니다.

`com.unity.pipeline`은 로컬 Unity 편집·검증 도구이며, Player 빌드에서는 비활성화됩니다. 편집 도구의 임시 eval 어셈블리 때문에 Input System 링크 단계에서 문자열 변환 오류가 발생하면, 씬을 저장하고 Unity를 재시작한 뒤 빌드합니다. 게임 코드나 아트 파일을 바꿀 필요는 없습니다.

글꼴은 SIL Open Font License의 나눔고딕입니다. 라이선스는 `Assets/Prototype/Fonts/OFL.txt`에 포함되어 있습니다.
