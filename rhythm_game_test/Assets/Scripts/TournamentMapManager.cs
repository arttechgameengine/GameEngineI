using UnityEngine;
using UnityEngine.UI;
using Unity.Cinemachine;
using UnityEngine.SceneManagement;

/// <summary>
/// 토너먼트 맵 관리
/// Cinemachine으로 라운드 적에게 줌인/줌아웃
/// </summary>
public class TournamentMapManager : MonoBehaviour
{
    [Header("Cinemachine")]
    public CinemachineCamera mainCamera; // 전체 맵 보기용 카메라
    public CinemachineCamera[] roundCameras = new CinemachineCamera[5]; // 각 라운드별 카메라

    [Header("Camera Constraints")]
    public PolygonCollider2D mapBoundary; // 전체 맵 경계 (Cinemachine Confiner용)
    public PolygonCollider2D[] progressiveBoundaries = new PolygonCollider2D[5]; // 각 진행도별 경계

    [Header("Tournament Silhouettes")]
    public GameObject tournamentSilhouettesContainer; // 전체 브래킷 실루엣 컨테이너 (줌아웃 시에만 표시, 실제 5라운드 제외한 더미들)

    [Header("Round Buttons")]
    public RoundButton[] roundButtons = new RoundButton[5]; // 각 라운드 버튼

    [Header("UI")]
    public GameObject roundInfoPanel; // 라운드 정보 패널
    public Text roundNameText;
    public Text enemyNameText;
    public Image enemyPortraitImage;
    public Text storyText;
    public Button startBattleButton;
    public Button backButton;

    private int selectedRoundIndex = -1;
    private CinemachineConfiner2D mainCameraConfiner; // 메인 카메라의 Confiner 컴포넌트

    void Start()
    {
        // Confiner 컴포넌트 가져오기 또는 추가
        mainCameraConfiner = mainCamera.GetComponent<CinemachineConfiner2D>();
        if (mainCameraConfiner == null)
        {
            mainCameraConfiner = mainCamera.gameObject.AddComponent<CinemachineConfiner2D>();
        }

        // 초기화: 전체 맵 보기
        ShowFullMap();

        // 라운드 버튼 설정
        for (int i = 0; i < 5; i++)
        {
            int index = i; // 클로저 문제 방지
            roundButtons[i].Setup(i, GameModeManager.Instance.allRounds[i]);
            roundButtons[i].onClicked += () => OnRoundButtonClicked(index);

            // 잠금 상태 설정
            bool isLocked = GameModeManager.Instance.IsRoundLocked(i);
            roundButtons[i].SetLocked(isLocked);
        }

        // UI 버튼 설정
        startBattleButton.onClick.AddListener(OnStartBattle);
        backButton.onClick.AddListener(OnBack);

        // 초기 상태: 정보 패널 숨김
        roundInfoPanel.SetActive(false);

        // 카메라 경계 설정 (현재 진행도에 맞춰)
        UpdateCameraBoundary();

        // 초기 상태: 실루엣 표시, 버튼 숨김
        ShowSilhouettes(true);
        ShowButtons(false);
    }

    /// <summary>
    /// 전체 맵 보기
    /// </summary>
    void ShowFullMap()
    {
        mainCamera.Priority.Value = 10;
        foreach (var cam in roundCameras)
        {
            cam.Priority.Value = 0;
        }
        selectedRoundIndex = -1;
        roundInfoPanel.SetActive(false);

        // 줌아웃 시: 실루엣 표시, 버튼 숨김
        ShowSilhouettes(true);
        ShowButtons(false);
    }

    /// <summary>
    /// 라운드 버튼 클릭 시
    /// </summary>
    void OnRoundButtonClicked(int roundIndex)
    {
        // 잠긴 라운드는 선택 불가
        if (GameModeManager.Instance.IsRoundLocked(roundIndex))
        {
            Debug.Log($"[Tournament] Round {roundIndex + 1}은 잠겨있습니다!");
            return;
        }

        selectedRoundIndex = roundIndex;

        // 해당 라운드 카메라로 줌인
        ZoomToRound(roundIndex);

        // 라운드 정보 표시
        ShowRoundInfo(roundIndex);
    }

    /// <summary>
    /// 라운드로 줌인 (Cinemachine)
    /// </summary>
    void ZoomToRound(int roundIndex)
    {
        Debug.Log($"[Tournament] Round {roundIndex + 1}로 줌인!");

        // 메인 카메라 우선순위 낮춤
        mainCamera.Priority.Value = 0;

        // 선택된 라운드 카메라 우선순위 높임
        for (int i = 0; i < roundCameras.Length; i++)
        {
            roundCameras[i].Priority.Value = (i == roundIndex) ? 10 : 0;
        }

        // 줌인 시: 실루엣 숨김, 버튼 표시
        ShowSilhouettes(false);
        ShowButtons(true);
    }

    /// <summary>
    /// 라운드 정보 표시
    /// </summary>
    void ShowRoundInfo(int roundIndex)
    {
        RoundData round = GameModeManager.Instance.allRounds[roundIndex];

        roundNameText.text = round.roundName;
        enemyNameText.text = round.enemyName;
        enemyPortraitImage.sprite = round.enemyPortrait;
        storyText.text = round.storyText;

        roundInfoPanel.SetActive(true);
    }

    /// <summary>
    /// 전투 시작 버튼
    /// </summary>
    void OnStartBattle()
    {
        if (selectedRoundIndex < 0) return;

        Debug.Log($"[Tournament] Round {selectedRoundIndex + 1} 전투 시작!");

        // 선택된 라운드 데이터를 NoteSpawner에 전달하기 위해 저장
        PlayerPrefs.SetInt("SelectedRound", selectedRoundIndex);
        PlayerPrefs.Save();

        // 게임 씬으로 이동
        SceneManager.LoadScene("GameScene"); // 실제 게임 씬 이름으로 변경
    }

    /// <summary>
    /// 뒤로 가기 버튼
    /// </summary>
    void OnBack()
    {
        // 줌아웃하여 전체 맵 보기
        ShowFullMap();
    }

    /// <summary>
    /// 카메라 경계 업데이트
    /// Story Mode: 현재 라운드까지만 탐색 가능
    /// Free Mode: 전체 맵 탐색 가능
    /// </summary>
    void UpdateCameraBoundary()
    {
        if (mainCameraConfiner == null)
        {
            Debug.LogWarning("[Tournament] Confiner가 없습니다!");
            return;
        }

        // Free Mode에서는 전체 맵 경계 사용
        if (GameModeManager.Instance.currentMode == GameMode.FreeMode)
        {
            if (mapBoundary != null)
            {
                mainCameraConfiner.BoundingShape2D = mapBoundary;
                Debug.Log("[Tournament] Free Mode - 전체 맵 탐색 가능");
            }
            return;
        }

        // Story Mode에서는 현재 진행도에 맞는 경계 사용
        int currentRound = GameModeManager.Instance.currentStoryRound;

        if (progressiveBoundaries != null && currentRound < progressiveBoundaries.Length)
        {
            PolygonCollider2D boundary = progressiveBoundaries[currentRound];
            if (boundary != null)
            {
                mainCameraConfiner.BoundingShape2D = boundary;
                Debug.Log($"[Tournament] Story Mode - Round {currentRound + 1}까지 탐색 가능");
            }
            else
            {
                Debug.LogWarning($"[Tournament] Round {currentRound + 1}의 경계가 설정되지 않았습니다!");
            }
        }
    }

    /// <summary>
    /// 게임 모드 변경 시 호출 (외부에서 호출 가능)
    /// </summary>
    public void RefreshCameraBoundary()
    {
        UpdateCameraBoundary();
    }

    /// <summary>
    /// 토너먼트 브래킷 실루엣 표시/숨김
    /// 전체 브래킷 구조를 보여주는 더미 실루엣들 (실제 5라운드 제외)
    /// </summary>
    void ShowSilhouettes(bool show)
    {
        if (tournamentSilhouettesContainer != null)
        {
            tournamentSilhouettesContainer.SetActive(show);
            Debug.Log($"[Tournament] 브래킷 실루엣 {(show ? "표시" : "숨김")}");
        }
    }

    /// <summary>
    /// 라운드 버튼 표시/숨김
    /// </summary>
    void ShowButtons(bool show)
    {
        for (int i = 0; i < roundButtons.Length; i++)
        {
            if (roundButtons[i] != null && roundButtons[i].gameObject != null)
            {
                roundButtons[i].gameObject.SetActive(show);
            }
        }
        Debug.Log($"[Tournament] 버튼 {(show ? "표시" : "숨김")}");
    }
}
