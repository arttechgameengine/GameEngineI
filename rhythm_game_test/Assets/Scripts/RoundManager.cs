using UnityEngine;
using System.Linq;
using System.Collections;

public class RoundManager : MonoBehaviour
{
    public NoteSpawner spawner;
    public AudioSource bgmSource;
    public TextAsset jsonPattern;

    [Header("Timing Settings")]
    [Tooltip("음악 시작 후 첫 노트까지 준비 시간 (초)")]
    public float prepareTime = 3f;

    void Start()
    {
        // 라운드 시작 시 ingredient idle 애니메이션 재개 (이전 라운드에서 멈췄을 수 있음)
        if (CookingAreaManager.Instance != null)
        {
            CookingAreaManager.Instance.ResumeAllIdleAnimations();
            Debug.Log("[RoundManager] Resumed all ingredient idle animations");
        }

        PatternData pattern = PatternLoader.Load(jsonPattern.text);

        // 준비 시간 적용
        ApplyPrepareTime(pattern, prepareTime);

        spawner.LoadPattern(pattern);

        // ⚠️ SceneFader의 fade in이 완료될 때까지 대기한 후 음악 시작
        StartCoroutine(WaitForFadeInAndStartMusic());
    }

    /// <summary>
    /// 팝업 먼저 표시, Fade In 완료 후 팝업 닫힘 대기, 그 다음 음악 시작
    /// </summary>
    IEnumerator WaitForFadeInAndStartMusic()
    {
        // 1. 팝업 UI 먼저 표시 (Fade In과 동시에, Time.timeScale은 건드리지 않음)
        if (RoundStartPopup.Instance != null)
        {
            RoundStartPopup.Instance.ShowPopup();
            Debug.Log("[RoundManager] Popup UI shown (during fade in, timeScale not changed yet)");
        }

        Debug.Log("[RoundManager] Waiting for SceneFader fade in to complete...");

        // 2. SceneFader의 fade in이 완료될 때까지 대기
        while (!SceneFader.IsFadeInComplete)
        {
            yield return null;
        }

        Debug.Log("[RoundManager] Fade in complete!");

        // 3. Fade In 완료 후 게임 일시정지 (Time.timeScale = 0)
        if (RoundStartPopup.Instance != null)
        {
            RoundStartPopup.Instance.PauseGame();
            Debug.Log("[RoundManager] Game paused (Time.timeScale = 0)");
        }

        // 4. 팝업이 닫힐 때까지 대기 (팝업이 있으면)
        if (RoundStartPopup.Instance != null)
        {
            Debug.Log("[RoundManager] Waiting for popup to close...");

            while (!RoundStartPopup.IsPopupClosed)
            {
                yield return null;
            }

            Debug.Log("[RoundManager] Popup closed!");
        }

        Debug.Log("[RoundManager] Starting music now...");

        // 5. 음악 시작 (음악은 즉시 시작, 노트는 prepareTime만큼 늦게)
        spawner.StartSong(bgmSource);
    }

    /// <summary>
    /// 준비 시간을 모든 노트에 적용 (음악은 즉시 시작, 노트는 prepareTime 후 시작)
    /// </summary>
    void ApplyPrepareTime(PatternData pattern, float prepareTime)
    {
        if (pattern == null || pattern.notes == null) return;

        Debug.Log($"[RoundManager] Applying prepare time: {prepareTime}s to {pattern.notes.Count} notes");

        foreach (var note in pattern.notes)
        {
            note.time += prepareTime;
        }

        if (pattern.notes.Count > 0)
        {
            var firstNote = pattern.notes.OrderBy(n => n.time).First();
            Debug.Log($"[RoundManager] First note time after prepare: {firstNote.time:F3}s ({firstNote.arrow} {firstNote.noteSubType})");
        }
    }
}
