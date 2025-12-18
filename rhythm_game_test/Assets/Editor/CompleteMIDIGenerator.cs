using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;

// MIDI 파싱을 위해 DryWetMIDI 라이브러리 필요
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;

/// <summary>
/// 모든 MIDI 노트를 빠짐없이 추출하는 개선된 Generator
/// RapidNoteMIDIGenerator 기반 + 노트 누락 문제 해결
/// 단일 레인만 사용, 모든 MIDI 노트 타이밍 추출
/// </summary>
public class CompleteMIDIGenerator : EditorWindow
{
    private DefaultAsset midiFile;
    private string outputPath = "Assets/Charts/";
    private string songName = "";

    [Header("Chart Settings")]
    private bool includeHoldNotes = true;
    private float holdThreshold = 0.2f;

    [Header("Arrow Key Assignment")]
    private bool includeSpaceBar = false;
    private int spaceBarNoteCount = 5;
    private System.Random randomGen;

    [Header("Auto Alignment")]
    private bool autoAlignFirstNote = true;
    private float offset = 0f;

    [Header("BPM Settings")]
    private bool autoDetectBPM = true;
    private float manualBPM = 120f;

    [Header("Note Speed")]
    private float noteSpeed = 500f;

    [Header("Snapping Settings")]
    private bool enableSnapping = false;
    private float snapTolerance = 0.1f;
    private enum SnapGridSize
    {
        SixteenthNote,  // 16분음표
        EighthNote,     // 8분음표
        QuarterNote,    // 4분음표
        HalfNote,       // 2분음표
        WholeNote       // 온음표
    }
    private SnapGridSize snapGrid = SnapGridSize.SixteenthNote;

    [Header("🔥 Rapid Note Settings")]
    [Tooltip("연타 노트 자동 삽입 활성화")]
    private bool includeRapidNotes = true;

    [Tooltip("연타 노트 개수 (0 = 자동)")]
    private int rapidNoteCount = 3;

    [Tooltip("연타 노트 배치 방식")]
    private RapidPlacementMode rapidPlacementMode = RapidPlacementMode.EveryNBeats;

    [Tooltip("N 박자마다 연타 노트 배치 (EveryNBeats 모드)")]
    private int rapidEveryNBeats = 8;

    [Tooltip("연타 노트 필요 횟수 (최소-최대)")]
    private Vector2Int rapidHitCountRange = new Vector2Int(3, 8);

    [Tooltip("연타 노트 제한 시간 (초, 최소-최대)")]
    private Vector2 rapidDurationRange = new Vector2(0.8f, 1.5f);

    [Tooltip("난이도별 연타 설정 자동 조절")]
    private bool autoScaleRapidByDifficulty = true;

    private enum RapidPlacementMode
    {
        Manual,            // 수동 배치 (개수만 지정)
        EveryNBeats,       // N 박자마다 자동 배치
        ProgressiveSections // 섹션별 점진적 배치
    }

    [System.Serializable]
    private class DifficultySection
    {
        public float startPercentage;
        public int rapidCount;
    }

    private List<DifficultySection> difficultySections = new List<DifficultySection>
    {
        new DifficultySection { startPercentage = 0.0f, rapidCount = 0 },
        new DifficultySection { startPercentage = 0.33f, rapidCount = 1 },
        new DifficultySection { startPercentage = 0.66f, rapidCount = 2 }
    };

    [Header("🔍 Debugging")]
    private bool verboseLogging = false;
    private bool showNoteStatistics = true;

    [MenuItem("Tools/Complete MIDI Generator")]
    public static void ShowWindow()
    {
        GetWindow<CompleteMIDIGenerator>("Complete MIDI Generator");
    }

    private Vector2 scrollPosition;

    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        GUILayout.Label("Complete MIDI Generator", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "✅ 모든 MIDI 노트를 빠짐없이 추출합니다!\n" +
            "• 단일 레인 사용\n" +
            "• 모든 MIDI 노트 타이밍 100% 추출\n" +
            "• Rapid Note 기능 유지",
            MessageType.Info
        );
        EditorGUILayout.Space();

        midiFile = (DefaultAsset)EditorGUILayout.ObjectField("MIDI File", midiFile, typeof(DefaultAsset), false);
        songName = EditorGUILayout.TextField("Song Name", songName);

        EditorGUILayout.BeginHorizontal();
        outputPath = EditorGUILayout.TextField("Output Path", outputPath);
        if (GUILayout.Button("Browse", GUILayout.Width(70)))
        {
            string selectedPath = EditorUtility.OpenFolderPanel("Select Output Folder", outputPath, "");
            if (!string.IsNullOrEmpty(selectedPath))
            {
                if (selectedPath.StartsWith(Application.dataPath))
                    outputPath = "Assets" + selectedPath.Substring(Application.dataPath.Length);
                else
                    outputPath = selectedPath;
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();
        GUILayout.Label("Chart Settings", EditorStyles.boldLabel);

        includeHoldNotes = EditorGUILayout.Toggle("Include Hold Notes", includeHoldNotes);
        if (includeHoldNotes)
            holdThreshold = EditorGUILayout.Slider("Hold Threshold (seconds)", holdThreshold, 0.1f, 2f);

        EditorGUILayout.Space();
        GUILayout.Label("Arrow Key Assignment", EditorStyles.boldLabel);
        includeSpaceBar = EditorGUILayout.Toggle("Include SPACE Bar Notes", includeSpaceBar);
        if (includeSpaceBar)
            spaceBarNoteCount = EditorGUILayout.IntField("Number of SPACE Notes", spaceBarNoteCount);

        EditorGUILayout.Space();
        GUILayout.Label("Timing Settings", EditorStyles.boldLabel);
        autoDetectBPM = EditorGUILayout.Toggle("Auto Detect BPM", autoDetectBPM);
        if (!autoDetectBPM)
            manualBPM = EditorGUILayout.FloatField("Manual BPM", manualBPM);

        autoAlignFirstNote = EditorGUILayout.Toggle("Auto Align First Note to 0", autoAlignFirstNote);
        if (!autoAlignFirstNote)
            offset = EditorGUILayout.FloatField("Manual Offset (seconds)", offset);

        EditorGUILayout.Space();
        noteSpeed = EditorGUILayout.FloatField("Note Speed", noteSpeed);

        // 📐 Snapping Settings
        EditorGUILayout.Space();
        GUILayout.Label("📐 Snapping Settings", EditorStyles.boldLabel);
        enableSnapping = EditorGUILayout.Toggle("Enable Snapping", enableSnapping);

        if (enableSnapping)
        {
            EditorGUI.indentLevel++;
            snapGrid = (SnapGridSize)EditorGUILayout.EnumPopup("Snap Grid", snapGrid);
            snapTolerance = EditorGUILayout.Slider("Snap Tolerance (seconds)", snapTolerance, 0.01f, 0.5f);

            EditorGUILayout.HelpBox(
                "Snapping: MIDI 노트를 가장 가까운 그리드에 정렬합니다.\n" +
                "• SixteenthNote: 16분음표 (가장 정밀)\n" +
                "• EighthNote: 8분음표\n" +
                "• QuarterNote: 4분음표\n" +
                "• HalfNote: 2분음표\n" +
                "• WholeNote: 온음표 (가장 넓음)\n\n" +
                "Tolerance: 이 범위 안의 노트만 그리드에 스냅됩니다.",
                MessageType.Info
            );
            EditorGUI.indentLevel--;
        }

        // 🔥 연타 노트 설정
        EditorGUILayout.Space();
        GUILayout.Label("🔥 Rapid Note Settings", EditorStyles.boldLabel);

        includeRapidNotes = EditorGUILayout.Toggle("Include Rapid Notes", includeRapidNotes);

        if (includeRapidNotes)
        {
            EditorGUI.indentLevel++;

            rapidPlacementMode = (RapidPlacementMode)EditorGUILayout.EnumPopup("Placement Mode", rapidPlacementMode);

            if (rapidPlacementMode == RapidPlacementMode.Manual)
            {
                rapidNoteCount = EditorGUILayout.IntField("Rapid Note Count", rapidNoteCount);
            }
            else if (rapidPlacementMode == RapidPlacementMode.EveryNBeats)
            {
                rapidEveryNBeats = EditorGUILayout.IntSlider("Every N Beats", rapidEveryNBeats, 4, 32);
            }

            EditorGUILayout.Space();
            rapidHitCountRange = EditorGUILayout.Vector2IntField("Hit Count Range (Min-Max)", rapidHitCountRange);
            rapidDurationRange = EditorGUILayout.Vector2Field("Duration Range (s)", rapidDurationRange);
            autoScaleRapidByDifficulty = EditorGUILayout.Toggle("Auto Scale by Difficulty", autoScaleRapidByDifficulty);

            EditorGUILayout.HelpBox(
                "연타 노트 배치 모드:\n" +
                "• Manual: 지정한 개수만큼 랜덤 배치\n" +
                "• EveryNBeats: N 박자마다 자동 배치\n" +
                "• ProgressiveSections: 섹션별 점진적 배치\n\n" +
                "Hit Count: 연타 필요 횟수 (예: 3~8회)\n" +
                "Duration: 연타 제한 시간 (예: 0.8~1.5초)",
                MessageType.Info
            );

            EditorGUI.indentLevel--;
        }

        // 🔍 디버깅 옵션
        EditorGUILayout.Space();
        GUILayout.Label("🔍 Debugging Options", EditorStyles.boldLabel);
        verboseLogging = EditorGUILayout.Toggle("Verbose Logging", verboseLogging);
        showNoteStatistics = EditorGUILayout.Toggle("Show Note Statistics", showNoteStatistics);

        EditorGUILayout.Space();

        if (GUILayout.Button("Generate Complete Chart", GUILayout.Height(40)))
        {
            if (midiFile != null)
                ConvertMidiToJson();
            else
                EditorUtility.DisplayDialog("Error", "Please select a MIDI file!", "OK");
        }

        EditorGUILayout.EndScrollView();
    }

    private void ConvertMidiToJson()
    {
        string midiPath = AssetDatabase.GetAssetPath(midiFile);
        randomGen = new System.Random(midiPath.GetHashCode());

        try
        {
            var midiFile = MidiFile.Read(midiPath);
            var tempoMap = midiFile.GetTempoMap();

            // BPM 설정 (더 정확한 감지)
            float bpm;
            if (autoDetectBPM)
            {
                // MIDI 파일의 모든 Tempo 변화를 확인
                var tempoChanges = midiFile.GetTempoMap().GetTempoChanges();

                if (tempoChanges.Any())
                {
                    // 첫 번째 템포 사용
                    var firstTempo = tempoChanges.First();
                    bpm = (float)(60000000.0 / firstTempo.Value.MicrosecondsPerQuarterNote);
                    Debug.Log($"[CompleteMIDI] ✅ Detected BPM from tempo changes: {bpm}");

                    // 여러 템포 변화가 있으면 경고
                    if (tempoChanges.Count() > 1)
                    {
                        Debug.LogWarning($"[CompleteMIDI] ⚠️ Multiple tempo changes detected ({tempoChanges.Count()}). Using first tempo: {bpm}");
                        foreach (var tc in tempoChanges.Take(5))
                        {
                            float tempoBpm = (float)(60000000.0 / tc.Value.MicrosecondsPerQuarterNote);
                            Debug.LogWarning($"  - Tempo at {tc.Time}: {tempoBpm} BPM");
                        }
                    }
                }
                else
                {
                    // Tempo 변화가 없으면 기본 템포 사용
                    var tempo = tempoMap.GetTempoAtTime(new MetricTimeSpan(0));
                    bpm = (float)(60000000.0 / tempo.MicrosecondsPerQuarterNote);
                    Debug.Log($"[CompleteMIDI] Detected BPM from default tempo: {bpm}");
                }
            }
            else
            {
                bpm = manualBPM;
                Debug.Log($"[CompleteMIDI] Using manual BPM: {bpm}");
            }

            // ✅ 모든 MIDI 노트 추출 (100% 누락 없음!)
            var chartNotes = ExtractAllNotesFromMIDI(midiFile, tempoMap, bpm, out float songDuration, out float firstNoteOffset);

            Debug.Log($"[CompleteMIDI] ✅ Extracted ALL {chartNotes.Count} notes from MIDI!");

            // 🔥 연타 노트 삽입
            if (includeRapidNotes)
            {
                InsertRapidNotes(chartNotes, bpm, songDuration, firstNoteOffset);
            }

            // 시간순 정렬
            chartNotes = chartNotes.OrderBy(n => n.time).ToList();

            // 연타 노트 충돌 제거 (HitLine 도달 시간 + 연타 시간 고려)
            if (includeRapidNotes)
            {
                chartNotes = RemoveRapidNoteConflicts(chartNotes, noteSpeed);
            }

            // 방향키 랜덤 할당
            AssignArrowKeys(chartNotes);

            // 통계 출력
            if (showNoteStatistics)
            {
                ShowStatistics(chartNotes, songDuration);
            }

            // JSON 생성
            var chart = new PatternData
            {
                songName = string.IsNullOrEmpty(songName) ? Path.GetFileNameWithoutExtension(midiPath) : songName,
                bpm = bpm,
                offset = firstNoteOffset,
                numberOfLanes = 1,  // 단일 레인!
                noteSpeed = noteSpeed,
                notes = chartNotes
            };

            string json = JsonUtility.ToJson(chart, true);

            // 파일 저장
            if (!Directory.Exists(outputPath))
                Directory.CreateDirectory(outputPath);

            string fileName = $"{chart.songName}_complete_chart.json";
            string fullPath = Path.Combine(outputPath, fileName);
            File.WriteAllText(fullPath, json);

            AssetDatabase.Refresh();

            int rapidCount = chartNotes.Count(n => n.type == "rapid");
            EditorUtility.DisplayDialog(
                "Success",
                $"✅ Complete Chart Generated!\n\n" +
                $"Total Notes: {chartNotes.Count}\n" +
                $"Rapid Notes: {rapidCount}\n" +
                $"Lanes: 1 (Single Lane)\n" +
                $"Saved to: {fullPath}",
                "OK"
            );

            Debug.Log($"[CompleteMIDI] ✅ Chart conversion complete: {chartNotes.Count} notes ({rapidCount} rapid)");
        }
        catch (System.Exception e)
        {
            EditorUtility.DisplayDialog("Error", $"Failed to convert MIDI:\n{e.Message}", "OK");
            Debug.LogError(e);
        }
    }

    /// <summary>
    /// ✅ 모든 MIDI 노트를 100% 추출 (누락 없음!)
    /// </summary>
    private List<NoteData> ExtractAllNotesFromMIDI(MidiFile midi, TempoMap tempoMap, float bpm, out float songDuration, out float firstNoteOffset)
    {
        var allMidiNotes = midi.GetNotes();
        var chartNotes = new List<NoteData>();

        Debug.Log($"[CompleteMIDI] 🎵 Total MIDI notes found: {allMidiNotes.Count()}");

        // 첫 노트 offset 계산
        firstNoteOffset = 0f;
        if (autoAlignFirstNote && allMidiNotes.Any())
        {
            var firstNote = allMidiNotes.OrderBy(n => n.Time).First();
            var firstMetricTime = TimeConverter.ConvertTo<MetricTimeSpan>(firstNote.Time, tempoMap);
            firstNoteOffset = -(float)firstMetricTime.TotalSeconds;
            Debug.Log($"[CompleteMIDI] Auto-detected first note offset: {firstNoteOffset}s");
        }
        else
        {
            firstNoteOffset = offset;
        }

        // 곡 전체 길이
        songDuration = 0f;
        if (allMidiNotes.Any())
        {
            var lastNote = allMidiNotes.OrderBy(n => n.Time + n.Length).Last();
            var lastMetricTime = TimeConverter.ConvertTo<MetricTimeSpan>(lastNote.Time + lastNote.Length, tempoMap);
            songDuration = (float)lastMetricTime.TotalSeconds;
        }

        Debug.Log($"[CompleteMIDI] Song duration: {songDuration:F2}s");

        // 📊 MIDI 노트 시간대별 분포 확인 (디버깅)
        var midiNotesBySecond = allMidiNotes
            .GroupBy(n => {
                var mt = TimeConverter.ConvertTo<MetricTimeSpan>(n.Time, tempoMap);
                return Mathf.FloorToInt((float)mt.TotalSeconds);
            })
            .OrderBy(g => g.Key)
            .ToList();

        Debug.Log($"[CompleteMIDI] 📊 MIDI Note Distribution by Second:");
        foreach (var group in midiNotesBySecond.Take(20))
        {
            Debug.Log($"  {group.Key}s: {group.Count()} notes");
        }
        if (midiNotesBySecond.Count > 20)
        {
            Debug.Log($"  ... and {midiNotesBySecond.Count - 20} more seconds");
        }

        // Snap 그리드 간격 계산
        float snapGridInterval = 0f;
        if (enableSnapping)
        {
            snapGridInterval = GetSnapGridInterval(bpm);
            Debug.Log($"[CompleteMIDI] 📐 Snapping enabled - Grid: {snapGrid}, Interval: {snapGridInterval:F3}s, Tolerance: {snapTolerance:F3}s");
        }

        // ✅ 모든 MIDI 노트를 하나도 빠뜨리지 않고 추출!
        int noteIndex = 0;
        int snappedCount = 0;
        foreach (var midiNote in allMidiNotes.OrderBy(n => n.Time))
        {
            var metricTime = TimeConverter.ConvertTo<MetricTimeSpan>(midiNote.Time, tempoMap);
            float originalTime = (float)metricTime.TotalSeconds + firstNoteOffset;
            float time = originalTime;

            // 📐 Snapping 적용
            if (enableSnapping && snapGridInterval > 0)
            {
                float snappedTime = SnapToGrid(originalTime, snapGridInterval);
                float snapDistance = Mathf.Abs(snappedTime - originalTime);

                if (snapDistance <= snapTolerance)
                {
                    time = snappedTime;
                    snappedCount++;

                    if (verboseLogging)
                        Debug.Log($"[CompleteMIDI] 📐 Snapped: {originalTime:F3}s -> {snappedTime:F3}s (distance: {snapDistance:F3}s)");
                }
                else
                {
                    if (verboseLogging)
                        Debug.Log($"[CompleteMIDI] No snap: {originalTime:F3}s (distance {snapDistance:F3}s > tolerance {snapTolerance:F3}s)");
                }
            }

            var metricLength = LengthConverter.ConvertTo<MetricTimeSpan>(midiNote.Length, midiNote.Time, tempoMap);
            float duration = (float)metricLength.TotalSeconds;

            string noteType = (includeHoldNotes && duration >= holdThreshold) ? "hold" : "tap";

            var chartNote = new NoteData
            {
                time = Mathf.Round(time * 1000f) / 1000f,
                lane = 0,  // 단일 레인만 사용!
                type = noteType,
                arrow = "",
                duration = (noteType == "hold") ? Mathf.Round(duration * 1000f) / 1000f : 0
            };

            chartNotes.Add(chartNote);
            noteIndex++;

            if (verboseLogging)
                Debug.Log($"[CompleteMIDI] Note {noteIndex}/{allMidiNotes.Count()}: time={time:F3}s, type={noteType}, duration={duration:F3}s");
        }

        if (enableSnapping)
        {
            Debug.Log($"[CompleteMIDI] 📐 Snapped {snappedCount}/{chartNotes.Count} notes to grid");
        }

        Debug.Log($"[CompleteMIDI] ✅ Successfully extracted ALL {chartNotes.Count} notes!");

        return chartNotes;
    }

    /// <summary>
    /// 🔥 연타 노트 자동 삽입
    /// </summary>
    private void InsertRapidNotes(List<NoteData> chartNotes, float bpm, float songDuration, float firstNoteOffset)
    {
        float beatDuration = 60f / bpm;
        List<NoteData> rapidNotes = new List<NoteData>();

        switch (rapidPlacementMode)
        {
            case RapidPlacementMode.Manual:
                rapidNotes = GenerateManualRapidNotes(chartNotes, songDuration, beatDuration);
                break;

            case RapidPlacementMode.EveryNBeats:
                rapidNotes = GenerateEveryNBeatsRapidNotes(chartNotes, songDuration, beatDuration);
                break;

            case RapidPlacementMode.ProgressiveSections:
                rapidNotes = GenerateProgressiveRapidNotes(chartNotes, songDuration, beatDuration);
                break;
        }

        // 모든 연타 노트에 offset 적용
        foreach (var rapidNote in rapidNotes)
        {
            rapidNote.time += firstNoteOffset;
        }

        chartNotes.AddRange(rapidNotes);
        Debug.Log($"[CompleteMIDI] Generated {rapidNotes.Count} rapid notes");
    }

    private List<NoteData> GenerateManualRapidNotes(List<NoteData> existingNotes, float songDuration, float beatDuration)
    {
        List<NoteData> rapidNotes = new List<NoteData>();
        int count = Mathf.Max(1, rapidNoteCount);

        var candidateNotes = existingNotes
            .Where(n => n.type == "tap" || n.type == "hold")
            .OrderBy(n => n.time)
            .ToList();

        if (candidateNotes.Count == 0)
        {
            Debug.LogWarning("[CompleteMIDI] No notes available for Manual rapid mode");
            return rapidNotes;
        }

        int actualCount = Mathf.Min(count, candidateNotes.Count);
        var selectedNotes = candidateNotes.OrderBy(x => Random.value).Take(actualCount).ToList();

        foreach (var selectedNote in selectedNotes)
        {
            var rapidNote = CreateRapidNote(selectedNote.time, beatDuration, songDuration);
            rapidNotes.Add(rapidNote);

            if (verboseLogging)
                Debug.Log($"[CompleteMIDI] Manual rapid at {selectedNote.time:F2}s");
        }

        return rapidNotes;
    }

    private List<NoteData> GenerateEveryNBeatsRapidNotes(List<NoteData> existingNotes, float songDuration, float beatDuration)
    {
        List<NoteData> rapidNotes = new List<NoteData>();
        float interval = beatDuration * rapidEveryNBeats;
        float nextRapidTime = interval;

        var sortedNotes = existingNotes
            .Where(n => n.type == "tap" || n.type == "hold")
            .OrderBy(n => n.time)
            .ToList();

        while (nextRapidTime < songDuration - interval)
        {
            var nearbyNote = sortedNotes
                .Where(n => Mathf.Abs(n.time - nextRapidTime) <= beatDuration)
                .OrderBy(n => Mathf.Abs(n.time - nextRapidTime))
                .FirstOrDefault();

            if (nearbyNote != null)
            {
                var rapidNote = CreateRapidNote(nearbyNote.time, beatDuration, songDuration);
                rapidNotes.Add(rapidNote);

                if (verboseLogging)
                    Debug.Log($"[CompleteMIDI] EveryNBeats rapid at {nearbyNote.time:F2}s");
            }

            nextRapidTime += interval;
        }

        return rapidNotes;
    }

    private List<NoteData> GenerateProgressiveRapidNotes(List<NoteData> existingNotes, float songDuration, float beatDuration)
    {
        List<NoteData> rapidNotes = new List<NoteData>();

        var candidateNotes = existingNotes
            .Where(n => n.type == "tap" || n.type == "hold")
            .OrderBy(n => n.time)
            .ToList();

        if (candidateNotes.Count == 0)
        {
            Debug.LogWarning("[CompleteMIDI] No notes for ProgressiveSections rapid mode");
            return rapidNotes;
        }

        for (int sectionIdx = 0; sectionIdx < difficultySections.Count; sectionIdx++)
        {
            var section = difficultySections[sectionIdx];
            float sectionStartTime = section.startPercentage * songDuration;
            float sectionEndTime = (sectionIdx + 1 < difficultySections.Count)
                ? difficultySections[sectionIdx + 1].startPercentage * songDuration
                : songDuration;

            var sectionNotes = candidateNotes
                .Where(n => n.time >= sectionStartTime && n.time < sectionEndTime)
                .ToList();

            if (sectionNotes.Count == 0)
                continue;

            int rapidCountInSection = section.rapidCount;
            if (rapidCountInSection <= 0)
                continue;

            int actualCount = Mathf.Min(rapidCountInSection, sectionNotes.Count);

            var selectedNotes = sectionNotes.OrderBy(x => Random.value).Take(actualCount).ToList();

            foreach (var selectedNote in selectedNotes)
            {
                var rapidNote = CreateRapidNote(selectedNote.time, beatDuration, songDuration);
                rapidNotes.Add(rapidNote);

                if (verboseLogging)
                    Debug.Log($"[CompleteMIDI] Progressive rapid at {selectedNote.time:F2}s (Section {sectionIdx})");
            }
        }

        return rapidNotes;
    }

    private NoteData CreateRapidNote(float time, float beatDuration, float songDuration)
    {
        int hitCount = Random.Range(rapidHitCountRange.x, rapidHitCountRange.y + 1);
        float duration = Random.Range(rapidDurationRange.x, rapidDurationRange.y);

        if (autoScaleRapidByDifficulty)
        {
            float progress = time / songDuration;
            hitCount = Mathf.RoundToInt(Mathf.Lerp(rapidHitCountRange.x, rapidHitCountRange.y, progress));
            duration = Mathf.Lerp(rapidDurationRange.y, rapidDurationRange.x, progress);
        }

        return new NoteData
        {
            time = Mathf.Round(time * 1000f) / 1000f,
            lane = 0,  // 단일 레인!
            type = "rapid",
            arrow = "",
            rapidCount = hitCount,
            rapidDuration = duration
        };
    }

    private void AssignArrowKeys(List<NoteData> notes)
    {
        string[] arrowKeys = { "UP", "DOWN", "LEFT", "RIGHT" };
        List<int> spaceIndices = new List<int>();

        if (includeSpaceBar && spaceBarNoteCount > 0)
        {
            List<int> tapNoteIndices = new List<int>();
            for (int i = 0; i < notes.Count; i++)
            {
                if (notes[i].type == "tap")
                    tapNoteIndices.Add(i);
            }

            int actualSpaceCount = Mathf.Min(spaceBarNoteCount, tapNoteIndices.Count);
            for (int i = 0; i < actualSpaceCount; i++)
            {
                int randomIndex = randomGen.Next(i, tapNoteIndices.Count);
                int temp = tapNoteIndices[i];
                tapNoteIndices[i] = tapNoteIndices[randomIndex];
                tapNoteIndices[randomIndex] = temp;
            }
            spaceIndices = tapNoteIndices.Take(actualSpaceCount).ToList();
        }

        for (int i = 0; i < notes.Count; i++)
        {
            if (spaceIndices.Contains(i))
            {
                notes[i].arrow = "SPACE";
            }
            else
            {
                notes[i].arrow = arrowKeys[randomGen.Next(arrowKeys.Length)];
            }
        }
    }

    private List<NoteData> RemoveRapidNoteConflicts(List<NoteData> notes, float noteSpeed)
    {
        List<(float hitLineTime, float endTime)> rapidZones = new List<(float, float)>();

        foreach (var note in notes)
        {
            if (note.type == "rapid")
            {
                float hitLineTime = note.time;
                float endTime = hitLineTime + note.rapidDuration;
                rapidZones.Add((hitLineTime, endTime));

                if (verboseLogging)
                    Debug.Log($"[CompleteMIDI] Rapid zone: {hitLineTime:F2} ~ {endTime:F2}");
            }
        }

        List<NoteData> result = new List<NoteData>();

        foreach (var note in notes)
        {
            if (note.type == "rapid")
            {
                result.Add(note);
                continue;
            }

            bool inConflict = false;
            float noteHitLineTime = note.time;

            foreach (var zone in rapidZones)
            {
                if (noteHitLineTime >= zone.hitLineTime && noteHitLineTime < zone.endTime)
                {
                    inConflict = true;

                    if (verboseLogging)
                        Debug.Log($"[CompleteMIDI] Removed {note.arrow} at {note.time:F2} - conflicts with rapid");
                    break;
                }
            }

            if (!inConflict)
            {
                result.Add(note);
            }
        }

        int removedCount = notes.Count - result.Count;
        if (removedCount > 0)
        {
            Debug.Log($"[CompleteMIDI] Removed {removedCount} notes due to rapid conflicts");
        }

        return result;
    }

    /// <summary>
    /// 📐 그리드 간격 계산 (BPM 기반)
    /// </summary>
    private float GetSnapGridInterval(float bpm)
    {
        float quarterNoteDuration = 60f / bpm;  // 4분음표 길이

        switch (snapGrid)
        {
            case SnapGridSize.WholeNote:
                return quarterNoteDuration * 4f;  // 온음표
            case SnapGridSize.HalfNote:
                return quarterNoteDuration * 2f;  // 2분음표
            case SnapGridSize.QuarterNote:
                return quarterNoteDuration;       // 4분음표
            case SnapGridSize.EighthNote:
                return quarterNoteDuration / 2f;  // 8분음표
            case SnapGridSize.SixteenthNote:
                return quarterNoteDuration / 4f;  // 16분음표
            default:
                return quarterNoteDuration / 4f;
        }
    }

    /// <summary>
    /// 📐 가장 가까운 그리드에 스냅
    /// </summary>
    private float SnapToGrid(float time, float gridInterval)
    {
        if (gridInterval <= 0) return time;

        int gridIndex = Mathf.RoundToInt(time / gridInterval);
        return gridIndex * gridInterval;
    }

    private void ShowStatistics(List<NoteData> notes, float songDuration)
    {
        int totalNotes = notes.Count;
        int tapNotes = notes.Count(n => n.type == "tap");
        int holdNotes = notes.Count(n => n.type == "hold");
        int rapidNotes = notes.Count(n => n.type == "rapid");

        float avgNoteDensity = totalNotes / songDuration;

        var timeGroups = notes.GroupBy(n => Mathf.FloorToInt(n.time / 10f) * 10);
        var densityPerSection = timeGroups.Select(g => new { Time = g.Key, Count = g.Count() }).ToList();

        Debug.Log("=== Chart Statistics ===");
        Debug.Log($"Total Notes: {totalNotes}");
        Debug.Log($"  - Tap: {tapNotes}");
        Debug.Log($"  - Hold: {holdNotes}");
        Debug.Log($"  - Rapid: {rapidNotes}");
        Debug.Log($"Song Duration: {songDuration:F2}s");
        Debug.Log($"Avg Note Density: {avgNoteDensity:F2} notes/sec");
        Debug.Log($"Lanes: 1 (Single Lane)");

        Debug.Log("Note Distribution (10s intervals):");
        foreach (var section in densityPerSection)
        {
            Debug.Log($"  {section.Time}s ~ {section.Time + 10}s: {section.Count} notes");
        }
        Debug.Log("========================");
    }
}