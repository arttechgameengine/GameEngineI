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
    public CinemachineCamera[] roundCameras = new CinemachineCamera[5]; // 각 라운드별 카메라
    public PolygonCollider2D[] roundBoundaries = new PolygonCollider2D[5]; // 각 라운드 카메라의 Confiner 경계

    [Header("Tournament Silhouettes")]
    public GameObject tournamentSilhouettesContainer; // 전체 브래킷 실루엣 컨테이너 (줌아웃 시에만 표시, 실제 5라운드 제외한 더미들)

    [Header("Round Buttons")]
    public RoundButton[] roundButtons = new RoundButton[5]; // 각 라운드 버튼

    [Header("Navigation UI")]
    public Button leftArrowButton; // 이전 라운드로
    public Button rightArrowButton; // 다음 라운드로
    public Text currentRoundIndicator; // 현재 라운드 표시 (예: "Round 1/5")

    [Header("Round Info UI")]
    public GameObject roundInfoPanel; // 라운드 정보 패널
    public Text roundNameText;
    public Text enemyNameText;
    public Image enemyPortraitImage;
    public Text difficultyText; // 난이도 표시
    public Text storyText;
    public Button startBattleButton;

    private int currentRoundIndex = 0; // 현재 보고 있는 라운드

    void Start()
    {
        // 각 라운드 카메라에 Confiner 설정
        SetupRoundCameraConfiners();

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
        leftArrowButton.onClick.AddListener(OnPreviousRound);
        rightArrowButton.onClick.AddListener(OnNextRound);
        startBattleButton.onClick.AddListener(OnStartBattle);

        // Story Mode: 현재 진행 중인 라운드로 시작
        // Free Mode: Round 1부터 시작
        if (GameModeManager.Instance.currentMode == GameMode.StoryMode)
        {
            currentRoundIndex = GameModeManager.Instance.currentStoryRound;
        }
        else
        {
            currentRoundIndex = 0;
        }

        // 초기 라운드로 이동
        ShowRound(currentRoundIndex);
    }

    /// <summary>
    /// 각 라운드 카메라에 Confiner 설정
    /// </summary>
    void SetupRoundCameraConfiners()
    {
        for (int i = 0; i < roundCameras.Length; i++)
        {
            if (roundCameras[i] != null && roundBoundaries[i] != null)
            {
                CinemachineConfiner2D confiner = roundCameras[i].GetComponent<CinemachineConfiner2D>();
                if (confiner == null)
                {
                    confiner = roundCameras[i].gameObject.AddComponent<CinemachineConfiner2D>();
                }
                confiner.BoundingShape2D = roundBoundaries[i];
                Debug.Log($"[Tournament] Round {i + 1} 카메라 Confiner 설정 완료");
            }
        }
    }

    /// <summary>
    /// 특정 라운드 표시 (카메라 전환 + UI 업데이트)
    /// </summary>
    void ShowRound(int roundIndex)
    {
        currentRoundIndex = roundIndex;

        // 카메라 우선순위 설정
        for (int i = 0; i < roundCameras.Length; i++)
        {
            roundCameras[i].Priority.Value = (i == roundIndex) ? 10 : 0;
        }

        // 라운드 정보 표시
        ShowRoundInfo(roundIndex);

        // 화살표 버튼 활성화 상태 업데이트
        UpdateNavigationButtons();

        // 현재 라운드 표시 업데이트
        if (currentRoundIndicator != null)
        {
            currentRoundIndicator.text = $"Round {roundIndex + 1} / 5";
        }

        // 실루엣 숨김, 버튼 표시
        ShowSilhouettes(false);
        ShowButtons(true);

        Debug.Log($"[Tournament] Round {roundIndex + 1} 표시");
    }

    /// <summary>
    /// 라운드 버튼 클릭 시 (사용 안 함 - 화살표로 대체)
    /// </summary>
    void OnRoundButtonClicked(int roundIndex)
    {
        // 잠긴 라운드는 선택 불가
        if (GameModeManager.Instance.IsRoundLocked(roundIndex))
        {
            Debug.Log($"[Tournament] Round {roundIndex + 1}은 잠겨있습니다!");
            return;
        }

        // 해당 라운드로 이동
        ShowRound(roundIndex);
    }

    /// <summary>
    /// 이전 라운드로 이동
    /// </summary>
    void OnPreviousRound()
    {
        int prevIndex = currentRoundIndex - 1;

        // Story Mode: 잠긴 라운드로는 이동 불가
        if (GameModeManager.Instance.currentMode == GameMode.StoryMode)
        {
            if (prevIndex < 0 || GameModeManager.Instance.IsRoundLocked(prevIndex))
            {
                Debug.Log("[Tournament] 이전 라운드로 이동할 수 없습니다.");
                return;
            }
        }
        else // Free Mode: 범위 체크만
        {
            if (prevIndex < 0)
            {
                Debug.Log("[Tournament] 첫 번째 라운드입니다.");
                return;
            }
        }

        ShowRound(prevIndex);
    }

    /// <summary>
    /// 다음 라운드로 이동
    /// </summary>
    void OnNextRound()
    {
        int nextIndex = currentRoundIndex + 1;

        // Story Mode: 잠긴 라운드로는 이동 불가
        if (GameModeManager.Instance.currentMode == GameMode.StoryMode)
        {
            if (nextIndex >= 5 || GameModeManager.Instance.IsRoundLocked(nextIndex))
            {
                Debug.Log("[Tournament] 다음 라운드로 이동할 수 없습니다. (잠김)");
                return;
            }
        }
        else // Free Mode: 범위 체크만
        {
            if (nextIndex >= 5)
            {
                Debug.Log("[Tournament] 마지막 라운드입니다.");
                return;
            }
        }

        ShowRound(nextIndex);
    }

    /// <summary>
    /// 화살표 버튼 활성화 상태 업데이트
    /// </summary>
    void UpdateNavigationButtons()
    {
        if (GameModeManager.Instance.currentMode == GameMode.StoryMode)
        {
            // Story Mode: 잠금 상태 고려
            leftArrowButton.interactable = (currentRoundIndex > 0 && !GameModeManager.Instance.IsRoundLocked(currentRoundIndex - 1));
            rightArrowButton.interactable = (currentRoundIndex < 4 && !GameModeManager.Instance.IsRoundLocked(currentRoundIndex + 1));
        }
        else
        {
            // Free Mode: 범위만 체크
            leftArrowButton.interactable = (currentRoundIndex > 0);
            rightArrowButton.interactable = (currentRoundIndex < 4);
        }
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

        // 난이도 표시
        if (difficultyText != null)
        {
            string difficultyStr = GetDifficultyString(round.difficulty);
            Color difficultyColor = GetDifficultyColor(round.difficulty);
            difficultyText.text = $"난이도: {difficultyStr}";
            difficultyText.color = difficultyColor;
        }

        roundInfoPanel.SetActive(true);
    }

    /// <summary>
    /// 난이도를 한글로 변환
    /// </summary>
    string GetDifficultyString(RoundData.Difficulty difficulty)
    {
        switch (difficulty)
        {
            case RoundData.Difficulty.Easy: return "쉬움";
            case RoundData.Difficulty.Normal: return "보통";
            case RoundData.Difficulty.Hard: return "어려움";
            case RoundData.Difficulty.VeryHard: return "매우 어려움";
            default: return "보통";
        }
    }

    /// <summary>
    /// 난이도별 색상
    /// </summary>
    Color GetDifficultyColor(RoundData.Difficulty difficulty)
    {
        switch (difficulty)
        {
            case RoundData.Difficulty.Easy: return new Color(0.5f, 1f, 0.5f); // 초록
            case RoundData.Difficulty.Normal: return new Color(1f, 1f, 0.5f); // 노랑
            case RoundData.Difficulty.Hard: return new Color(1f, 0.6f, 0.3f); // 주황
            case RoundData.Difficulty.VeryHard: return new Color(1f, 0.3f, 0.3f); // 빨강
            default: return Color.white;
        }
    }

    /// <summary>
    /// 전투 시작 버튼
    /// </summary>
    void OnStartBattle()
    {
        RoundData round = GameModeManager.Instance.allRounds[currentRoundIndex];

        Debug.Log($"[Tournament] Round {currentRoundIndex + 1} 전투 시작! → {round.sceneName}");

        // 선택된 라운드 인덱스 저장 (진행도 추적용)
        PlayerPrefs.SetInt("SelectedRound", currentRoundIndex);
        PlayerPrefs.Save();

        // 해당 라운드의 전용 씬으로 이동
        if (!string.IsNullOrEmpty(round.sceneName))
        {
            SceneFader.LoadScene(round.sceneName);
        }
        else
        {
            Debug.LogError($"[Tournament] Round {currentRoundIndex + 1}의 씬 이름이 설정되지 않았습니다!");
        }
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
