using UnityEngine;
using UnityEngine.UI;
using Unity.Cinemachine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using TMPro;

/// <summary>
/// Waypoint 배열을 Inspector에서 보이게 하기 위한 Wrapper 클래스
/// </summary>
[System.Serializable]
public class WaypointPath
{
    public Transform[] waypoints;
}

/// <summary>
/// Free Mode 전용 토너먼트 맵 관리
/// 모든 라운드 해금, 자유롭게 선택 가능
/// </summary>
public class FreeModeTournamentManager : MonoBehaviour
{
    [Header("Cinemachine")]
    public CinemachineCamera[] roundCameras = new CinemachineCamera[5]; // 각 라운드별 카메라
    public Transform[] roundCameraTargetPositions = new Transform[5]; // 각 라운드 카메라의 최종 목표 위치 (도착 후 이동할 위치)
    public PolygonCollider2D[] roundBoundaries = new PolygonCollider2D[5]; // 각 라운드 카메라의 Confiner 경계
    public float cameraRepositionDuration = 1.0f; // 도착 후 카메라 재배치 시간 (초)

    [Header("Player Character")]
    public Transform playerCharacter; // 플레이어 캐릭터 (브래킷 따라 이동)
    public SpriteRenderer playerCharacterRenderer; // 주인공 SpriteRenderer (Fade In용)
    public Transform[] roundPositions = new Transform[5]; // 각 라운드별 캐릭터 위치
    public float characterMoveDuration = 1.5f; // 캐릭터 이동 시간 (초)

    [Header("Bracket Path Waypoints")]
    [Tooltip("각 라운드 간 이동 경로의 중간 지점들\n[0]: Round1→2 경로\n[1]: Round2→3 경로\n[2]: Round3→4 경로\n[3]: Round4→5 경로")]
    public WaypointPath[] bracketWaypoints = new WaypointPath[4]; // 라운드 간 경로

    [Header("Round Buttons")]
    public RoundButton[] roundButtons = new RoundButton[5]; // 각 라운드 버튼

    [Header("Navigation UI")]
    public Button leftArrowButton; // 이전 라운드로
    public Button rightArrowButton; // 다음 라운드로
    public TextMeshProUGUI currentRoundIndicator; // 현재 라운드 표시 (예: "Round 1/5")
    public GameObject screenOverlayUICanvas; // Screen Space - Overlay UI 전체 (Intro 후 표시)

    [Header("Round Info UI")]
    public GameObject roundInfoPanel; // 라운드 정보 패널
    public TextMeshProUGUI roundNameText;
    public TextMeshProUGUI enemyNameText;
    public Image enemyPortraitImage;
    public TextMeshProUGUI difficultyText; // 난이도 표시
    public TextMeshProUGUI storyText;
    public Button startBattleButton;
    public Button closeInfoPanelButton; // X 버튼 (패널 닫기)
    public GameObject infoPanelBackground; // 패널 바깥 배경 (클릭 시 닫기)

    [Header("Free Mode UI")]
    public Button backToMenuButton; // 메인 메뉴로 돌아가기

    [Header("Intro Animation")]
    public CinemachineCamera overviewCamera; // 전체 브래킷 보기용 카메라 (인트로 전용)
    public GameObject tournamentSilhouettesContainer; // 토너먼트 브래킷 실루엣 (인트로에만 표시)
    public float introZoomDuration = 2f; // 인트로 줌인 시간 (초)
    public float silhouetteFadeDuration = 0.5f; // 실루엣 페이드아웃 시간 (초)

    private int currentRoundIndex = 0; // 현재 보고 있는 라운드
    private CanvasGroup silhouetteCanvasGroup; // 실루엣 페이드용
    private CanvasGroup screenOverlayCanvasGroup; // Screen Overlay UI 페이드용
    private bool isCharacterMoving = false; // 캐릭터 이동 중 여부

    void Start()
    {
        // 실루엣 CanvasGroup 설정
        SetupSilhouetteCanvasGroup();

        // Screen Overlay UI CanvasGroup 설정
        SetupScreenOverlayCanvasGroup();

        // 주인공 초기 상태: 투명 (Fade In 준비)
        if (playerCharacterRenderer != null)
        {
            Color color = playerCharacterRenderer.color;
            color.a = 0f;
            playerCharacterRenderer.color = color;
        }

        // 각 라운드 카메라에 Confiner 설정
        SetupRoundCameraConfiners();

        // 각 라운드 카메라가 플레이어 캐릭터를 Follow하도록 설정
        SetupCameraFollowTarget();

        // 라운드 버튼 설정
        for (int i = 0; i < 5; i++)
        {
            int index = i; // 클로저 문제 방지
            roundButtons[i].Setup(i, GameModeManager.Instance.allRounds[i]);
            roundButtons[i].onClicked += () => OnRoundButtonClicked(index);

            // Free Mode는 모든 라운드 해금
            roundButtons[i].SetLocked(false);
        }

        // UI 버튼 설정
        leftArrowButton.onClick.AddListener(OnPreviousRound);
        rightArrowButton.onClick.AddListener(OnNextRound);
        startBattleButton.onClick.AddListener(OnStartBattle);

        if (backToMenuButton != null)
        {
            backToMenuButton.onClick.AddListener(OnBackToMenu);
        }

        // Info Panel 닫기 버튼 설정
        if (closeInfoPanelButton != null)
        {
            closeInfoPanelButton.onClick.AddListener(CloseInfoPanel);
        }

        // Info Panel 배경 클릭 시 닫기 (투명 배경)
        if (infoPanelBackground != null)
        {
            // Button 컴포넌트 추가
            Button bgButton = infoPanelBackground.GetComponent<Button>();
            if (bgButton == null)
            {
                bgButton = infoPanelBackground.AddComponent<Button>();
            }
            bgButton.onClick.AddListener(CloseInfoPanel);

            // Image 컴포넌트 확인 및 투명도 설정 (클릭 감지를 위해 필요)
            Image bgImage = infoPanelBackground.GetComponent<Image>();
            if (bgImage != null)
            {
                // 완전 투명하게 설정 (색은 유지, 알파만 0)
                Color transparentColor = bgImage.color;
                transparentColor.a = 0.01f; // 완전히 0이면 클릭이 안 되므로 0.01
                bgImage.color = transparentColor;
            }
        }

        // Info Panel 초기 상태: 닫힘
        if (roundInfoPanel != null)
        {
            roundInfoPanel.SetActive(false);
        }

        // Free Mode: Round 1부터 시작
        currentRoundIndex = 0;

        // 캐릭터를 Round 1 위치로 초기 배치
        if (playerCharacter != null && roundPositions[0] != null)
        {
            playerCharacter.position = roundPositions[0].position;
        }

        // 인트로 애니메이션 시작 (전체 브래킷 → Round 1로 줌인)
        StartCoroutine(PlayIntroAnimation());
    }

    /// <summary>
    /// 실루엣 CanvasGroup 설정 (페이드아웃용)
    /// </summary>
    void SetupSilhouetteCanvasGroup()
    {
        if (tournamentSilhouettesContainer != null)
        {
            silhouetteCanvasGroup = tournamentSilhouettesContainer.GetComponent<CanvasGroup>();
            if (silhouetteCanvasGroup == null)
            {
                // AddComponent 대신 에디터에서 미리 추가하도록 경고
                Debug.LogWarning("[Free Tournament] tournamentSilhouettesContainer에 CanvasGroup 컴포넌트를 미리 추가해주세요!");
                return;
            }
            silhouetteCanvasGroup.alpha = 1f; // 초기 상태: 완전 불투명
        }
    }

    /// <summary>
    /// Screen Overlay UI CanvasGroup 설정 (페이드인용)
    /// </summary>
    void SetupScreenOverlayCanvasGroup()
    {
        if (screenOverlayUICanvas != null)
        {
            screenOverlayCanvasGroup = screenOverlayUICanvas.GetComponent<CanvasGroup>();
            if (screenOverlayCanvasGroup == null)
            {
                // CanvasGroup이 없으면 자동 추가
                screenOverlayCanvasGroup = screenOverlayUICanvas.AddComponent<CanvasGroup>();
                Debug.Log("[Free Tournament] Screen Overlay Canvas에 CanvasGroup 자동 추가됨");
            }
            screenOverlayCanvasGroup.alpha = 0f; // 초기 상태: 완전 투명
        }
    }

    /// <summary>
    /// 인트로 애니메이션: 전체 브래킷 보기 → Round 1로 줌인
    /// </summary>
    System.Collections.IEnumerator PlayIntroAnimation()
    {
        // 1단계: 전체 브래킷 보기 (실루엣 표시)
        if (overviewCamera != null)
        {
            overviewCamera.Priority.Value = 10; // Overview 카메라 활성화
            foreach (var cam in roundCameras)
            {
                cam.Priority.Value = 0;
            }
        }

        if (tournamentSilhouettesContainer != null)
        {
            tournamentSilhouettesContainer.SetActive(true);
        }

        // Screen Overlay UI는 이미 alpha=0으로 숨겨짐 (SetupScreenOverlayCanvasGroup)
        // CanvasGroup으로 처리하므로 SetActive는 그대로 유지
        if (screenOverlayUICanvas != null)
        {
            screenOverlayUICanvas.SetActive(true); // 활성화 상태 유지 (alpha로 투명화)
        }

        // World Space UI도 숨김
        if (roundInfoPanel != null)
            roundInfoPanel.SetActive(false);

        // Round Buttons는 계속 보이게 (Overview에서도 보여야 함)
        ShowButtons(true);

        Debug.Log("[Free Tournament] 인트로 시작 - 전체 브래킷 표시");

        // 2단계: 잠시 대기 (전체 브래킷을 보여줌)
        yield return new WaitForSeconds(2f); // 대기 시간 증가 (1f → 2f)

        // 3단계: Round 1 카메라를 먼저 target position으로 설정
        Debug.Log("[Free Tournament] Round 1 카메라를 target position으로 설정");
        if (roundCameraTargetPositions[0] != null)
        {
            roundCameras[0].Follow = roundCameraTargetPositions[0];
        }

        // 4단계: Round 1로 줌인 시작
        Debug.Log("[Free Tournament] 줌인 시작 - Round 1로");

        // 라운드 카메라로 전환
        if (overviewCamera != null)
        {
            overviewCamera.Priority.Value = 0;
        }
        roundCameras[0].Priority.Value = 10;

        // 줌인하면서 주인공 + Screen Overlay UI Fade In (동시 진행)
        yield return StartCoroutine(FadeInCharacterAndUI());

        // 4단계: 실루엣 페이드아웃
        Debug.Log("[Free Tournament] 실루엣 페이드아웃 시작");

        float fadeElapsed = 0f;
        while (fadeElapsed < silhouetteFadeDuration)
        {
            fadeElapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, fadeElapsed / silhouetteFadeDuration);
            if (silhouetteCanvasGroup != null)
            {
                silhouetteCanvasGroup.alpha = alpha;
            }
            yield return null;
        }

        // 실루엣 완전 숨김
        if (tournamentSilhouettesContainer != null)
        {
            tournamentSilhouettesContainer.SetActive(false);
        }

        // 5단계: 줌인 완료 후 UI 표시
        Debug.Log("[Free Tournament] 줌인 완료 - UI 표시 시작");

        // Screen Overlay UI는 이미 Fade In으로 보임 (alpha = 1)
        // 추가 처리 불필요

        // World Space UI 표시 (RoundInfoPanel은 Round Button 클릭 시에만 열림)
        UpdateNavigationButtons();
        ShowButtons(true);

        if (currentRoundIndicator != null)
        {
            currentRoundIndicator.text = "Round 1 / 5";
        }

        Debug.Log("[Free Tournament] 인트로 완료");
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
                    // AddComponent 대신 에디터에서 미리 추가하도록 경고
                    Debug.LogWarning($"[Free Tournament] Round {i + 1} 카메라에 CinemachineConfiner2D 컴포넌트를 미리 추가해주세요!");
                    continue;
                }
                confiner.BoundingShape2D = roundBoundaries[i];
                Debug.Log($"[Free Tournament] Round {i + 1} 카메라 Confiner 설정 완료");
            }
        }
    }

    /// <summary>
    /// 각 라운드 카메라가 플레이어 캐릭터를 Follow하도록 설정
    /// Priority가 활성화된 카메라만 실제로 보이므로, 모든 카메라가 playerCharacter를 따라가도 문제없음
    /// </summary>
    void SetupCameraFollowTarget()
    {
        if (playerCharacter == null)
        {
            Debug.LogWarning("[Free Tournament] 플레이어 캐릭터가 설정되지 않았습니다.");
            return;
        }

        for (int i = 0; i < roundCameras.Length; i++)
        {
            if (roundCameras[i] != null)
            {
                // 모든 Round Camera가 playerCharacter를 Follow
                // Priority로 하나만 활성화되므로, 활성화된 카메라만 렌더링됨
                roundCameras[i].Follow = playerCharacter;
                Debug.Log($"[Free Tournament] Round {i + 1} 카메라 Follow → playerCharacter 설정 완료");
            }
        }
    }

    /// <summary>
    /// 특정 라운드 표시 (카메라 전환 + UI 업데이트 + 캐릭터 이동)
    /// </summary>
    void ShowRound(int roundIndex)
    {
        if (isCharacterMoving)
        {
            Debug.Log("[Free Tournament] 캐릭터 이동 중에는 라운드를 변경할 수 없습니다.");
            return;
        }

        // 캐릭터 이동 애니메이션 시작
        StartCoroutine(MoveToRound(roundIndex));
    }

    /// <summary>
    /// 캐릭터를 특정 라운드로 이동시키는 애니메이션 (브래킷 경로 따라)
    /// </summary>
    System.Collections.IEnumerator MoveToRound(int targetRoundIndex)
    {
        isCharacterMoving = true;

        // 네비게이션 버튼 비활성화 (이동 중에는 조작 불가)
        leftArrowButton.interactable = false;
        rightArrowButton.interactable = false;

        // 목표 위치 확인
        if (playerCharacter == null || roundPositions[targetRoundIndex] == null)
        {
            Debug.LogError($"[Free Tournament] 캐릭터 또는 Round {targetRoundIndex + 1} 위치가 설정되지 않았습니다!");
            isCharacterMoving = false;
            UpdateNavigationButtons();
            yield break;
        }

        // 이동 중: 목표 라운드 카메라 활성화 + Confiner 비활성화 (boundary 무시)
        for (int i = 0; i < roundCameras.Length; i++)
        {
            roundCameras[i].Priority.Value = (i == targetRoundIndex) ? 10 : 0;

            // Confiner 비활성화 (이동 중에는 boundary 무시)
            if (i == targetRoundIndex)
            {
                CinemachineConfiner2D confiner = roundCameras[i].GetComponent<CinemachineConfiner2D>();
                if (confiner != null)
                {
                    confiner.enabled = false;
                }
            }
        }

        // 이동 경로 구성
        List<Vector3> pathPoints = BuildPath(currentRoundIndex, targetRoundIndex);

        // 경로를 따라 이동
        yield return StartCoroutine(MoveAlongPath(pathPoints));

        // 이동 완료 후: 카메라를 목표 위치로 재배치
        yield return StartCoroutine(RepositionCameraToTarget(targetRoundIndex));

        // 이동 완료 후: Confiner 다시 활성화
        CinemachineConfiner2D targetConfiner = roundCameras[targetRoundIndex].GetComponent<CinemachineConfiner2D>();
        if (targetConfiner != null)
        {
            targetConfiner.enabled = true;
        }

        // 라운드 인덱스 업데이트
        currentRoundIndex = targetRoundIndex;

        // 라운드 정보 패널은 자동으로 열지 않음 (Round Button 클릭 시에만 열림)
        // ShowRoundInfo(targetRoundIndex); // 제거

        // 화살표 버튼 활성화 상태 업데이트
        UpdateNavigationButtons();

        // 현재 라운드 표시 업데이트
        if (currentRoundIndicator != null)
        {
            currentRoundIndicator.text = $"Round {targetRoundIndex + 1} / 5";
        }

        // 라운드 버튼 표시
        ShowButtons(true);

        isCharacterMoving = false;

        Debug.Log($"[Free Tournament] Round {targetRoundIndex + 1} 이동 완료");
    }

    /// <summary>
    /// 현재 라운드에서 목표 라운드까지의 경로 구성
    /// </summary>
    List<Vector3> BuildPath(int fromRound, int toRound)
    {
        List<Vector3> path = new List<Vector3>();

        // 시작 위치
        path.Add(roundPositions[fromRound].position);

        // 이동 방향 결정 (앞으로 or 뒤로)
        int direction = (toRound > fromRound) ? 1 : -1;
        int current = fromRound;

        // 경로 중간 지점들 추가
        while (current != toRound)
        {
            int pathIndex = Mathf.Min(current, current + direction);

            // Waypoints가 설정되어 있으면 추가
            if (pathIndex >= 0 && pathIndex < bracketWaypoints.Length &&
                bracketWaypoints[pathIndex] != null &&
                bracketWaypoints[pathIndex].waypoints != null &&
                bracketWaypoints[pathIndex].waypoints.Length > 0)
            {
                // 앞으로 가는 경우: waypoints 순서대로
                // 뒤로 가는 경우: waypoints 역순으로
                if (direction > 0)
                {
                    foreach (Transform waypoint in bracketWaypoints[pathIndex].waypoints)
                    {
                        if (waypoint != null)
                        {
                            path.Add(waypoint.position);
                        }
                    }
                }
                else
                {
                    for (int i = bracketWaypoints[pathIndex].waypoints.Length - 1; i >= 0; i--)
                    {
                        if (bracketWaypoints[pathIndex].waypoints[i] != null)
                        {
                            path.Add(bracketWaypoints[pathIndex].waypoints[i].position);
                        }
                    }
                }
            }

            current += direction;
        }

        // 최종 목표 위치
        path.Add(roundPositions[toRound].position);

        Debug.Log($"[Path] Round {fromRound + 1} → {toRound + 1}: {path.Count}개 지점");
        return path;
    }

    /// <summary>
    /// 경로를 따라 캐릭터 이동 (부드러운 EaseInOut)
    /// </summary>
    System.Collections.IEnumerator MoveAlongPath(List<Vector3> pathPoints)
    {
        if (pathPoints.Count < 2)
        {
            Debug.LogWarning("[Path] 경로가 너무 짧습니다.");
            yield break;
        }

        // 전체 경로 길이 계산
        float totalDistance = 0f;
        for (int i = 0; i < pathPoints.Count - 1; i++)
        {
            totalDistance += Vector3.Distance(pathPoints[i], pathPoints[i + 1]);
        }

        // 각 구간을 이동
        float elapsed = 0f;
        int currentSegment = 0;

        while (currentSegment < pathPoints.Count - 1)
        {
            Vector3 segmentStart = pathPoints[currentSegment];
            Vector3 segmentEnd = pathPoints[currentSegment + 1];
            float segmentDistance = Vector3.Distance(segmentStart, segmentEnd);
            float segmentDuration = (segmentDistance / totalDistance) * characterMoveDuration;

            float segmentElapsed = 0f;

            while (segmentElapsed < segmentDuration)
            {
                segmentElapsed += Time.deltaTime;
                elapsed += Time.deltaTime;

                float segmentT = segmentElapsed / segmentDuration;
                float globalT = elapsed / characterMoveDuration;

                // 전체 이동에 대한 EaseInOut 적용
                float smoothGlobalT = globalT < 0.5f ? 2f * globalT * globalT : 1f - Mathf.Pow(-2f * globalT + 2f, 2f) / 2f;

                // 구간 내 선형 보간
                playerCharacter.position = Vector3.Lerp(segmentStart, segmentEnd, segmentT);

                yield return null;
            }

            // 정확히 구간 끝 지점으로 이동
            playerCharacter.position = segmentEnd;
            currentSegment++;
        }

        // 최종 위치 보장
        playerCharacter.position = pathPoints[pathPoints.Count - 1];
    }

    /// <summary>
    /// 줌인하면서 주인공 + Screen Overlay UI Fade In
    /// </summary>
    System.Collections.IEnumerator FadeInCharacterAndUI()
    {
        Debug.Log("[Free Tournament] 주인공 + Screen Overlay UI Fade In 시작");

        float elapsed = 0f;

        while (elapsed < introZoomDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(0f, 1f, elapsed / introZoomDuration);

            // 주인공 Fade In
            if (playerCharacterRenderer != null)
            {
                Color color = playerCharacterRenderer.color;
                color.a = alpha;
                playerCharacterRenderer.color = color;
            }

            // Screen Overlay UI Fade In
            if (screenOverlayCanvasGroup != null)
            {
                screenOverlayCanvasGroup.alpha = alpha;
            }

            yield return null;
        }

        // 최종 상태: 완전 불투명
        if (playerCharacterRenderer != null)
        {
            Color color = playerCharacterRenderer.color;
            color.a = 1f;
            playerCharacterRenderer.color = color;
        }

        if (screenOverlayCanvasGroup != null)
        {
            screenOverlayCanvasGroup.alpha = 1f;
        }

        Debug.Log("[Free Tournament] 주인공 + Screen Overlay UI Fade In 완료");
    }

    /// <summary>
    /// 카메라를 Inspector에서 설정한 목표 위치로 재배치
    /// </summary>
    System.Collections.IEnumerator RepositionCameraToTarget(int roundIndex)
    {
        if (roundIndex < 0 || roundIndex >= roundCameraTargetPositions.Length)
        {
            Debug.LogWarning($"[Free Tournament] Round {roundIndex + 1} 카메라 목표 위치 인덱스가 범위를 벗어났습니다.");
            yield break;
        }

        if (roundCameraTargetPositions[roundIndex] == null)
        {
            Debug.LogWarning($"[Free Tournament] Round {roundIndex + 1} 카메라 목표 위치가 설정되지 않았습니다. 재배치를 건너뜁니다.");
            yield break;
        }

        CinemachineCamera targetCamera = roundCameras[roundIndex];
        if (targetCamera == null)
        {
            Debug.LogWarning($"[Free Tournament] Round {roundIndex + 1} 카메라가 설정되지 않았습니다.");
            yield break;
        }

        // 카메라의 Follow를 목표 위치로 변경
        Transform originalFollow = targetCamera.Follow;
        targetCamera.Follow = roundCameraTargetPositions[roundIndex];

        Debug.Log($"[Free Tournament] Round {roundIndex + 1} 카메라를 목표 위치로 재배치 중... ({cameraRepositionDuration}초)");

        // 카메라가 목표 위치로 이동할 시간 대기
        yield return new WaitForSeconds(cameraRepositionDuration);

        Debug.Log($"[Free Tournament] Round {roundIndex + 1} 카메라 재배치 완료");
    }

    /// <summary>
    /// 라운드 버튼 클릭 시
    /// </summary>
    void OnRoundButtonClicked(int roundIndex)
    {
        // Free Mode는 모든 라운드 선택 가능
        // 라운드 정보 패널만 열기 (카메라 이동 없음)
        currentRoundIndex = roundIndex;
        ShowRoundInfo(roundIndex);

        if (currentRoundIndicator != null)
        {
            currentRoundIndicator.text = $"Round {roundIndex + 1} / 5";
        }
    }

    /// <summary>
    /// 이전 라운드로 이동
    /// </summary>
    void OnPreviousRound()
    {
        int prevIndex = currentRoundIndex - 1;

        if (prevIndex < 0)
        {
            Debug.Log("[Free Tournament] 첫 번째 라운드입니다.");
            return;
        }

        ShowRound(prevIndex);
    }

    /// <summary>
    /// 다음 라운드로 이동
    /// </summary>
    void OnNextRound()
    {
        int nextIndex = currentRoundIndex + 1;

        if (nextIndex >= 5)
        {
            Debug.Log("[Free Tournament] 마지막 라운드입니다.");
            return;
        }

        ShowRound(nextIndex);
    }

    /// <summary>
    /// 화살표 버튼 활성화 상태 업데이트
    /// </summary>
    void UpdateNavigationButtons()
    {
        // Free Mode: 범위만 체크
        leftArrowButton.interactable = (currentRoundIndex > 0);
        rightArrowButton.interactable = (currentRoundIndex < 4);
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

        // 패널과 배경 활성화
        if (roundInfoPanel != null)
        {
            roundInfoPanel.SetActive(true);
        }
        if (infoPanelBackground != null)
        {
            infoPanelBackground.SetActive(true);
        }

        Debug.Log($"[Free Tournament] Round {roundIndex + 1} 정보 패널 열림");
    }

    /// <summary>
    /// Info Panel 닫기
    /// </summary>
    void CloseInfoPanel()
    {
        if (roundInfoPanel != null)
        {
            roundInfoPanel.SetActive(false);
        }
        if (infoPanelBackground != null)
        {
            infoPanelBackground.SetActive(false);
        }

        Debug.Log("[Free Tournament] 정보 패널 닫힘");
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

        Debug.Log($"[Free Tournament] Round {currentRoundIndex + 1} 전투 시작! → {round.sceneName}");

        // 선택된 라운드 인덱스 저장
        PlayerPrefs.SetInt("SelectedRound", currentRoundIndex);
        PlayerPrefs.Save();

        // 해당 라운드의 전용 씬으로 이동
        if (!string.IsNullOrEmpty(round.sceneName))
        {
            SceneFader.LoadScene(round.sceneName);
        }
        else
        {
            Debug.LogError($"[Free Tournament] Round {currentRoundIndex + 1}의 씬 이름이 설정되지 않았습니다!");
        }
    }

    /// <summary>
    /// 메인 메뉴로 돌아가기
    /// </summary>
    void OnBackToMenu()
    {
        Debug.Log("[Free Tournament] 메인 메뉴로 돌아가기");
        SceneFader.LoadScene("GameStart");
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
        Debug.Log($"[Free Tournament] 버튼 {(show ? "표시" : "숨김")}");
    }
}
