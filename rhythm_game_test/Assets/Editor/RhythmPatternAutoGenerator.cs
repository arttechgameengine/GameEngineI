using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections.Generic;

public class RhythmPatternAutoGenerator : EditorWindow
{
    public AudioClip audioClip;

    // 기본 민감도 (멜로디 감지)
    public float melodySensitivity = 0.3f;

    // 노트 속도 영향값
    public float noteSpeedFactor = 1.0f;

    // 최소 간격 (겹침 방지)
    public float minNoteInterval = 0.25f;

    // 난이도 설정 (새로운!)
    public enum DifficultyMode { Easy, Normal, Hard, VeryHard, Custom }
    public DifficultyMode difficulty = DifficultyMode.Normal;

    // BPM 설정
    public float bpm = 120f; // 기본 BPM (120)
    public bool autoDetectOffset = true; // 자동으로 첫 비트 감지

    // 비트 간격 필터링 옵션
    public enum BeatInterval {
        WholeBeat,     // 1박 - 가장 느림
        HalfBeat,      // 1/2박
        QuarterBeat,   // 1/4박 - 보통
        EighthBeat,    // 1/8박 - 빠름
        SixteenthBeat, // 1/16박 - 매우 빠름
        Free           // 자유 (필터링 없음)
    }
    public bool useBeatFiltering = true;
    public BeatInterval beatInterval = BeatInterval.QuarterBeat;

    // 정박 스냅 옵션
    public bool snapToGrid = false; // 정박에 정확히 맞춰 떨어지게

    // 방향 단순화 옵션
    public bool simplifyDirections = false;
    public int maxConsecutiveSameDirection = 2; // 같은 방향 최대 연속 횟수

    // 롱노트 옵션
    public bool enableLongNotes = false;
    public float longNoteThreshold = 0.8f; // 롱노트로 판정할 최소 지속 시간 (초)
    public float sustainedAmplitudeRatio = 0.7f; // 지속 판정 진폭 비율 (peak의 70%)

    // 패링(SPACE) 옵션
    public bool enableParryNotes = true;
    public float minParryInterval = 1.0f; // 패링 노트 간 최소 간격 (초)

    public string saveFileName = "pattern.json";

    private static readonly string[] arrowTypes = { "LEFT", "RIGHT", "UP", "DOWN" };
    private static readonly string[] allTypes = { "LEFT", "RIGHT", "UP", "DOWN", "SPACE" };

    [MenuItem("Tools/Rhythm Pattern Auto Generator")]
    public static void OpenWindow()
    {
        GetWindow<RhythmPatternAutoGenerator>("Rhythm Pattern Auto Generator");
    }

    void OnGUI()
    {
        GUILayout.Label("Auto Rhythm Pattern Generator", EditorStyles.boldLabel);

        audioClip = (AudioClip)EditorGUILayout.ObjectField("Music", audioClip, typeof(AudioClip), false);

        EditorGUILayout.Space();
        GUILayout.Label("Difficulty Settings", EditorStyles.boldLabel);
        difficulty = (DifficultyMode)EditorGUILayout.EnumPopup("Difficulty Preset", difficulty);

        // 난이도 프리셋 적용
        if (difficulty != DifficultyMode.Custom)
        {
            ApplyDifficultyPreset();
        }

        EditorGUILayout.Space();
        GUILayout.Label("Basic Settings", EditorStyles.boldLabel);
        melodySensitivity = EditorGUILayout.Slider("Melody Sensitivity", melodySensitivity, 0.1f, 1.0f);
        noteSpeedFactor = EditorGUILayout.Slider("Note Speed Factor", noteSpeedFactor, 0.3f, 2.0f);
        minNoteInterval = EditorGUILayout.Slider("Min Note Interval", minNoteInterval, 0.1f, 1.0f);

        EditorGUILayout.Space();
        GUILayout.Label("Beat Interval Filtering", EditorStyles.boldLabel);

        // BPM 입력
        bpm = EditorGUILayout.FloatField("BPM (Beats Per Minute)", bpm);
        bpm = Mathf.Clamp(bpm, 60f, 240f); // BPM 범위 제한 (60~240)

        autoDetectOffset = EditorGUILayout.Toggle("Auto Detect First Beat", autoDetectOffset);
        if (!autoDetectOffset)
        {
            EditorGUILayout.HelpBox("자동 감지가 꺼져 있습니다. 곡이 0초부터 시작한다고 가정합니다.", MessageType.Warning);
        }

        useBeatFiltering = EditorGUILayout.Toggle("Use Beat Filtering", useBeatFiltering);
        if (useBeatFiltering)
        {
            EditorGUI.indentLevel++;
            beatInterval = (BeatInterval)EditorGUILayout.EnumPopup("Beat Interval", beatInterval);

            // 정박 스냅 옵션
            snapToGrid = EditorGUILayout.Toggle("Snap to Grid (정박만)", snapToGrid);
            if (snapToGrid)
            {
                EditorGUILayout.HelpBox("노트들이 정확히 정박(BPM 그리드)에만 배치됩니다. 오프비트는 무시됩니다.", MessageType.Info);
            }

            // 설명 표시 (BPM 기반)
            string intervalDesc = GetBeatIntervalDescription(beatInterval, bpm);
            EditorGUILayout.HelpBox(intervalDesc, MessageType.Info);
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space();
        GUILayout.Label("Direction Simplification", EditorStyles.boldLabel);
        simplifyDirections = EditorGUILayout.Toggle("Simplify Directions", simplifyDirections);
        if (simplifyDirections)
        {
            EditorGUI.indentLevel++;
            maxConsecutiveSameDirection = EditorGUILayout.IntSlider("Max Same Direction", maxConsecutiveSameDirection, 1, 5);
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space();
        GUILayout.Label("Note Type Options", EditorStyles.boldLabel);
        enableParryNotes = EditorGUILayout.Toggle("Enable Parry (SPACE)", enableParryNotes);
        if (enableParryNotes)
        {
            EditorGUI.indentLevel++;
            minParryInterval = EditorGUILayout.Slider("Min Parry Interval", minParryInterval, 0.5f, 3.0f);
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space();
        GUILayout.Label("Long Note Options", EditorStyles.boldLabel);
        enableLongNotes = EditorGUILayout.Toggle("Enable Long Notes", enableLongNotes);
        if (enableLongNotes)
        {
            EditorGUI.indentLevel++;
            longNoteThreshold = EditorGUILayout.Slider("Min Sustain Duration", longNoteThreshold, 0.5f, 2.0f);
            sustainedAmplitudeRatio = EditorGUILayout.Slider("Sustain Amplitude Ratio", sustainedAmplitudeRatio, 0.5f, 0.9f);
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space();
        saveFileName = EditorGUILayout.TextField("Save JSON Name", saveFileName);

        if (GUILayout.Button("Generate JSON"))
        {
            Generate();
        }
    }

    /// <summary>
    /// 비트 간격에 대한 설명 반환 (BPM 기반)
    /// </summary>
    string GetBeatIntervalDescription(BeatInterval interval, float bpm)
    {
        float intervalSeconds = GetBeatIntervalInSeconds(interval, bpm);

        switch (interval)
        {
            case BeatInterval.WholeBeat:
                return $"1박 ({intervalSeconds:F3}초) - 가장 느린 페이스, 노트 수 매우 적음";
            case BeatInterval.HalfBeat:
                return $"1/2박 ({intervalSeconds:F3}초) - 느린 페이스, 노트 수 적음 (쉬움)";
            case BeatInterval.QuarterBeat:
                return $"1/4박 ({intervalSeconds:F3}초) - 보통 페이스, 적당한 노트 수 (보통)";
            case BeatInterval.EighthBeat:
                return $"1/8박 ({intervalSeconds:F3}초) - 빠른 페이스, 많은 노트 (어려움)";
            case BeatInterval.SixteenthBeat:
                return $"1/16박 ({intervalSeconds:F3}초) - 매우 빠른 페이스, 매우 많은 노트 (매우 어려움)";
            case BeatInterval.Free:
                return "자유 - 비트 필터링 없음, 모든 감지된 노트 포함";
            default:
                return "";
        }
    }

    /// <summary>
    /// 비트 간격을 초(second) 단위로 변환 (BPM 기반)
    /// 공식: 1박 = 60 / BPM 초
    /// </summary>
    float GetBeatIntervalInSeconds(BeatInterval interval, float bpm)
    {
        // 1박의 길이 (초) = 60초 / BPM
        float oneBeatDuration = 60f / bpm;

        switch (interval)
        {
            case BeatInterval.WholeBeat: return oneBeatDuration;           // 1박
            case BeatInterval.HalfBeat: return oneBeatDuration / 2f;       // 1/2박
            case BeatInterval.QuarterBeat: return oneBeatDuration / 4f;    // 1/4박
            case BeatInterval.EighthBeat: return oneBeatDuration / 8f;     // 1/8박
            case BeatInterval.SixteenthBeat: return oneBeatDuration / 16f; // 1/16박
            case BeatInterval.Free: return 0f;                              // 필터링 없음
            default: return oneBeatDuration / 4f;
        }
    }

    void ApplyDifficultyPreset()
    {
        switch (difficulty)
        {
            case DifficultyMode.Easy:
                bpm = 120f; // 기본 BPM
                melodySensitivity = 0.5f;
                minNoteInterval = 0.5f;
                beatInterval = BeatInterval.WholeBeat; // 1박
                useBeatFiltering = true;
                simplifyDirections = true;
                maxConsecutiveSameDirection = 3;
                enableParryNotes = false;
                enableLongNotes = false;
                break;

            case DifficultyMode.Normal:
                bpm = 120f;
                melodySensitivity = 0.35f;
                minNoteInterval = 0.3f;
                beatInterval = BeatInterval.HalfBeat; // 1/2박
                useBeatFiltering = true;
                simplifyDirections = true;
                maxConsecutiveSameDirection = 2;
                enableParryNotes = true;
                minParryInterval = 2.0f;
                enableLongNotes = true;
                longNoteThreshold = 1.0f;
                break;

            case DifficultyMode.Hard:
                bpm = 120f;
                melodySensitivity = 0.25f;
                minNoteInterval = 0.2f;
                beatInterval = BeatInterval.QuarterBeat; // 1/4박
                useBeatFiltering = true;
                simplifyDirections = false;
                enableParryNotes = true;
                minParryInterval = 1.5f;
                enableLongNotes = true;
                longNoteThreshold = 0.8f;
                break;

            case DifficultyMode.VeryHard:
                bpm = 120f;
                melodySensitivity = 0.2f;
                minNoteInterval = 0.15f;
                beatInterval = BeatInterval.EighthBeat; // 1/8박
                useBeatFiltering = true;
                simplifyDirections = false;
                enableParryNotes = true;
                minParryInterval = 1.0f;
                enableLongNotes = true;
                longNoteThreshold = 0.6f;
                break;
        }
    }

    /// <summary>
    /// 첫 번째 강한 비트 자동 감지
    /// </summary>
    float DetectFirstBeat(float[] samples, float audioFrequency)
    {
        float step = 0.01f; // 10ms 단위로 분석 (더 정밀하게)
        float threshold = melodySensitivity * 0.8f; // 민감도보다 약간 낮은 임계값

        // 처음 5초 내에서 첫 강한 비트 찾기
        float searchDuration = Mathf.Min(5f, audioClip.length);

        for (float t = 0; t < searchDuration; t += step)
        {
            int sampleIndex = (int)(t * audioFrequency);
            if (sampleIndex >= samples.Length) break;

            float amplitude = Mathf.Abs(samples[sampleIndex]);

            // 첫 번째로 임계값을 넘는 강한 비트 발견
            if (amplitude > threshold)
            {
                Debug.Log($"[Auto Offset] 첫 비트 감지: {t:F3}초");
                return t;
            }
        }

        Debug.LogWarning("[Auto Offset] 첫 비트를 찾지 못했습니다. 0초부터 시작합니다.");
        return 0f;
    }

    void Generate()
    {
        if (audioClip == null)
        {
            Debug.LogError("AudioClip is null!");
            return;
        }

        float length = audioClip.length;
        float[] samples = new float[audioClip.samples];

        audioClip.GetData(samples, 0);

        // 첫 비트 자동 감지
        float firstBeatOffset = 0f;
        if (autoDetectOffset)
        {
            firstBeatOffset = DetectFirstBeat(samples, audioClip.frequency);
        }

        List<NoteData> notes = new List<NoteData>();
        List<NoteData> candidateNotes = new List<NoteData>(); // 스마트 필터링용 후보 노트

        float lastNoteTime = -999f;
        float lastParryTime = -999f; // 마지막 패링 노트 시간 추적
        float activeLongNoteEndTime = -999f; // 현재 진행 중인 롱노트의 종료 시간
        float step = 0.05f; // 20 FPS 분석 (빠른 분석)
        int longNoteGroupCounter = 0;

        // 노트 타입 배열 선택
        string[] noteTypePool = enableParryNotes ? allTypes : arrowTypes;

        // 방향 단순화를 위한 변수
        string lastDirection = "";
        int sameDirectionCount = 0;

        for (float t = 0; t < length; t += step)
        {
            int sampleIndex = (int)(t * audioClip.frequency);
            if (sampleIndex < 0 || sampleIndex >= samples.Length) continue;

            float amplitude = Mathf.Abs(samples[sampleIndex]);
            float threshold = melodySensitivity * (1.2f - noteSpeedFactor);

            if (amplitude > threshold)
            {
                if (t - lastNoteTime >= minNoteInterval)
                {
                    string type = noteTypePool[Random.Range(0, noteTypePool.Length)];

                    // 방향 단순화 적용
                    if (simplifyDirections && type != "SPACE")
                    {
                        if (type == lastDirection)
                        {
                            sameDirectionCount++;
                            // 같은 방향이 너무 많으면 다른 방향으로 변경
                            if (sameDirectionCount >= maxConsecutiveSameDirection)
                            {
                                // 현재 방향을 제외한 다른 방향 선택
                                List<string> otherDirections = new List<string>();
                                foreach (string dir in arrowTypes)
                                {
                                    if (dir != lastDirection) otherDirections.Add(dir);
                                }
                                type = otherDirections[Random.Range(0, otherDirections.Count)];
                                sameDirectionCount = 1;
                                lastDirection = type;
                            }
                        }
                        else
                        {
                            sameDirectionCount = 1;
                            lastDirection = type;
                        }
                    }

                    // 패링 노트면 별도의 간격 체크 + 롱노트 진행 중인지 확인
                    if (type == "SPACE")
                    {
                        // 패링 간격이 너무 가깝거나 롱노트 진행 중이면 화살표 노트로 변경
                        if (t - lastParryTime < minParryInterval || t < activeLongNoteEndTime)
                        {
                            type = arrowTypes[Random.Range(0, arrowTypes.Length)];
                        }
                    }
                    // 일반 노트인데 마지막 패링 노트와 너무 가까우면 스킵
                    else if (t - lastParryTime < minParryInterval && lastParryTime > -999f)
                    {
                        continue; // 이 노트는 생성하지 않음
                    }

                    // 롱노트 지속 시간 분석 (SPACE는 롱노트 불가)
                    if (enableLongNotes && type != "SPACE")
                    {
                        float sustainDuration = AnalyzeSustainDuration(samples, t, amplitude, step);

                        // 지속 시간이 임계값 이상이면 롱노트로 생성
                        if (sustainDuration >= longNoteThreshold)
                        {
                            CreateLongNote(notes, t, type, sustainDuration, ref longNoteGroupCounter);
                            lastNoteTime = t + sustainDuration;
                            activeLongNoteEndTime = t + sustainDuration; // 롱노트 종료 시간 기록
                            continue;
                        }
                    }

                    // 일반 노트 (스마트 필터링 사용 시 후보 리스트에 추가)
                    NoteData newNote = new NoteData()
                    {
                        time = t,
                        type = type,
                        noteSubType = "NORMAL",
                        longNoteGroupId = -1
                    };

                    if (useBeatFiltering && beatInterval != BeatInterval.Free)
                    {
                        candidateNotes.Add(newNote);
                    }
                    else
                    {
                        notes.Add(newNote);
                    }

                    lastNoteTime = t;

                    // 패링 노트면 패링 타임 업데이트
                    if (type == "SPACE")
                    {
                        lastParryTime = t;
                    }
                }
            }
        }

        // 비트 간격 필터링 적용
        if (useBeatFiltering && beatInterval != BeatInterval.Free && candidateNotes.Count > 0)
        {
            notes = ApplyBeatFiltering(candidateNotes, firstBeatOffset);
        }

        string json = JsonHelper.ToJson(notes.ToArray(), true);
        File.WriteAllText(Application.dataPath + "/" + saveFileName, json);

        Debug.Log($"JSON created: {saveFileName} ({notes.Count} notes, {longNoteGroupCounter} long notes)");
    }

    /// <summary>
    /// 비트 간격 필터링: 설정된 비트 간격보다 가까운 노트들을 제거
    /// snapToGrid가 true면 정확히 정박에만 노트 배치, false면 원본 타이밍 유지
    /// 예: BPM 120, 1/2박 (0.25초) → 0.25초보다 가까운 노트들 제거
    /// </summary>
    List<NoteData> ApplyBeatFiltering(List<NoteData> candidates, float firstBeatOffset)
    {
        List<NoteData> filtered = new List<NoteData>();
        float beatIntervalSeconds = GetBeatIntervalInSeconds(beatInterval, bpm);

        if (snapToGrid)
        {
            // 정박 스냅 모드: 정확히 BPM 그리드에 맞춰 노트 배치
            // 각 후보 노트를 가장 가까운 그리드 포인트에 스냅
            Dictionary<float, NoteData> gridMap = new Dictionary<float, NoteData>();

            foreach (NoteData note in candidates)
            {
                // 가장 가까운 그리드 포인트 찾기
                float relativeTime = note.time - firstBeatOffset;
                int gridIndex = Mathf.RoundToInt(relativeTime / beatIntervalSeconds);
                float gridTime = firstBeatOffset + (gridIndex * beatIntervalSeconds);

                // 그리드 포인트가 음수가 아니고, 곡 길이를 넘지 않으면
                if (gridTime >= 0 && gridTime < audioClip.length)
                {
                    // 이미 해당 그리드에 노트가 있으면 더 가까운 것만 선택
                    if (!gridMap.ContainsKey(gridTime))
                    {
                        NoteData snappedNote = note;
                        snappedNote.time = gridTime; // 정박에 정확히 스냅
                        gridMap[gridTime] = snappedNote;
                    }
                }
            }

            // 그리드 맵에서 정렬된 노트 리스트 생성
            var sortedKeys = new List<float>(gridMap.Keys);
            sortedKeys.Sort();
            foreach (float key in sortedKeys)
            {
                filtered.Add(gridMap[key]);
            }

            Debug.Log($"[Beat Filtering - SNAP] BPM {bpm}, {beatInterval} ({beatIntervalSeconds:F3}s), Offset {firstBeatOffset:F3}s - {candidates.Count} → {filtered.Count} notes (snapped to grid)");
        }
        else
        {
            // 일반 필터링 모드: 원본 타이밍 유지, 너무 가까운 노트만 제거
            float lastAddedTime = firstBeatOffset - beatIntervalSeconds; // 첫 노트가 오프셋 이후 바로 추가될 수 있도록

            foreach (NoteData note in candidates)
            {
                // 마지막 추가된 노트로부터 비트 간격 이상 떨어져 있으면 추가
                if (note.time - lastAddedTime >= beatIntervalSeconds)
                {
                    // 원본 타이밍 그대로 유지
                    filtered.Add(note);
                    lastAddedTime = note.time;
                }
                // 너무 가까운 노트는 건너뜀 (필터링)
            }

            Debug.Log($"[Beat Filtering] BPM {bpm}, {beatInterval} ({beatIntervalSeconds:F3}s), Offset {firstBeatOffset:F3}s - {candidates.Count} → {filtered.Count} notes");
        }

        return filtered;
    }

    // 음의 지속 시간 분석
    float AnalyzeSustainDuration(float[] samples, float startTime, float peakAmplitude, float step)
    {
        float sustainThreshold = peakAmplitude * sustainedAmplitudeRatio;
        float duration = 0f;
        float t = startTime;

        while (t < audioClip.length)
        {
            int sampleIndex = (int)(t * audioClip.frequency);
            if (sampleIndex < 0 || sampleIndex >= samples.Length) break;

            float amplitude = Mathf.Abs(samples[sampleIndex]);

            // 진폭이 임계값 이하로 떨어지면 지속 종료
            if (amplitude < sustainThreshold)
            {
                break;
            }

            duration += step;
            t += step;
        }

        return duration;
    }

    void CreateLongNote(List<NoteData> notes, float startTime, string type, float duration, ref int groupCounter)
    {
        int groupId = groupCounter++;

        // LONG_START 노트 (duration 정보 포함 - 시각적 막대 길이 계산용)
        notes.Add(new NoteData()
        {
            time = startTime,
            type = type,
            noteSubType = "LONG_START",
            longNoteGroupId = groupId,
            longNoteDuration = duration
        });

        // LONG_HOLD 노트 (0.05초 간격으로 생성 - 거의 겹치도록)
        float holdInterval = 0.05f;
        for (float t = startTime + holdInterval; t < startTime + duration - holdInterval; t += holdInterval)
        {
            notes.Add(new NoteData()
            {
                time = t,
                type = type,
                noteSubType = "LONG_HOLD",
                longNoteGroupId = groupId
            });
        }

        // LONG_END 노트
        notes.Add(new NoteData()
        {
            time = startTime + duration,
            type = type,
            noteSubType = "LONG_END",
            longNoteGroupId = groupId
        });
    }
}

// NoteData는 PatternData.cs에 정의되어 있음

public static class JsonHelper
{
    public static string ToJson<T>(T[] array, bool pretty)
    {
        Wrapper<T> wrapper = new Wrapper<T>();
        wrapper.notes = array;
        return JsonUtility.ToJson(wrapper, pretty);
    }

    [System.Serializable]
    private class Wrapper<T>
    {
        public T[] notes;
    }
}
