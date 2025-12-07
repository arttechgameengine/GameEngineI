# 연타 노트 (Rapid Note) 시스템 가이드

## 개요

태고의 달인 스타일의 **Count Note (목표 횟수형)** 연타 노트 시스템입니다.
정해진 시간 안에 N회 연타하여 성공/실패가 명확한 특수 노트입니다.

## 특징

- ✅ **명확한 성공/실패**: 목표 횟수 달성 여부로 판정
- ✅ **난이도 조절 용이**: 횟수 + 시간 두 가지 변수로 조절
- ✅ **긴장감**: 타이머 + 카운터로 압박감 생성
- ✅ **DifficultyFilter 완벽 호환**: Easy에서 자동 제거 가능
- ✅ **요리 테마 적합**: "야채 5번 썰기!", "달걀 10번 휘젓기!" 등

## JSON 설정

### 기본 구조

```json
{
  "time": 10.0,
  "lane": 0,
  "type": "rapid",
  "arrow": "UP",
  "rapidCount": 5,
  "rapidDuration": 1.0
}
```

### 필드 설명

- **type**: `"rapid"` (필수)
- **arrow**: 입력 키 (`"UP"`, `"DOWN"`, `"LEFT"`, `"RIGHT"`, `"SPACE"`)
- **rapidCount**: 필요한 연타 횟수 (예: 5회)
- **rapidDuration**: 제한 시간 (초) (예: 1.0초)

## 난이도별 권장 설정

### Easy (제거됨)
```
DifficultyFilter가 자동으로 제거
→ 연타 노트 없음
```

### Normal (쉬움)
```json
{
  "type": "rapid",
  "rapidCount": 3,
  "rapidDuration": 1.5
}
```
- 3회 연타 / 1.5초
- 초당 2회 (여유로움)

### Hard (보통)
```json
{
  "type": "rapid",
  "rapidCount": 5,
  "rapidDuration": 1.0
}
```
- 5회 연타 / 1.0초
- 초당 5회 (적당함)

### Expert (어려움)
```json
{
  "type": "rapid",
  "rapidCount": 10,
  "rapidDuration": 0.8
}
```
- 10회 연타 / 0.8초
- 초당 12.5회 (매우 어려움)

## 게임플레이 흐름

### 1. 노트 스폰
```
[연타 노트] → 화면 왼쪽에서 등장
"0/5" 카운터 표시
```

### 2. HitLine 도달
```
플레이어가 첫 입력
→ 타이머 시작! (1.0초)
→ "1/5" 카운터 업데이트
```

### 3. 연타 진행
```
플레이어: ↑↑↑↑↑
카운터: 2/5 → 3/5 → 4/5 → 5/5
타이머 게이지: ████░ → ███░░ → ██░░░
```

### 4. 성공
```
5/5 달성!
→ 판정 계산 (빠를수록 Perfect)
→ "PERFECT" 표시
→ 점수 추가 + 요리 애니메이션
```

### 5. 실패 (시간 초과)
```
1.0초 경과, 4/5만 달성
→ "MISS"
→ 콤보 끊김
```

## 판정 시스템

### 성공 시 판정 (완료 속도 기반)

```csharp
시간 비율 = 경과 시간 / 제한 시간

50% 이내:  Perfect
75% 이내:  Good
나머지:     Bad
```

예시 (rapidDuration = 1.0초):
- 0.5초 이내 완료: **Perfect**
- 0.75초 이내 완료: **Good**
- 1.0초 이내 완료: **Bad**
- 1.0초 초과: **Miss**

## DifficultyFilter 설정

### Easy 모드 (연타 노트 제거)

```csharp
settings.removeRapidNotes = true;
```

### Normal/Hard 모드 (연타 노트 유지)

```csharp
settings.removeRapidNotes = false;
```

### 커스텀 설정

```csharp
DifficultyFilter.FilterSettings custom = new DifficultyFilter.FilterSettings
{
    removeRapidNotes = true,  // 연타 노트 제거 여부
    // ... 다른 설정
};
```

## 비주얼 커스터마이징

### RapidNoteVisual 컴포넌트

연타 노트에 자동으로 추가되는 비주얼 컴포넌트입니다.

```csharp
public Text counterText;           // "3/5" 카운터
public Image progressBar;          // 타이머 게이지
public Image backgroundImage;      // 배경
public GameObject successEffect;   // 성공 이펙트
public GameObject hitEffect;       // 연타 히트 이펙트
```

### 색상 설정

```csharp
public Color normalColor = Color.white;    // 기본 색
public Color activeColor = Color.yellow;   // 활성화 시
public Color successColor = Color.green;   // 성공 시
public Color failColor = Color.red;        // 실패 시
```

## 사용 예제

### 예제 1: JSON 직접 작성

```json
{
  "songName": "example_song",
  "bpm": 120,
  "notes": [
    {
      "time": 5.0,
      "type": "tap",
      "arrow": "UP"
    },
    {
      "time": 10.0,
      "type": "rapid",
      "arrow": "DOWN",
      "rapidCount": 5,
      "rapidDuration": 1.0
    },
    {
      "time": 15.0,
      "type": "hold",
      "arrow": "LEFT",
      "duration": 2.0
    }
  ]
}
```

### 예제 2: MIDI Generator 사용

현재 MIDI Generator는 rapid 노트를 자동 생성하지 않습니다.
JSON을 수동으로 편집하여 추가하세요:

```
1. MIDI → JSON 생성
2. JSON 파일 열기
3. 원하는 위치의 노트를 "rapid"로 변경
4. rapidCount, rapidDuration 필드 추가
```

### 예제 3: 난이도별 연타 노트

```csharp
// RoundDifficultyManager에서
public RoundSettings[] rounds = new RoundSettings[]
{
    new RoundSettings
    {
        roundName = "Normal",
        difficulty = DifficultyFilter.Difficulty.Normal,
        // JSON에 rapid 노트 포함
        // Normal 필터는 연타 노트 유지 (removeRapidNotes = false)
    },
    new RoundSettings
    {
        roundName = "Easy",
        difficulty = DifficultyFilter.Difficulty.Easy,
        // Easy 필터가 연타 노트 자동 제거 (removeRapidNotes = true)
    }
};
```

## 요리 테마 연출 아이디어

### 야채 썰기 (3회 연타)
```json
{
  "type": "rapid",
  "arrow": "DOWN",
  "rapidCount": 3,
  "rapidDuration": 1.2
}
```
비주얼: 칼질 이펙트 + "톡톡톡" 소리

### 달걀 휘젓기 (5회 연타)
```json
{
  "type": "rapid",
  "arrow": "RIGHT",
  "rapidCount": 5,
  "rapidDuration": 1.0
}
```
비주얼: 휘젓기 이펙트 + 원형 모션

### 고기 뒤집기 (10회 연타)
```json
{
  "type": "rapid",
  "arrow": "SPACE",
  "rapidCount": 10,
  "rapidDuration": 1.5
}
```
비주얼: 불꽃 이펙트 + 지글지글 소리

## 테스트 JSON

```json
{
  "songName": "rapid_note_test",
  "bpm": 120,
  "offset": 0,
  "numberOfLanes": 1,
  "noteSpeed": 500,
  "notes": [
    {
      "time": 2.0,
      "lane": 0,
      "type": "rapid",
      "arrow": "UP",
      "rapidCount": 3,
      "rapidDuration": 1.5
    },
    {
      "time": 5.0,
      "lane": 0,
      "type": "rapid",
      "arrow": "DOWN",
      "rapidCount": 5,
      "rapidDuration": 1.0
    },
    {
      "time": 8.0,
      "lane": 0,
      "type": "rapid",
      "arrow": "LEFT",
      "rapidCount": 8,
      "rapidDuration": 0.8
    },
    {
      "time": 11.0,
      "lane": 0,
      "type": "rapid",
      "arrow": "RIGHT",
      "rapidCount": 10,
      "rapidDuration": 0.6
    }
  ]
}
```

## 디버그

### 콘솔 로그 확인

```
[Spawn] Rapid Note - Arrow: UP, Count: 5, Duration: 1.0s
[RapidNoteJudge] Initialized - Arrow: UP, Required: 5, Time: 1.0s
[RapidNoteJudge] Activated! Hit 5 times in 1.0s
[RapidNoteJudge] Hit 1/5
[RapidNoteJudge] Hit 2/5
...
[RapidNoteJudge] SUCCESS! Completed in 0.72s
```

### 문제 해결

**Q: 연타 노트가 일반 노트처럼 동작해요**
- A: `type: "rapid"` 확인, `rapidCount`와 `rapidDuration` 필드 확인

**Q: 카운터 UI가 안 보여요**
- A: RapidNoteVisual의 `counterText` 참조 확인

**Q: Easy 모드에서도 연타 노트가 나와요**
- A: DifficultyFilter 설정 확인 (`removeRapidNotes = true`)

**Q: 타이머가 너무 짧아요/길어요**
- A: `rapidDuration` 값 조정 (0.5 ~ 2.0 권장)

## 성능 최적화

- ✅ 연타 노트는 개별 판정 (다른 노트에 영향 없음)
- ✅ RapidNoteJudge는 활성화 시에만 업데이트
- ✅ 비주얼 업데이트는 프레임마다 실행 (가볍게 유지)

## 향후 확장

### 추가 가능한 기능:

1. **Grade별 연타 횟수 차등**
   ```
   Perfect: 5회
   Good: 4회
   Bad: 3회
   ```

2. **연타 속도 보너스**
   - 빠르게 완료할수록 점수 배율 증가

3. **콤보 보너스**
   - 연타 노트 성공 시 콤보 x2

4. **시각 효과 강화**
   - 파티클 시스템
   - 화면 흔들림
   - 타이머 경고 (빨간색 깜빡임)

## 라이선스

프로젝트 라이선스를 따릅니다.
