---
name: synth
description: sin/cos 파형을 합성해 NES·패미콤 느낌의 chiptune WAV 를 생성하는 신디사이저. 피아노·패드·리드·베이스·앰비언트 5 프리셋을 ADSR·비브라토·디튠·하모닉 파라미터로 다듬고, 선택적으로 bitcrush/다운샘플해 8비트 콘솔 질감으로 만든다. 외부 의존성 없음 (Python 3 stdlib 만). 효과음·점프/피격/메뉴 사운드·타이틀 BGM·간단한 멜로디·루프 필요할 때 에이전트가 선제적으로 호출해도 된다. "삐삐 소리", "retro sfx", "chiptune 음악", "점프 SE", "메뉴 선택음", "8비트 느낌", "따뜻한 앰비언트 패드" 같은 요청이면 이 스킬.
---

# Skill: /synth

sin/cos 오실레이터로 가산 합성을 돌려 **피아노 · 패드 · 리드 · 베이스** 4 프리셋 중 하나로 WAV를 뽑는다. 엔진은 [`scripts/chiptune-synth/synth.py`](../../scripts/chiptune-synth/synth.py) 한 파일, Python 3 stdlib 만 사용 (`math`, `struct`, `wave`, `argparse`). pip 설치 불필요.

이 스킬은 **에이전트 자율 호출 가능**. 게임 로직 만들던 중 "점프할 때 소리 필요", "메뉴 커서 이동 SE 필요" 같은 판단이 서면 바로 호출해 임시 사운드를 만든다.

---

## 1. 프리셋

| 프리셋 | 성격 | 하모닉 구성 | 엔벨롭 | 특수 |
|---|---|---|---|---|
| `piano` | 밝은 플럭 | 1·2·3·4·5·6·7차 (1/n^1.5 감쇠) | A 5ms / D 350ms / S 0.15 / R 300ms | 어택 피치 +18¢ 20ms |
| `pad` | 부드러운 패드 (살짝 코러스 느낌) | 1·2·3차 | A 350ms / D 250ms / S 0.75 / R 1.2s | 디튠 ±7¢ 3레이어, 비브라토 5Hz·8¢ |
| `lead` | 각지고 날카로움 (NES 펄스 근사) | 홀수차 1·3·5·7·9·11 (1/n) | A 10ms / D 80ms / S 0.65 / R 180ms | 비브라토 6.5Hz·12¢ (150ms 후) |
| `bass` | 두꺼운 저역 | 1·2·3·4차 | A 5ms / D 120ms / S 0.5 / R 120ms | 옥타브 강화 |
| `ambient` | 따뜻하게 퍼지는 앰비언트 (밝은 에어·넓은 드리프트) | 1·2·3·4차 (4차가 에어감) | A 1.5s / D 600ms / S 0.85 / R 3.0s | 디튠 ±20¢ 5레이어, 비브라토 0.25Hz·7¢ (1s 후) |

펄스파·톱니파는 푸리에 급수로 sin 합성 가능하기 때문에 sin 기반으로도 펄스웨이브 특유의 8비트 리드 톤이 나온다 (`lead` 프리셋).

---

## 2. CLI

```bash
# 단음
python3 scripts/chiptune-synth/synth.py <preset> <note> --duration <s> --out <wav>

# 시퀀스 (짧은 멜로디·아르페지오)
python3 scripts/chiptune-synth/synth.py <preset> --sequence "C4 E4 G4 C5" --tempo <bpm> --out <wav>
```

`<preset>`: `piano` | `pad` | `lead` | `bass`
`<note>`: 과학적 음명 표기 (`C4`, `A#3`, `Bb5`). A4 = 440Hz.

### 파라미터 오버라이드

| 플래그 | 용도 |
|---|---|
| `--attack` `--decay` `--sustain` `--release` | ADSR 개별 덮어쓰기 |
| `--gain` | 마스터 볼륨 (0..1) |
| `--vibrato-rate` / `--vibrato-depth` | 비브라토 Hz / cents |
| `--detune "-10,0,10"` | 디튠 레이어 (cents, 콤마 구분) |
| `--bitcrush 4` 또는 `8` | 비트 뎁스 축소 (NES=4~5bit, Game Boy=4bit) |
| `--lofi-sr 11025` | DAC 다운샘플 느낌 (sample-and-hold) |
| `--sr 22050` | 출력 샘플레이트 (기본 44100) |
| `--layer "preset:notes:tempo:gain"` | 다른 프리셋을 위에 겹침. 반복 지정 가능. 예: `--layer "lead:C5 E5 G5:160:0.3"` → 리드 아르페지오를 160bpm·gain 0.3 으로 믹스. 여러 번 지정해서 3~4 파트 합주 가능 |

### 레시피

```bash
# 메뉴 선택음 (짧은 리드 삐)
python3 scripts/chiptune-synth/synth.py lead G5 --duration 0.12 --release 0.05 --out menu_blip.wav

# 점프 SE (짧게 올라가는 피치는 sequence로 근사)
python3 scripts/chiptune-synth/synth.py lead --sequence "C5 E5 G5" --tempo 280 --release 0.05 --out jump.wav

# 8비트 보스 리드
python3 scripts/chiptune-synth/synth.py lead --sequence "E4 G4 A4 C5 A4 G4" --tempo 180 --bitcrush 5 --lofi-sr 11025 --out boss_lead.wav

# 타이틀 화면용 긴 패드
python3 scripts/chiptune-synth/synth.py pad C3 --duration 4.0 --release 2.0 --out title_pad.wav

# 따뜻하게 퍼지는 앰비언트 드론 (맵 화면·명상 씬·엔딩 암전 BGM)
python3 scripts/chiptune-synth/synth.py ambient C3 --duration 8.0 --out ambient_drone.wav

# 아주 느린 코드 진행 (60s 이상 루프 가능한 배경음)
python3 scripts/chiptune-synth/synth.py ambient --sequence "C3 G3 E3 A3" --tempo 30 --out ambient_bed.wav

# 베이스 라인 + 리드 멜로디 레이어 (초보적 2-파트 합주)
python3 scripts/chiptune-synth/synth.py bass --sequence "C2 C2 G1 C2" --tempo 120 \
    --layer "lead:E4 G4 C5 G4:120:0.35" \
    --out duet.wav

# 베이스 라인
python3 scripts/chiptune-synth/synth.py bass --sequence "C2 C2 G1 C2" --tempo 120 --out bassline.wav
```

---

## 3. Unity 프로젝트에 저장하기

### 저장 경로 규칙 (중요 — RULE-02 및 parallel-work.md)

- **메인 워크트리에서 작업 중이면**: `Assets/Audio/` 에 직접 저장해도 된다.
- **워크트리 (`agent-0`, `agent-1` 등)에서 작업 중이면**: `Assets/Audio/` 는 심링크라 거기에 쓴 파일은 **워크트리 git 이 안 본다** (메인으로 새어나감). 기능 폴더에 같이 둔다:
  - ✅ `Assets/Scripts/_UI/Menu/SE/blip.wav`
  - ✅ `Assets/Scripts/_Core/Game2048/Audio/move.wav`
  - ❌ `Assets/Audio/blip.wav` (워크트리에서 금지)

판별:
```bash
# 현재 위치가 primary worktree 인지
git rev-parse --show-toplevel
# 반환 경로가 메인 레포면 Assets/Audio/ OK, 다른 경로(워크트리)면 기능 폴더로
```

### 생성 후 Unity 임포트

WAV 를 프로젝트 경로에 쓰면 Unity 가 다음 포커스 시 자동 임포트한다. 에디터가 열려 있으면 더 확실히 하려면 ClaudeBridge 로:

```python
unity_call("Asset.Refresh", {})
```

AudioClip import 설정(Load Type, Compression)이 필요하면 `make-asset` 스킬의 §4-4-A 텍스처 임포터 예시와 같은 `Reflection.Invoke` 패턴으로 `UnityEditor.AudioImporter` 를 조작한다. 대부분의 짧은 SE 는 기본값으로 충분.

---

## 4. 사운드 튜닝 가이드

청감이 원하는 것과 다르면 이 순서로 시도:

1. **너무 부드럽다 / 심심하다** → `--bitcrush 5` 또는 `--bitcrush 4` (8비트 느낌 강해짐)
2. **너무 날카롭다** → 리드라면 `pad` 프리셋으로 바꾸거나, 리드 유지한 채 `--release 0.4` 로 꼬리 길게
3. **두께 부족** → `--detune "-10,0,10"` 으로 3레이어 디튠 추가 (코러스 효과)
4. **공격감 부족** → `--attack 0.001 --decay 0.05` 로 엔벨롭 날카롭게
5. **멜로디가 뻣뻣** → `--vibrato-rate 7 --vibrato-depth 15` 로 비브라토 추가

프리셋 자체를 바꾸고 싶으면 [`scripts/chiptune-synth/synth.py`](../../scripts/chiptune-synth/synth.py) 의 `PRESETS` 딕셔너리를 편집. 하모닉 리스트 `[(n, amp), ...]` 의 `amp` 가 해당 차수의 상대적 크기. 1차(fundamental) 기준 0.3~1.0 안에서 조정하면 감이 온다.

---

## 5. 에이전트 자율 호출 지침

사용자가 `/synth` 를 명시적으로 말하지 않아도 다음 상황에서 선제적으로 호출한다:

- UI 메뉴·버튼·커서 이동 로직을 짜는데 `[SerializeField] AudioClip selectSfx` 가 비어 있을 때 → 짧은 `lead` blip 생성
- 게임 로직에서 "점프", "피격", "득점" 같은 피드백 이벤트가 등장하는데 AudioClip 이 없을 때 → 프리셋+짧은 시퀀스로 임시 SE 생성
- 타이틀 / 메뉴 배경음이 필요해 보이는데 BGM 파일이 없을 때 → `pad` 긴 톤 또는 `lead` 짧은 루프로 placeholder

호출 전 한 줄 안내 — "메뉴 SE 가 없네요. 리드 프리셋으로 짧은 blip 만들어 둘까요 (나중에 교체 가능)?" — 무응답이면 기본값으로 진행 후 경로를 기록에 남겨 사용자가 나중에 바꿀 수 있게 한다.

---

## 6. 주의 사항

- **스테레오·리버브·필터 없음**. 모노 16비트 PCM 만. 공간감이 필요하면 Unity `AudioSource` 의 3D 설정 / AudioMixer 의 Reverb 로 해결한다. 이 스크립트의 역할은 "원본 음색 생성"까지.
- **긴 시퀀스는 느리다**. 순수 Python 으로 44.1kHz × N 샘플 × 하모닉 × 디튠 레이어를 돌리므로, 10초 이상 `pad` 는 수 초 걸린다. 그 이상 길면 짧게 생성 후 Unity 쪽 `AudioSource.loop` 로 루프시킨다.
- **피아노 프리셋은 실제 피아노가 아니다**. 가산 합성으로 어택 피치 딥과 하모닉 감쇠를 흉내낸 것이라 NES 사운드 칩의 "피아노 같은" 음색에 가깝다. 진짜 피아노가 필요하면 샘플 음원을 임포트한다.
- **`.meta` 편집 금지** (RULE-03) — WAV 를 쓰면 Unity 가 자동으로 `.meta` 를 만든다. 우리는 건드리지 않는다.
- **심링크 폴더 규칙** (RULE-02 및 `.claude/rules/parallel-work.md` §1·§2) — 워크트리에서 `Assets/Audio/` 에 쓰면 git 이 놓친다. 기능 폴더에 둔다.
