using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 연타 노트 비주얼 (카운터, 타이머, 이펙트)
/// </summary>
public class RapidNoteVisual : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI counterText;   // "3/5" 같은 카운터 텍스트
    public GameObject rapidTimerBarRoot; // 타이머 바 루트 (전체 컨테이너)
    public Image rapidTimerBarBG;        // 타이머 바 배경 (회색, 항상 가득참)
    public Image rapidTimerBarFill;      // 타이머 바 Fill (1.0 → 0.0, 색상 변화)
    public Image rapidBackground;        // 연타 노트 배경 이미지
    public Image rapidOverlay;           // 노란색 오버레이 (rapidBackground를 masking)
    public Image whiteOverlay;           // 흰색 오버레이 (성공 시 번쩍 효과용)

    [Header("Visual Settings")]
    public Color normalColor = Color.white;
    public Color activeColor = Color.yellow;
    public Color successColor = Color.green;
    public Color failColor = Color.red;
    public float pulseScale = 1.2f;    // 히트 시 펄스 효과

    [Header("Success Effect Settings")]
    public float flashDuration = 0.15f;     // 흰색 번쩍 효과 지속 시간
    public float scaleMultiplier = 1.3f;    // 성공 시 커지는 배율
    public Color flashColor = Color.white;  // 번쩍 효과 색상

    private Image noteImage;
    private Vector3 originalScale;
    private bool isActivated = false;

    void Awake()
    {
        // noteImage는 rapidBackground 자체를 사용 (부모는 투명)
        // rapidBackground에 방향키 sprite가 설정됨
        noteImage = rapidBackground;  // rapidBackground가 실제 노트 이미지

        // originalScale을 1,1,1로 저장 (스폰될 때 크기)
        originalScale = Vector3.one;

        // UI 초기화
        if (counterText != null)
        {
            counterText.text = "";
        }

        // 타이머 바 초기화
        if (rapidTimerBarRoot != null)
        {
            rapidTimerBarRoot.SetActive(false); // 처음엔 숨김
        }

        if (rapidTimerBarFill != null)
        {
            rapidTimerBarFill.fillAmount = 1f;
            rapidTimerBarFill.color = Color.yellow;
        }

        if (rapidTimerBarBG != null)
        {
            rapidTimerBarBG.fillAmount = 1f;
            rapidTimerBarBG.color = new Color(0.3f, 0.3f, 0.3f, 0.8f); // 회색 배경
        }

        // 흰색 오버레이 초기에 숨김 (alpha=0)
        if (whiteOverlay != null)
        {
            Color c = whiteOverlay.color;
            c.a = 0f;
            whiteOverlay.color = c;
        }
    }

    /// <summary>
    /// Rapid 노트 정보 설정
    /// </summary>
    public void SetRapidInfo(int requiredCount)
    {
        if (counterText != null)
        {
            counterText.text = $"0/{requiredCount}";
            counterText.fontSize = 50;
            counterText.fontStyle = TMPro.FontStyles.Bold;
            counterText.color = normalColor;
        }

        // rapidOverlay는 Inspector에서 이미 노란색으로 설정되어 있음
    }

    /// <summary>
    /// 방향키 sprite를 rapidBackground에 설정
    /// rapidOverlay는 rapidBackground의 sprite를 masking함
    /// </summary>
    public void SetArrowSprite(NoteVisual visual, string arrowKey)
    {
        if (rapidBackground == null || visual == null) return;

        Sprite arrowSprite = null;

        // NoteVisual의 sprite를 rapidBackground에 복사
        switch (arrowKey)
        {
            case "LEFT":
                arrowSprite = visual.leftSprite;
                break;
            case "RIGHT":
                arrowSprite = visual.rightSprite;
                break;
            case "UP":
                arrowSprite = visual.upSprite;
                break;
            case "DOWN":
                arrowSprite = visual.downSprite;
                break;
            case "SPACE":
                arrowSprite = visual.spaceSprite;
                break;
        }

        rapidBackground.sprite = arrowSprite;

        // Overlay도 같은 sprite 사용 (masking용)
        if (rapidOverlay != null)
        {
            rapidOverlay.sprite = arrowSprite;
        }

        Debug.Log($"[RapidNoteVisual] Set arrow sprite: {arrowKey}");
    }

    /// <summary>
    /// 연타 판정 시작 (활성화)
    /// </summary>
    public void OnActivated()
    {
        isActivated = true;

        if (counterText != null)
        {
            counterText.color = activeColor;
        }

        if (rapidBackground != null)
        {
            rapidBackground.color = activeColor;
        }

        // 타이머 바 표시
        if (rapidTimerBarRoot != null)
        {
            rapidTimerBarRoot.SetActive(true);
        }

        // 펄스 애니메이션 없음 (크기 유지)
        // transform.localScale = originalScale * 1.2f;
    }

    /// <summary>
    /// 진행 상황 업데이트 (매 프레임)
    /// </summary>
    public void UpdateProgress(int currentCount, int requiredCount, float elapsedTime, float timeLimit)
    {
        // 카운터 업데이트
        if (counterText != null)
        {
            counterText.text = $"{currentCount}/{requiredCount}";
        }

        // 타이머 게이지 업데이트
        if (rapidTimerBarFill != null)
        {
            float remaining = 1f - (elapsedTime / timeLimit);
            rapidTimerBarFill.fillAmount = Mathf.Clamp01(remaining);

            // 시간 얼마 안 남으면 빨간색
            if (remaining < 0.3f)
            {
                rapidTimerBarFill.color = Color.Lerp(Color.yellow, Color.red, 1f - (remaining / 0.3f));
            }
            else
            {
                rapidTimerBarFill.color = Color.yellow;
            }
        }
    }

    /// <summary>
    /// 연타 히트 이벤트
    /// </summary>
    public void OnHit(int currentCount, int requiredCount)
    {
        // 카운터 업데이트
        if (counterText != null)
        {
            counterText.text = $"{currentCount}/{requiredCount}";

            // 간단한 펄스 효과
            counterText.transform.localScale = Vector3.one * pulseScale;
            StartCoroutine(ResetScaleAfterDelay(counterText.transform, 0.1f));
        }

        // 노트 히트 애니메이션 (커지면서 흰색 번쩍)
        StartCoroutine(HitFlashAnimation());
    }

    /// <summary>
    /// 성공 연출 (일반 노트처럼 흰색 번쩍 효과)
    /// </summary>
    public void OnSuccess(string judgement, System.Action onComplete = null)
    {
        // 타이머 바 숨기기
        if (rapidTimerBarRoot != null)
        {
            rapidTimerBarRoot.SetActive(false);
        }

        // 판정 텍스트 표시
        if (counterText != null)
        {
            counterText.text = judgement.ToUpper();
            counterText.color = successColor;
            counterText.fontSize = 48;
        }

        // 흰색 번쩍 효과 + 페이드아웃 + 커지기
        StartCoroutine(SuccessFlashEffect(onComplete));
    }

    /// <summary>
    /// 실패 연출
    /// </summary>
    public void OnFail()
    {
        if (counterText != null)
        {
            counterText.text = "MISS";
            counterText.color = failColor;
        }

        if (rapidBackground != null)
        {
            rapidBackground.color = failColor;
        }

        if (noteImage != null)
        {
            noteImage.color = failColor;
        }

        // 타이머 바 숨기기
        if (rapidTimerBarRoot != null)
        {
            rapidTimerBarRoot.SetActive(false);
        }

        // 페이드 아웃 (간단 버전)
        StartCoroutine(FadeOut(0.3f));
    }

    /// <summary>
    /// 스케일 리셋 코루틴
    /// </summary>
    System.Collections.IEnumerator ResetScaleAfterDelay(Transform target, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (target != null)
        {
            target.localScale = Vector3.one;
        }
    }

    /// <summary>
    /// 페이드 아웃 코루틴
    /// </summary>
    System.Collections.IEnumerator FadeOut(float duration)
    {
        CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = 1f - (elapsed / duration);
            yield return null;
        }
        canvasGroup.alpha = 0f;
    }

    /// <summary>
    /// 히트 플래시 애니메이션 (다른 노트 성공 애니메이션과 유사)
    /// 노트가 커지면서 흰색으로 번쩍한 후 원래 색상으로 복귀
    /// </summary>
    System.Collections.IEnumerator HitFlashAnimation()
    {
        Color originalNoteColor = noteImage != null ? noteImage.color : Color.white;
        Color originalBgColor = rapidBackground != null ? rapidBackground.color : activeColor;
        Vector3 startScale = transform.localScale;
        Vector3 targetScale = originalScale * 1.3f;

        float flashDuration = 0.15f;
        float elapsed = 0f;

        // 커지면서 흰색으로
        while (elapsed < flashDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / flashDuration;

            transform.localScale = Vector3.Lerp(startScale, targetScale, t);

            if (noteImage != null)
            {
                noteImage.color = Color.Lerp(originalNoteColor, Color.white, t);
            }

            if (rapidBackground != null)
            {
                rapidBackground.color = Color.Lerp(originalBgColor, Color.white, t);
            }

            yield return null;
        }

        // 원래 크기와 색상으로 복귀
        elapsed = 0f;
        while (elapsed < flashDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / flashDuration;

            transform.localScale = Vector3.Lerp(targetScale, startScale, t);

            if (noteImage != null)
            {
                noteImage.color = Color.Lerp(Color.white, originalNoteColor, t);
            }

            if (rapidBackground != null)
            {
                rapidBackground.color = Color.Lerp(Color.white, originalBgColor, t);
            }

            yield return null;
        }

        // 최종 값 보정
        transform.localScale = startScale;
        if (noteImage != null) noteImage.color = originalNoteColor;
        if (rapidBackground != null) rapidBackground.color = originalBgColor;
    }

    /// <summary>
    /// 성공 시 흰색 번쩍 효과 (NoteEffect.HitEffectRoutine과 완전 동일)
    /// </summary>
    System.Collections.IEnumerator SuccessFlashEffect(System.Action onComplete)
    {
        // 흰색 오버레이 즉시 표시 (흰색 번쩍 효과)
        if (whiteOverlay != null)
        {
            Color c = flashColor;
            c.a = 1f;
            whiteOverlay.color = c;
        }

        float elapsed = 0f;
        Color originalBgColor = rapidBackground != null ? rapidBackground.color : Color.white;
        Color originalOverlayColor = rapidOverlay != null ? rapidOverlay.color : Color.yellow;
        Color originalTextColor = counterText != null ? counterText.color : Color.white;

        // 흰색 오버레이 페이드아웃 + 노트 페이드아웃 + 커지기
        while (elapsed < flashDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / flashDuration;

            // 흰색 오버레이 페이드아웃
            if (whiteOverlay != null)
            {
                Color c = flashColor;
                c.a = 1f - t;
                whiteOverlay.color = c;
            }

            // rapidBackground 페이드아웃
            if (rapidBackground != null)
            {
                Color c = originalBgColor;
                c.a = 1f - t;
                rapidBackground.color = c;
            }

            // 노란색 오버레이도 페이드아웃
            if (rapidOverlay != null)
            {
                Color c = originalOverlayColor;
                c.a = 1f - t;
                rapidOverlay.color = c;
            }

            // 카운터 텍스트도 페이드아웃
            if (counterText != null)
            {
                Color c = originalTextColor;
                c.a = 1f - t;
                counterText.color = c;
            }

            // 커지기
            float scale = Mathf.Lerp(1f, scaleMultiplier, t);
            transform.localScale = originalScale * scale;

            yield return null;
        }

        // 완료 콜백 호출
        onComplete?.Invoke();
    }
}
