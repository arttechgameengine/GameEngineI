using UnityEngine;
using System.Collections;

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

    [Header("Target Reference")]
    [Tooltip("흔들릴 UI Panel (Canvas 안의 Panel)")]
    public RectTransform shakeablePanel;

    private Vector2 originalPosition;
    private bool isShaking = false;

    void Start()
    {
        if (shakeablePanel != null)
        {
            originalPosition = shakeablePanel.anchoredPosition;
            Debug.Log($"[CameraShakeManager] ShakeablePanel 찾음! 원래 위치: {originalPosition}");
        }
        else
        {
            Debug.LogError("[CameraShakeManager] ShakeablePanel이 설정되지 않았습니다! Inspector에서 설정하세요.");
        }
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
}
