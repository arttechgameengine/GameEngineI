using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class JudgePopup : MonoBehaviour
{
    // ===== 기존 Text (유지, 사용 안 해도 됨) =====
    public TextMeshProUGUI judgeText;

    // ===== Image 기반 판정 표시 =====
    public Image judgeImage;
    public Sprite perfectImage;
    public Sprite greatImage;
    public Sprite goodImage;
    public Sprite missImage;

    public float fadeDuration = 0.3f;
    public float displayDuration = 0.5f;

    private Coroutine currentPopup;

    void Awake()
    {
        // 초기 상태: 완전 투명 + 비활성
        if (judgeImage != null)
        {
            Color c = judgeImage.color;
            c.a = 0f;
            judgeImage.color = c;
            judgeImage.gameObject.SetActive(false);
        }

        if (judgeText != null)
            judgeText.gameObject.SetActive(false);
    }

    public void ShowJudge(string judge)
    {
        if (currentPopup != null)
            StopCoroutine(currentPopup);

        currentPopup = StartCoroutine(PopupRoutine(judge));
    }

    IEnumerator PopupRoutine(string judge)
    {
        if (judgeImage != null)
        {
            judgeImage.sprite = GetJudgeSprite(judge);
            Color c = judgeImage.color;
            c.a = 0f;
            judgeImage.color = c;
            judgeImage.gameObject.SetActive(true);
        }

        // Fade In
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeDuration;

            if (judgeImage != null)
            {
                Color c = judgeImage.color;
                c.a = Mathf.Lerp(0f, 1f, t);
                judgeImage.color = c;
            }
            yield return null;
        }

        yield return new WaitForSeconds(displayDuration);

        // Fade Out
        elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeDuration;

            if (judgeImage != null)
            {
                Color c = judgeImage.color;
                c.a = Mathf.Lerp(1f, 0f, t);
                judgeImage.color = c;
            }
            yield return null;
        }

        // 종료 후 완전 투명 + 비활성
        if (judgeImage != null)
        {
            Color c = judgeImage.color;
            c.a = 0f;
            judgeImage.color = c;
            judgeImage.gameObject.SetActive(false);
        }

        currentPopup = null;
    }

    Sprite GetJudgeSprite(string judge)
    {
        switch (judge)
        {
            case "PERFECT": return perfectImage;
            case "GREAT":   return greatImage;
            case "GOOD":    return goodImage;
            case "MISS":    return missImage;
            default:        return null;
        }
    }
}
