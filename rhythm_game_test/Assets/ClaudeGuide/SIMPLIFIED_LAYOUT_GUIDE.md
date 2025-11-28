# 간소화된 토너먼트 맵 레이아웃 가이드

## ✅ 개선된 구조 (RoundPosition 제거)

```
TournamentMapScene
├── Main Camera (Cinemachine Camera)
├── EventSystem
├── MapBackground (Sprite)
├── CameraSystem
│   ├── MainCinemachineCamera
│   └── RoundCameras
│       ├── Round1Camera (위치: -8, -3, -10)
│       ├── Round2Camera (위치: 8, -3, -10)
│       ├── Round3Camera (위치: 0, 2, -10)
│       ├── Round4Camera (위치: 0, 7, -10)
│       └── Round5Camera (위치: 0, 12, -10)
├── Boundaries
│   ├── FullMapBoundary
│   └── ProgressiveBoundaries
│       ├── Round1Boundary
│       ├── Round2Boundary
│       ├── Round3Boundary
│       ├── Round4Boundary
│       └── Round5Boundary
├── WorldCanvas (World Space) ⭐ 핵심!
│   └── RoundButtons
│       ├── RoundButton1 (위치: -8, -3, 0)
│       ├── RoundButton2 (위치: 8, -3, 0)
│       ├── RoundButton3 (위치: 0, 2, 0)
│       ├── RoundButton4 (위치: 0, 7, 0)
│       └── RoundButton5 (위치: 0, 12, 0)
├── UICanvas (Screen Space - Overlay)
│   └── RoundInfoPanel
│       ├── RoundNameText
│       ├── EnemyNameText
│       ├── EnemyPortrait
│       ├── StoryText
│       ├── StartBattleButton
│       └── BackButton
└── TournamentMapManager
```

---

## 🎯 핵심 변경점

### World Space Canvas로 RoundButton 배치

**장점:**
1. ✅ 월드 공간에 직접 배치 → 카메라 줌인/줌아웃에 자연스럽게 반응
2. ✅ RoundPosition 불필요 → 버튼 자체가 위치 역할
3. ✅ 적 아바타/이미지를 버튼에 바로 표시
4. ✅ Cinemachine 카메라가 버튼 위치로 줌인

---

## 📝 단계별 설정

### Step 1: World Canvas 생성

1. `Hierarchy` 우클릭 → `UI` → `Canvas` 생성
2. 이름을 "WorldCanvas"로 변경
3. Inspector 설정:
   ```
   Render Mode: World Space ⭐
   Event Camera: Main Camera 드래그
   Sorting Layer: Default
   Order in Layer: 0
   ```
4. RectTransform 설정:
   ```
   Pos X: 0, Pos Y: 0, Pos Z: 0
   Width: 40
   Height: 40
   Scale: 0.1, 0.1, 0.1 (크기 조정)
   ```

### Step 2: RoundButtons 배치 (World Canvas 하위)

WorldCanvas 하위에 5개의 RoundButton 생성:

#### RoundButton1 예시:
1. WorldCanvas 우클릭 → `UI` → `Image` 생성 → "RoundButton1"
2. RectTransform:
   ```
   Pos X: -80  (월드 좌표 -8에 해당)
   Pos Y: -30  (월드 좌표 -3에 해당)
   Pos Z: 0
   Width: 150
   Height: 150
   ```
3. 구조:
   ```
   RoundButton1
   ├── Background (Image) - 버튼 배경
   ├── EnemyImage (Image) - 적 초상화 (큰 원형)
   ├── LockIcon (Image) - 자물쇠 아이콘
   └── ClearedIcon (Image) - 체크마크
   ```
4. 컴포넌트:
   - `Add Component` → `Button`
   - `Add Component` → `RoundButton` 스크립트

#### 전체 RoundButton 좌표:
```
RoundButton1: Pos X = -80, Pos Y = -30   (월드: -8, -3)
RoundButton2: Pos X = 80,  Pos Y = -30   (월드: 8, -3)
RoundButton3: Pos X = 0,   Pos Y = 20    (월드: 0, 2)
RoundButton4: Pos X = 0,   Pos Y = 70    (월드: 0, 7)
RoundButton5: Pos X = 0,   Pos Y = 120   (월드: 0, 12)
```

**팁:** WorldCanvas의 Scale이 0.1이므로, 월드 좌표 × 10 = Canvas 내부 좌표

### Step 3: Round Cameras 타겟 설정

각 Round Camera를 RoundButton 위치로 설정:

```
Round1Camera:
  Position: (-8, -3, -10)
  Look At: RoundButton1 (선택사항)

Round2Camera:
  Position: (8, -3, -10)
  Look At: RoundButton2

Round3Camera:
  Position: (0, 2, -10)
  Look At: RoundButton3

Round4Camera:
  Position: (0, 7, -10)
  Look At: RoundButton4

Round5Camera:
  Position: (0, 12, -10)
  Look At: RoundButton5
```

---

## 🎨 RoundButton 디자인 예시

### 잠긴 상태 (Story Mode)
```
┌───────────────┐
│   🔒          │  ← LockIcon (활성화)
│               │
│   ❓ ???      │  ← EnemyImage (어둡게, alpha=0.3)
│               │
│               │
└───────────────┘
```

### 잠금 해제 상태
```
┌───────────────┐
│               │  ← LockIcon (비활성화)
│               │
│   😈 Enemy    │  ← EnemyImage (밝게, alpha=1.0)
│               │
│               │
└───────────────┘
```

### 클리어 상태
```
┌───────────────┐
│          ✓    │  ← ClearedIcon (활성화)
│               │
│   😈 Enemy    │  ← EnemyImage (밝게)
│               │
│               │
└───────────────┘
```

---

## 🔧 TournamentMapManager 연결

Inspector에서 동일하게 연결하되, RoundButton만 사용:

**Round Buttons (Size: 5):**
- [0]: WorldCanvas/RoundButton1
- [1]: WorldCanvas/RoundButton2
- [2]: WorldCanvas/RoundButton3
- [3]: WorldCanvas/RoundButton4
- [4]: WorldCanvas/RoundButton5

---

## 💡 대안: Screen Space Overlay 사용 (기존 방식)

만약 World Space가 복잡하다면, 기존 방식(Screen Space Overlay)도 괜찮습니다:

**장점:**
- 설정이 더 간단
- UI 위치 관리가 쉬움

**단점:**
- 카메라 줌인 시 버튼 크기가 변하지 않음
- 월드와 UI가 분리된 느낌

**이 경우:**
- RoundPosition (적 아바타)을 월드에 배치
- RoundButton을 UI Canvas에 배치
- 둘 다 유지해야 함

---

## ✨ 추천 방법

### 방법 1: World Space Canvas (추천) ⭐
- **RoundButton만 사용**
- 월드 공간에 배치
- Cinemachine 줌인과 자연스러운 연동

### 방법 2: Screen Space + 장식용 아바타
- **RoundPosition**: 월드에 적 실루엣/아바타 배치 (Sprite, 장식용)
- **RoundButton**: UI Canvas에 버튼 배치
- 카메라는 RoundPosition으로 줌인
- 버튼은 화면 고정 위치

**선택 기준:**
- 토너먼트 맵에 **3D 적 모델**이나 **애니메이션**을 넣고 싶다 → 방법 2
- **심플하게 2D UI**만 원한다 → 방법 1

---

## 🎮 최종 추천 구조 (가장 간단)

```
TournamentMapScene
├── Main Camera (Cinemachine)
├── MapBackground
├── CameraSystem (Round Cameras × 5)
├── Boundaries (경계 × 6)
├── WorldCanvas (World Space) ⭐
│   └── RoundButtons (× 5) - 여기 하나로 통합!
├── UICanvas (Screen Space - Overlay)
│   └── RoundInfoPanel
└── TournamentMapManager
```

**RoundPosition 삭제!** 버튼이 곧 위치입니다.

---

## 📋 업데이트된 체크리스트

- [ ] WorldCanvas를 World Space로 설정
- [ ] WorldCanvas의 Event Camera를 Main Camera로 설정
- [ ] 5개의 RoundButton을 WorldCanvas 하위에 배치
- [ ] 각 RoundButton의 월드 좌표가 올바른지 확인
- [ ] Round Cameras가 각 RoundButton 위치를 바라보는지 확인
- [ ] TournamentMapManager에 RoundButtons 배열 연결
- [ ] 불필요한 RoundPosition 오브젝트 삭제

---

이제 훨씬 간단해졌습니다! RoundButton 하나로 모든 역할을 수행합니다.
