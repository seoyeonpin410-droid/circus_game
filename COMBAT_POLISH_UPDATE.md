# 전투 완성도 개선 (2026-10-20)

기존 `CircusPrototype` 씬과 전투 코드를 확장했다. 새 보스를 중복 제작하지 않았고, 이동/점프, 좌클릭 투척, 우클릭 근접, V 시점 전환, 일반 근접·원거리 적, 8/12마리 웨이브, 체력/감염/점수, 기존 보스 장판을 유지했다. 커밋·푸시는 하지 않았다.

## 현재 구조 분석과 변경

| 시스템 | 기존 상태 | 이번 변경 |
|---|---|---|
| 플레이어 | 카메라 기준 이동/점프, 근접 예고, 투척, 두 시점 | 유지. 감염 상태의 강한 근접 적중에만 미세 흔들림 |
| 일반 근접 적 | 추적, 0.45초 원형 예고, 공격 후 쿨타임 | 유지. 별도 거친 스윙음과 피격 반짝임/작은 효과 추가 |
| 일반 원거리 적 | 고정형, 조준선, 청록 투사체/트레일/빛 | 예고 구체 추가, 예고 시 조준 방향 고정 후 발사 |
| 스폰 | 즉시 생성 | 0.5초 보라색 바닥 예고. 동안 모델/HP바/충돌/피해 비활성 |
| 1/2막 | 8/12마리, 2막 HP 1.4배·공격간격 0.8배 | 유지 |
| 막 사이 | 2.5초 정리/타이머 정지/HP 20 회복 | 유지 |
| 3막 | 일반 스폰 종료, 650 HP 보스 1마리 | 이름·HP바 표시 후 2.5초 전투 차단, 일시정지 지원 |
| 보스 | 근접/원거리/3개 장판 | 상태 분리, 예고 0.65초, 확실한 회복 시간, 2페이즈 |
| 피격 | HP/사운드 중심 | 0.1초 머티리얼 반짝임, 작은 금색 적중 효과 |
| 효과음 | 19개 합성음, 이전 무료 편집 팩은 보관 | 역할별 22개만 연결, 비행 루프 제거, 볼륨/중첩 제한 |

## 보스 AI

`Entrance → Chase → MeleeWindup → MeleeAttack → Recovery` 또는 `RangedWindup → RangedAttack → Recovery`.
추가로 기존 `GroundCast`, `PhaseTransition`, `Dead`, 대기용 `Idle` 상태가 있다.

- 가까이(3.6m 이내): 근접. 반경 3.8m, 전방 130도 부채꼴을 **현재 위치/방향에 고정**하여 0.65초 먼저 표시한다. 표시와 피해는 같은 `CircusAttackArea.Contains`를 사용한다. 공격 후 0.1초 동작과 1.4초 회복.
- 멀리(6m 이상): 충전 구체가 커진 뒤 원거리 발사. 조준 방향은 예고 시작 때 고정된다. 발사 후 0.12초 동작과 0.8초 회복. 원거리 재사용 간격 2.1초.
- 중간 거리/재사용 대기: 기존 CharacterController로 추적한다.
- 장판: 기존 3개 원을 0.45초 간격으로 표시, 1.15초 후 발동, 1.4초 지속. 반경 1.8m, 주기 피해 18. 점프해도 바닥 위치에 생성된다. 기존 7.5초 재사용 대기 유지.
- 플레이어 피격 무적 0.65초 유지. 보스 근접 피해 24 이상에서 카메라가 최대 0.04m/0.12초만 흔들린다.

| 항목 | Phase 1 | Phase 2 (HP 50% 이하) |
|---|---|---|
| 진입 | 기본 | 1.2초 빛/안내, 동안 새 공격 없음, 이후 0.6초 회복 |
| 이동 | 2.1m/s | 15% 증가 |
| 재사용/근접 회복 | 기본 | 20% 감소 |
| 근접 | 1회 | 첫 타 후 0.32초 정리 → 아직 가까우면 **0.65초 재예고** → 2타 → 회복. 멀어졌으면 두 번째 타 취소 |
| 원거리 | 1발 | 기존 투사체를 -17/0/+17도 3방향 발사 |
| 공격 피해/장판 | 기본 | 동일, 불필요한 피해 증가 없음 |
| HUD | 청록 체력바, 1페이즈 | 붉은 체력바, 2페이즈 |

## 스크립트

새 런타임: `CircusCharge.cs` — 충돌/피해 없는 마법 충전 구체. 새 Editor 도구: `CircusCombatPolishSetup.cs`, `CircusCombatPolishVerification.cs`.
이전 작업에서 추가되어 이번에도 유지한 런타임: `CircusGroundFlame.cs`.

수정: `CircusBoss.cs`, `CircusEnemy.cs`, `CircusSession.cs`, `CircusPlayer.cs`, `CircusCamera.cs`, `CircusHud.cs`, `CircusAudio.cs`, `CircusImpact.cs`.
검증 갱신: `CircusThreeActVerification.cs`, `CircusGroundPatternVerification.cs`. 이전 사운드 편집 도구의 배열 길이도 새 큐 추가에 맞춰 방어했다(`CircusPolishSetup.cs`).
사운드 생성 도구: `Tools/GenerateCombatSounds.py`.

## 효과음 출처와 역할

상용 게임 음원을 가져오지 않았다. 기존 `Free/`에서 어울리는 CC0 소스 3개만 사용하고, 나머지 19개는 고정 난수/필터 노이즈/감쇠 공명/사인파로 직접 만든 **임시 효과음**이다. AI 생성 음원이나 전문 녹음이라고 주장하지 않는다. 종전 긴 멜로디·금속 냄비·방울을 물리 타격에 섞은 팩은 비활성 상태로 보관한다.

- Kenney RPG Audio: https://kenney.nl/assets/rpg-audio — `knifeSlice.ogg` 플레이어 스윙, `knifeSlice2.ogg` 일반 적 스윙.
- Kenney Interface Sounds: https://kenney.nl/assets/interface-sounds — `click_001.ogg` UI 클릭.
- 위 파일의 원본 License.txt는 각 `Audio/Free/` 폴더에 보관. CC0.
- 직접 생성본: `Audio/Combat/*.wav`, 44.1kHz 모노 16bit. 재생성은 `Tools/GenerateCombatSounds.py`.

| 큐 | 상황 | 개별 볼륨 배수 |
|---|---|---|
| Countdown / Start | 짧은 전자 알림 / 시작 | 0.25 / 0.35 |
| ActChange / Clear | 막 변경 / 클리어 시스템 알림 | 0.30 / 0.45 |
| UIClick | 버튼 클릭, Kenney | 0.20 |
| PlayerMelee / EnemyMelee | 빠른 스윙 / 별도 거친 스윙, Kenney | 0.60 / 0.50 |
| PlayerShot | 방울 마법 투척 에너지 발사 | 0.45 |
| PlayerHit / PlayerDeath | 둔한 피격 / 소멸 | 0.85 / 0.65 |
| EnemyHit / EnemyDeath | 짧은 별도 타격 / 소멸 | 0.30 / 0.35 |
| BossArrival | 낮은 에너지 상승 | 0.55 |
| BossMelee / BossShot | 무거운 공격 / 강한 마법 발사 | 0.85 / 0.80 |
| BossHit / BossDeath | 낮은 타격 / 긴 저음 소멸 | 0.40 / 0.85 |
| MagicCharge | 일반/보스 마법 충전, 일반 적은 추가 0.25배 | 0.30 |
| Warning | 도깨비불 충전 때만 작은 금속성 울림, 추가 0.3배 | 0.15 |
| FlameLaunch / FlameImpact | 일반 도깨비불 발사 / 충돌 에너지 타격 | 0.50 / 0.40 |
| PhaseShift | 낮은 페이즈 전환 충전 | 0.55 |
| FlameFlight | 비행 루프 사용하지 않음 | 0 |

전체 효과음 볼륨 0.55와 기존 설정 화면의 마스터 슬라이더가 추가 적용된다. 12개 보이스 풀, 같은 클립 동시 최대 2개, 적 피격/불 충돌 최소 0.12초 간격, 다른 큐 최소 0.075초 간격. 군중 타격음이 겹치면 추가 소리를 생략한다. UI/물리 스윙/타격/마법 소스를 분리했다.

## 아트와 이펙트

Assets 전체 이미지/프리팹/음원을 확인한 뒤 기존 Canvas를 그대로 사용했다.
`이미지/자산 8.png` 전통 프레임은 기존 카운트다운·보스 HP·버튼/패널에서 유지.
`자산 5.png` 도깨비 문양, `자산 9.png` 배경, `무인.png` 캐릭터 및 `Font/11/GowunBatang` 글꼴 유지.
막 안내·등장·2페이즈 문구는 기존 HUD 안내 영역을 재사용했다. 별도 기본 네모 패널을 덧씌우지 않았다.

새 효과는 충전 구체, 스폰 예고 원, 0.1초 피격 플래시, 작은 적중 섬광. 기존 발광 청록 도깨비불/금색 플레이어 투사체, 트레일, 빛, 충돌 효과와 보라→주황 장판을 유지했다.

## Inspector와 실행

`Assets/Prototype/Scenes/CircusPrototype.unity` 기준으로 연결 완료. **직접 연결할 필수 항목 없음.**
보스 프리팹 `DokkaebiBoss`의 Phase Two Speed(1.15), Phase Two Cooldown(0.8), Transition Seconds(1.2), Windup Seconds(0.65)로 조절 가능.
`CircusAudio`의 Clips/Cue Volumes/Effects Volume으로 역할별 소리를 교체·조절할 수 있다. 비행 큐 null은 의도적이다.
아트 모델/씬/설정 화면을 다시 만드는 전체 Builder는 실행하지 않는다. 사운드 재연결은 `CircusCombatPolishSetup.Configure()`를 사용한다.

## 검증

실제 Editor Play에서 **101개 항목 통과**: 신규 전투 30개, 기존 3막 전체 흐름 54개, 장판 13개, 일반 원거리 회피·피해 4개.
스폰 예고 중 충돌/피해 차단과 종료 후 체력바 복구, 수입 모델의 실제 발광 키워드, 근접 회피/적중, 공격 후 회복, 50% 전환, 2연격 재예고, 3개 투사체 실제 생성, 보스 등장 중 입력/피해/타이머 차단, 일시정지/재시작/클리어/정리를 포함한다.
보고서는 작업 폴더 `work/combat-polish-verification.txt`, `work/combat-polish-three-act-verification.txt`, `work/combat-polish-ground-verification.txt`, `work/combat-polish-normal-ranged-verification.txt`에 보관했다. 컴파일 오류/경고 0개.

Windows 실행 빌드 성공(오류 0개). 경고 1개는 개발 제어용 Pipeline이 배포 실행본에서 비활성이라는 안내이며 게임 코드 오류가 아니다. 결과: `CircusGame/Builds/CircusPrototype/CircusPrototype.exe`.

효과음은 소스/연결/볼륨/중첩 로직을 점검했으며 임시 생성음의 청감 품질을 전문 음원 수준으로 보장하지 않는다. 실제 스피커에서의 최종 취향 조정은 `Cue Volumes`에서 가능하다.
