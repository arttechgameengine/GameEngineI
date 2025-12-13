using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public enum SplatterDisplayMode
{
    Sequential,  // 순차적으로 하나씩 표시
    Random,      // 랜덤하게 하나 선택
    All          // 모두 동시에 표시
}

/// <summary>
/// UI Panel 흔들기 (Screen Space - Overlay용)
/// PlayerJudge 오브젝트에 붙여서 사용
/// Canvas 안에 ShakeablePanel을 만들고, 모든 UI를 그 안에 넣은 후 사용
/// </summary>
public class CameraShakeManager : MonoBehaviour
{
    [Header("Shake Settings")]
    [Tooltip("패링 노트 미스 시 흔들림 강도")]
    public float parryMissShakeForce = 30f;

    [Tooltip("일반 미스 시 흔들림 강도")]
    public float normalMissShakeForce = 15f;

    [Tooltip("흔들림 지속 시간")]
    public float shakeDuration = 0.2f;

    [Header("Splatter Effect Settings (ImageSplatter 선택 시 사용)")]
    [Tooltip("스플래터 Panel (메인 캔버스 안, UI 이미지들의 부모)")]
    public GameObject splatterPanel;

    [Tooltip("스플래터 이미지 배열 (Inspector에서 직접 할당)")]
    public Image[] splatterImages = new Image[0];

    [Tooltip("스플래터 표시 방식: Sequential(순차), Random(랜덤), All(모두)")]
    public SplatterDisplayMode splatterDisplayMode = SplatterDisplayMode.Random;

    [Tooltip("스플래터 페이드인 시간")]
    public float splatterFadeInDuration = 0.2f;

    [Tooltip("스플래터 효과 지속 시간 (페이드인/아웃 제외)")]
    public float splatterDuration = 1.5f;

    [Tooltip("스플래터 페이드아웃 시간")]
    public float splatterFadeOutDuration = 0.5f;

    [Header("Flash Effect Settings (ScreenFlash 선택 시 사용)")]
    [Tooltip("플래시 Panel (메인 캔버스 안)")]
    public GameObject flashPanel;

    [Tooltip("플래시 이미지 (전체 화면 크기)")]
    public Image flashImage;

    [Tooltip("플래시 효과 지속 시간")]
    public float flashDuration = 1f;

    [Header("Target Reference")]
    [Tooltip("흔들릴 UI Panel (Canvas 안의 Panel)")]
    public RectTransform shakeablePanel;

    private Vector2 originalPosition;
    private bool isShaking = false;
    private Color[] originalSplatterColors;  // 각 스플래터 이미지의 원래 색상 저장
    private Color originalFlashColor;  // 플래시 이미지의 원래 색상 저장
    private bool isSplatterPlaying = false;  // 스플래터 효과 진행 중 여부
    private bool isFlashPlaying = false;  // 플래시 효과 진행 중 여부

    void Start()
    {
        // ShakeablePanel 설정
        if (shakeablePanel != null)
        {
            originalPosition = shakeablePanel.anchoredPosition;
            Debug.Log($"[CameraShakeManager] ShakeablePanel 찾음! 원래 위치: {originalPosition}");
        }
        else
        {
            Debug.LogError("[CameraShakeManager] ShakeablePanel이 설정되지 않았습니다! Inspector에서 설정하세요.");
        }

        // 스플래터 이미지 원래 색상 저장 & Panel 숨기기
        if (splatterImages != null && splatterImages.Length > 0)
        {
            originalSplatterColors = new Color[splatterImages.Length];
            for (int i = 0; i < splatterImages.Length; i++)
            {
                if (splatterImages[i] != null)
                {
                    originalSplatterColors[i] = splatterImages[i].color;
                }
            }
        }

        if (splatterPanel != null)
        {
            splatterPanel.SetActive(false);
            Debug.Log("[CameraShakeManager] Splatter Panel 초기 비활성화");
        }

        // 플래시 이미지 원래 색상 저장 & Panel 숨기기
        if (flashImage != null)
        {
            originalFlashColor = flashImage.color;
        }

        if (flashPanel != null)
        {
            flashPanel.SetActive(false);
            Debug.Log("[CameraShakeManager] Flash Panel 초기 비활성화");
        }
    }

    public void ShakeOnParrySuccess()
    {
        Debug.Log($"[Camera Shake] 패링 성공! 강도: {normalMissShakeForce}");
        StartCoroutine(Shake(normalMissShakeForce, shakeDuration));
    }

    public void ShakeOnParryMiss()
    {
        Debug.Log($"[Camera Shake] 패링 미스! 강도: {parryMissShakeForce}");
        StartCoroutine(Shake(parryMissShakeForce, shakeDuration));
    }

    public void ShakeOnNormalMiss()
    {
        Debug.Log($"[Camera Shake] 일반 미스! 강도: {normalMissShakeForce}");
        StartCoroutine(Shake(normalMissShakeForce, shakeDuration));
    }

    /// <summary>
    /// 패링 미스 시 선택된 효과 재생
    /// </summary>
    public void PlayParryMissEffect(ParryMissEffectType effectType)
    {
        switch (effectType)
        {
            case ParryMissEffectType.ScreenShake:
                ShakeOnParryMiss();
                break;
            case ParryMissEffectType.ImageSplatter:
                StartCoroutine(PlaySplatterEffect());
                break;
            case ParryMissEffectType.ScreenFlash:
                StartCoroutine(PlayFlashEffect());
                break;
        }
    }

    private IEnumerator Shake(float intensity, float duration)
    {
        if (shakeablePanel == null || isShaking)
            yield break;

        isShaking = true;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * intensity;
            float y = Random.Range(-1f, 1f) * intensity;

            shakeablePanel.anchoredPosition = originalPosition + new Vector2(x, y);

            elapsed += Time.deltaTime;
            yield return null;
        }

        shakeablePanel.anchoredPosition = originalPosition;
        isShaking = false;
        Debug.Log("[Camera Shake] 흔들림 완료!");
    }

    /// <summary>
    /// 스플래터 효과: Panel 활성화 → 선택된 이미지 표시 → duration 후 fade out
    /// </summary>
    private IEnumerator PlaySplatterEffect()
    {
        // 이미 효과 진행 중이면 무시
        if (isSplatterPlaying)
        {
            Debug.LogWarning("[Splatter Effect] 이미 스플래터 효과 진행 중!");
            yield break;
        }

        if (splatterPanel == null)
        {
            Debug.LogError("[CameraShakeManager] splatterPanel이 설정되지 않았습니다!");
            yield break;
        }

        if (splatterImages == null || splatterImages.Length == 0)
        {
            Debug.LogError("[CameraShakeManager] splatterImages가 설정되지 않았습니다!");
            yield break;
        }

        isSplatterPlaying = true;

        Debug.Log($"[Splatter Effect] 스플래터 효과 시작! Mode: {splatterDisplayMode}");

        // Panel 활성화
        splatterPanel.SetActive(true);

        // 표시 방식에 따라 다르게 처리
        switch (splatterDisplayMode)
        {
            case SplatterDisplayMode.Sequential:
                yield return StartCoroutine(ShowSplatterSequential());
                break;
            case SplatterDisplayMode.Random:
                yield return StartCoroutine(ShowSplatterRandom());
                break;
            case SplatterDisplayMode.All:
                yield return StartCoroutine(ShowSplatterAll());
                break;
        }

        // Panel 비활성화
        splatterPanel.SetActive(false);

        isSplatterPlaying = false;

        Debug.Log("[Splatter Effect] 스플래터 효과 완료!");
    }

    /// <summary>
    /// 순차적으로 모든 이미지를 고정 순서(0→1→2...)로 차례대로 fade in → 함께 유지 → 함께 fade out
    /// </summary>
    private IEnumerator ShowSplatterSequential()
    {
        // 모든 이미지를 투명하게 시작
        for (int i = 0; i < splatterImages.Length; i++)
        {
            if (splatterImages[i] != null && i < originalSplatterColors.Length)
            {
                Color transparentColor = originalSplatterColors[i];
                transparentColor.a = 0f;
                splatterImages[i].color = transparentColor;
            }
        }

        // 1단계: 순서대로 fade in (약간의 delay와 함께)
        for (int i = 0; i < splatterImages.Length; i++)
        {
            if (splatterImages[i] != null && i < originalSplatterColors.Length)
            {
                StartCoroutine(FadeInSplatter(i));
                yield return new WaitForSeconds(0.1f); // 각 이미지 사이 짧은 딜레이
            }
        }

        // 마지막 이미지의 fade in이 끝날 때까지 대기
        yield return new WaitForSeconds(splatterFadeInDuration);

        // 2단계: 모두 함께 화면에 유지
        yield return new WaitForSeconds(splatterDuration);

        // 3단계: 모두 함께 fade out
        yield return StartCoroutine(FadeOutAllSplatters());
    }

    /// <summary>
    /// 모든 이미지를 랜덤한 순서로 차례대로 fade in → 함께 유지 → 함께 fade out
    /// </summary>
    private IEnumerator ShowSplatterRandom()
    {
        // 모든 이미지를 투명하게 시작
        for (int i = 0; i < splatterImages.Length; i++)
        {
            if (splatterImages[i] != null && i < originalSplatterColors.Length)
            {
                Color transparentColor = originalSplatterColors[i];
                transparentColor.a = 0f;
                splatterImages[i].color = transparentColor;
            }
        }

        // 인덱스 배열 생성 후 섞기
        int[] indices = new int[splatterImages.Length];
        for (int i = 0; i < indices.Length; i++)
        {
            indices[i] = i;
        }

        // Fisher-Yates 셔플
        for (int i = indices.Length - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            int temp = indices[i];
            indices[i] = indices[randomIndex];
            indices[randomIndex] = temp;
        }

        // 1단계: 섞인 순서대로 fade in (약간의 delay와 함께)
        for (int i = 0; i < indices.Length; i++)
        {
            if (splatterImages[indices[i]] != null && indices[i] < originalSplatterColors.Length)
            {
                StartCoroutine(FadeInSplatter(indices[i]));
                yield return new WaitForSeconds(0.1f); // 각 이미지 사이 짧은 딜레이
            }
        }

        // 마지막 이미지의 fade in이 끝날 때까지 대기
        yield return new WaitForSeconds(splatterFadeInDuration);

        // 2단계: 모두 함께 화면에 유지
        yield return new WaitForSeconds(splatterDuration);

        // 3단계: 모두 함께 fade out
        yield return StartCoroutine(FadeOutAllSplatters());
    }

    /// <summary>
    /// 모두 동시에 표시
    /// </summary>
    private IEnumerator ShowSplatterAll()
    {
        // 1단계: 페이드인 - 0에서 원래 alpha로
        float elapsed = 0f;

        // 모든 이미지를 투명하게 시작
        for (int i = 0; i < splatterImages.Length; i++)
        {
            if (splatterImages[i] != null && i < originalSplatterColors.Length)
            {
                Color transparentColor = originalSplatterColors[i];
                transparentColor.a = 0f;
                splatterImages[i].color = transparentColor;
            }
        }

        // 페이드인
        while (elapsed < splatterFadeInDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / splatterFadeInDuration;

            for (int i = 0; i < splatterImages.Length; i++)
            {
                if (splatterImages[i] != null && i < originalSplatterColors.Length)
                {
                    Color fadeColor = originalSplatterColors[i];
                    fadeColor.a = Mathf.Lerp(0f, originalSplatterColors[i].a, t);
                    splatterImages[i].color = fadeColor;
                }
            }

            yield return null;
        }

        // 완전히 원래 색상으로 (페이드인 완료)
        for (int i = 0; i < splatterImages.Length; i++)
        {
            if (splatterImages[i] != null && i < originalSplatterColors.Length)
            {
                splatterImages[i].color = originalSplatterColors[i];
            }
        }

        // 2단계: 지속 시간 대기
        yield return new WaitForSeconds(splatterDuration);

        // 3단계: 페이드아웃
        elapsed = 0f;
        while (elapsed < splatterFadeOutDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / splatterFadeOutDuration;

            for (int i = 0; i < splatterImages.Length; i++)
            {
                if (splatterImages[i] != null && i < originalSplatterColors.Length)
                {
                    Color fadeColor = originalSplatterColors[i];
                    fadeColor.a = Mathf.Lerp(originalSplatterColors[i].a, 0f, t);
                    splatterImages[i].color = fadeColor;
                }
            }

            yield return null;
        }

        // 모든 이미지를 원래 색상으로 복원 (다음 번을 위해)
        for (int i = 0; i < splatterImages.Length; i++)
        {
            if (splatterImages[i] != null && i < originalSplatterColors.Length)
            {
                splatterImages[i].color = originalSplatterColors[i];
            }
        }
    }

    /// <summary>
    /// 개별 이미지 fade in (coroutine으로 병렬 실행)
    /// </summary>
    private IEnumerator FadeInSplatter(int imageIndex)
    {
        if (imageIndex < 0 || imageIndex >= splatterImages.Length)
        {
            yield break;
        }

        Image image = splatterImages[imageIndex];
        if (image == null || imageIndex >= originalSplatterColors.Length)
        {
            yield break;
        }

        Color originalColor = originalSplatterColors[imageIndex];
        float elapsed = 0f;

        while (elapsed < splatterFadeInDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / splatterFadeInDuration;

            Color fadeColor = originalColor;
            fadeColor.a = Mathf.Lerp(0f, originalColor.a, t);
            image.color = fadeColor;

            yield return null;
        }

        // 완전히 원래 색상으로
        image.color = originalColor;
    }

    /// <summary>
    /// 모든 이미지 함께 fade out
    /// </summary>
    private IEnumerator FadeOutAllSplatters()
    {
        float elapsed = 0f;

        while (elapsed < splatterFadeOutDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / splatterFadeOutDuration;

            for (int i = 0; i < splatterImages.Length; i++)
            {
                if (splatterImages[i] != null && i < originalSplatterColors.Length)
                {
                    Color fadeColor = originalSplatterColors[i];
                    fadeColor.a = Mathf.Lerp(originalSplatterColors[i].a, 0f, t);
                    splatterImages[i].color = fadeColor;
                }
            }

            yield return null;
        }

        // 모든 이미지를 원래 색상으로 복원 (다음 번을 위해)
        for (int i = 0; i < splatterImages.Length; i++)
        {
            if (splatterImages[i] != null && i < originalSplatterColors.Length)
            {
                splatterImages[i].color = originalSplatterColors[i];
            }
        }
    }

    /// <summary>
    /// 개별 스플래터 표시 (구버전 - 사용 안 함)
    /// </summary>
    private IEnumerator ShowSingleSplatter(int imageIndex)
    {
        if (imageIndex < 0 || imageIndex >= splatterImages.Length)
        {
            Debug.LogError($"[Splatter Effect] Invalid image index: {imageIndex}");
            yield break;
        }

        Image image = splatterImages[imageIndex];

        if (image == null)
        {
            Debug.LogError($"[Splatter Effect] Splatter image {imageIndex} is null!");
            yield break;
        }

        if (imageIndex >= originalSplatterColors.Length)
        {
            Debug.LogError($"[Splatter Effect] Original color not saved for index {imageIndex}!");
            yield break;
        }

        Color originalColor = originalSplatterColors[imageIndex];

        // 1단계: 페이드인 - 0에서 원래 alpha로
        Color transparentColor = originalColor;
        transparentColor.a = 0f;
        image.color = transparentColor;

        float elapsed = 0f;
        while (elapsed < splatterFadeInDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / splatterFadeInDuration;

            Color fadeColor = originalColor;
            fadeColor.a = Mathf.Lerp(0f, originalColor.a, t);
            image.color = fadeColor;

            yield return null;
        }

        // 완전히 원래 색상으로 (페이드인 완료)
        image.color = originalColor;

        Debug.Log($"[Splatter Effect] Showing splatter {imageIndex}");

        // 2단계: 지속 시간 대기
        yield return new WaitForSeconds(splatterDuration);

        // 3단계: 페이드아웃
        elapsed = 0f;
        while (elapsed < splatterFadeOutDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / splatterFadeOutDuration;

            Color fadeColor = originalColor;
            fadeColor.a = Mathf.Lerp(originalColor.a, 0f, t);
            image.color = fadeColor;

            yield return null;
        }

        // 원래 색상으로 복원 (다음 번을 위해)
        image.color = originalColor;

        Debug.Log($"[Splatter Effect] Splatter {imageIndex} faded out");
    }

    /// <summary>
    /// 플래시 효과: Panel 활성화 → 최대 밝기 → fade out
    /// </summary>
    private IEnumerator PlayFlashEffect()
    {
        // 이미 효과 진행 중이면 무시
        if (isFlashPlaying)
        {
            Debug.LogWarning("[Flash Effect] 이미 플래시 효과 진행 중!");
            yield break;
        }

        if (flashPanel == null)
        {
            Debug.LogError("[CameraShakeManager] flashPanel이 설정되지 않았습니다!");
            yield break;
        }

        if (flashImage == null)
        {
            Debug.LogError("[CameraShakeManager] flashImage가 설정되지 않았습니다!");
            yield break;
        }

        isFlashPlaying = true;

        Debug.Log("[Flash Effect] 플래시 효과 시작!");

        // Panel 활성화
        flashPanel.SetActive(true);

        // 즉시 최대 밝기로 표시
        Color flashColor = originalFlashColor;
        flashColor.a = 1f;
        flashImage.color = flashColor;

        // 서서히 페이드아웃
        float elapsed = 0f;
        while (elapsed < flashDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / flashDuration;

            Color fadeColor = originalFlashColor;
            fadeColor.a = Mathf.Lerp(1f, 0f, t);
            flashImage.color = fadeColor;

            yield return null;
        }

        // 원래 색상으로 복원 (다음 번을 위해)
        flashImage.color = originalFlashColor;

        // Panel 비활성화
        flashPanel.SetActive(false);

        isFlashPlaying = false;

        Debug.Log("[Flash Effect] 플래시 효과 완료!");
    }
}
