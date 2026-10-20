# 효과음 출처 및 재생성

이 폴더의 19개 WAV는 이 프로젝트를 위해 수학적 음 합성으로 직접 제작했습니다.
외부 음원·녹음·게임 샘플·음원 사이트 파일을 사용하지 않았습니다.
추가 저작권 표기나 유료 음원 라이선스가 필요한 외부 소스가 없습니다.

- 생성 도구: 저장소 `Tools/GenerateCircusSounds.py` (Python 표준 라이브러리만 사용)
- 포맷: 22,050 Hz / 모노 / 16-bit PCM WAV
- 방식: 사인파와 비정수 배음의 종소리, 주파수 스윕, 고정 시드 잡음, 감쇠·페이드 합성
- 정규화: 각 파일 최대 피크 0.62, 시작·끝 페이드 적용
- 재생성: 저장소에서 `python Tools/GenerateCircusSounds.py`

## 종류

| 구분 | 파일 |
|---|---|
| 시스템 | Countdown, Start, ActChange, Clear |
| 플레이어 | PlayerMelee, PlayerShot, PlayerHit, PlayerDeath |
| 일반 적 | EnemyHit, EnemyDeath |
| 보스 | BossArrival, BossMelee, BossShot, BossHit, BossDeath |
| 도깨비불 | FlameLaunch, FlameFlight, FlameImpact |
| 강한 공격 예고 | Warning |

FlameLaunch는 작은 종과 상승하는 마법 소리, FlameFlight는 조용한 영적 불꽃의 이동음,
FlameImpact는 짧은 종·불꽃 타격음입니다. 일반 총성은 사용하지 않았습니다.

`Show Director > Circus Audio`의 Effects Volume, Max Voices, Min Repeat Seconds로 조정합니다.
기존 설정 메뉴의 전체 음량도 모든 효과음에 적용됩니다.
