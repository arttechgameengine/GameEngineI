# GameModeManager vs TournamentMapManager 차이점

## 🎯 핵심 차이

### GameModeManager
- **역할**: 전역 게임 데이터 관리 (싱글톤)
- **위치**: GameStartScene에서 생성, 모든 씬에 존재
- **생명주기**: `DontDestroyOnLoad` - 게임이 끝날 때까지 살아있음
- **담당**: 게임 모드, 진행도, 라운드 데이터 저장

### TournamentMapManager
- **역할**: 토너먼트 맵 씬의 UI/카메라 제어
- **위치**: TournamentMapScene에만 존재
- **생명주기**: 해당 씬에만 존재, 씬 전환 시 삭제됨
- **담당**: 카메라 줌인/줌아웃, 버튼 클릭 처리, UI 표시

---

## 📊 상세 비교표

| 구분 | GameModeManager | TournamentMapManager |
|------|-----------------|----------------------|
| **타입** | 싱글톤 (Singleton) | 일반 매니저 |
| **생명주기** | DontDestroyOnLoad | 씬 전용 |
| **씬** | 모든 씬 | TournamentMapScene만 |
| **데이터** | 라운드 데이터, 진행도 저장 | UI 참조만 보유 |
| **접근 방법** | `GameModeManager.Instance` | 직접 참조 불가 |

---

## 🔍 GameModeManager (전역 데이터 관리자)

### 위치
```
GameStartScene
└── GameModeManager ⭐ 여기서 생성!
    - DontDestroyOnLoad로 인해 모든 씬에서 접근 가능
```

### 주요 역할
1. **게임 모드 저장**: Story Mode인지 Free Mode인지
2. **진행도 관리**: 현재 어느 라운드까지 진행했는지
3. **라운드 데이터**: 5개 라운드의 정보 (RoundData ScriptableObject)
4. **세이브/로드**: PlayerPrefs로 진행도 저장

### 코드에서 사용
```csharp
// 어느 씬에서든 접근 가능 (싱글톤)
GameModeManager.Instance.currentMode
GameModeManager.Instance.currentStoryRound
GameModeManager.Instance.IsRoundLocked(0)
GameModeManager.Instance.ClearRound(0)
```

### 데이터 예시
```
currentMode: StoryMode
currentStoryRound: 2 (Round 3까지 진행 중)
allRounds: [Round1Data, Round2Data, ..., Round5Data]
roundCleared: [true, true, false, false, false]
```

---

## 🎮 TournamentMapManager (씬 전용 UI 관리자)

### 위치
```
TournamentMapScene
└── TournamentMapManager ⭐ 이 씬에만 존재!
    - 씬 전환 시 삭제됨
    - GameModeManager의 데이터를 읽어서 UI/카메라 제어
```

### 주요 역할
1. **카메라 제어**: Cinemachine으로 줌인/줌아웃
2. **UI 제어**: 라운드 버튼 활성화/비활성화
3. **버튼 이벤트**: 라운드 클릭 시 정보 표시
4. **카메라 경계**: Story Mode에서 이동 범위 제한

### 코드에서 사용
```csharp
// TournamentMapManager는 씬 내부에서만 동작
void Start() {
    // GameModeManager의 데이터를 읽어옴
    bool isLocked = GameModeManager.Instance.IsRoundLocked(i);
    roundButtons[i].SetLocked(isLocked);

    // 카메라 경계 설정
    UpdateCameraBoundary();
}
```

### 데이터 예시
```
mainCamera: MainCinemachineCamera 참조
roundCameras: [Round1Cam, Round2Cam, ...]
roundButtons: [Button1, Button2, ...]
progressiveBoundaries: [Boundary1, Boundary2, ...]
```

---

## 🔄 둘의 관계

```
GameModeManager (전역 데이터)
    ↓ 데이터 제공
TournamentMapManager (UI/카메라 제어)
    ↓ 사용자 입력 처리
GameModeManager (진행도 업데이트)
```

### 예시 플로우

#### 1. 게임 시작
```
GameStartScene
├── User: "Story Mode" 클릭
├── GameStartManager: GameModeManager.Instance.SetGameMode(StoryMode)
└── GameModeManager: currentMode = StoryMode 저장 ⭐
```

#### 2. 토너먼트 맵 로드
```
TournamentMapScene 로드
├── TournamentMapManager.Start() 실행
├── GameModeManager.Instance.currentMode 읽기 ⭐
├── Story Mode라서 Round 1만 잠금 해제
└── 카메라 경계를 Round1Boundary로 설정
```

#### 3. 라운드 클릭
```
User: Round 1 클릭
├── TournamentMapManager: OnRoundButtonClicked(0)
├── GameModeManager.Instance.IsRoundLocked(0) 확인 ⭐
├── 잠겨있지 않으면 줌인
└── 라운드 정보 표시
```

#### 4. 라운드 클리어
```
GameScene (게임 플레이)
├── 게임 클리어!
├── GameModeManager.Instance.ClearRound(0) ⭐
├── currentStoryRound = 1로 업데이트
└── Round 2 잠금 해제
```

#### 5. 토너먼트 맵 재진입
```
TournamentMapScene 재로드
├── TournamentMapManager.Start() 실행
├── GameModeManager.Instance.currentStoryRound = 1 읽기 ⭐
├── Round 1, 2 잠금 해제
└── 카메라 경계를 Round2Boundary로 확장
```

---

## 🏗️ 씬별 구조

### GameStartScene
```
GameStartScene
├── Canvas
│   └── GameStartManager (씬 전용)
└── GameModeManager ⭐ (DontDestroyOnLoad)
```

### TournamentMapScene
```
TournamentMapScene
├── TournamentMapManager (씬 전용)
└── (GameModeManager는 여기 없지만 Instance로 접근 가능)
```

### GameScene (실제 게임)
```
GameScene
├── NoteSpawner
│   └── GameModeManager.Instance.allRounds[selectedRound] 읽기 ⭐
├── ScoreManager
│   └── 클리어 시 GameModeManager.Instance.ClearRound() 호출 ⭐
└── (GameModeManager는 여기 없지만 Instance로 접근 가능)
```

---

## 💡 왜 분리했는가?

### GameModeManager를 싱글톤으로 만든 이유
1. **데이터 유지**: 씬 전환 시에도 게임 진행도가 사라지면 안 됨
2. **전역 접근**: 모든 씬에서 게임 모드/진행도를 확인해야 함
3. **세이브/로드**: 중앙 집중식 데이터 관리

### TournamentMapManager를 씬 전용으로 만든 이유
1. **UI/카메라는 씬마다 다름**: 다른 씬에서는 필요 없음
2. **메모리 효율**: 씬 종료 시 자동으로 정리됨
3. **단일 책임**: 토너먼트 맵 UI만 관리

---

## ⚠️ 주의사항

### GameModeManager 생성 위치
GameModeManager는 **GameStartScene에서 처음 생성**되어야 합니다!

```
잘못된 예:
TournamentMapScene에서 GameModeManager.Instance 접근
→ Instance가 null이면 에러 발생!

올바른 예:
GameStartScene → GameModeManager 생성 (DontDestroyOnLoad)
→ TournamentMapScene → GameModeManager.Instance 사용 ✓
```

### 체크 방법
TournamentMapManager에서 안전하게 접근:
```csharp
void Start() {
    if (GameModeManager.Instance == null) {
        Debug.LogError("GameModeManager가 없습니다! GameStartScene을 먼저 로드하세요.");
        return;
    }

    // 정상 동작
    UpdateCameraBoundary();
}
```

---

## 📝 요약

| | GameModeManager | TournamentMapManager |
|---|---|---|
| **생성 위치** | GameStartScene | TournamentMapScene |
| **생명주기** | 게임 종료까지 | 씬 전환 시 삭제 |
| **역할** | 데이터 저장/관리 | UI/카메라 제어 |
| **접근** | `Instance`로 어디서나 | 해당 씬에서만 |
| **예시** | "현재 Round 2까지 진행" | "Round 2 버튼 클릭 시 줌인" |

**간단히 말하면:**
- **GameModeManager** = 게임 전체의 "뇌" (데이터 저장소)
- **TournamentMapManager** = 토너먼트 맵 씬의 "손과 발" (UI 조작)

---

완료! 이제 두 매니저의 차이가 명확해졌나요?
