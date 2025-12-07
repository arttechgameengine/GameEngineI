using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Linq;

public static class PatternLoader
{
    // LONG_HOLD 노트 생성 간격 (초)
    private const float LONG_HOLD_INTERVAL = 0.25f;

    public static PatternData LoadPattern(string fileName, DifficultyFilter.Difficulty difficulty = DifficultyFilter.Difficulty.Normal)
    {
        string path = Path.Combine(Application.dataPath, fileName);
        if (!File.Exists(path))
        {
            Debug.LogError("JSON 파일 찾을 수 없음: " + path);
            return new PatternData();
        }

        string jsonText = File.ReadAllText(path);
        return Load(jsonText, difficulty);
    }

    public static PatternData Load(string jsonText, DifficultyFilter.Difficulty difficulty = DifficultyFilter.Difficulty.Normal)
    {
        PatternData pattern = JsonUtility.FromJson<PatternData>(jsonText);

        // offset 적용: 모든 노트 시간에서 offset을 빼서 0초 기준으로 정렬
        ApplyOffset(pattern);

        // 난이도 필터 적용 (Hold 변환 전에 적용)
        pattern.notes = DifficultyFilter.ApplyFilter(pattern.notes, difficulty);
        Debug.Log($"[PatternLoader] Applied {difficulty} filter - {pattern.notes.Count} notes remaining");

        // Hold 노트를 LONG_START/HOLD/END로 변환
        ConvertHoldNotesToLongNotes(pattern);

        return pattern;
    }

    /// <summary>
    /// 모든 노트 시간에 offset을 적용하여 첫 노트가 0초 기준으로 시작하도록 조정
    /// offset이 음수면 더하고, 양수면 빼서 첫 노트 시간을 0으로 만듦
    /// </summary>
    static void ApplyOffset(PatternData pattern)
    {
        if (pattern == null || pattern.notes == null || pattern.notes.Count == 0) return;

        float offset = pattern.offset;
        Debug.Log($"[PatternLoader] JSON offset value: {offset:F3}");

        // offset이 음수인 경우: 첫 노트 시간이 음수였던 것 -> 더해서 0으로
        // offset이 양수인 경우: 첫 노트 시간이 양수였던 것 -> 빼서 0으로
        // 결과적으로 항상 빼기 (offset = -(firstNoteTime)이므로)
        foreach (var note in pattern.notes)
        {
            note.time -= offset;
        }

        Debug.Log($"[PatternLoader] Offset applied. First note time: {pattern.notes.OrderBy(n => n.time).First().time:F3}");
    }

    /// <summary>
    /// "hold" 타입 노트를 LONG_START, LONG_HOLD, LONG_END로 분리
    /// "rapid" 타입 노트는 noteSubType만 설정
    /// </summary>
    static void ConvertHoldNotesToLongNotes(PatternData pattern)
    {
        if (pattern == null || pattern.notes == null) return;

        List<NoteData> newNotes = new List<NoteData>();
        int longNoteGroupId = 0;

        foreach (var note in pattern.notes)
        {
            // Rapid 노트는 그대로 유지 (변환 안 함)
            if (note.type == "rapid")
            {
                note.noteSubType = "RAPID";
                note.longNoteGroupId = -1;
                note.longNoteDuration = 0;
                newNotes.Add(note);
                continue;
            }

            if (note.type == "hold" && note.duration > 0)
            {
                // Hold 노트를 Long Note로 변환
                float startTime = note.time;
                float endTime = note.time + note.duration;

                // LONG_START 노트
                NoteData startNote = new NoteData
                {
                    time = startTime,
                    lane = note.lane,
                    type = "hold",           // type은 "hold" 유지
                    arrow = note.arrow,      // arrow는 방향키
                    duration = note.duration,
                    noteSubType = "LONG_START",
                    longNoteGroupId = longNoteGroupId,
                    longNoteDuration = note.duration
                };
                newNotes.Add(startNote);

                // LONG_HOLD 노트들 (중간에 일정 간격으로 배치)
                float currentTime = startTime + LONG_HOLD_INTERVAL;
                while (currentTime < endTime - 0.01f) // 끝 노트 직전까지
                {
                    NoteData holdNote = new NoteData
                    {
                        time = currentTime,
                        lane = note.lane,
                        type = "hold",
                        arrow = note.arrow,
                        duration = 0,
                        noteSubType = "LONG_HOLD",
                        longNoteGroupId = longNoteGroupId,
                        longNoteDuration = 0
                    };
                    newNotes.Add(holdNote);
                    currentTime += LONG_HOLD_INTERVAL;
                }

                // LONG_END 노트
                NoteData endNote = new NoteData
                {
                    time = endTime,
                    lane = note.lane,
                    type = "hold",
                    arrow = note.arrow,
                    duration = 0,
                    noteSubType = "LONG_END",
                    longNoteGroupId = longNoteGroupId,
                    longNoteDuration = 0
                };
                newNotes.Add(endNote);

                longNoteGroupId++;
            }
            else
            {
                // 일반 tap 노트
                note.noteSubType = "NORMAL";
                note.longNoteGroupId = -1;
                note.longNoteDuration = 0;
                // type과 arrow는 JSON에서 이미 설정되어 있음
                newNotes.Add(note);
            }
        }

        // 시간순 정렬
        pattern.notes = newNotes.OrderBy(n => n.time).ToList();

        Debug.Log($"[PatternLoader] Loaded {pattern.notes.Count} notes (long notes converted)");
    }
}