using UnityEngine; 
using UnityEngine.UI;

public class SongProgressBar : MonoBehaviour
{
    [Header("References")]
    public AudioSource audioSource;

    [Header("UI")]
    public RectTransform fillBar;  // Fill 이미지의 RectTransform

    private float fullWidth;

    void Start()
    {
        if (fillBar != null)
        {
            fullWidth = fillBar.sizeDelta.x;
            SetProgress(0f);
        }
    }

    void Update()
    {
        if (audioSource == null || audioSource.clip == null) return;
        if (!audioSource.isPlaying && audioSource.time == 0) return;

        float progress = audioSource.time / audioSource.clip.length;
        SetProgress(progress);
    }

    void SetProgress(float progress)
    {
        if (fillBar != null)
        {
            // Width를 직접 조절
            Vector2 size = fillBar.sizeDelta;
            size.x = fullWidth * progress;
            fillBar.sizeDelta = size;
        }
    }
}
