> 현재 사운드는 이전 합성음 19종으로 되돌렸으며 보스 장판 패턴을 추가했습니다. [GROUND_PATTERN_UPDATE.md](GROUND_PATTERN_UPDATE.md)를 확인하세요.

# UI 및 무료 효과음 개선

이전 3막 구현은 `3dff5aa`에 먼저 커밋했습니다. 이번 변경은 해당 커밋 위의 별도 수정입니다.

## 캡처 수정

- 설정: 음량 라벨과 슬라이더를 분리하고 프레임 안쪽에 Track/Fill Area/Handle Area를 배치했습니다.
  Fill은 Slider 값으로만 폭이 정해지도록 sizeDelta를 0으로 설정했습니다.
- 검은 그림을 사용하던 손잡이를 밝은 단순 손잡이로 교체했습니다. 음량 퍼센트를 표시합니다.
- 설정 화면의 전투 설명을 실제 설정 도움말로 바꿨습니다.
- 보스: 프레임 높이를 110으로 늘리고 체력 채움을 폭 696으로 줄였습니다.
  체력 이름·수치는 안쪽 여백을 확보하고, 기존 프레임과 차분한 청록색을 유지했습니다.

## 효과음

19종 단순 합성음을 기본으로 사용하던 구성을 외부 무료 CC0 효과음 기반으로 개선했습니다.
현재 20개 큐이며 UI 버튼 클릭음이 추가됐습니다.

- Kenney RPG Audio: 베기, 타격, 가죽, 금속 등 물리적 효과음.
  https://kenney.nl/assets/rpg-audio
- Kenney Interface Sounds: 카운트다운, 시작, 경고, 클릭, 충돌.
  https://kenney.nl/assets/interface-sounds
- JaggedStone Magic Spell SFX: 플레이어/보스 발사, 막 변경, 등장·사망·클리어 마법음.
  https://opengameart.org/content/magic-spell-sfx

사용 음원 23개를 Free에 보관하고, 원본 라이선스와 출처를 함께 기록했습니다.
실제 재생용은 Polished의 WAV 20개입니다. 전투 조작에 맞춰 0.02~1.8초로 편집하고 페이드·피크 정규화,
효과음별 음량과 공격·피격의 작은 피치 변주를 적용했습니다. 비행 루프와 일부 보조 레이어에는 기존 합성음을 유지했습니다.
모든 큐의 소스 대응표는 `CircusGame/Assets/Prototype/Audio/SOURCES.md`에 있습니다.

## 수정 파일

- CircusHud: 음량 퍼센트, 공통 버튼 클릭음.
- CircusAudio: UIClick 큐, 큐별 음량, 전투음 변주.
- CircusThreeActSetup: 사운드 재연결 시 Polished 우선 사용.
- 새 CircusPolishSetup: UI 수정과 음원 가공·연결을 재현하는 편집기 도구.
- CircusPrototype.unity: 수정 UI와 20종 사운드 뱅크 참조.

필수 Inspector 수동 연결은 없습니다. `Show Director > Circus Audio > Cue Volumes`로 각 효과음의 크기를 조정할 수 있습니다.
기존 전체 음량, 동시 재생 한도, 반복 제한도 유지됩니다.

## 검증

설정·음원 검증 13개(0/35/100% 음량, 실제 채움 길이, 퍼센트, 보스 여백, 20개 음원 연결),
전체 3막 전투 회귀 검증 51개가 통과했습니다. Unity 콘솔 오류·경고 0개이며,
실제 설정 화면과 체력 350/650 보스 화면을 캡처해 수정 결과를 확인했습니다.

Windows 빌드를 갱신했으며 오류 없이 성공했습니다. 편집 도구 Pipeline의 Player 비활성화 안내 1건은 게임 오류가 아닙니다.
