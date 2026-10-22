> 최신 상태: BGM 3개와 낮은 근접 적중음(MeleeAccent), 내려찍기 충격음(SlamAccent)을 유지합니다. 보스 등장 요청에 따라 등장 전용 BossLandingAccent(0.24), BossRoarAccent(0.15)만 추가했습니다. 새 두 소리는 외부 샘플 없이 Tools/GenerateBossEntranceAudio.py로 저역 진동·부드러운 잡음을 직접 합성했습니다. 나머지 일반 효과음은 비활성 상태입니다. 최신 연결과 볼륨은 BOSS_ENTRANCE_UPDATE.md를 확인하세요. 아래는 이전 작업의 출처 기록입니다.

# 무료 효과음 출처

## 이전 사용 기록 (현재 비활성)

현재 씬은 `Polished/`의 20개 큐를 사용합니다. 기존 `Generated/`의 19개 합성음은 원본·보조 레이어로 보관했습니다.
외부 CC0 음원을 기본으로 사용하며, 비행 루프와 근접·충돌의 작은 보조 레이어만 기존 합성을 사용합니다.

- Kenney — RPG Audio: https://kenney.nl/assets/rpg-audio
- Kenney — Interface Sounds: https://kenney.nl/assets/interface-sounds
- JaggedStone — Magic Spell SFX: https://opengameart.org/content/magic-spell-sfx
- 세 배포 페이지 모두 CC0로 표시됩니다. Kenney 원본 License.txt는 Free 하위 팩 폴더에 보관했습니다.
- CC0 안내: https://creativecommons.org/publicdomain/zero/1.0/
- 다운로드 날짜: 2026-10-20. 로그인·유료 구독이 필요한 소스나 상용 게임 음원은 사용하지 않았습니다.

원본 OGG는 `Free/`, 편집한 모노 PCM WAV는 `Polished/`입니다.
`CircusPolishSetup.PrepareSounds()`가 원본의 앞부분을 상황별 길이로 자르고 최대 피크 0.65로 정규화하며,
시작 3ms·끝 35ms 페이드를 적용합니다. PlayerMelee, BossMelee, FlameImpact에는 합성 레이어를 20% 혼합합니다.
출력은 44,100Hz 모노 16-bit PCM이며, 별도 cueVolumes와 반복 제한으로 게임 내 음량을 관리합니다.

| 큐 | 주 소스 |
|---|---|
| Countdown | Kenney Interface glass_002 |
| Start | Kenney Interface confirmation_001 |
| ActChange | JaggedStone magical_2 |
| Clear | JaggedStone magical_7 |
| PlayerMelee | Kenney RPG knifeSlice + 합성 방울 |
| PlayerShot | JaggedStone magical_1 |
| PlayerHit | Kenney RPG chop |
| PlayerDeath | Kenney RPG creak1 |
| EnemyHit | Kenney RPG knifeSlice2 |
| EnemyDeath | Kenney RPG dropLeather |
| BossArrival | JaggedStone magical_4 |
| BossMelee | Kenney RPG chop + 저음 합성 |
| BossShot | JaggedStone magical_5 |
| BossHit | Kenney RPG metalPot1 |
| BossDeath | JaggedStone magical_6 |
| FlameFlight | 기존 직접 합성한 조용한 루프 |
| FlameImpact | Kenney Interface glass_001 + 합성 충돌음 |
| Warning | Kenney Interface bong_001 |
| FlameLaunch | JaggedStone magical_3 |
| UIClick | Kenney Interface click_001 |

## 2026-10-22 경기장 패턴 전용 음원

`Arena/*.wav` 12개는 `Tools/GenerateArenaAudio.py`에서 사인파, 스윕, 고정 시드 잡음과 각기 다른 엔벌로프로 직접 합성한 44,100Hz 모노 PCM WAV입니다. 외부 샘플이나 상용 작품 음원을 사용하지 않았습니다.

SequencePrepare/Release, OrbitPrepare/Release, GroundMark/Impact, DashPrepare/Release, PhaseShift, UltimateCharge/Burst/Impact는 각각 해당 보스 행동에만 연결됩니다. 전용 마스터 0.65, 개별 볼륨 0.13(필살기 충전 0.18, 필살기 충격 0.24), 4채널 및 0.16초 반복 제한을 적용했습니다. 과거 일반 효과음 설정은 꺼진 상태를 유지합니다. 취향에 맞지 않으면 `CircusAudio.Boss Pattern Sounds Enabled`만 꺼도 기존 BGM/등장/조용한 적중음은 유지됩니다.

## 이전 합성 원본

Generated는 Python 표준 라이브러리만 사용하는 `Tools/GenerateCircusSounds.py`로 만들었습니다.
22,050Hz 모노 16-bit PCM WAV이며 사인파·비정수 배음·스윕·고정 시드 잡음을 합성했습니다.
이 도구를 다시 실행해도 현재 연결된 Polished 음원은 덮어쓰지 않습니다.

