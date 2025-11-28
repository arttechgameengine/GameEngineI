# Unity 씬 배치 가이드

## 📋 목차
1. [GameStartScene 배치](#1-gamestartscene-배치)
2. [TournamentMapScene 배치](#2-tournamentmapscene-배치)
3. [카메라와 경계 설정](#3-카메라와-경계-설정)
4. [시각적 레이아웃 예시](#4-시각적-레이아웃-예시)

---

## 1. GameStartScene 배치

### 1.1 기본 구조
```
GameStartScene
├── Main Camera (일반 Camera)
├── EventSystem
├── Canvas (Screen Space - Overlay)
│   ├── MainMenuPanel
│   │   ├── TitleText (위쪽)
│   │   ├── StartButton (중앙)
│   │   └── QuitButton (하단)
│   └── ModeSelectionPanel (초기에는 비활성화)
│       ├── TitleText ("Select Mode")
│       ├── StoryModeButton (왼쪽)
│       ├── FreeModeButton (오른쪽)
│       └── BackButton (하단)
└── GameModeManager (DontDestroyOnLoad)
```

### 1.2 단계별 생성

#### Step 1: Canvas 생성
1. `Hierarchy` 우클릭 → `UI` → `Canvas`
2. Canvas 설정:
   - Render Mode: Screen Space - Overlay
   - UI Scale Mode: Scale With Screen Size
   - Reference Resolution: 1920 x 1080

#### Step 2: MainMenuPanel 생성
1. Canvas 하위에 `UI` → `Panel` 생성 → 이름을 "MainMenuPanel"로 변경
2. 위치: Anchor를 중앙으로 (Shift + Alt 누르고 중앙 클릭)
3. 하위 요소 추가:

   **TitleText:**
   - `UI` → `Text - TextMeshPro` 생성
   - 위치: Pos Y = 200
   - 텍스트: "RHYTHM GAME"
   - Font Size: 72
   - Alignment: Center

   **StartButton:**
   - `UI` → `Button - TextMeshPro` 생성
   - 위치: Pos Y = 0
   - 크기: Width = 300, Height = 80
   - 버튼 텍스트: "START"

   **QuitButton:**
   - `UI` → `Button - TextMeshPro` 생성
   - 위치: Pos Y = -100
   - 크기: Width = 300, Height = 80
   - 버튼 텍스트: "QUIT"

#### Step 3: ModeSelectionPanel 생성
1. Canvas 하위에 `UI` → `Panel` 생성 → 이름을 "ModeSelectionPanel"로 변경
2. **Inspector에서 초기에 비활성화 체크 해제!**
3. 하위 요소 추가:

   **TitleText:**
   - `UI` → `Text - TextMeshPro`
   - 위치: Pos Y = 200
   - 텍스트: "Select Game Mode"
   - Font Size: 60

   **StoryModeButton:**
   - `UI` → `Button - TextMeshPro`
   - 위치: Pos X = -250, Pos Y = 0
   - 크기: Width = 400, Height = 120
   - 버튼 텍스트: "STORY MODE"

   **FreeModeButton:**
   - `UI` → `Button - TextMeshPro`
   - 위치: Pos X = 250, Pos Y = 0
   - 크기: Width = 400, Height = 120
   - 버튼 텍스트: "FREE MODE"

   **BackButton:**
   - `UI` → `Button - TextMeshPro`
   - 위치: Pos Y = -200
   - 크기: Width = 200, Height = 60
   - 버튼 텍스트: "BACK"

#### Step 4: GameStartManager 연결
1. Canvas에 `GameStartManager` 스크립트 추가
2. Inspector에서 연결:
   - Main Menu Panel → MainMenuPanel 드래그
   - Start Button → StartButton 드래그
   - Quit Button → QuitButton 드래그
   - Mode Selection Panel → ModeSelectionPanel 드래그
   - Story Mode Button → StoryModeButton 드래그
   - Free Mode Button → FreeModeButton 드래그
   - Back Button → BackButton 드래그

#### Step 5: GameModeManager 생성
1. `Hierarchy` 우클릭 → `Create Empty` → "GameModeManager"
2. `GameModeManager` 스크립트 추가
3. Inspector에서:
   - Current Mode: StoryMode
   - All Rounds: Size를 5로 설정 (나중에 RoundData 드래그)
   - Current Story Round: 0
   - Round Cleared: Size를 5로 설정 (모두 체크 해제)

---

## 2. TournamentMapScene 배치

### 2.1 전체 구조
```
TournamentMapScene (2D 또는 3D)
├── Main Camera (Cinemachine Camera)
├── EventSystem
├── MapBackground (Sprite 또는 Quad)
├── RoundPositions (Empty GameObject - 구조용)
│   ├── Round1Position (적 아바타)
│   ├── Round2Position (적 아바타)
│   ├── Round3Position (적 아바타)
│   ├── Round4Position (적 아바타)
│   └── Round5Position (적 아바타)
├── CameraSystem (Empty GameObject - 구조용)
│   ├── MainCinemachineCamera
│   └── RoundCameras (Empty GameObject)
│       ├── Round1Camera
│       ├── Round2Camera
│       ├── Round3Camera
│       ├── Round4Camera
│       └── Round5Camera
├── Boundaries (Empty GameObject - 구조용)
│   ├── FullMapBoundary
│   └── ProgressiveBoundaries (Empty GameObject)
│       ├── Round1Boundary
│       ├── Round2Boundary
│       ├── Round3Boundary
│       ├── Round4Boundary
│       └── Round5Boundary
├── UI Canvas
│   ├── RoundButtons (Empty GameObject)
│   │   ├── RoundButton1
│   │   ├── RoundButton2
│   │   ├── RoundButton3
│   │   ├── RoundButton4
│   │   └── RoundButton5
│   └── RoundInfoPanel
│       ├── Background
│       ├── RoundNameText
│       ├── EnemyNameText
│       ├── EnemyPortrait
│       ├── StoryText
│       ├── StartBattleButton
│       └── BackButton
└── TournamentMapManager (Empty GameObject)
```

### 2.2 단계별 생성

#### Step 1: 토너먼트 맵 배경 설정

**2D 방식 (추천):**
1. `Hierarchy` 우클릭 → `2D Object` → `Sprite` → "MapBackground"
2. 토너먼트 맵 이미지를 Sprite로 할당
3. 위치: X=0, Y=0, Z=0
4. Scale을 조정하여 적절한 크기로 설정

**예시 맵 레이아웃:**
```
      [Round5]
          |
      [Round4]
          |
      [Round3]
    /         \
[Round1]    [Round2]
```

#### Step 2: 라운드 위치 배치

1. `Create Empty` → "RoundPositions" (구조용 부모)
2. 각 라운드마다 적 아바타/아이콘 배치:

```
Round 1: Position (-8, -3, 0)  // 왼쪽 하단
Round 2: Position (8, -3, 0)   // 오른쪽 하단
Round 3: Position (0, 2, 0)    // 중앙
Round 4: Position (0, 7, 0)    // 중상단
Round 5: Position (0, 12, 0)   // 최상단
```

**각 라운드마다:**
1. RoundPositions 하위에 `2D Object` → `Sprite` 생성
2. 이름: "Round1Position", "Round2Position", etc.
3. 적 실루엣이나 아이콘 Sprite 할당
4. 위의 좌표대로 배치

#### Step 3: Cinemachine 카메라 시스템

**Main Camera 변환:**
1. 기존 Main Camera 선택
2. `Add Component` → `Cinemachine Camera`
3. 설정:
   - Priority: 10
   - Lens → Field of View: 60 (3D) 또는 Orthographic Size: 10 (2D)
4. 이름을 "MainCinemachineCamera"로 변경

**Round Cameras 생성:**
1. `Create Empty` → "CameraSystem" (구조용)
2. CameraSystem 하위에 `Create Empty` → "RoundCameras"
3. RoundCameras 하위에 5개의 카메라 생성:

**각 Round Camera마다:**
1. `Create Empty` → "Round1Camera"
2. `Add Component` → `Cinemachine Camera`
3. 위치를 해당 라운드 위치와 동일하게 설정
4. 설정:
   - Priority: 0
   - Lens → Field of View: 30 (3D) 또는 Orthographic Size: 3 (2D)
   - Look At: 해당 RoundPosition 드래그 (선택사항)

```
Round1Camera: Position (-8, -3, -10)
Round2Camera: Position (8, -3, -10)
Round3Camera: Position (0, 2, -10)
Round4Camera: Position (0, 7, -10)
Round5Camera: Position (0, 12, -10)
```

#### Step 4: 카메라 경계 설정

**FullMapBoundary (전체 맵):**
1. `Create Empty` → "Boundaries" (구조용)
2. Boundaries 하위에 `Create Empty` → "FullMapBoundary"
3. `Add Component` → `Polygon Collider 2D`
4. Inspector에서:
   - Is Trigger: ✓ 체크
   - `Edit Collider` 버튼 클릭
   - Scene 뷰에서 전체 맵을 감싸는 큰 사각형 그리기

**예시 좌표 (시계방향):**
```
(-15, -8)  → (15, -8)  → (15, 18)  → (-15, 18)  → (-15, -8)
```

**Progressive Boundaries (진행도별):**
1. Boundaries 하위에 `Create Empty` → "ProgressiveBoundaries"
2. 5개의 경계 생성:

**Round1Boundary:**
- `Create Empty` → `Add Component` → `Polygon Collider 2D`
- Is Trigger: ✓
- 좌표: Round 1 주변만 포함
```
(-12, -6)  → (-4, -6)  → (-4, 0)  → (-12, 0)  → (-12, -6)
```

**Round2Boundary:**
- Round 1~2 포함 (좌우로 확장)
```
(-12, -6)  → (12, -6)  → (12, 0)  → (-12, 0)  → (-12, -6)
```

**Round3Boundary:**
- Round 1~3 포함 (위로 확장)
```
(-12, -6)  → (12, -6)  → (12, 5)  → (-12, 5)  → (-12, -6)
```

**Round4Boundary:**
- Round 1~4 포함
```
(-12, -6)  → (12, -6)  → (12, 10)  → (-12, 10)  → (-12, -6)
```

**Round5Boundary:**
- Round 1~5 포함 (전체와 동일)
```
(-15, -8)  → (15, -8)  → (15, 18)  → (-15, 18)  → (-15, -8)
```

#### Step 5: UI Canvas 설정

**Canvas 생성:**
1. `UI` → `Canvas`
2. Render Mode: Screen Space - Overlay

**RoundButtons 배치:**
1. Canvas 하위에 `Create Empty` → "RoundButtons"
2. 각 라운드마다 버튼 생성:

**RoundButton1 (예시):**
1. `UI` → `Image` → "RoundButton1"
2. RectTransform 설정:
   - Anchor: Bottom Left
   - Pos X: 200, Pos Y: 200 (Round1Position이 화면에서 보이는 위치)
   - Width: 150, Height: 150
3. 하위 요소 추가:
   - **EnemyImage**: `UI` → `Image` (적 초상화)
   - **LockIcon**: `UI` → `Image` (자물쇠 아이콘, 초기 활성화)
   - **ClearedIcon**: `UI` → `Image` (체크마크, 초기 비활성화)
4. `Add Component` → `Button`
5. `Add Component` → `RoundButton` 스크립트
6. Inspector에서 연결:
   - Button: 자신의 Button 컴포넌트
   - Enemy Image: EnemyImage 드래그
   - Lock Icon: LockIcon 드래그
   - Cleared Icon: ClearedIcon 드래그

**나머지 RoundButton2~5도 동일하게 생성**
- 각각의 Pos X, Pos Y를 해당 라운드 위치에 맞게 조정

**RoundInfoPanel:**
1. Canvas 하위에 `UI` → `Panel` → "RoundInfoPanel"
2. 위치: 화면 오른쪽 또는 하단
3. 크기: Width: 500, Height: 700
4. **초기에 비활성화!**
5. 하위 요소:

```
└── RoundInfoPanel
    ├── RoundNameText (Pos Y = 250, 텍스트: "Round X")
    ├── EnemyNameText (Pos Y = 180, 텍스트: "Enemy Name")
    ├── EnemyPortrait (Pos Y = 80, Width: 200, Height: 200)
    ├── StoryText (Pos Y = -80, Width: 450, Height: 150)
    ├── StartBattleButton (Pos Y = -200, 텍스트: "Start Battle")
    └── BackButton (Pos Y = -280, 텍스트: "Back")
```

#### Step 6: TournamentMapManager 연결

1. `Create Empty` → "TournamentMapManager"
2. `Add Component` → `TournamentMapManager` 스크립트
3. Inspector에서 **모든 참조 연결**:

**Cinemachine:**
- Main Camera → MainCinemachineCamera
- Round Cameras → Size 5:
  - [0]: Round1Camera
  - [1]: Round2Camera
  - [2]: Round3Camera
  - [3]: Round4Camera
  - [4]: Round5Camera

**Camera Constraints:**
- Map Boundary → FullMapBoundary
- Progressive Boundaries → Size 5:
  - [0]: Round1Boundary
  - [1]: Round2Boundary
  - [2]: Round3Boundary
  - [3]: Round4Boundary
  - [4]: Round5Boundary

**Round Buttons:**
- Round Buttons → Size 5:
  - [0]: RoundButton1
  - [1]: RoundButton2
  - [2]: RoundButton3
  - [3]: RoundButton4
  - [4]: RoundButton5

**UI:**
- Round Info Panel → RoundInfoPanel
- Round Name Text → RoundNameText
- Enemy Name Text → EnemyNameText
- Enemy Portrait Image → EnemyPortrait
- Story Text → StoryText
- Start Battle Button → StartBattleButton
- Back Button → BackButton

---

## 3. 카메라와 경계 설정

### 3.1 카메라 설정 (2D 게임 기준)

**Main Cinemachine Camera:**
```
Position: (0, 5, -10)
Rotation: (0, 0, 0)
Lens:
  - Orthographic: ✓
  - Orthographic Size: 10
  - Near Clip: 0.01
  - Far Clip: 1000
Priority: 10
```

**Round Cameras:**
```
Orthographic Size: 3 (줌인 효과)
Priority: 0 (기본 비활성화)
```

### 3.2 경계 시각화 (디버그용)

경계가 올바르게 설정되었는지 확인하려면:
1. Scene 뷰에서 Gizmos 버튼 클릭 → Colliders 체크
2. Boundaries 오브젝트들이 초록색 선으로 보임
3. 각 경계가 의도한 영역을 감싸는지 확인

---

## 4. 시각적 레이아웃 예시

### 토너먼트 맵 2D 뷰 (위에서 본 모습)

```
                      [Round5] (0, 12)
                          ●
                          |
                          |
                      [Round4] (0, 7)
                          ●
                          |
                          |
                      [Round3] (0, 2)
                          ●
                         / \
                        /   \
                       /     \
                      /       \
         (-8, -3)    ●         ●    (8, -3)
                [Round1]     [Round2]


[FullMapBoundary 경계선]
┌─────────────────────────────────┐
│                                   │
│         (진행도별 경계선)           │
│   ┌───────────────────────┐     │
│   │                       │     │
│   │                       │     │
│   │                       │     │
│   └───────────────────────┘     │
│                                   │
└─────────────────────────────────┘
```

### UI 배치 (화면 뷰)

```
┌─────────────────────────────────────────┐
│  [RoundButton5]                          │
│                                          │
│      [RoundButton4]                      │
│                                          │
│           [RoundButton3]                 │
│                                          │
│  [RoundButton1]    [RoundButton2]        │
│                                          │
│                     ┌──────────────────┐ │
│                     │ Round Info Panel │ │
│                     │                  │ │
│                     │  [Start Battle]  │ │
│                     │  [Back]          │ │
│                     └──────────────────┘ │
└─────────────────────────────────────────┘
```

---

## 5. RoundData ScriptableObject 생성

1. Project 창에서 `Assets/Data` 폴더 생성 (없으면)
2. `Data` 폴더에서 우클릭 → `Create` → `Tournament` → `Round Data`
3. 이름: "Round1Data"
4. 설정:
   - Round Number: 1
   - Round Name: "Round 1"
   - Enemy Name: "적 이름"
   - Enemy Portrait: 적 Sprite 드래그
   - Map Position: (-8, -3)
   - Song Name: "곡 이름"
   - Song Clip: AudioClip 드래그
   - Beatmap File: CSV 파일 드래그
   - Story Text: "스토리 내용..."

5. Round2~5Data도 동일하게 생성

6. **GameModeManager의 All Rounds 배열에 순서대로 드래그!**

---

## 6. 최종 체크리스트

### GameStartScene
- [ ] MainMenuPanel과 ModeSelectionPanel이 Canvas 하위에 있음
- [ ] GameStartManager에 모든 버튼/패널 연결됨
- [ ] ModeSelectionPanel이 초기에 비활성화됨
- [ ] GameModeManager가 생성되고 All Rounds에 5개의 RoundData가 할당됨

### TournamentMapScene
- [ ] MainCinemachineCamera에 CinemachineCamera 컴포넌트 있음
- [ ] 5개의 Round Camera가 각 라운드 위치에 배치됨
- [ ] FullMapBoundary와 5개의 Progressive Boundaries 생성됨
- [ ] 모든 Boundary의 Is Trigger가 체크됨
- [ ] 5개의 RoundButton이 UI Canvas에 배치됨
- [ ] TournamentMapManager에 모든 참조가 연결됨
- [ ] RoundInfoPanel이 초기에 비활성화됨

### Build Settings
- [ ] GameStartScene이 Build Settings에 Index 0으로 추가됨
- [ ] TournamentMapScene이 Build Settings에 추가됨
- [ ] GameScene(실제 게임 씬)이 Build Settings에 추가됨

---

## 7. 테스트

1. GameStartScene 실행
2. Start 버튼 클릭 → 모드 선택 화면 표시되는지 확인
3. Story Mode 선택 → TournamentMapScene 로드되는지 확인
4. Scene 뷰에서 경계선(초록색)이 올바르게 표시되는지 확인
5. Game 뷰에서 라운드 버튼 클릭 → 카메라가 줌인되는지 확인
6. Story Mode에서 Round 2~5가 잠겨있는지 확인
7. 카메라를 드래그해서 이동 시 경계를 넘지 않는지 확인

---

완료! 이제 Unity 에디터에서 이 가이드대로 씬을 구성하면 됩니다.
