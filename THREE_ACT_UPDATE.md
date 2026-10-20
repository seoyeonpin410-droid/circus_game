# 3막 보스전 확장 안내

기준 버전: 팀 저장소 `06bd18c` (시작 카운트 다운 구현).
작업 전 변경은 `최신 팀 버전 반영 전 보스전 확장 작업 백업 2026-10-20` 스태시와
작업 공간의 `work/backup-before-team-update-20261020`에 보관했습니다.
최신 팀 씬, UI 배치·이미지, 글꼴, 일반 적 프리팹을 기준으로 확장했습니다.

## 실행 및 진행

Unity 6000.3.14f1에서 `Assets/Prototype/Scenes/CircusPrototype.unity`를 열고 Play → 곡예 시작.
WASD 이동, Space 점프, 마우스 시점, V 1/3인칭 전환, 좌클릭 음파, 우클릭 근접, ESC 정지.

- 시작: 3 → 2 → 1 (3초), START! 표시 후 전투 시작. 이동·점프·공격·스폰·게임 진행 시간은 준비 중 정지합니다. ESC로 카운트다운도 정지/재개됩니다.
- 1막: 기본 체력 50의 근접 적 8마리, 동시 최대 4마리. 스폰 간격 1.3초.
- 2막: 총 12마리(근접 8, 원거리 4), 체력 70(1.4배), 공격 간격 0.8배, 스폰 간격 0.975초, 동시 최대 6마리.
- 막간: 2.5초 안내, 체력 20 회복, 전투와 게임 진행 시간 정지.
- 3막: 일반 웨이브 없이 보스 한 마리(체력 650). 보스 처치 시 클리어. 전체 기본 점수 3400.
- 재시작은 체력·감염·점수·막·보스·투사체를 초기화하고 카운트다운부터 시작합니다.

## 스크립트

새 런타임 코드:
- `CircusBoss.cs`: 등장, 추적, 근접 준비, 원거리 준비, 회복, 사망 상태.
- `CircusAttackArea.cs`: 바닥 부채꼴/원과 동일한 평면 범위 판정.
- `CircusAudio.cs`: 19종 사운드 뱅크, 효과음 볼륨, 최대 음성 수, 반복 재생 간격 및 비행음 관리.
- `CircusImpact.cs`: 투사체 충돌 시 짧게 퍼지는 발광 구체.

수정한 런타임 코드:
- `CircusSession.cs`: 기존 카운트다운 확장, 3막 진행, 보스 생성, 진행 시간과 상태별 입력 차단.
- `CircusPlayer.cs`: 기존 이동·점프·좌클릭 음파 유지, 우클릭 0.18초 준비 후 범위 타격, 사운드 연결.
- `CircusEnemy.cs`: 기존 추적·고정형 원거리 적 유지, 2막 난이도 배율, 0.45초 공격 예고, 피격·사망음.
- `CircusProjectile.cs`: 기존 SphereCast 연속 충돌 판정 유지, 발사별 피해·속도, 비행음·충돌음·충돌 효과.
- `CircusHud.cs`: 팀 UI·글꼴 유지, 기존 IMGUI 카운트다운을 아트 기반 Canvas 표시로 통합, 보스 체력·3막 클리어 표시.
- `CircusCamera.cs`: 기존 1/3인칭 유지, 준비·막간에도 자연스러운 전투 카메라 유지.

새 편집기 도구:
- `CircusThreeActSetup.cs`: 기존 씬을 통째로 만들지 않고 보스·투사체·UI·사운드 연결을 추가하는 설정 도구.
- `CircusThreeActVerification.cs`: 실제 Play Mode에서 카운트다운, 기존 조작, 3막, 보스 공격/회피/피격/체력바/클리어/재시작 검증.
- `Tools/GenerateCircusSounds.py`: 외부 의존성이 없는 원본 효과음 합성기.

## 보스 AI와 판정

등장 대기 1.4초 → 추적 → 거리별 공격 선택 → 준비 0.5초 → 공격 → 회복 → 다시 추적합니다.
근접은 반경 3.8m, 앞쪽 반각 65도(전체 130도)의 붉은 부채꼴입니다.
준비가 시작될 때 위치와 방향이 고정되므로 옆이나 뒤로 피할 수 있습니다.
공격 후 1.4초 회복하며, 피해는 24입니다.
6m 이상 떨어지면 발광 예고 후 원거리 도깨비불(피해 14, 속도 8.5m/s)을 발사합니다.
원거리 쿨타임은 2.1초, 발사 후 0.65초 회복합니다. 쿨타임 중에는 접근합니다.

플레이어는 반경 3.6m, 앞쪽 전체 약 151도 금색 부채꼴을 먼저 보여준 뒤 0.18초 후 타격합니다.
일반 근접 적은 반경 2.2m 붉은 원을 0.45초 먼저 보여줍니다.
렌더링과 피해 판정이 같은 반경·각도를 사용하며, 적 중심이 범위에 들어오면 피격됩니다.
점프로 공격 높이(1.6m)를 넘으면 회피됩니다.

플레이어 투사체는 금색, 적 투사체는 청록색입니다. 구체 크기, 발광 재질, 짧은 Trail과
작은 점광원을 추가했습니다. 피격 시 0.18초 퍼지는 구체로 충돌 위치를 표시합니다.

## 팀 아트 재사용

Assets 전체와 실제 Sprite 임포트를 확인했습니다.
- `Assets/이미지/자산 8.png`: 카운트다운 테두리와 보스 체력 패널. 기존 UI 프레임 스타일 유지.
- `Assets/이미지/자산 5.png`: 카운트다운 위의 도깨비 문양.
- `Assets/이미지/자산 9.png`, `자산 6.png`, `무인.png`: 최신 팀 씬의 기존 배경·장식·인물 사용 위치 유지.
- `Assets/Font/11`의 팀 SDF 글꼴: 새 안내·보스 이름에도 기존 글꼴 재사용.
- `Assets/모델링/근거리.obj`와 `근거리.png`: 보스용으로 크게 배치하고 원본 텍스처 유지. 원본 모델은 수정하지 않았습니다.
기존 시작·설정·결과 화면과 버튼을 유지하며, 클리어 문구만 3막 기준으로 바꿨습니다.

## 효과음 및 Inspector

외부 녹음이나 상용 게임 음원을 사용하지 않았습니다. 19종 모두 사인파·비정수 배음·주파수 스윕·잡음으로 직접 합성한
22,050Hz 모노 16-bit PCM WAV입니다. 출처와 재생성은 `Assets/Prototype/Audio/SOURCES.md`에 기록했습니다.
카운트다운·시작·막 변경·클리어 / 플레이어 근접·원거리·피격·사망 / 적 피격·사망 /
보스 등장·근접·원거리·피격·사망 / 도깨비불 발사·비행·충돌 / 강한 공격 예고를 구분합니다.

사운드는 `CircusAudio`에서 중앙 관리하고, 한 효과음 반복 간격은 기본 0.075초, 일반 음성은 최대 12개,
조용한 비행 루프는 최대 4개로 제한합니다. 기존 전체 음량 설정도 적용됩니다.

추가로 수동 연결할 필수 항목은 없습니다. 다음 항목은 조정용입니다.
- Show Director → Circus Session: Boss Prefab, Round Counts(일반 웨이브 2개), Spawn Interval, Max Alive, Intermission Seconds.
- Show Director → Circus Audio: Effects Volume(기본 0.55), Max Voices, Min Repeat Seconds, Clips 뱅크.
- `DokkaebiBoss.prefab` → Circus Enemy: Health(650), Model, Projectile.
- 같은 보스 → Circus Boss: Move Speed, Melee Radius/Half Angle/Damage, Windup Seconds, 쿨타임, Ranged Distance, Projectile Speed/Damage.
- Circus HUD: Countdown Text/Panel, Boss Panel, Boss Health Fill/Text 참조가 연결되어 있습니다.
- 모든 일반 공격/시점/이동 조정은 기존 Inspector 값을 계속 사용합니다.

Windows 실행 파일은 `CircusGame/Builds/CircusPrototype/CircusPrototype.exe`입니다.
전달 시 같은 폴더의 데이터·DLL도 함께 포함해야 합니다. 빌드는 Git에 포함되지 않습니다.

## 검증 결과

- Play Mode 3막 전체 흐름 검증: 51개 통과.
- 기존 1/3인칭 카메라 회귀 검증: 12개 통과.
- 두 시점의 투사체·근접 공격 회귀 검증: 10개 통과.
- 사운드 반복 제한·동시 재생 한도·전체 음량: 4개 통과.
- 합계 77개 통과, 최종 확인 시 Unity 콘솔 오류·경고 0개.
- 실제 Game View에서 아트 프레임 카운트다운, 보스 체력바와 붉은 경고 범위를 캡처해 확인했습니다.
- 카운트다운 숫자는 팀 GowunBatang 글꼴을 유지하고 전용 재질의 중복 굵기만 제거했습니다.
- 기존 검증 진입점 `CircusPrototypeVerification.Run()`은 새 3막 검증으로 연결됩니다.
  `CircusCameraVerification`, `CircusCombatVerification`도 새 카운트다운과 공격 준비 시간에 맞춰 갱신했습니다.
- Windows 빌드 성공(오류 0). 편집 도구 Pipeline의 Player 비활성화 안내 1건이 있습니다.
- 완성된 실행 파일을 8초간 별도 실행해 시작 화면 초기화와 씬/사운드 로딩 예외가 없는지 확인했습니다.
- 커밋·푸시는 하지 않았으며, 기준 HEAD는 팀 최신 `06bd18c`입니다.
