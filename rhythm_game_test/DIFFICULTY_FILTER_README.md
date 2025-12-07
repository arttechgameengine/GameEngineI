# 난이도 필터 시스템 가이드

## 개요

하나의 JSON 패턴 파일로 Easy/Normal/Hard 등 여러 난이도를 구현하는 시스템입니다.

### 핵심 개념

- **REAL SPEED**: 음악 타이밍을 맞추는 절대 속도 (절대 변하면 안 됨)
- **VISUAL SPEED**: 화면에서 보이는 속도 (noteSpeed로 조절 가능)
- **노트 밀도**: 시간당 노트 개수 (필터링으로 조절)

## 주요 파일

### 1. DifficultyFilter.cs
노트를 논리적으로 필터링하는 핵심 시스템

**필터링 방식:**
- 타입 필터: 홀드/SPACE 노트 제거
- 밀도 필터: 너무 가까운 노트 제거
- 연타 필터: 연속된 빠른 노트 제거
- Lane 필터: 특정 lane만 사용
- N번째마다 유지: 전체 노트 수 조절

### 2. PatternLoader.cs (수정됨)
난이도 파라미터를 받아 자동으로 필터링 적용

```csharp
// 기본 사용법
PatternData pattern = PatternLoader.LoadPattern("Charts/song.json", DifficultyFilter.Difficulty.Easy);
```

### 3. RoundDifficultyManager.cs
라운드별 난이도 관리 (실전 예제)

## 사용 방법

### 기본 사용법

```csharp
// 1. 난이도를 지정하여 패턴 로드
PatternData easyPattern = PatternLoader.LoadPattern(
    "Charts/my_song_chart.json",
    DifficultyFilter.Difficulty.Easy
);

// 2. NoteSpawner에 로드
noteSpawner.LoadPattern(easyPattern);

// 3. 음악 시작
noteSpawner.StartSong(bgmSource);
```

### 라운드별 난이도 설정

```csharp
public class MyGameManager : MonoBehaviour
{
    void StartRound1()
    {
        // Easy: 느린 속도 + 적은 노트
        var pattern = PatternLoader.LoadPattern("Charts/song.json", DifficultyFilter.Difficulty.Easy);
        pattern.noteSpeed = 300f;
        noteSpawner.LoadPattern(pattern);
    }

    void StartRound2()
    {
        // Normal: 보통 속도 + 중간 노트
        var pattern = PatternLoader.LoadPattern("Charts/song.json", DifficultyFilter.Difficulty.Normal);
        pattern.noteSpeed = 500f;
        noteSpawner.LoadPattern(pattern);
    }

    void StartRound3()
    {
        // Hard: 빠른 속도 + 모든 노트
        var pattern = PatternLoader.LoadPattern("Charts/song.json", DifficultyFilter.Difficulty.Hard);
        pattern.noteSpeed = 700f;
        noteSpawner.LoadPattern(pattern);
    }
}
```

## 난이도별 특징

### Easy (쉬움)
- **노트 개수**: 원본의 40-50%
- **필터링**:
  - 연타의 80% 제거
  - 밀도 높은 구간 60% 제거
  - 홀드 노트 제거
  - SPACE 노트 제거
  - 2번째 노트마다만 유지
- **권장 설정**:
  - noteSpeed: 300-400
  - spawnDistance: 1200-1500
- **체감**: 느리고 여유로움

### Normal (보통)
- **노트 개수**: 원본의 70-80%
- **필터링**:
  - 연타의 40% 제거
  - 밀도 높은 구간 30% 제거
  - 홀드/SPACE 유지
- **권장 설정**:
  - noteSpeed: 500-600
  - spawnDistance: 1000
- **체감**: 적당한 난이도

### Hard (어려움)
- **노트 개수**: 원본 그대로 (100%)
- **필터링**: 거의 없음 (최소 필터링만)
- **권장 설정**:
  - noteSpeed: 700-900
  - spawnDistance: 800
- **체감**: 빠르고 어려움

## 속도와 간격 관계

### 타이밍 보장 공식

```
spawnLeadTime = spawnDistance / noteSpeed
도착 시간 = 스폰 시간 + spawnLeadTime = note.time (항상!)
```

### 속도별 권장 설정

| 난이도 | noteSpeed | spawnDistance | spawnLeadTime | 체감 |
|--------|-----------|---------------|---------------|------|
| Easy   | 300       | 1200          | 4.0초         | 느림, 여유로움 |
| Normal | 500       | 1000          | 2.0초         | 보통 |
| Hard   | 700       | 800           | 1.14초        | 빠름, 긴장감 |

### 간격 유지 원리

- **noteSpeed ↑ (빠름)** → **spawnDistance ↓ (가까이)**
  - 이유: 빠르게 이동하므로 가까워도 시간 충분

- **noteSpeed ↓ (느림)** → **spawnDistance ↑ (멀리)**
  - 이유: 느리게 이동하므로 멀어야 간격 유지

## 고급 사용법

### 커스텀 필터 설정

```csharp
// 커스텀 필터 설정 생성
DifficultyFilter.FilterSettings custom = new DifficultyFilter.FilterSettings
{
    minNoteInterval = 0.15f,              // 150ms보다 짧은 간격 필터링
    rapidFireReductionRatio = 0.5f,       // 연타의 50% 제거
    densityReductionRatio = 0.3f,         // 밀도 높은 구간 30% 제거
    removeHoldNotes = false,              // 홀드 유지
    removeSpaceNotes = true,              // SPACE 제거
    allowedLanes = new List<int> { 0, 1 }, // Lane 0, 1만 사용
    keepEveryNthNote = 1                  // 모든 노트 유지
};

// 패턴 로드
string jsonText = File.ReadAllText("path/to/pattern.json");
PatternData pattern = JsonUtility.FromJson<PatternData>(jsonText);

// 커스텀 필터 적용
pattern.notes = DifficultyFilter.ApplyCustomFilter(pattern.notes, custom);
```

### spawnPoint 동적 조정

```csharp
void AdjustSpawnDistance(float distance)
{
    // hitLine 위치는 고정
    float hitLineX = noteSpawner.hitLine.localPosition.x;

    // spawnPoint만 이동
    float newSpawnX = hitLineX + distance;
    noteSpawner.spawnPoint.localPosition = new Vector3(newSpawnX, 0, 0);

    // NoteSpawner가 자동으로 spawnLeadTime 재계산함
}
```

## 중요 사항

### ✅ 보장되는 것

1. **음악 타이밍 100% 유지**
   - note.time은 절대 변하지 않음
   - 모든 난이도에서 음악과 정확히 싱크

2. **자동 타이밍 보정**
   - spawnLeadTime 자동 재계산
   - noteSpeed나 spawnDistance 바꿔도 타이밍 유지

3. **Scene 분리 불필요**
   - 한 Scene에서 모든 난이도 구현 가능
   - 런타임에서 난이도 전환 가능

### ⚠️ 주의사항

1. **필터링 순서**
   - Hold 노트 변환 **전**에 필터링 적용
   - PatternLoader.cs에서 자동 처리됨

2. **원본 JSON은 완전한 패턴이어야 함**
   - 가장 어려운 난이도(Hard)의 모든 노트 포함
   - 필터링으로 노트를 줄이는 방식

3. **REAL SPEED는 변하지 않음**
   - 음악 타이밍 기준 속도는 고정
   - noteSpeed는 시각적 속도만 변경

## 설정 커스터마이징

### 필터 설정 변경

[DifficultyFilter.cs](Assets/Scripts/DifficultyFilter.cs)의 `GetFilterSettings()` 함수에서 난이도별 설정 조정:

```csharp
case Difficulty.Easy:
    settings.minNoteInterval = 0.2f;        // 이 값 조정
    settings.rapidFireReductionRatio = 0.8f; // 이 값 조정
    settings.densityReductionRatio = 0.6f;   // 이 값 조정
    settings.removeHoldNotes = true;         // 홀드 제거 여부
    settings.removeSpaceNotes = true;        // SPACE 제거 여부
    settings.keepEveryNthNote = 2;           // 간격 조절
    break;
```

### 필터 파라미터 설명

- **minNoteInterval**: 노트 간 최소 시간 간격 (초)
  - 높을수록 밀도 낮아짐
  - 예: 0.2 = 200ms보다 가까운 노트는 "밀도 높음"으로 간주

- **rapidFireReductionRatio**: 연타 제거 비율 (0.0 ~ 1.0)
  - 0.0 = 제거 안 함
  - 0.5 = 연타의 50% 제거
  - 1.0 = 모든 연타 제거

- **densityReductionRatio**: 밀도 높은 구간 제거 비율 (0.0 ~ 1.0)
  - 0.0 = 제거 안 함
  - 0.6 = 밀도 높은 노트 중 60% 제거

- **keepEveryNthNote**: N번째 노트마다 유지
  - 1 = 모든 노트 유지
  - 2 = 짝수번째만 유지 (50%)
  - 3 = 3번째마다 유지 (33%)

## 테스트 방법

### 런타임 테스트

1. Scene에 `RoundDifficultyManager` 추가
2. Inspector에서 rounds 배열 설정
3. Play 모드에서 키보드 입력으로 테스트:
   - `1`: Easy 모드
   - `2`: Normal 모드
   - `3`: Hard 모드
   - `N`: 다음 라운드

### 디버그 로그 확인

```
[DifficultyFilter] Easy - Original: 1000, Filtered: 450
[PatternLoader] Applied Easy filter - 450 notes remaining
[NoteSpawner] Loaded noteSpeed from JSON: 300
[NoteSpawner] spawnLocalX: 1200, hitLineLocalX: 0, distance: 1200, spawnLeadTime: 4.0
```

## 문제 해결

### Q: 필터링 후 노트가 너무 적어요
A: `DifficultyFilter.cs`의 설정값 조정:
- `densityReductionRatio` 낮추기 (0.6 → 0.3)
- `keepEveryNthNote` 낮추기 (2 → 1)

### Q: 타이밍이 안 맞아요
A: 타이밍은 자동 보장됩니다. 확인 사항:
- JSON의 `offset` 값 확인
- `note.time` 값 확인
- AudioSource의 dspTime 사용 확인

### Q: 속도를 바꿨는데 체감이 안 바뀌어요
A: `spawnDistance`도 같이 조정하세요:
- noteSpeed ↑ → spawnDistance ↓
- noteSpeed ↓ → spawnDistance ↑

### Q: 한 Scene에서 여러 난이도를 사용하고 싶어요
A: `RoundDifficultyManager.cs` 사용하세요. 자동으로 처리됩니다.

## 예제 코드

전체 예제는 다음 파일 참고:
- [DifficultyFilterUsageExample.cs](Assets/Scripts/DifficultyFilterUsageExample.cs)
- [RoundDifficultyManager.cs](Assets/Scripts/RoundDifficultyManager.cs)

## 라이선스

프로젝트 라이선스를 따릅니다.
