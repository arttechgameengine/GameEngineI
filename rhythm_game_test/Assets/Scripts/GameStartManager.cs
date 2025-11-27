using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// 게임 시작 화면 관리
/// Story Mode / Free Mode 선택
/// </summary>
public class GameStartManager : MonoBehaviour
{
    [Header("Main Menu")]
    public GameObject mainMenuPanel;
    public Button startButton;
    public Button quitButton;

    [Header("Mode Selection")]
    public GameObject modeSelectionPanel;
    public Button storyModeButton;
    public Button freeModeButton;
    public Button backButton;

    void Start()
    {
        // 버튼 이벤트 연결
        startButton.onClick.AddListener(OnStartClicked);
        quitButton.onClick.AddListener(OnQuitClicked);

        storyModeButton.onClick.AddListener(OnStoryModeClicked);
        freeModeButton.onClick.AddListener(OnFreeModeClicked);
        backButton.onClick.AddListener(OnBackClicked);

        // 초기 상태: 메인 메뉴 표시
        ShowMainMenu();
    }

    /// <summary>
    /// 메인 메뉴 표시
    /// </summary>
    void ShowMainMenu()
    {
        mainMenuPanel.SetActive(true);
        modeSelectionPanel.SetActive(false);
    }

    /// <summary>
    /// 모드 선택 화면 표시
    /// </summary>
    void ShowModeSelection()
    {
        mainMenuPanel.SetActive(false);
        modeSelectionPanel.SetActive(true);
    }

    /// <summary>
    /// Start 버튼 클릭
    /// </summary>
    void OnStartClicked()
    {
        Debug.Log("[GameStart] Start 버튼 클릭!");
        ShowModeSelection();
    }

    /// <summary>
    /// Story Mode 버튼 클릭
    /// </summary>
    void OnStoryModeClicked()
    {
        Debug.Log("[GameStart] Story Mode 선택!");
        GameModeManager.Instance.SetGameMode(GameMode.StoryMode);
        LoadTournamentMap();
    }

    /// <summary>
    /// Free Mode 버튼 클릭
    /// </summary>
    void OnFreeModeClicked()
    {
        Debug.Log("[GameStart] Free Mode 선택!");
        GameModeManager.Instance.SetGameMode(GameMode.FreeMode);
        LoadTournamentMap();
    }

    /// <summary>
    /// Back 버튼 클릭
    /// </summary>
    void OnBackClicked()
    {
        Debug.Log("[GameStart] Back 버튼 클릭!");
        ShowMainMenu();
    }

    /// <summary>
    /// Quit 버튼 클릭
    /// </summary>
    void OnQuitClicked()
    {
        Debug.Log("[GameStart] Quit 버튼 클릭!");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    /// <summary>
    /// 토너먼트 맵 씬 로드
    /// </summary>
    void LoadTournamentMap()
    {
        SceneManager.LoadScene("TournamentMap"); // 토너먼트 맵 씬 이름
    }
}
