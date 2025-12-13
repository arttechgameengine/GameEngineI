using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;

public class DishRevealTransition : MonoBehaviour
{
    public static DishRevealTransition Instance { get; private set; }

    [Header("UI References")]
    public RectTransform transitionPanel;  // 전체 전환 화면 Panel
    public Image coverImage;                // 요리 커버 이미지
    public Image dishImage;                 // 요리 이미지 (등급에 따라 변경)
    public TextMeshProUGUI gradeText;       // 등급 텍스트 (Perfect!, Good!, Bad...)

    [Header("Dish Images by Grade")]
    [Tooltip("S/A 등급 요리 이미지 (최고급)")]
    public Sprite dishPerfect;

    [Tooltip("B/C 등급 요리 이미지 (보통)")]
    public Sprite dishGood;

    [Tooltip("D/F 등급 요리 이미지 (실패)")]
    public Sprite dishBad;

    [Header("Grade Text Messages")]
    [Tooltip("S/A 등급 텍스트")]
    public string textPerfect = "Perfect!";

    [Tooltip("B/C 등급 텍스트")]
    public string textGood = "Good!";

    [Tooltip("D/F 등급 텍스트")]
    public string textBad = "Bad...";

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

    [Header("Text Animation")]
    [Tooltip("텍스트 Fade In 시간 (초)")]
    public float textFadeInDuration = 0.5f;

    [Tooltip("요리를 보여준 후 대기 시간 (초)")]
    public float displayDuration = 2.0f;

    [Tooltip("텍스트 Fade Out 시간 (초) - 최종 Scale Up과 함께")]
    public float textFadeOutDuration = 0.5f;

    [Header("Scene Transition")]
    [Tooltip("전환할 결과 씬 이름")]
    public string resultSceneName = "ScoreScene";

    [Header("Final Scale Up Animation")]
    [Tooltip("Display duration 후 요리가 최종적으로 커지는 애니메이션 여부")]
    public bool finalScaleUp = true;

    [Tooltip("최종 Scale Up 애니메이션 시간 (초)")]
    public float finalScaleUpDuration = 0.5f;

    [Tooltip("최종 Scale Up 배율 (dishScaleMultiplier에서 추가로 커지는 배율)")]
    public float finalScaleUpMultiplier = 1.3f;

    [Tooltip("최종 Scale Up 시 화면 Fade Out 여부")]
    public bool fadeOutDuringFinalScale = true;

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

        // Grade 텍스트 초기화 (투명하게)
        if (gradeText != null)
        {
            Color textColor = gradeText.color;
            textColor.a = 0f;
            gradeText.color = textColor;
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

        // 0. 모든 ingredient idle 애니메이션 멈추기
        if (CookingAreaManager.Instance != null)
        {
            CookingAreaManager.Instance.StopAllIdleAnimations();
        }

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

        // 7. 등급 텍스트 Fade In
        yield return StartCoroutine(ShowGradeText());

        // 8. 완성된 요리를 보여주며 대기
        yield return new WaitForSeconds(displayDuration);

        // 9. 최종 Scale Up 애니메이션과 동시에 Fade Out
        if (finalScaleUp)
        {
            // Scale Up과 Fade Out을 동시에 시작
            if (fadeOutDuringFinalScale && SceneFader.Instance != null)
            {
                // Scale Up과 텍스트 Fade Out 동시 시작
                StartCoroutine(FinalScaleUpAnimation());
                StartCoroutine(FadeOutGradeText());

                // Fade Out 후 씬 전환
                SceneFader.Instance.FadeToScene(resultSceneName);
            }
            else
            {
                // Fade Out 없이 Scale Up만
                yield return StartCoroutine(FinalScaleUpAnimation());
                SceneFader.LoadScene(resultSceneName);
            }
        }
        else
        {
            // Scale Up 없이 바로 씬 전환
            SceneFader.LoadScene(resultSceneName);
        }
    }

    void SelectDishImage(string rank)
    {
        if (dishImage == null) return;

        // 등급에 따라 요리 이미지 및 텍스트 선택
        switch (rank)
        {
            case "S":
            case "A":
                dishImage.sprite = dishPerfect;
                if (gradeText != null) gradeText.text = textPerfect;
                Debug.Log($"[DishReveal] 등급 {rank} → 최고급 요리 ({textPerfect})");
                break;
            case "B":
            case "C":
                dishImage.sprite = dishGood;
                if (gradeText != null) gradeText.text = textGood;
                Debug.Log($"[DishReveal] 등급 {rank} → 보통 요리 ({textGood})");
                break;
            case "D":
            case "F":
                dishImage.sprite = dishBad;
                if (gradeText != null) gradeText.text = textBad;
                Debug.Log($"[DishReveal] 등급 {rank} → 실패 요리 ({textBad})");
                break;
            default:
                dishImage.sprite = dishBad;
                if (gradeText != null) gradeText.text = textBad;
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

    IEnumerator ShowGradeText()
    {
        if (gradeText == null) yield break;

        float elapsed = 0f;
        Color textColor = gradeText.color;
        Color targetColor = new Color(textColor.r, textColor.g, textColor.b, 1f);

        Debug.Log($"[DishReveal] Grade Text Fade In: {gradeText.text}");

        while (elapsed < textFadeInDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / textFadeInDuration;

            // EaseOut으로 부드러운 fade in
            float easeT = 1f - (1f - t) * (1f - t);

            textColor.a = Mathf.Lerp(0f, 1f, easeT);
            gradeText.color = textColor;

            yield return null;
        }

        gradeText.color = targetColor;
    }

    IEnumerator FadeOutGradeText()
    {
        if (gradeText == null) yield break;

        float elapsed = 0f;
        Color textColor = gradeText.color;
        float startAlpha = textColor.a;

        Debug.Log($"[DishReveal] Grade Text Fade Out");

        while (elapsed < textFadeOutDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / textFadeOutDuration;

            textColor.a = Mathf.Lerp(startAlpha, 0f, t);
            gradeText.color = textColor;

            yield return null;
        }

        textColor.a = 0f;
        gradeText.color = textColor;
    }

    IEnumerator FinalScaleUpAnimation()
    {
        if (dishImage == null) yield break;

        RectTransform dishRect = dishImage.rectTransform;
        Vector3 currentScale = dishRect.localScale;
        Vector3 targetScale = currentScale * finalScaleUpMultiplier;

        float elapsed = 0f;

        Debug.Log($"[DishReveal] Final Scale Up: {currentScale} → {targetScale}");

        while (elapsed < finalScaleUpDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / finalScaleUpDuration;

            // EaseOutBack으로 튕기는 효과
            float overshoot = 1.2f;
            float easeT = t * t * ((overshoot + 1) * t - overshoot);

            dishRect.localScale = Vector3.Lerp(currentScale, targetScale, easeT);

            yield return null;
        }

        dishRect.localScale = targetScale;
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
