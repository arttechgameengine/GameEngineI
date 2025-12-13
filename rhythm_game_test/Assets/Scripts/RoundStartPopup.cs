using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// 페이지 데이터 구조 (각 페이지에 표시할 UI 요소들)
/// </summary>
[System.Serializable]
public class PopupPage
{
    [Tooltip("이 페이지의 모든 UI 요소들 (GameObject, TextMeshPro, Image 등)")]
    public List<GameObject> pageElements = new List<GameObject>();
}


/// <summary>
/// 라운드 시작 전 팝업 (fade in 후 표시, 닫으면 게임 시작)
/// 다중 페이지 지원 (노트 설명, 기믹 설명 등)
/// </summary>
public class RoundStartPopup : MonoBehaviour
{
    public static RoundStartPopup Instance { get; private set; }

    [Header("UI References")]
    public GameObject popupPanel;       // 팝업 전체 Panel
        public List<PopupPage> pages = new List<PopupPage>();  // 페이지 리스트 (각 페이지는 여러 UI 요소 포함)

    [Header("Navigation Buttons")]
    public Button nextButton;           // 다음 페이지 버튼
    public Button prevButton;           // 이전 페이지 버튼
    public Button closeButton;          // 닫기 버튼 (마지막 페이지에서만 표시)

    [Header("Page Indicator")]
    public Transform indicatorContainer; // 인디케이터들이 들어갈 Container
    public GameObject indicatorPrefab;   // 원 Prefab (Image)
    public Color activeColor = Color.white;   // 현재 페이지 색상
    public Color inactiveColor = new Color(1f, 1f, 1f, 0.3f); // 다른 페이지 색상

    // 팝업이 닫혔는지 여부
    public static bool IsPopupClosed { get; private set; } = false;

    private int currentPageIndex = 0;
    private List<Image> indicators = new List<Image>(); // 생성된 인디케이터들

    void Awake()
    {
        Instance = this;

        // 처음에는 숨겨둠
        if (popupPanel != null)
        {
            popupPanel.SetActive(false);
        }

        // 버튼 이벤트 연결
        if (nextButton != null)
        {
            nextButton.onClick.AddListener(NextPage);
        }

        if (prevButton != null)
        {
            prevButton.onClick.AddListener(PrevPage);
        }

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(ClosePopup);
        }
    }

    /// <summary>
    /// 팝업 표시 (RoundManager에서 호출) - UI만 준비, Time.timeScale은 나중에
    /// </summary>
    public void ShowPopup()
    {
        if (popupPanel == null) return;

        // 팝업 미닫힘 상태로 리셋
        IsPopupClosed = false;

        // 페이지 인디케이터 생성
        CreateIndicators();

        // 첫 페이지로 초기화
        currentPageIndex = 0;
        ShowCurrentPage();

        // 팝업 활성화 (Time.timeScale은 아직 건드리지 않음)
        popupPanel.SetActive(true);

        Debug.Log("[RoundStartPopup] 팝업 UI 표시 (Time.timeScale은 아직 건드리지 않음)");
    }

    /// <summary>
    /// 게임 일시정지 (Fade In 완료 후 호출)
    /// </summary>
    public void PauseGame()
    {
        Time.timeScale = 0f;
        Debug.Log("[RoundStartPopup] Time.timeScale = 0 (게임 일시정지)");
    }

    /// <summary>
    /// 현재 페이지 표시
    /// </summary>
    void ShowCurrentPage()
    {
        if (pages == null || pages.Count == 0)
        {
            Debug.LogWarning("[RoundStartPopup] 페이지가 설정되지 않았습니다!");
            return;
        }

        // 모든 페이지의 모든 요소 비활성화 후, 현재 페이지만 활성화
        for (int i = 0; i < pages.Count; i++)
        {
            bool isCurrentPage = (i == currentPageIndex);

            // 각 페이지의 모든 UI 요소들을 켜거나 끔
            foreach (GameObject element in pages[i].pageElements)
            {
                if (element != null)
                {
                    element.SetActive(isCurrentPage);
                }
            }
        }

        // 버튼 상태 업데이트
        UpdateNavigationButtons();

        // 페이지 인디케이터 업데이트
        UpdateIndicators();

        Debug.Log($"[RoundStartPopup] 페이지 {currentPageIndex + 1}/{pages.Count} 표시");
    }

    /// <summary>
    /// 페이지 인디케이터 생성
    /// </summary>
    void CreateIndicators()
    {
        if (indicatorContainer == null || indicatorPrefab == null || pages == null) return;

        // 기존 인디케이터 삭제
        foreach (var indicator in indicators)
        {
            if (indicator != null)
            {
                Destroy(indicator.gameObject);
            }
        }
        indicators.Clear();

        // 페이지 수만큼 인디케이터 생성
        for (int i = 0; i < pages.Count; i++)
        {
            GameObject indicatorObj = Instantiate(indicatorPrefab, indicatorContainer);
            Image indicatorImage = indicatorObj.GetComponent<Image>();

            if (indicatorImage != null)
            {
                indicators.Add(indicatorImage);
            }
        }

        Debug.Log($"[RoundStartPopup] {indicators.Count}개 인디케이터 생성");
    }

    /// <summary>
    /// 페이지 인디케이터 업데이트
    /// </summary>
    void UpdateIndicators()
    {
        for (int i = 0; i < indicators.Count; i++)
        {
            if (indicators[i] != null)
            {
                // 현재 페이지는 activeColor, 나머지는 inactiveColor
                indicators[i].color = (i == currentPageIndex) ? activeColor : inactiveColor;
            }
        }
    }

    /// <summary>
    /// 네비게이션 버튼 상태 업데이트
    /// </summary>
    void UpdateNavigationButtons()
    {
        if (pages == null || pages.Count == 0) return;

        bool isFirstPage = (currentPageIndex == 0);
        bool isLastPage = (currentPageIndex == pages.Count - 1);

        // 이전 버튼: 첫 페이지가 아닐 때만 활성화
        if (prevButton != null)
        {
            prevButton.gameObject.SetActive(!isFirstPage);
        }

        // 다음 버튼: 마지막 페이지가 아닐 때만 활성화
        if (nextButton != null)
        {
            nextButton.gameObject.SetActive(!isLastPage);
        }

        // 닫기 버튼: 마지막 페이지일 때만 활성화
        if (closeButton != null)
        {
            closeButton.gameObject.SetActive(isLastPage);
        }
    }

    /// <summary>
    /// 다음 페이지로 이동
    /// </summary>
    public void NextPage()
    {
        if (pages == null || pages.Count == 0) return;

        if (currentPageIndex < pages.Count - 1)
        {
            currentPageIndex++;
            ShowCurrentPage();
            Debug.Log($"[RoundStartPopup] 다음 페이지: {currentPageIndex + 1}/{pages.Count}");
        }
    }

    /// <summary>
    /// 이전 페이지로 이동
    /// </summary>
    public void PrevPage()
    {
        if (pages == null || pages.Count == 0) return;

        if (currentPageIndex > 0)
        {
            currentPageIndex--;
            ShowCurrentPage();
            Debug.Log($"[RoundStartPopup] 이전 페이지: {currentPageIndex + 1}/{pages.Count}");
        }
    }

    /// <summary>
    /// 팝업 닫기 (외부에서 호출 - 버튼 등)
    /// </summary>
    public void ClosePopup()
    {
        if (popupPanel == null) return;

        // 팝업 비활성화
        popupPanel.SetActive(false);

        // 팝업 닫힘 상태로 설정
        IsPopupClosed = true;

        // ⚠️ 게임 재개 (Time.timeScale 복원)
        Time.timeScale = 1f;

        Debug.Log("[RoundStartPopup] 팝업 닫힘 → 게임 시작 허용 (Time.timeScale = 1)");
    }
}
