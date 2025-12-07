using UnityEngine;
using System.Linq;

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
        PatternData pattern = PatternLoader.Load(jsonPattern.text);

        // 준비 시간 적용
        ApplyPrepareTime(pattern, prepareTime);

        spawner.LoadPattern(pattern);

        // 음악 즉시 시작 (준비 시간은 노트 타이밍에 이미 반영됨)
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
