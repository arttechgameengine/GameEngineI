using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 게임 모드 선택 팝업
/// Story Mode / Free Mode 선택
/// </summary>
public class GameModeSelectionPopup : MonoBehaviour
{
    [Header("Popup Panel")]
    public GameObject popupPanel; // 팝업 전체 패널
    public GameObject backgroundOverlay; // 어두운 배경 (선택사항)

    [Header("Buttons")]
    public Button storyModeButton; // Story Mode 버튼
    public Button freeModeButton; // Free Mode 버튼
    public Button closeButton; // 닫기 버튼 (X)

    [Header("Descriptions (Optional)")]
    public TextMeshProUGUI storyModeDescription; // Story Mode 설명
    public TextMeshProUGUI freeModeDescription; // Free Mode 설명

    void Start()
    {
        // 버튼 이벤트 설정
        if (storyModeButton != null)
            storyModeButton.onClick.AddListener(OnStoryModeClicked);

        if (freeModeButton != null)
            freeModeButton.onClick.AddListener(OnFreeModeClicked);

        if (closeButton != null)
            closeButton.onClick.AddListener(ClosePopup);

        // 배경 클릭 시 닫기 (선택사항)
        if (backgroundOverlay != null)
        {
            Button bgButton = backgroundOverlay.GetComponent<Button>();
            if (bgButton == null)
            {
                bgButton = backgroundOverlay.AddComponent<Button>();
            }
            bgButton.onClick.AddListener(ClosePopup);

            // 배경 투명도 설정
            Image bgImage = backgroundOverlay.GetComponent<Image>();
            if (bgImage != null)
            {
                Color bgColor = bgImage.color;
                bgColor.a = 0.7f; // 반투명 어두운 배경
                bgImage.color = bgColor;
            }
        }

        // 설명 텍스트 설정 (옵션)
        if (storyModeDescription != null)
        {
            storyModeDescription.text = "순차적으로 라운드를 진행하는\n스토리 모드";
        }

        if (freeModeDescription != null)
        {
            freeModeDescription.text = "모든 라운드를 자유롭게\n선택할 수 있는 연습 모드";
        }

        // 초기 상태: 팝업 숨김
        HidePopup();
    }

    /// <summary>
    /// 팝업 표시
    /// </summary>
    public void ShowPopup()
    {
        if (popupPanel != null)
            popupPanel.SetActive(true);

        if (backgroundOverlay != null)
            backgroundOverlay.SetActive(true);

        Debug.Log("[GameModeSelection] 팝업 열림");
    }

    /// <summary>
    /// 팝업 숨김
    /// </summary>
    public void HidePopup()
    {
        if (popupPanel != null)
            popupPanel.SetActive(false);

        if (backgroundOverlay != null)
            backgroundOverlay.SetActive(false);

        Debug.Log("[GameModeSelection] 팝업 닫힘");
    }

    /// <summary>
    /// 팝업 닫기
    /// </summary>
    public void ClosePopup()
    {
        HidePopup();
    }

    /// <summary>
    /// Story Mode 선택
    /// </summary>
    void OnStoryModeClicked()
    {
        Debug.Log("[GameModeSelection] Story Mode 선택");

        // GameModeManager가 있으면 사용, 없으면 생성
        if (GameModeManager.Instance == null)
        {
            // GameModeManager가 없으면 생성
            GameObject gmManager = new GameObject("GameModeManager");
            gmManager.AddComponent<GameModeManager>();
        }

        // Story Mode 시작
        GameModeManager.Instance.StartStoryMode();
    }

    /// <summary>
    /// Free Mode 선택
    /// </summary>
    void OnFreeModeClicked()
    {
        Debug.Log("[GameModeSelection] Free Mode 선택");

        // GameModeManager가 있으면 사용, 없으면 생성
        if (GameModeManager.Instance == null)
        {
            // GameModeManager가 없으면 생성
            GameObject gmManager = new GameObject("GameModeManager");
            gmManager.AddComponent<GameModeManager>();
        }

        // Free Mode 시작
        GameModeManager.Instance.StartFreeMode();
    }
}
