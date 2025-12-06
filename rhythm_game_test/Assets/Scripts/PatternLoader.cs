using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Linq;

public static class PatternLoader
{
    // LONG_HOLD 노트 생성 간격 (초)
    private const float LONG_HOLD_INTERVAL = 0.25f;

    public static PatternData LoadPattern(string fileName)
    {
        string path = Path.Combine(Application.dataPath, fileName);
        if (!File.Exists(path))
        {
            Debug.LogError("JSON 파일 찾을 수 없음: " + path);
            return new PatternData();
        }

        string jsonText = File.ReadAllText(path);
        return Load(jsonText);
    }

    public static PatternData Load(string jsonText)
    {
        PatternData pattern = JsonUtility.FromJson<PatternData>(jsonText);

        // Hold 노트를 LONG_START/HOLD/END로 변환
        ConvertHoldNotesToLongNotes(pattern);

        return pattern;
    }

    /// <summary>
    /// "hold" 타입 노트를 LONG_START, LONG_HOLD, LONG_END로 분리
    /// </summary>
    static void ConvertHoldNotesToLongNotes(PatternData pattern)
    {
        if (pattern == null || pattern.notes == null) return;

        List<NoteData> newNotes = new List<NoteData>();
        int longNoteGroupId = 0;

        foreach (var note in pattern.notes)
        {
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