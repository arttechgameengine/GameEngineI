# 토너먼트 맵 설정 가이드

이 가이드는 토너먼트 맵 씬을 Unity에서 설정하는 방법을 설명합니다.

## 1. 씬 구조

### GameStartScene (게임 시작 화면)
- **Main Menu Panel**: 초기 시작 화면
  - Start Button
  - Quit Button
- **Mode Selection Panel**: 모드 선택 화면
  - Story Mode Button
  - Free Mode Button
  - Back Button

### TournamentMapScene (토너먼트 맵)
```
TournamentMapScene
├── Main Camera (Cinemachine Camera)
│   └── CinemachineConfiner2D (자동 추가됨)
├── Round Cameras (5개)
│   ├── Round1 Camera (Cinemachine Camera)
│   ├── Round2 Camera (Cinemachine Camera)
│   ├── Round3 Camera (Cinemachine Camera)
│   ├── Round4 Camera (Cinemachine Camera)
│   └── Round5 Camera (Cinemachine Camera)
├── Map Boundaries
│   ├── FullMapBoundary (PolygonCollider2D, isTrigger=true)
│   ├── Round1Boundary (PolygonCollider2D, isTrigger=true)
│   ├── Round2Boundary (PolygonCollider2D, isTrigger=true)
│   ├── Round3Boundary (PolygonCollider2D, isTrigger=true)
│   ├── Round4Boundary (PolygonCollider2D, isTrigger=true)
│   └── Round5Boundary (PolygonCollider2D, isTrigger=true)
├── Round Buttons (5개)
│   ├── RoundButton1
│   ├── RoundButton2
│   ├── RoundButton3
│   ├── RoundButton4
│   └── RoundButton5
├── UI Canvas
│   └── RoundInfoPanel
│       ├── RoundNameText
│       ├── EnemyNameText
│       ├── EnemyPortraitImage
│       ├── StoryText
│       ├── StartBattleButton
│       └── BackButton
└── GameModeManager (Singleton)
```

---

## 2. 상세 설정

### 2.1 Cinemachine 카메라 설정

#### Main Camera (전체 맵 보기용)
1. GameObject 생성: `Create Empty` → 이름을 "MainCamera"로 변경
2. `CinemachineCamera` 컴포넌트 추가
3. 설정:
   - **Priority**: 10 (시작 시 활성화)
   - **Lens → Field of View**: 60 (또는 원하는 값)
   - **Body → Dead Zone**: 적절히 조정
   - **Aim**: Do Nothing
4. **CinemachineConfiner2D**는 스크립트에서 자동으로 추가됩니다

#### Round Cameras (각 라운드별)
1. 5개의 Cinemachine Camera 생성
2. 각 카메라를 해당 라운드 위치에 배치
3. 설정:
   - **Priority**: 0 (기본적으로 비활성화)
   - **Lens → Field of View**: 30-40 (줌인 효과를 위해 작게)
   - **Follow Target**: 각 라운드의 적 오브젝트 (선택사항)

---

### 2.2 Map Boundaries (카메라 경계) 설정

#### FullMapBoundary (Free Mode용)
1. GameObject 생성: `Create Empty` → "FullMapBoundary"
2. `PolygonCollider2D` 컴포넌트 추가
3. 설정:
   - **Is Trigger**: ✓ (체크)
   - **Edit Collider** 버튼을 눌러서 전체 맵을 감싸는 다각형 그리기
   - 맵의 모든 라운드를 포함하도록 큰 사각형/다각형 생성

#### Progressive Boundaries (Story Mode용)
각 라운드까지의 진행도에 맞는 경계를 5개 생성합니다.

**Round1Boundary** (라운드 1까지만):
- PolygonCollider2D로 라운드 1 주변만 포함

**Round2Boundary** (라운드 1~2까지):
- PolygonCollider2D로 라운드 1, 2 주변 포함

**Round3Boundary** (라운드 1~3까지):
- PolygonCollider2D로 라운드 1, 2, 3 주변 포함

이런 식으로 **Round5Boundary**까지 생성합니다.

**팁**:
- 경계는 왼쪽에서 오른쪽으로 점진적으로 확장되도록 설정
- 각 경계는 이전 경계를 포함해야 함

---

### 2.3 Round Button 설정

각 라운드마다 버튼을 생성합니다.

#### RoundButton Prefab 구조
```
RoundButton (Canvas 내부)
├── Button (Image)
│   └── EnemyImage (Image) - 적 초상화
├── LockIcon (Image) - 잠금 아이콘
└── ClearedIcon (Image) - 클리어 표시
```

#### 설정:
1. 5개의 RoundButton을 토너먼트 맵 상의 각 라운드 위치에 배치
2. 각 버튼에 `RoundButton` 스크립트 추가
3. Inspector에서 설정:
   - **Button**: Button 컴포넌트 참조
   - **Enemy Image**: 적 이미지 참조
   - **Lock Icon**: 잠금 아이콘 오브젝트 참조
   - **Cleared Icon**: 클리어 표시 오브젝트 참조

---

### 2.4 TournamentMapManager 설정

1. 빈 GameObject 생성 → "TournamentMapManager"
2. `TournamentMapManager` 스크립트 추가
3. Inspector에서 모든 참조 연결:

#### Cinemachine
- **Main Camera**: MainCamera 드래그
- **Round Cameras**: 5개의 라운드 카메라 배열에 순서대로 드래그

#### Camera Constraints
- **Map Boundary**: FullMapBoundary 드래그
- **Progressive Boundaries**: Round1~5Boundary를 배열에 순서대로 드래그

#### Round Buttons
- **Round Buttons**: 5개의 RoundButton을 배열에 순서대로 드래그

#### UI
- **Round Info Panel**: 라운드 정보 패널 드래그
- **Round Name Text**: 라운드 이름 텍스트 드래그
- **Enemy Name Text**: 적 이름 텍스트 드래그
- **Enemy Portrait Image**: 적 초상화 이미지 드래그
- **Story Text**: 스토리 텍스트 드래그
- **Start Battle Button**: 전투 시작 버튼 드래그
- **Back Button**: 뒤로가기 버튼 드래그

---

### 2.5 GameModeManager 설정

1. DontDestroyOnLoad 오브젝트로 생성 (GameStartScene에)
2. `GameModeManager` 스크립트 추가
3. Inspector에서 설정:
   - **Current Mode**: StoryMode (기본값)
   - **All Rounds**: 5개의 RoundData ScriptableObject 배열에 드래그
   - **Current Story Round**: 0 (처음 시작)
   - **Round Cleared**: 크기 5 배열 (모두 false)

---

## 3. RoundData ScriptableObject 생성

1. Project 창에서 우클릭 → `Create` → `Tournament` → `Round Data`
2. 5개의 RoundData 생성 (Round1 ~ Round5)
3. 각각 설정:
   - **Round Number**: 1~5
   - **Round Name**: "Round 1", "Round 2", etc.
   - **Enemy Name**: 적 이름
   - **Enemy Portrait**: 적 초상화 스프라이트
   - **Map Position**: 토너먼트 맵에서의 위치 (Vector2)
   - **Song Name**: 곡 이름
   - **Song Clip**: AudioClip
   - **Beatmap File**: CSV TextAsset
   - **Story Text**: 라운드 시작 전 스토리

---

## 4. 작동 방식

### Story Mode
1. 시작 시 Round 1만 잠금 해제
2. 카메라는 Round 1 영역까지만 탐색 가능 (Round1Boundary)
3. Round 1 클리어 시:
   - Round 2 잠금 해제
   - 카메라 경계가 Round2Boundary로 확장
4. 이런 식으로 순차적으로 진행

### Free Mode
1. 모든 라운드 잠금 해제
2. 카메라는 전체 맵 탐색 가능 (FullMapBoundary)
3. 원하는 라운드 자유롭게 선택 가능

---

## 5. 테스트 체크리스트

- [ ] GameStartScene에서 Start 버튼 클릭 → 모드 선택 화면 표시
- [ ] Story Mode 선택 → TournamentMapScene 로드
- [ ] Free Mode 선택 → TournamentMapScene 로드
- [ ] Story Mode: Round 1만 클릭 가능, 나머지 잠김
- [ ] Story Mode: 카메라가 Round 1 영역까지만 이동 가능
- [ ] Free Mode: 모든 라운드 클릭 가능
- [ ] Free Mode: 카메라가 전체 맵 탐색 가능
- [ ] 라운드 클릭 → Cinemachine으로 부드럽게 줌인
- [ ] 라운드 정보 패널에 올바른 정보 표시
- [ ] Start Battle 버튼 → GameScene 로드
- [ ] Back 버튼 → 줌아웃하여 전체 맵 보기

---

## 6. 디버깅 팁

### 카메라 경계가 작동하지 않을 때
- Main Camera에 CinemachineConfiner2D가 추가되었는지 확인
- PolygonCollider2D의 Is Trigger가 체크되어 있는지 확인
- 경계 콜라이더가 올바른 위치에 있는지 Scene 뷰에서 확인

### 라운드 잠금이 작동하지 않을 때
- GameModeManager.Instance가 null이 아닌지 확인
- currentStoryRound 값이 올바른지 확인 (Inspector에서)
- IsRoundLocked() 메서드가 올바르게 호출되는지 확인

### Cinemachine 줌인이 작동하지 않을 때
- 각 Round Camera의 Priority 값 확인 (기본 0)
- Main Camera의 Priority 값 확인 (기본 10)
- ZoomToRound() 메서드에서 Priority 값이 올바르게 변경되는지 확인

---

## 7. 추가 개선 사항 (선택사항)

- **카메라 전환 애니메이션**: Cinemachine Blend 설정으로 부드러운 전환
- **라운드 버튼 애니메이션**: 호버/클릭 시 스케일 변화
- **배경 음악**: 토너먼트 맵 BGM 추가
- **파티클 효과**: 잠금 해제 시 파티클 효과
- **미니맵**: 전체 맵 미니맵 UI 추가

---

완료되었습니다! 궁금한 점이 있으면 언제든지 물어보세요.
