# 연타 노트 빠른 시작 가이드

## 🎯 연타 노트란?

정해진 시간 안에 N회 연타하여 성공/실패를 판정하는 특수 노트입니다.

```
예: 5회 연타 / 1초 안에
→ 플레이어: ↑↑↑↑↑
→ 성공! Perfect!
```

## 📝 JSON에 추가하기

### 1. 기본 연타 노트

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

**설명:**
- `type: "rapid"` - 연타 노트
- `rapidCount: 5` - 5회 연타 필요
- `rapidDuration: 1.0` - 1초 안에

### 2. 난이도별 예시

**Easy (자동 제거됨)**
```
DifficultyFilter가 제거 → 연타 노트 없음
```

**Normal (쉬움)**
```json
{
  "type": "rapid",
  "rapidCount": 3,
  "rapidDuration": 1.5
}
```

**Hard (어려움)**
```json
{
  "type": "rapid",
  "rapidCount": 10,
  "rapidDuration": 0.8
}
```

## 🎮 게임플레이

### 1. 노트 등장
```
[연타 노트 → ]
카운터: 0/5
```

### 2. 첫 입력
```
플레이어: ↑
→ 타이머 시작!
카운터: 1/5
```

### 3. 연타 진행
```
↑↑↑↑
카운터: 2/5 → 3/5 → 4/5 → 5/5
게이지: ████░ → ███░░
```

### 4. 성공!
```
5/5 달성
→ Perfect / Good / Bad (속도에 따라)
→ 점수 추가
```

### 5. 실패
```
시간 초과 (4/5만 달성)
→ Miss
→ 콤보 끊김
```

## 🔧 설치된 파일

### 새로운 스크립트:

1. **RapidNoteJudge.cs**
   - 연타 판정 로직
   - 카운터, 타이머 관리

2. **RapidNoteVisual.cs**
   - 카운터 UI ("3/5")
   - 타이머 게이지
   - 성공/실패 이펙트

### 업데이트된 파일:

1. **PatternData.cs**
   - `rapidCount`, `rapidDuration` 필드 추가

2. **PatternLoader.cs**
   - Rapid 노트 처리 추가

3. **PlayerJudge.cs**
   - Rapid 노트 판정 통합

4. **NoteSpawner.cs**
   - Rapid 노트 스폰 처리

5. **DifficultyFilter.cs**
   - `removeRapidNotes` 옵션 추가

## ⚙️ DifficultyFilter 설정

### Easy: 연타 노트 제거
```csharp
settings.removeRapidNotes = true;  // Easy에서 자동 제거
```

### Normal/Hard: 연타 노트 유지
```csharp
settings.removeRapidNotes = false;  // 연타 노트 표시
```

## 🧪 테스트 JSON

`Assets/Charts/rapid_test.json` 생성:

```json
{
  "songName": "rapid_test",
  "bpm": 120,
  "offset": 0,
  "numberOfLanes": 1,
  "noteSpeed": 500,
  "notes": [
    {
      "time": 2.0,
      "lane": 0,
      "type": "tap",
      "arrow": "UP"
    },
    {
      "time": 4.0,
      "lane": 0,
      "type": "rapid",
      "arrow": "DOWN",
      "rapidCount": 5,
      "rapidDuration": 1.0
    },
    {
      "time": 7.0,
      "lane": 0,
      "type": "tap",
      "arrow": "LEFT"
    }
  ]
}
```

## 🎨 요리 테마 예시

### 야채 썰기
```json
{
  "type": "rapid",
  "arrow": "DOWN",
  "rapidCount": 3,
  "rapidDuration": 1.2
}
```
→ 비주얼: 칼질 이펙트 "톡톡톡"

### 달걀 휘젓기
```json
{
  "type": "rapid",
  "arrow": "RIGHT",
  "rapidCount": 5,
  "rapidDuration": 1.0
}
```
→ 비주얼: 휘젓기 모션

### 고기 뒤집기
```json
{
  "type": "rapid",
  "arrow": "SPACE",
  "rapidCount": 8,
  "rapidDuration": 1.5
}
```
→ 비주얼: 불꽃 이펙트

## 🐛 문제 해결

### 연타 노트가 일반 노트처럼 동작
→ `type: "rapid"` 확인
→ `rapidCount`, `rapidDuration` 필드 추가 확인

### 카운터 UI가 안 보임
→ RapidNoteVisual 컴포넌트 확인
→ counterText 참조 설정 확인

### Easy에서도 연타 노트가 나옴
→ DifficultyFilter 설정 확인
→ `removeRapidNotes = true` 설정

## 📚 자세한 가이드

전체 문서: [RAPID_NOTE_GUIDE.md](RAPID_NOTE_GUIDE.md)

- 판정 시스템 상세
- 비주얼 커스터마이징
- 고급 설정
- 성능 최적화

## ✅ 체크리스트

- [ ] JSON에 `type: "rapid"` 노트 추가
- [ ] `rapidCount`, `rapidDuration` 설정
- [ ] Normal/Hard 난이도로 테스트
- [ ] Easy 난이도에서 자동 제거 확인
- [ ] 카운터 UI 정상 동작 확인
- [ ] 타이머 게이지 확인
- [ ] 성공/실패 판정 확인

---

**연타 노트 시스템 완성!** 🎉

질문이나 문제가 있으면 [RAPID_NOTE_GUIDE.md](RAPID_NOTE_GUIDE.md)를 참고하세요.
