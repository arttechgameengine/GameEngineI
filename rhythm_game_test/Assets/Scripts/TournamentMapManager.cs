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

    void Start()
    {
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
}
