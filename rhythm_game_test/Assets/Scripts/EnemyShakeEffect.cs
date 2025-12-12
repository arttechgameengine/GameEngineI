using UnityEngine;
using System.Collections;

/// <summary>
/// Enemy Sprite를 흔드는 효과
/// 패링 노트가 맞았을 때 사용
/// </summary>
public class EnemyShakeEffect : MonoBehaviour
{
    [Header("Shake Settings")]
    [Tooltip("흔들림 강도")]
    public float shakeIntensity = 20f;

    [Tooltip("흔들림 지속 시간")]
    public float shakeDuration = 0.3f;

    [Tooltip("흔들림 감쇠 속도 (클수록 빨리 줄어듦)")]
    public float dampingSpeed = 3f;

    private RectTransform rectTransform;
    private Vector2 originalPosition;
    private bool isShaking = false;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            originalPosition = rectTransform.anchoredPosition;
        }
    }

    /// <summary>
    /// Enemy를 흔드는 효과 재생
    /// </summary>
    public void PlayShake()
    {
        if (rectTransform == null)
        {
            Debug.LogWarning("[EnemyShakeEffect] RectTransform을 찾을 수 없습니다!");
            return;
        }

        if (isShaking)
        {
            // 이미 흔들리고 있으면 코루틴 중단 후 재시작
            StopAllCoroutines();
        }

        StartCoroutine(ShakeRoutine());
    }

    private IEnumerator ShakeRoutine()
    {
        isShaking = true;
        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            elapsed += Time.deltaTime;

            // 시간에 따라 점점 약해지는 강도 계산
            float progress = elapsed / shakeDuration;
            float currentIntensity = shakeIntensity * (1f - Mathf.Pow(progress, dampingSpeed));

            // 랜덤 방향으로 흔들기
            float x = Random.Range(-1f, 1f) * currentIntensity;
            float y = Random.Range(-1f, 1f) * currentIntensity;

            rectTransform.anchoredPosition = originalPosition + new Vector2(x, y);

            yield return null;
        }

        // 원래 위치로 복귀
        rectTransform.anchoredPosition = originalPosition;
        isShaking = false;

        Debug.Log("[EnemyShakeEffect] Enemy shake 완료!");
    }

    /// <summary>
    /// 즉시 흔들림 중지하고 원래 위치로 복귀
    /// </summary>
    public void StopShake()
    {
        if (isShaking)
        {
            StopAllCoroutines();
            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = originalPosition;
            }
            isShaking = false;
        }
    }
}
