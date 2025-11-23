using UnityEngine; 
using TMPro;
using System.Collections;

public class JudgePopup : MonoBehaviour
{
    public TextMeshProUGUI judgeText;
    public float fadeDuration = 0.3f;
    public float displayDuration = 0.5f;

    private Coroutine currentPopup;

    public void ShowJudge(string judge)
    {
        // 이전 팝업 중단 (겹침 방지)
        if (currentPopup != null)
        {
            StopCoroutine(currentPopup);
        }

        currentPopup = StartCoroutine(PopupRoutine(judge));
    }

    IEnumerator PopupRoutine(string judge)
    {
        judgeText.text = judge;

        // 판정별 색상 설정
        switch (judge)
        {
            case "PERFECT":
                judgeText.color = new Color(1f, 0.84f, 0f, 0f); // 금색
                break;
            case "GREAT":
                judgeText.color = new Color(0f, 1f, 0.5f, 0f); // 초록
                break;
            case "GOOD":
                judgeText.color = new Color(0.3f, 0.7f, 1f, 0f); // 하늘색
                break;
            case "MISS":
                judgeText.color = new Color(1f, 0.3f, 0.3f, 0f); // 빨강
                break;
            default:
                judgeText.color = new Color(1f, 1f, 1f, 0f); // 흰색
                break;
        }

        // Fade In
        float elapsed = 0f;
        Color startColor = judgeText.color;
        Color targetColor = new Color(startColor.r, startColor.g, startColor.b, 1f);

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeDuration;
            judgeText.color = Color.Lerp(startColor, targetColor, t);
            yield return null;
        }
        judgeText.color = targetColor;

        // 잠시 유지
        yield return new WaitForSeconds(displayDuration);

        // Fade Out
        elapsed = 0f;
        startColor = judgeText.color;
        targetColor = new Color(startColor.r, startColor.g, startColor.b, 0f);

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeDuration;
            judgeText.color = Color.Lerp(startColor, targetColor, t);
            yield return null;
        }
        judgeText.color = targetColor;

        currentPopup = null;
    }
}
