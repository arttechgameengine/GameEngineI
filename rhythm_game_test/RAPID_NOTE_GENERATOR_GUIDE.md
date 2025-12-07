# 연타 노트 Generator 사용 가이드

## 🎯 개요

**RapidNoteMIDIGenerator**는 **ProgressiveDifficultyMIDIGenerator의 모든 기능**을 포함하면서, **연타 노트 자동 생성 옵션**을 추가한 확장 Generator입니다.

### 주요 기능:
- ✅ Progressive Difficulty (점진적 난이도) 지원
- ✅ Hold Note (롱노트) 지원
- ✅ SPACE Bar 노트 지원
- ✅ 🔥 **Rapid Note (연타 노트) 자동 생성** 🔥

## 🚀 사용 방법

### 1. Generator 열기

Unity Editor → **Tools → Rapid Note MIDI Generator**

### 2. 기본 설정

#### MIDI File
- MIDI 파일 선택 (.mid)

#### Output Path
- JSON 저장 경로 (기본: `Assets/Charts/`)

#### Chart Settings
- **Number of Lanes**: 1~8 (기본: 4)
- **Include Hold Notes**: 홀드 노트 포함 여부
- **Hold Threshold**: 홀드 노트 최소 길이 (초)

### 3. 🔥 연타 노트 설정

#### Include Rapid Notes
✅ 체크하면 연타 노트 자동 생성

#### Placement Mode (배치 모드)

**1. Manual (수동)**
```
Rapid Note Count: 3
→ 곡 전체에 3개의 연타 노트를 균등 배치
```

**2. EveryNBeats (N 박자마다)**
```
Every N Beats: 8
→ 8박자마다 자동으로 연타 노트 배치
예: BPM 120일 때 8박자 = 4초마다
```

**3. ProgressiveSections (점진적)**
```
섹션별로 연타 노트 개수 증가
Section 1: 1개
Section 2: 2개
Section 3: 3개
```

#### Hit Count Range (연타 횟수)
```
Min: 3, Max: 8
→ 각 연타 노트는 3~8회 사이 랜덤
```

#### Duration Range (제한 시간)
```
Min: 0.8초, Max: 1.5초
→ 각 연타 노트의 제한 시간 랜덤
```

#### Auto Scale by Difficulty
✅ 체크하면 곡 진행에 따라 자동 난이도 조절
- 초반: 적은 횟수 + 긴 시간
- 후반: 많은 횟수 + 짧은 시간

## 📋 예시 설정

### 쉬운 난이도
```
Placement Mode: Manual
Rapid Note Count: 2
Hit Count Range: 3-5
Duration Range: 1.2-1.5s
Auto Scale: OFF
```
→ 곡에 2개, 3~5회 연타, 여유로운 시간

### 보통 난이도
```
Placement Mode: EveryNBeats
Every N Beats: 8
Hit Count Range: 4-6
Duration Range: 1.0-1.3s
Auto Scale: ON
```
→ 8박자마다, 점진적 난이도 증가

### 어려운 난이도
```
Placement Mode: EveryNBeats
Every N Beats: 4
Hit Count Range: 6-10
Duration Range: 0.6-1.0s
Auto Scale: ON
```
→ 4박자마다, 많은 횟수, 짧은 시간

## 🎮 생성 결과

### JSON 예시
```json
{
  "songName": "example_song",
  "bpm": 120,
  "notes": [
    {
      "time": 2.0,
      "type": "tap",
      "arrow": "UP"
    },
    {
      "time": 4.0,
      "type": "rapid",
      "arrow": "DOWN",
      "rapidCount": 5,
      "rapidDuration": 1.2
    },
    {
      "time": 8.0,
      "type": "rapid",
      "arrow": "LEFT",
      "rapidCount": 7,
      "rapidDuration": 1.0
    },
    {
      "time": 10.0,
      "type": "tap",
      "arrow": "RIGHT"
    }
  ]
}
```

## 🔧 고급 설정

### Progressive Difficulty 연동

Generator는 기존 Progressive Difficulty 시스템과 통합됩니다:

```
Section 1 (0-33%): QuarterBeat
Section 2 (33-66%): EighthBeat
Section 3 (66-100%): SixteenthBeat

ProgressiveSections 모드 사용 시:
→ 각 섹션마다 연타 노트 개수 증가
```

### 연타 노트 배치 로직

```csharp
// Manual 모드
곡을 N등분 → 각 구간에 1개씩 배치

// EveryNBeats 모드
interval = beatDuration * N
→ interval 간격마다 배치

// ProgressiveSections 모드
각 섹션의 길이에 비례하여 배치
```

## 📊 충돌 방지

Generator가 생성한 JSON은 **DifficultyFilter**를 거치면서 자동으로 충돌 제거:

```
연타 노트 (10.0, UP, duration=1.0)
→ 보호 구간: 10.0~11.0

tap 노트 (10.5, UP)
→ 자동 제거됨 (충돌!)

tap 노트 (10.5, DOWN)
→ 유지됨 (다른 키)
```

## 🎨 요리 테마 예시

### 야채 썰기 구간
```
Placement Mode: Manual
Count: 3
Hit Count: 3-5
Duration: 1.2-1.5s
```

### 요리 중간 보스
```
Placement Mode: EveryNBeats
Every N Beats: 16 (4소절마다)
Hit Count: 6-8
Duration: 0.8-1.2s
```

### 최종 요리 완성
```
Placement Mode: Manual
Count: 1 (마지막 1개)
Hit Count: 10
Duration: 1.5s
```

## ⚙️ 생성 프로세스

```
1. MIDI 로드
   ↓
2. 기본 노트 추출 (tap, hold)
   ↓
3. 🔥 연타 노트 삽입 (선택한 모드)
   ↓
4. 시간순 정렬
   ↓
5. 방향키 할당
   ↓
6. JSON 저장
```

## 🐛 문제 해결

### 연타 노트가 생성 안 됨
- "Include Rapid Notes" 체크 확인
- Rapid Note Count > 0 확인

### 연타 노트가 너무 많음
- Placement Mode 확인
- EveryNBeats 값 증가 (8 → 16)

### 연타 노트 난이도가 안 맞음
- Hit Count Range 조정
- Duration Range 조정
- Auto Scale 활용

### 다른 노트와 겹침
- DifficultyFilter가 자동 처리
- 수동 조정 필요시 JSON 직접 편집

## 📝 팁

### 1. 테스트 빌드
```
Manual 모드로 Count=2 설정
→ 빠르게 테스트
→ 만족하면 다른 모드 시도
```

### 2. 난이도 조절
```
쉬움 → Manual (2개)
보통 → EveryNBeats (8박자)
어려움 → EveryNBeats (4박자)
```

### 3. 요리 테마 매핑
```
야채 썰기: 3-5회, 1.2초
달걀 휘젓기: 5-7회, 1.0초
고기 뒤집기: 8-10회, 0.8초
```

## 🔗 관련 문서

- [RAPID_NOTE_GUIDE.md](RAPID_NOTE_GUIDE.md) - 연타 노트 상세 가이드
- [DIFFICULTY_FILTER_README.md](DIFFICULTY_FILTER_README.md) - 난이도 필터 시스템

## 라이선스

프로젝트 라이선스를 따릅니다.
