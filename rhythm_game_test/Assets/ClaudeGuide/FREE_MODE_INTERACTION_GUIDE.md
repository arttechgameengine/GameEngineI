# Free Mode 인터랙션 가이드

## 🎯 Free Mode vs Story Mode 차이

### Story Mode
- 현재 라운드까지만 잠금 해제
- 카메라 이동 범위 제한 (Progressive Boundaries)
- 순차적 진행 강제

### Free Mode
- **모든 라운드 잠금 해제**
- **자유로운 선택 가능**
- 카메라 이동 자유 (전체 맵)

---

## 💡 Free Mode 인터랙션 방식 (추천 순서)

### 방법 1: 줌아웃 상태에서 클릭 선택 ⭐ (가장 추천)

**개념:**
- 전체 브래킷 뷰(줌아웃)에서 5개 라운드를 **직접 클릭**
- 클릭하면 해당 라운드로 줌인
- 간단하고 직관적!

**장점:**
- ✅ 가장 직관적 (보이는 것을 바로 클릭)
- ✅ 추가 UI 불필요
- ✅ 토너먼트 구조를 계속 볼 수 있음

**단점:**
- ❌ 줌아웃 상태에서 5개 버튼이 숨겨져 있어서 클릭 영역 인식 필요

**구현:**
```
줌아웃 상태:
- 실루엣 표시 ✓
- 버튼 숨김 ✗
- BUT! 버튼은 투명하게 유지하여 클릭 가능

→ 사용자는 실루엣을 보고 클릭
→ 실제로는 투명한 버튼이 클릭됨
```

---

### 방법 2: 사이드 메뉴 선택

**개념:**
- 화면 왼쪽/오른쪽에 라운드 목록 메뉴
- 목록에서 라운드 선택
- 선택하면 해당 라운드로 줌인

**장점:**
- ✅ 명확한 선택 UI
- ✅ 라운드 정보 미리 확인 가능
- ✅ 클릭 실수 없음

**단점:**
- ❌ 화면 공간 차지
- ❌ 토너먼트 브래킷 분위기 감소

**UI 예시:**
```
┌─────────────────────────────┐
│ [Round List]                │
│  ○ Round 1                  │
│  ○ Round 2                  │
│  ○ Round 3                  │
│  ○ Round 4                  │
│  ○ Round 5 (Final)          │
│                             │
│      [브래킷 뷰]              │
│                             │
└─────────────────────────────┘
```

---

### 방법 3: 카메라 드래그 + 버튼 상시 표시

**개념:**
- Free Mode에서는 **버튼을 계속 표시**
- 실루엣은 숨김
- 사용자가 카메라를 드래그하여 이동
- 원하는 라운드를 찾아서 클릭

**장점:**
- ✅ 직관적인 탐색
- ✅ 게임적 자유도

**단점:**
- ❌ 웅장한 브래킷 연출 상실
- ❌ 드래그가 귀찮을 수 있음

---

### 방법 4: 빠른 이동 단축키 ⭐ (추천 보조 수단)

**개념:**
- 숫자 키 1~5로 빠른 이동
- 또는 화면 하단에 작은 아이콘 버튼 5개

**장점:**
- ✅ 빠른 접근
- ✅ 다른 방법과 병행 가능

**단점:**
- ❌ PC 전용 (모바일에서는 터치 UI 필요)

**UI 예시:**
```
화면 하단:
┌─────────────────────────────┐
│                             │
│      [브래킷 뷰]              │
│                             │
└─────────────────────────────┘
  [1] [2] [3] [4] [5👑]
```

---

## 🎯 최종 추천: 방법 1 + 방법 4 조합

### 기본: 줌아웃 상태에서 클릭 선택
### 보조: 화면 하단 빠른 선택 버튼

---

## 🔧 구현: 방법 1 (클릭 가능한 투명 버튼)

### 수정할 코드

현재 로직:
```
줌아웃: 실루엣 표시 ✓, 버튼 숨김 ✗
줌인: 실루엣 숨김 ✗, 버튼 표시 ✓
```

**Free Mode 전용 로직 추가:**
```
Free Mode + 줌아웃:
  - 실루엣 표시 ✓
  - 버튼 투명하게 유지 (Alpha = 0)
  - 클릭은 가능 (Button.interactable = true)

Story Mode + 줌아웃:
  - 실루엣 표시 ✓
  - 버튼 완전히 숨김 (SetActive(false))
```

### TournamentMapManager.cs 수정

```csharp
void ShowButtons(bool show)
{
    // Free Mode 전용: 줌아웃 시에도 투명 버튼으로 클릭 가능
    bool isFreeMode = GameModeManager.Instance.currentMode == GameMode.FreeMode;
    bool isZoomOut = selectedRoundIndex == -1;

    for (int i = 0; i < roundButtons.Length; i++)
    {
        if (roundButtons[i] != null && roundButtons[i].gameObject != null)
        {
            if (isFreeMode && !show && isZoomOut)
            {
                // Free Mode 줌아웃: 투명하게만 (클릭 가능)
                roundButtons[i].gameObject.SetActive(true);
                SetButtonAlpha(roundButtons[i], 0f); // 투명
            }
            else if (show)
            {
                // 줌인: 정상 표시
                roundButtons[i].gameObject.SetActive(true);
                SetButtonAlpha(roundButtons[i], 1f); // 불투명
            }
            else
            {
                // Story Mode 줌아웃: 완전 숨김
                roundButtons[i].gameObject.SetActive(false);
            }
        }
    }
}

/// <summary>
/// 버튼의 투명도 설정 (클릭은 유지)
/// </summary>
void SetButtonAlpha(RoundButton button, float alpha)
{
    // 버튼의 Image 컴포넌트들의 Alpha 조정
    Image[] images = button.GetComponentsInChildren<Image>();
    foreach (Image img in images)
    {
        Color c = img.color;
        c.a = alpha;
        img.color = c;
    }

    // CanvasGroup 사용 (더 간단한 방법)
    CanvasGroup canvasGroup = button.GetComponent<CanvasGroup>();
    if (canvasGroup == null)
    {
        canvasGroup = button.gameObject.AddComponent<CanvasGroup>();
    }
    canvasGroup.alpha = alpha;
    canvasGroup.interactable = true; // 클릭은 항상 가능
    canvasGroup.blocksRaycasts = true; // 레이캐스트 차단 유지
}
```

---

## 🎨 UI 개선: 방법 4 (빠른 선택 버튼 추가)

### Unity 설정

```
UICanvas (Screen Space - Overlay)
├── RoundInfoPanel
└── QuickSelectPanel ⭐ 새로 추가
    ├── Background (어두운 반투명)
    └── QuickButtons (Horizontal Layout Group)
        ├── QuickButton1 (Round 1)
        ├── QuickButton2 (Round 2)
        ├── QuickButton3 (Round 3)
        ├── QuickButton4 (Round 4)
        └── QuickButton5 (Round 5 👑)
```

**QuickSelectPanel 설정:**
```
위치: 화면 하단 중앙
Anchor: Bottom Center
Pos Y: 50
Width: 600, Height: 80
```

**QuickButton 예시:**
```
크기: 100 x 60
텍스트: "R1", "R2", "R3", "R4", "👑"
배경: 어두운 회색
클릭 시: 해당 라운드로 줌인
```

### QuickSelectPanel.cs (새 스크립트)

```csharp
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Free Mode 전용 빠른 라운드 선택 패널
/// </summary>
public class QuickSelectPanel : MonoBehaviour
{
    public Button[] quickButtons = new Button[5];
    private TournamentMapManager mapManager;

    void Start()
    {
        mapManager = FindObjectOfType<TournamentMapManager>();

        // 버튼 이벤트 연결
        for (int i = 0; i < 5; i++)
        {
            int index = i;
            quickButtons[i].onClick.AddListener(() => OnQuickButtonClicked(index));
        }

        // Free Mode에서만 표시
        UpdateVisibility();
    }

    void UpdateVisibility()
    {
        bool isFreeMode = GameModeManager.Instance.currentMode == GameMode.FreeMode;
        gameObject.SetActive(isFreeMode);
    }

    void OnQuickButtonClicked(int roundIndex)
    {
        Debug.Log($"[QuickSelect] Round {roundIndex + 1} 선택!");

        // TournamentMapManager의 OnRoundButtonClicked 호출
        // (public으로 변경 필요)
        mapManager.SelectRound(roundIndex);
    }
}
```

### TournamentMapManager.cs 수정

```csharp
// OnRoundButtonClicked를 public으로 변경하거나 새 메서드 추가
public void SelectRound(int roundIndex)
{
    OnRoundButtonClicked(roundIndex);
}
```

---

## 🎮 최종 Free Mode 사용자 경험

### 시나리오 1: 클릭 선택
```
1. Free Mode 선택
2. 토너먼트 맵 로드 (줌아웃 상태)
3. 전체 브래킷 실루엣 보임
4. 사용자가 "Round 3 영역"을 클릭
   → 투명 버튼이 클릭 감지
5. Round 3으로 줌인
6. 실루엣 사라지고 Round 3 버튼 표시
7. Start Battle 또는 Back
```

### 시나리오 2: 빠른 선택
```
1. Free Mode 선택
2. 토너먼트 맵 로드
3. 화면 하단에 [1][2][3][4][5] 버튼 표시
4. 사용자가 [5] 버튼 클릭
5. 바로 Round 5로 줌인
```

---

## 🎨 시각적 개선 아이디어

### 1. 호버 효과 (PC)
```
Free Mode 줌아웃 상태:
- 마우스를 라운드 위에 올리면
- 해당 실루엣이 밝아짐 (클릭 가능 표시)
```

### 2. 클릭 가능 표시
```
각 라운드 위에 작은 아이콘
예: [👆 Click] 또는 펄스 효과
```

### 3. 미니맵
```
화면 우측 상단에 작은 미니맵
현재 위치와 5개 라운드 표시
클릭하면 해당 위치로 이동
```

---

## 📋 구현 우선순위

### Phase 1 (필수)
- [x] Free Mode에서 모든 라운드 잠금 해제
- [ ] 줌아웃 상태에서 투명 버튼 클릭 가능 (방법 1)
- [ ] Free Mode 전용 로직 추가

### Phase 2 (권장)
- [ ] 화면 하단 빠른 선택 버튼 (방법 4)
- [ ] QuickSelectPanel 구현
- [ ] Free Mode에서만 표시

### Phase 3 (선택)
- [ ] 호버 효과 추가
- [ ] 클릭 가능 시각적 힌트
- [ ] 미니맵 또는 사이드 메뉴

---

## 💡 추가 고려사항

### 모바일 지원
- 터치 인터랙션도 동일하게 작동
- 빠른 선택 버튼이 더 유용

### 키보드 단축키 (PC)
```
키보드 1~5: 각 라운드로 이동
ESC: 줌아웃 (전체 뷰)
Space: 선택된 라운드 시작
```

---

## ✅ 최종 추천

**방법 1 (투명 버튼 클릭) + 방법 4 (빠른 선택 버튼)**

**이유:**
1. 웅장한 브래킷 연출 유지 (실루엣 표시)
2. 직관적인 클릭 선택 (투명 버튼)
3. 빠른 접근성 (하단 버튼)
4. 추가 개발 비용 낮음

```
┌─────────────────────────────────┐
│                                 │
│     [전체 브래킷 실루엣]           │
│     (클릭 가능 - 투명 버튼)        │
│                                 │
│                                 │
└─────────────────────────────────┘
      [1] [2] [3] [4] [5👑]
      ↑ 빠른 선택 버튼
```

이 방식이 가장 **사용자 친화적**이면서 **토너먼트 분위기**를 유지합니다!

---

완성! 어떤 방법이 가장 마음에 드시나요? 🎮
