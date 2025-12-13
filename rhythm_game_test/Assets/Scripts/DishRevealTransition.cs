using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class DishRevealTransition : MonoBehaviour
{
    public static DishRevealTransition Instance { get; private set; }

    [Header("UI References")]
    public RectTransform transitionPanel;  // 전체 전환 화면 Panel
    public Image coverImage;                // 요리 커버 이미지
    public Image dishImage;                 // 요리 이미지 (등급에 따라 변경)

    [Header("Dish Images by Grade")]
    [Tooltip("S/A 등급 요리 이미지 (최고급)")]
    public Sprite dishPerfect;

    [Tooltip("B/C 등급 요리 이미지 (보통)")]
    public Sprite dishGood;

    [Tooltip("D/F 등급 요리 이미지 (실패)")]
    public Sprite dishBad;

    [Header("Animation Settings")]
    [Tooltip("Panel이 아래에서 올라오는 속도 (초)")]
    public float slideUpDuration = 0.8f;

    [Tooltip("Panel이 다 올라온 후 커버 애니메이션 시작까지 대기 시간")]
    public float delayBeforeCoverLift = 0.3f;

    [Tooltip("커버가 들리는 애니메이션 시간 (초)")]
    public float coverLiftDuration = 1.2f;

    [Tooltip("커버 들림 방향 (위쪽으로 이동할 거리)")]
    public float coverLiftHeight = 300f;

    [Tooltip("커버 들릴 때 페이드 아웃 여부")]
    public bool fadeCoverOnLift = true;

    [Tooltip("요리 이미지 Scale Up 애니메이션 여부")]
    public bool scaleUpDish = true;

    [Tooltip("요리 이미지가 커질 최종 Scale 배율 (1.0 = 원본)")]
    public float dishScaleMultiplier = 1.15f;

    [Tooltip("요리를 보여준 후 대기 시간 (초)")]
    public float displayDuration = 2.0f;

    [Header("Scene Transition")]
    [Tooltip("전환할 결과 씬 이름")]
    public string resultSceneName = "ScoreScene";

    private Vector2 panelStartPos;  // Panel의 시작 위치 (화면 아래)
    private Vector2 panelEndPos;    // Panel의 최종 위치 (화면 중앙)
    private Vector2 coverStartPos;  // Cover의 시작 위치
    private Vector3 dishOriginalScale; // Dish의 원본 Scale (Inspector 설정값)
    private Vector3 dishStartScale; // Dish의 애니메이션 시작 Scale
    private bool isTransitioning = false;

    void Awake()
    {
        Instance = this;

        // 처음에는 숨겨둠
        if (transitionPanel != null)
        {
            transitionPanel.gameObject.SetActive(false);
        }

        // Dish 이미지의 원본 Scale 저장 (Inspector에서 설정된 값)
        if (dishImage != null)
        {
            dishOriginalScale = dishImage.rectTransform.localScale;
        }
    }

    /// <summary>
    /// 전환 화면 시작 (RoundManager나 다른 스크립트에서 호출)
    /// </summary>
    public void StartTransition()
    {
        if (isTransitioning) return;

        StartCoroutine(TransitionSequence());
    }

    IEnumerator TransitionSequence()
    {
        isTransitioning = true;

        // 1. 등급 계산 및 요리 이미지 선택
        string rank = GameResultData.GetRank();
        SelectDishImage(rank);

        // 2. Panel 초기 위치 설정 (화면 아래)
        SetupPanelPosition();

        // 3. Panel 활성화
        transitionPanel.gameObject.SetActive(true);

        // 4. Panel 슬라이드 업 애니메이션
        yield return StartCoroutine(SlideUpPanel());

        // 5. 잠시 대기
        yield return new WaitForSeconds(delayBeforeCoverLift);

        // 6. 커버 들림 애니메이션
        yield return StartCoroutine(LiftCover());

        // 7. 완성된 요리를 보여주며 대기
        yield return new WaitForSeconds(displayDuration);

        // 8. 결과 씬으로 전환
        SceneFader.LoadScene(resultSceneName);
    }

    void SelectDishImage(string rank)
    {
        if (dishImage == null) return;

        // 등급에 따라 요리 이미지 선택
        switch (rank)
        {
            case "S":
            case "A":
                dishImage.sprite = dishPerfect;
                Debug.Log($"[DishReveal] 등급 {rank} → 최고급 요리");
                break;
            case "B":
            case "C":
                dishImage.sprite = dishGood;
                Debug.Log($"[DishReveal] 등급 {rank} → 보통 요리");
                break;
            case "D":
            case "F":
                dishImage.sprite = dishBad;
                Debug.Log($"[DishReveal] 등급 {rank} → 실패 요리");
                break;
            default:
                dishImage.sprite = dishBad;
                break;
        }
    }

    void SetupPanelPosition()
    {
        if (transitionPanel == null) return;

        // Panel의 최종 위치 저장 (현재 위치)
        panelEndPos = transitionPanel.anchoredPosition;

        // Panel의 시작 위치 계산 (화면 아래로 완전히 내림)
        panelStartPos = new Vector2(panelEndPos.x, -Screen.height);

        // 시작 위치로 이동
        transitionPanel.anchoredPosition = panelStartPos;

        // 커버 시작 위치 저장
        if (coverImage != null)
        {
            coverStartPos = coverImage.rectTransform.anchoredPosition;

            // 커버 초기 상태 (불투명)
            Color coverColor = coverImage.color;
            coverColor.a = 1f;
            coverImage.color = coverColor;
        }

        // 요리 이미지 Scale 초기화 (항상 1, 1, 1부터 시작)
        if (dishImage != null)
        {
            dishStartScale = Vector3.one;
            dishImage.rectTransform.localScale = dishStartScale;
        }
    }

    IEnumerator SlideUpPanel()
    {
        float elapsed = 0f;

        while (elapsed < slideUpDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / slideUpDuration;

            // EaseOutCubic으로 부드러운 감속
            t = 1f - Mathf.Pow(1f - t, 3f);

            transitionPanel.anchoredPosition = Vector2.Lerp(panelStartPos, panelEndPos, t);

            yield return null;
        }

        transitionPanel.anchoredPosition = panelEndPos;
    }

    IEnumerator LiftCover()
    {
        if (coverImage == null) yield break;

        RectTransform coverRect = coverImage.rectTransform;
        Vector2 coverEndPos = new Vector2(coverStartPos.x, coverStartPos.y + coverLiftHeight);

        float elapsed = 0f;
        Color startColor = coverImage.color;
        Color endColor = new Color(startColor.r, startColor.g, startColor.b, fadeCoverOnLift ? 0f : 1f);

        // 요리 이미지 Scale Up 설정
        RectTransform dishRect = dishImage != null ? dishImage.rectTransform : null;
        Vector3 dishEndScale = dishStartScale * dishScaleMultiplier;

        while (elapsed < coverLiftDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / coverLiftDuration;

            // EaseOutQuad로 부드러운 감속
            float easeT = 1f - (1f - t) * (1f - t);

            // 커버 위로 이동
            coverRect.anchoredPosition = Vector2.Lerp(coverStartPos, coverEndPos, easeT);

            // 커버 페이드 아웃 (옵션)
            if (fadeCoverOnLift)
            {
                coverImage.color = Color.Lerp(startColor, endColor, easeT);
            }

            // 요리 이미지 Scale Up (옵션)
            if (scaleUpDish && dishRect != null)
            {
                // EaseOutBack으로 약간 튕기는 효과
                float scaleT = easeT;
                float overshoot = 1.1f; // 살짝 오버슈팅
                if (scaleT < 1f)
                {
                    scaleT = scaleT * scaleT * ((overshoot + 1) * scaleT - overshoot);
                }
                dishRect.localScale = Vector3.Lerp(dishStartScale, dishEndScale, scaleT);
            }

            yield return null;
        }

        coverRect.anchoredPosition = coverEndPos;
        coverImage.color = endColor;

        // 요리 이미지 최종 Scale 설정
        if (scaleUpDish && dishRect != null)
        {
            dishRect.localScale = dishEndScale;
        }
    }

    /// <summary>
    /// 외부에서 전환 화면을 즉시 숨기고 싶을 때
    /// </summary>
    public void Hide()
    {
        if (transitionPanel != null)
        {
            transitionPanel.gameObject.SetActive(false);
        }
        isTransitioning = false;
    }
}
