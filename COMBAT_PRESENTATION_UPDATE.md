# 세 공격 연출과 타격감 개선 (2026-10-21)

> 이후 보스 등장과 기존 공격 준비 동작을 개선했다. 최신 보스 Windup과 등장 전용 오디오 연결은 [BOSS_ENTRANCE_UPDATE.md](BOSS_ENTRANCE_UPDATE.md)를 따른다.

이번 변경은 기존 플레이어 근접, 일반 원거리 적, 보스 내려찍기에 집중했다. 기존 이동·1/3인칭·HP·충돌 판정·막 진행·보스 패턴 선택 구조와 아트 모델/UI를 유지했다. 새 패턴, 스테이지, UI, 캐릭터, 적 종류는 추가하지 않았다.

## 플레이어 근접

- 노란 바닥 부채꼴과 기존 바닥 LineRenderer를 렌더링하지 않는다. 부채꼴 데이터는 기존 데미지 검사에만 사용한다.
- 몸 전체를 반대 방향으로 살짝 틀어 준비 → 빠르게 회전하며 8cm 전진하는 시각적 동작 → 복귀. 실제 캐릭터 컨트롤러 위치를 강제로 움직이거나 관절을 가정하지 않는다.
- 허리 높이에서 짧게 이동하는 얇은 Slash ribbon을 0.16초 표시한다. 전체 공격 범위를 채우는 도형이 아니다.
- 적의 HP가 실제로 감소한 경우에만 적중음, 0.04초 히트 스톱, 아주 약한 카메라 흔들림을 한 번 적용한다. 기존 적 피격 Flash와 작은 입자 효과를 사용한다. 무적 보스·스폰 중인 적·미스에는 히트 스톱을 적용하지 않는다.
- 히트 스톱 중 일시정지/메뉴/막 전환으로 넘어가도 시간 배율이 잘못 남지 않도록 처리했다.

## 일반 원거리 적과 투사체

- 플레이어 방향 고정 → 0.35초 동안 몸을 뒤로 당기고 10도 젖힘 → 발사 때 작은 전방 반동 → 0.24초 복귀.
- 충전 표시도 기본 Sphere 대신 부드러운 입자를 사용한다. 플레이어를 향하던 긴 디버그 선은 표시하지 않는다.
- 원거리 적의 충전/발사 위치를 1.65m로 올려 플레이어 몸에 가려지는 정도를 줄였다.
- `RangProjectile`의 기본 Sphere Mesh와 기존 삼각형처럼 늘어지는 TrailRenderer는 끈다. 부드러운 발광 입자 코어와 0.18초짜리 짧은 입자 잔상, 발사 Burst, 충돌 입자로 대체했다.
- 기존 투사체 이동·수명·SphereCast·충돌 반경과 피해 처리 방식은 유지한다. 플레이어 원거리 공격도 기존 투척 방식 그대로다.

## 보스 내려찍기

`SpecialWindup → SpecialAttack(낙하) → Recovery`라는 기존 실행 흐름을 확장했다.

1. 이동을 멈추고 0.85초 동안 몸을 최대 0.45m 들어 올리면서 뒤로 젖힌다.
2. 낮은 투명도의 주황/붉은 바닥 원과 얇은 테두리가 점점 진해진다. 마지막 약 0.13초에 테두리를 강조한다.
3. 0.1초 동안 빠르게 내려온다. 준비 종료 시에는 피해를 주지 않는다.
4. 모델이 원래 바닥 높이에 도착한 단일 프레임에 피해, 경고 제거, 충격파, 먼지/불티, 낮은 충격음, 카메라 흔들림을 함께 발생시킨다.
5. 몸의 기울기는 0.3초 동안 풀고, 총 1.6초의 회복 시간 후 기존 AI로 복귀한다.

경고 원 반경과 기존 `Contains` 피해 검사는 같은 4.2m 값을 사용한다. 페이즈 전환이나 사망이 공격을 중단하면 들어 올린 모델 위치도 복원한다.

## Inspector 값

씬과 프리팹에 연결·저장했으므로 수동 연결은 필요 없다. 이후 수치 조정은 아래 컴포넌트에서 가능하다.

| 대상 / 컴포넌트 | 항목 | 값 |
|---|---|---|
| 씬 Player / CircusPlayer | Attack Range | 3.6m, 기존 판정 유지 |
| 동일 | Attack Cooldown / Ranged Cooldown | 0.56 / 0.65초 |
| 동일 | Melee Windup / Swing / Recovery | 0.11 / 0.09 / 0.18초 |
| 동일 | Melee Lunge / Counter Yaw / Swing Yaw | 0.08m / 14° / 24° |
| 동일 | Hit Stop Seconds | 0.04초 (0.03~0.05 범위) |
| 동일 | Boss Damage Scale | 0.5 (근접과 투척 모두 적용) |
| RangedRang / CircusEnemy | Windup Seconds | 0.35초 |
| 동일 | Ranged Lean / Pullback / Recoil / Recovery | 10° / 0.1m / 0.07m / 0.24초 |
| RangProjectile / CircusProjectileVfx | Core Size / Tail Lifetime | 0.42 / 0.18초 |
| DokkaebiBoss / CircusEnemy | Health | 900 (이전 650) |
| DokkaebiBoss / CircusBoss | Slam Damage / Windup / Recovery / Cooldown / Range | 22 / 0.85초 / 1.6초 / 7초 / 4.2m |
| 동일 | Slam Lift / Drop Seconds / Pose Recovery | 0.45m / 0.1초 / 0.3초 |
| 씬 Session / CircusAudio | Effects Enabled | 꺼짐: 이전 효과음 전체 차단 |
| 동일 | Combat Accents Enabled | 켜짐: 승인한 두 소리만 허용 |
| 동일 | Melee Impact Volume / Slam Impact Volume | 0.12 / 0.20 |

플레이어 HP 100, 일반 적 대상 기본 피해 25/감염 피해 40은 유지한다. 보스 대상 피해는 12.5/20이다. 보스의 다른 기존 공격 피해도 조정했다: 기본 근접 16, 연속 근접 14, 돌진 18, 일반/부채꼴/원형 투사체 10, 유도 투사체 8, 장판 12. 패턴 종류와 선택 규칙은 추가하지 않았다. 각 값은 기존 패턴별 Inspector 설정으로 조절할 수 있다.

## 효과음과 아트 리소스

기존 BGM 3개는 유지한다. 사용자 승인에 따라 아래 두 개만 낮게 재생한다.

- `MeleeAccent.wav`: 실제 플레이어 근접 적중 때 한 번. 짧고 낮은 타격음.
- `SlamAccent.wav`: 실제 보스 착지 순간 한 번. 낮은 충격/먼지 느낌.

외부 샘플 없이 `Tools/GenerateCombatAccents.py`로 직접 합성했다. 저역 진동과 저역 필터를 거친 잡음, 부드러운 시작과 감쇠를 사용하며 같은 소리의 재생 간격은 최소 0.12초로 제한한다. 일반 효과음 전체를 다시 켜지 않는다.

새 UI는 제작하지 않았다. 기존 서커스 배경·플레이어/적 OBJ·메뉴/HP 프레임·한글 폰트를 그대로 쓴다. VFX용 `SoftFlame.png`와 `SoftCombatGlow.mat`만 생성했다. 입자용 부드러운 알파 텍스처이며 새 캐릭터 아트가 아니다.

## 변경한 Script

새 런타임:

- `CircusCombatFx.cs`: 공통 작은 입자 Burst, 먼지, 짧은 Slash ribbon과 수명 처리.
- `CircusProjectileVfx.cs`: 원거리 적/기존 보스 공용 투사체의 부드러운 입자 코어와 짧은 잔상.

수정한 기존 런타임:

- `CircusPlayer.cs`: 전체 몸의 준비·휘두르기·복귀, 숨긴 기존 판정, 적중 전용 피드백, 보스 대상 피해 배율.
- `CircusEnemy.cs`: 일반 원거리 적의 준비·반동·복귀, 발사 위치, 피격 Flash 강도.
- `CircusCharge.cs`: 일반 원거리 적에만 부드러운 입자 충전 표시 선택 기능.
- `CircusBoss.cs`: 기존 Slam의 준비/낙하/접촉/복귀 타이밍, 중단 시 위치 복원.
- `CircusAttackArea.cs`: 점진적으로 강조되는 내려찍기 바닥 원 테두리.
- `CircusProjectile.cs`: 기존 투척 충돌 경로에서 보스 대상 피해 배율 적용.
- `CircusImpact.cs`: 기존 기본 Sphere 충돌 이펙트를 작은 입자로 교체.
- `CircusSession.cs`: 짧은 히트 스톱, 일시정지/화면 전환 시 시간 배율 복원.
- `CircusAudio.cs`: 전체 효과음과 별도로 승인한 두 낮은 적중음만 제한적으로 재생.

에디터 도구:

- 새 `CircusPresentationSetup.cs`: 기존 프리팹/씬을 Unity API로 설정하고 리소스 연결.
- 새 `CircusPresentationVerification.cs`: 적중/미스, 히트 스톱/일시정지, 발사/착지 타이밍 검사.
- 새 `CircusPresentationCapture.cs`: 실제 Play Mode 프레임 녹화 및 투사체 전투 시간 측정. 게임 빌드에는 포함되지 않는다.
- 수정 `CircusThreeActVerification.cs`: 노란 범위 제거와 조정한 보스 HP 기대값 반영.

## 검증 및 실제 화면

- Unity 컴파일 오류/경고 0.
- 새 연출 검사 26개 통과. 마지막 충전/발광 조정 후에도 재실행하여 통과했다.
- 기존 보스 패턴/페이즈 검사와 이동·시점·3막/클리어 회귀 검사를 포함해 총 141개 통과.
- 실제 Play Mode의 세 공격을 30fps, 총 156프레임으로 캡처하고 준비·휘두르기/발사·착지 전후를 확인했다.
- 보스 AI와 실제 플레이어 투사체를 사용한 측정: **54.14초**, 투척 **83회**, HP 900→0, 페이즈 전환 2회. 측정 중 플레이어 체력만 회복해 중단을 방지했다. 정지한 채 원거리 공격을 계속한 조건이며 실제 회피·근접 병행 여부에 따라 소요 시간은 달라진다.

로컬 미리보기와 검사 기록은 `CircusGame/Recordings/CombatPresentation/`에 있다. 녹화물 폴더는 기존 Git 제외 규칙을 따른다.

- [플레이어 준비 → 휘두르기 → 적중](CircusGame/Recordings/CombatPresentation/player-melee.gif)
- [일반 원거리 적 준비 → 발사 → 복귀](CircusGame/Recordings/CombatPresentation/enemy-ranged.gif)
- [보스 준비 → 내려찍기 → 회복](CircusGame/Recordings/CombatPresentation/boss-slam.gif)

Windows 빌드도 성공했다: `CircusGame/Builds/CircusPrototype/CircusPrototype.exe`, 오류 0개. 경고 1개는 배포 실행 파일에 개발용 Pipeline 원격 제어 설정이 없어 비활성화된다는 안내다. 게임 코드나 셰이더 컴파일 오류는 없다. 최종 씬/프리팹은 저장했고 Unity는 편집 상태로 남겨 두었다.
