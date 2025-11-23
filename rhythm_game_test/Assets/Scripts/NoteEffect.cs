using UnityEngine; 
using UnityEngine.UI;
using System.Collections;

public class NoteEffect : MonoBehaviour
{
    private Image image;
    private RectTransform rect;

    [Header("Overlay")]
    public Image overlayImage;  // Inspector에서 연결할 오버레이 이미지

    [Header("Hit Effect Settings")]
    public float flashDuration = 0.15f;
    public float scaleMultiplier = 1.3f;
    public Color flashColor = Color.white;

    [Header("Miss Effect Settings")]
    public float missFadeDuration = 0.3f;
    public Color missColor = new Color(0.5f, 0.5f, 0.5f, 1f); // 회색

    private Color originalColor;
    private Vector3 originalScale;

    void Awake()
    {
        image = GetComponent<Image>();
        rect = GetComponent<RectTransform>();

        if (image != null)
        {
            originalColor = image.color;
        }
        originalScale = transform.localScale;

        // 오버레이 초기에 숨김
        if (overlayImage != null)
        {
            Color c = overlayImage.color;
            c.a = 0f;
            overlayImage.color = c;
        }
    }

    // 히트 시 번쩍이며 사라지는 효과
    public void PlayHitEffect(System.Action onComplete)
    {
        StartCoroutine(HitEffectRoutine(onComplete));
    }

    // 미스 시 회색으로 변하며 사라지는 효과
    public void PlayMissEffect(System.Action onComplete)
    {
        StartCoroutine(MissEffectRoutine(onComplete));
    }

    IEnumerator HitEffectRoutine(System.Action onComplete)
    {
        // 이동 멈춤
        var movement = GetComponent<NoteMovement>();
        if (movement != null) movement.enabled = false;

        // 오버레이 즉시 표시 (흰색 번쩍 효과)
        if (overlayImage != null)
        {
            Color c = flashColor;
            c.a = 1f;
            overlayImage.color = c;
        }

        float elapsed = 0f;

        // 오버레이 페이드아웃 + 노트 페이드아웃 + 커지기
        while (elapsed < flashDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / flashDuration;

            // 오버레이 페이드아웃
            if (overlayImage != null)
            {
                Color c = flashColor;
                c.a = 1f - t;
                overlayImage.color = c;
            }

            // 노트 이미지 페이드아웃
            if (image != null)
            {
                Color c = originalColor;
                c.a = 1f - t;
                image.color = c;
            }

            // 커지기
            float scale = Mathf.Lerp(1f, scaleMultiplier, t);
            transform.localScale = originalScale * scale;

            yield return null;
        }

        onComplete?.Invoke();
    }

    IEnumerator MissEffectRoutine(System.Action onComplete)
    {
        // 이동 멈춤
        var movement = GetComponent<NoteMovement>();
        if (movement != null) movement.enabled = false;

        // 즉시 회색으로 변경
        if (image != null)
        {
            image.color = missColor;
        }

        float elapsed = 0f;

        // 회색 상태로 페이드아웃
        while (elapsed < missFadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / missFadeDuration;

            if (image != null)
            {
                Color currentColor = missColor;
                currentColor.a = 1f - t;
                image.color = currentColor;
            }

            yield return null;
        }

        onComplete?.Invoke();
    }
}
