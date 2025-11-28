# 롱노트 시스템 가이드 (개정판)

## 🎵 롱노트 시스템 개요

### 핵심 개념
롱노트는 **여러 노트들의 집합**입니다:
- **LONG_START**: 시작 노트 (판정 등급 결정)
- **LONG_HOLD**: 홀딩 노트들 (Start 등급 자동 적용)
- **LONG_END**: 종료 노트 (최종 판정)

### 작동 원리
1. **Start 노트 판정**: 사용자가 키를 누르면 PERFECT/GREAT/GOOD 판정
2. **Hold 노트 자동 판정**: 키를 누르고 있는 동안 Hold 노트들이 Start 등급으로 자동 판정
3. **키를 떼면**: 남은 Hold/End 노트들이 모두 MISS 처리
4. **End 노트 판정**: 키를 끝까지 누르고 있으면 End 노트 판정 후 성공

### 제약사항
- **방향키만 롱노트 가능** (UP, DOWN, LEFT, RIGHT)
- **SPACE(패링)는 롱노트 불가**
- **롱노트 진행 중에는 다른 노트 무시**

### 시각적 표현
- **LONG_START 노트**: 화면에 표시됨 + 시각적 막대 시작점
- **시각적 막대 (LongNoteBar)**: Start에서 End까지 연결된 긴 막대
- **LONG_HOLD 노트**: 화면에 표시 안 됨 (숨김 상태)
- **LONG_END 노트**: 화면에 표시 안 됨 (숨김 상태)

```
[START]━━━━━━━━━━━━━[END]
   ↑        막대         ↑
 보임                  안보임
```

---

## 📊 시스템 구조

```
NoteData (noteSubType: "LONG_START", "LONG_HOLD", "LONG_END")
    ↓ 스폰
NoteSpawner (Start/Hold/End 노트 개별 생성)
    ↓ 판정
PlayerJudge (Start 판정 → Hold 자동판정 → End 판정)
    ↓ 상태관리
LongNoteState (현재 롱노트 정보 저장)
```

---

## 🔧 구현된 파일

### 1. PatternData.cs
```csharp
[System.Serializable]
public class NoteData
{
    public float time;
    public string type;

    // 롱노트 타입: "NORMAL", "LONG_START", "LONG_HOLD", "LONG_END"
    public string noteSubType = "NORMAL";

    // 롱노트 그룹 ID (같은 롱노트는 같은 ID)
    public int longNoteGroupId = -1;
}
```

**변경사항**:
- `isLongNote` boolean → `noteSubType` string
- `longNoteGroupId` 추가 → 같은 롱노트 묶음 식별
- `longNoteDuration` 추가 → LONG_START에만 사용 (시각적 막대 길이 계산용)

### 2. NoteMovement.cs
```csharp
public string noteSubType = "NORMAL";
public int longNoteGroupId = -1;
public GameObject longNoteVisualBar;  // LONG_START만 가짐

public void Init(float s, float t, string type, string subType = "NORMAL", int groupId = -1)
{
    speed = s;
    noteTime = t;
    noteType = type;
    noteSubType = subType;
    longNoteGroupId = groupId;
    isJudged = false;

    // LONG_HOLD와 LONG_END는 화면에 표시 안 함
    if (subType == "LONG_HOLD" || subType == "LONG_END")
    {
        SetVisibility(false);
    }
    // ...
}
```

**변경사항**:
- `noteSubType`, `longNoteGroupId` 추가
- `longNoteVisualBar` 추가 → LONG_START 노트가 시각적 막대 참조
- LONG_HOLD, LONG_END 노트는 자동으로 숨김 처리

### 3. PlayerJudge.cs (완전 재작성)

#### LongNoteState 내부 클래스
```csharp
private class LongNoteState
{
    public int groupId;           // 롱노트 그룹 ID
    public string noteType;       // 노트 타입 (LEFT, RIGHT, UP, DOWN)
    public string startJudge;     // START 노트의 판정 등급
    public bool isHolding;        // 키를 누르고 있는지
}
private LongNoteState currentLongNote = null;
```

#### Update() - 키 홀딩 체크
```csharp
void Update()
{
    if (PauseManager.IsPaused) return;

    // 롱노트 진행 중이면 키 홀딩 체크
    if (currentLongNote != null)
    {
        KeyCode keyCode = GetKeyCode(currentLongNote.noteType);
        currentLongNote.isHolding = Input.GetKey(keyCode);

        // 키를 떼면 롱노트 실패 처리
        if (!currentLongNote.isHolding)
        {
            FailLongNote();
        }
    }

    // ... 키 입력 처리
    CheckMissedNotes();
    CheckLongNoteHold();
}
```

#### CheckLongNoteHold() - Hold 노트 자동 판정
```csharp
void CheckLongNoteHold()
{
    if (currentLongNote == null) return;

    double songTime = AudioSettings.dspTime - spawner.songStartDspTime;
    NoteMovement[] allNotes = FindObjectsOfType<NoteMovement>();

    foreach (var n in allNotes)
    {
        if (n.isJudged) continue;
        if (n.longNoteGroupId != currentLongNote.groupId) continue;
        if (n.noteSubType != "LONG_HOLD") continue;

        // Hold 노트가 판정 시간에 도달하면 Start 등급으로 자동 판정
        float timeDelta = Mathf.Abs((float)(songTime - n.noteTime));
        if (timeDelta <= goodRange)
        {
            AutoJudgeLongHold(n, currentLongNote.startJudge);
        }
    }
}
```

#### TryHit() - Start/End 판정
```csharp
void TryHit(string keyType)
{
    // ... 노트 찾기

    // 롱노트 진행 중이면 LONG_END만 판정 가능
    if (currentLongNote != null)
    {
        if (n.longNoteGroupId == currentLongNote.groupId && n.noteSubType == "LONG_END")
        {
            // End 노트만 target으로 설정
        }
        continue; // 다른 노트 무시
    }

    // ... 판정 등급 계산

    // 노트 타입별 처리
    if (target.noteSubType == "LONG_START")
    {
        HitLongStart(judge, target);
    }
    else if (target.noteSubType == "LONG_END")
    {
        HitLongEnd(judge, target);
    }
    else
    {
        Hit(judge, target);
    }
}
```

#### HitLongStart() - 롱노트 시작
```csharp
void HitLongStart(string judge, NoteMovement n)
{
    n.isJudged = true;

    Debug.Log($"[LongNote Start] {judge} ({n.noteType}), groupId: {n.longNoteGroupId}");
    judgePopup.ShowJudge(judge);
    ScoreManager.Instance.AddJudge(judge);

    // 롱노트 상태 시작
    currentLongNote = new LongNoteState
    {
        groupId = n.longNoteGroupId,
        noteType = n.noteType,
        startJudge = judge,  // ⭐ Start 등급 저장
        isHolding = true
    };

    // Start 노트는 히트 효과만 재생, 파괴하지 않음
    // ...
}
```

#### HitLongEnd() - 롱노트 종료
```csharp
void HitLongEnd(string judge, NoteMovement n)
{
    n.isJudged = true;

    Debug.Log($"[LongNote End] {judge} ({n.noteType}), groupId: {n.longNoteGroupId}");
    judgePopup.ShowJudge(judge);
    ScoreManager.Instance.AddJudge(judge);

    // 같은 그룹의 모든 노트 파괴 (Start, Hold 포함)
    DestroyLongNoteGroup(n.longNoteGroupId);

    // 롱노트 상태 종료
    currentLongNote = null;
}
```

#### FailLongNote() - 키를 뗀 경우
```csharp
void FailLongNote()
{
    if (currentLongNote == null) return;

    Debug.Log($"[LongNote Fail] 키를 뗌! groupId: {currentLongNote.groupId}");

    // 남은 Hold 노트와 End 노트를 모두 MISS 처리
    NoteMovement[] allNotes = FindObjectsOfType<NoteMovement>();
    foreach (var n in allNotes)
    {
        if (n.longNoteGroupId == currentLongNote.groupId && !n.isJudged)
        {
            n.isJudged = true;
            ScoreManager.Instance.AddJudge("MISS");
        }
    }

    // 같은 그룹의 모든 노트 파괴
    DestroyLongNoteGroup(currentLongNote.groupId);

    // 롱노트 상태 종료
    currentLongNote = null;
}
```

### 4. NoteSpawner.cs
```csharp
[Header("Long Note Visual")]
public RectTransform longNoteBarPrefab;  // 롱노트 시각적 막대 Prefab

void Spawn(NoteData data, double currentSongTime)
{
    // ... 노트 생성

    // 노트 초기화
    mv.Init(noteSpeed, actualHitTime, data.type, data.noteSubType, data.longNoteGroupId);

    // LONG_START 노트면 시각적 막대 생성
    if (data.noteSubType == "LONG_START" && data.longNoteDuration > 0f)
    {
        GameObject longBar = CreateLongNoteBar(n, data.longNoteDuration);
        mv.longNoteVisualBar = longBar;
    }
}

GameObject CreateLongNoteBar(RectTransform startNote, float duration)
{
    RectTransform bar = Instantiate(longNoteBarPrefab, notesParent);
    bar.localPosition = startNote.localPosition;

    // 길이 계산: duration * noteSpeed
    float barLength = duration * noteSpeed;
    bar.sizeDelta = new Vector2(barLength, bar.sizeDelta.y);

    // 왼쪽 정렬 (Start 노트에서 시작)
    bar.pivot = new Vector2(0, 0.5f);

    // Start 노트 뒤에 표시
    bar.SetSiblingIndex(startNote.GetSiblingIndex());

    return bar.gameObject;
}
```

**변경사항**:
- `longNoteBarPrefab` 추가 → 시각적 막대 Prefab
- `CreateLongNoteBar()` 메서드 추가 → Start에서 End까지 연결된 막대 생성
- LONG_START 스폰 시 자동으로 막대 생성

### 5. RhythmPatternAutoGenerator.cs (패턴 생성기)

#### 새로운 옵션
```csharp
// 롱노트 옵션
public bool enableLongNotes = false;
public float longNoteProbability = 0.2f;
public float minLongNoteDuration = 1.0f;
public float maxLongNoteDuration = 3.0f;

// 패링(SPACE) 옵션
public bool enableParryNotes = true;
```

#### CreateLongNote() 메서드
```csharp
void CreateLongNote(List<NoteData> notes, float startTime, string type, float duration, ref int groupCounter)
{
    int groupId = groupCounter++;

    // LONG_START 노트
    notes.Add(new NoteData()
    {
        time = startTime,
        type = type,
        noteSubType = "LONG_START",
        longNoteGroupId = groupId
    });

    // LONG_HOLD 노트 (0.25초 간격으로 생성)
    float holdInterval = 0.25f;
    for (float t = startTime + holdInterval; t < startTime + duration - holdInterval; t += holdInterval)
    {
        notes.Add(new NoteData()
        {
            time = t,
            type = type,
            noteSubType = "LONG_HOLD",
            longNoteGroupId = groupId
        });
    }

    // LONG_END 노트
    notes.Add(new NoteData()
    {
        time = startTime + duration,
        type = type,
        noteSubType = "LONG_END",
        longNoteGroupId = groupId
    });
}
```

---

## 🎮 작동 방식

### 예시: 2초 롱노트 (UP)

#### 1. 패턴 데이터
```json
{
  "notes": [
    {
      "time": 5.0,
      "type": "UP",
      "noteSubType": "LONG_START",
      "longNoteGroupId": 0
    },
    {
      "time": 5.25,
      "type": "UP",
      "noteSubType": "LONG_HOLD",
      "longNoteGroupId": 0
    },
    {
      "time": 5.5,
      "type": "UP",
      "noteSubType": "LONG_HOLD",
      "longNoteGroupId": 0
    },
    {
      "time": 5.75,
      "type": "UP",
      "noteSubType": "LONG_HOLD",
      "longNoteGroupId": 0
    },
    {
      "time": 6.0,
      "type": "UP",
      "noteSubType": "LONG_HOLD",
      "longNoteGroupId": 0
    },
    {
      "time": 6.25,
      "type": "UP",
      "noteSubType": "LONG_HOLD",
      "longNoteGroupId": 0
    },
    {
      "time": 6.5,
      "type": "UP",
      "noteSubType": "LONG_HOLD",
      "longNoteGroupId": 0
    },
    {
      "time": 6.75,
      "type": "UP",
      "noteSubType": "LONG_HOLD",
      "longNoteGroupId": 0
    },
    {
      "time": 7.0,
      "type": "UP",
      "noteSubType": "LONG_END",
      "longNoteGroupId": 0
    }
  ]
}
```

#### 2. 실행 흐름

**5.0초: Start 노트 판정**
```
사용자가 UP 키 누름
  ↓
PlayerJudge.TryHit("UP") 호출
  ↓
LONG_START 노트 발견
  ↓
HitLongStart("PERFECT", startNote)
  ↓
currentLongNote 생성:
  - groupId: 0
  - noteType: "UP"
  - startJudge: "PERFECT"
  - isHolding: true
  ↓
ScoreManager에 "PERFECT" 1개 추가
```

**5.25초: 첫 Hold 노트**
```
Update() → CheckLongNoteHold()
  ↓
5.25초 Hold 노트 발견
  ↓
AutoJudgeLongHold(holdNote, "PERFECT")
  ↓
ScoreManager에 "PERFECT" 1개 추가 (Start 등급 적용)
  ↓
Hold 노트 파괴
```

**5.5초, 5.75초, ..., 6.75초: 나머지 Hold 노트들**
```
각 Hold 노트마다 같은 방식으로 "PERFECT" 판정
```

**6.5초: 사용자가 키를 떼면?**
```
Update()
  ↓
Input.GetKey(KeyCode.UpArrow) == false
  ↓
FailLongNote()
  ↓
남은 Hold 노트들 (6.75초) + End 노트 (7.0초) 모두 MISS 처리
  ↓
ScoreManager에 "MISS" 2개 추가
  ↓
모든 그룹 노트 파괴
  ↓
currentLongNote = null
```

**7.0초: End 노트 판정 (키를 끝까지 누른 경우)**
```
사용자가 UP 키 누른 상태
  ↓
PlayerJudge.TryHit("UP") 호출
  ↓
LONG_END 노트 발견 (groupId: 0)
  ↓
HitLongEnd("PERFECT", endNote)
  ↓
ScoreManager에 "PERFECT" 1개 추가
  ↓
모든 그룹 노트 파괴 (Start, Hold 포함)
  ↓
currentLongNote = null
```

#### 3. 최종 점수
- **성공 시**: PERFECT 9개 (Start 1 + Hold 7 + End 1)
- **6.5초에 키를 뗀 경우**: PERFECT 6개 + MISS 2개

---

## 📝 패턴 자동 생성기 사용법

### Unity Editor에서

1. **메뉴 열기**: `Tools` → `Rhythm Pattern Auto Generator`
2. **설정**:
   - `Music`: 음악 AudioClip 드래그
   - `Melody Sensitivity`: 노트 생성 민감도 (0.3 권장)
   - `Enable Parry (SPACE)`: 패링 노트 포함 여부
   - `Enable Long Notes`: 롱노트 포함 여부
   - `Long Note Probability`: 롱노트 생성 확률 (0.2 = 20%)
   - `Min/Max Duration`: 롱노트 지속 시간 범위
3. **생성**: `Generate JSON` 버튼 클릭
4. **결과**: `Assets/pattern.json` 생성

### 라운드별 설정 예시

**Round 1~2: 롱노트 없음**
```
Enable Long Notes: ❌ (체크 해제)
Enable Parry (SPACE): ✅
```

**Round 3~5: 롱노트 포함**
```
Enable Long Notes: ✅
Long Note Probability: 0.15 (15%)
Min Duration: 1.0
Max Duration: 2.5
Enable Parry (SPACE): ✅
```

---

## 🎨 Unity 씬 설정

### LongNoteBar Prefab 생성

#### 방법 1: UI Image로 생성

1. **Canvas 하위에 UI → Image 생성**
2. **이름**: "LongNoteBar"
3. **설정**:
   ```
   Width: 100 (초기값, 런타임에 자동 계산됨)
   Height: 80 (노트와 같은 높이)
   Color: 반투명 색상 (예: 노란색, Alpha 0.3~0.5)
   Pivot: (0, 0.5) ⭐ 중요! 왼쪽 중심
   ```
4. **Project로 드래그**하여 Prefab 생성
5. **Hierarchy에서 삭제**

#### 방법 2: 그라데이션 효과

```
LongNoteBar (Image)
├── Color: 그라데이션 (왼쪽 밝음 → 오른쪽 어두움)
└── Material: UI/Default
```

#### 방법 3: 애니메이션 효과 (선택)

- UV 스크롤 효과로 막대가 흐르는 연출
- Material 사용 필요

### NoteSpawner 연결

1. **Hierarchy에서 NoteSpawner 선택**
2. **Inspector에서**:
   ```
   Long Note Visual
   └── Long Note Bar Prefab: LongNoteBar Prefab 드래그
   ```

### 시각적 확인

- **LONG_START 노트**: 화면에 보임
- **LongNoteBar**: Start 노트와 함께 생성, Start 뒤에 표시
- **LONG_HOLD, LONG_END**: 화면에 안 보임 (투명)

```
화면 표시:
[START 노트]━━━━━━━━━━━━━(막대)
      ↑
    판정 가능

실제 존재하는 오브젝트:
[START]─[HOLD]─[HOLD]─[HOLD]─[END]
  보임   안보임  안보임  안보임  안보임
  └── 막대 연결됨 ──────────┘
```

---

## ⚠️ 주의사항

### 1. SPACE는 롱노트 불가
패턴 생성기가 자동으로 필터링하지만, 수동으로 패턴을 작성할 때 주의

### 2. 롱노트 진행 중 다른 노트 생성 불가
```csharp
// PlayerJudge.TryHit()에서 자동으로 처리
if (currentLongNote != null)
{
    // LONG_END만 판정 가능, 다른 노트는 무시
}
```

### 3. Hold 노트 간격
0.25초 간격 권장 (너무 짧으면 판정 누락 가능)

### 4. LongNoteManager 삭제됨
이전 버전의 `LongNoteManager.cs`는 삭제되었습니다. 모든 롱노트 로직은 `PlayerJudge.cs`에 통합되었습니다.

---

## 🐛 디버깅

### 로그 확인
```
[LongNote Start] PERFECT (UP), groupId: 0
[LongNote Hold] PERFECT (UP), groupId: 0
[LongNote Hold] PERFECT (UP), groupId: 0
...
[LongNote End] PERFECT (UP), groupId: 0
```

또는 실패 시:
```
[LongNote Start] PERFECT (UP), groupId: 0
[LongNote Hold] PERFECT (UP), groupId: 0
[LongNote Fail] 키를 뗌! groupId: 0
[LongNote Fail] MISS (LONG_HOLD), groupId: 0
[LongNote Fail] MISS (LONG_END), groupId: 0
```

### 문제 해결

**Q: Hold 노트가 자동 판정되지 않아요**
- `CheckLongNoteHold()`가 `Update()`에서 호출되는지 확인
- `currentLongNote`가 null이 아닌지 확인
- Hold 노트의 `longNoteGroupId`가 일치하는지 확인

**Q: 키를 누르고 있는데도 실패해요**
- `GetKeyCode()` 메서드가 올바른 KeyCode를 반환하는지 확인
- `currentLongNote.isHolding` 값 확인

**Q: End 노트를 눌러도 판정이 안 돼요**
- `TryHit()` 내부에서 `currentLongNote != null` 조건 확인
- End 노트의 `noteSubType`과 `longNoteGroupId` 확인

**Q: 패턴 생성기에서 NoteData 중복 에러**
- `RhythmPatternAutoGenerator.cs`에서 중복 NoteData 정의 제거됨
- `PatternData.cs`의 NoteData만 사용

---

## ✅ 체크리스트

### 스크립트
- [x] LongNoteManager.cs 삭제됨
- [x] PatternData.cs에 noteSubType, longNoteGroupId 추가
- [x] NoteMovement.cs에 noteSubType, longNoteGroupId 추가
- [x] PlayerJudge.cs에 롱노트 판정 로직 통합
- [x] NoteSpawner.cs에 롱노트 생성 로직 수정
- [x] RhythmPatternAutoGenerator.cs에 롱노트 생성 옵션 추가

### 패턴 데이터
- [ ] Round 1~2: enableLongNotes = false로 패턴 생성
- [ ] Round 3~5: enableLongNotes = true로 패턴 생성
- [ ] SPACE 노트는 롱노트로 설정 안 됨 확인

### 테스트
- [ ] 롱노트 Start 판정 성공
- [ ] Hold 노트 자동 판정 확인
- [ ] 키를 떼면 남은 노트 MISS 처리
- [ ] End 판정 성공
- [ ] 롱노트 진행 중 다른 노트 무시 확인

---

## 🎯 기존 시스템과의 차이

### 이전 (LongNoteManager 사용)
```
롱노트 = Start 노트 1개 + 시각적 막대
판정 = Start 판정 + End 판정 (2개)
```

### 현재 (Note Collection)
```
롱노트 = Start + 여러 Hold + End (여러 개)
판정 = Start 판정 + Hold 자동판정(여러 개) + End 판정
```

### 장점
- ✅ Hold 노트마다 점수 적용 (더 정확한 점수 계산)
- ✅ 키를 떼면 남은 노트만 MISS (부분 성공 가능)
- ✅ 시각적 막대 없이 Start/End 노트만으로 표현 가능
- ✅ 코드 일관성 (모든 노트가 NoteData 기반)

---

완성! 롱노트 시스템이 완전히 재설계되었습니다! 🎵
