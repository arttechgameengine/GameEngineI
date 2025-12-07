using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;

// MIDI 파싱을 위해 DryWetMIDI 라이브러리 필요
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;

/// <summary>
/// 연타 노트를 자동으로 생성하는 MIDI Generator
/// 기존 Progressive Generator 기반 + 연타 노트 자동 삽입
/// </summary>
public class RapidNoteMIDIGenerator : EditorWindow
{
    private DefaultAsset midiFile;
    private string outputPath = "Assets/Charts/";
    private string songName = "";

    [Header("Chart Settings")]
    private int numberOfLanes = 4;
    private bool includeHoldNotes = true;
    private float holdThreshold = 0.2f;

    [Header("Long Note Settings")]
    private float longNoteHoldInterval = 0.25f;

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

    [Header("MIDI Mapping")]
    private bool ignoreLanes = false;
    private int lowestMidiNote = 60;

    [Header("Progressive Difficulty")]
    private bool useProgressiveDifficulty = true;
    private float noteSpeed = 500f;

    private enum BeatInterval
    {
        WholeBeat,
        HalfBeat,
        QuarterBeat,
        EighthBeat,
        SixteenthBeat,
        ThirtySecondBeat
    }

    [System.Serializable]
    private class DifficultySection
    {
        public float startPercentage;
        public BeatInterval beatInterval;
        public int addNoteCount;
    }

    private List<DifficultySection> difficultySections = new List<DifficultySection>
    {
        new DifficultySection { startPercentage = 0.0f, beatInterval = BeatInterval.QuarterBeat, addNoteCount = 0 },
        new DifficultySection { startPercentage = 0.33f, beatInterval = BeatInterval.EighthBeat, addNoteCount = 10 },
        new DifficultySection { startPercentage = 0.66f, beatInterval = BeatInterval.SixteenthBeat, addNoteCount = 15 }
    };

    private float quantizeTolerance = 0.1f;
    private bool snapToGrid = true;

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

    [MenuItem("Tools/Rapid Note MIDI Generator")]
    public static void ShowWindow()
    {
        GetWindow<RapidNoteMIDIGenerator>("Rapid MIDI Generator");
    }

    private Vector2 scrollPosition;

    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        GUILayout.Label("Rapid Note MIDI Generator", EditorStyles.boldLabel);
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

        numberOfLanes = EditorGUILayout.IntSlider("Number of Lanes", numberOfLanes, 1, 8);
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
        GUILayout.Label("MIDI Mapping", EditorStyles.boldLabel);
        ignoreLanes = EditorGUILayout.Toggle("Ignore Pitch (Timing Only)", ignoreLanes);
        if (!ignoreLanes)
            lowestMidiNote = EditorGUILayout.IntField("Lowest MIDI Note", lowestMidiNote);

        EditorGUILayout.Space();
        GUILayout.Label("Progressive Difficulty Settings", EditorStyles.boldLabel);
        useProgressiveDifficulty = EditorGUILayout.Toggle("Use Progressive Difficulty", useProgressiveDifficulty);

        if (useProgressiveDifficulty)
        {
            EditorGUI.indentLevel++;
            quantizeTolerance = EditorGUILayout.Slider("Snap Tolerance (seconds)", quantizeTolerance, 0.01f, 0.2f);
            snapToGrid = EditorGUILayout.Toggle("Snap to Perfect Grid", snapToGrid);
            noteSpeed = EditorGUILayout.FloatField("Note Speed", noteSpeed);
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

        EditorGUILayout.Space();

        if (GUILayout.Button("Generate Chart with Rapid Notes", GUILayout.Height(40)))
        {
            if (midiFile != null)
                ConvertMidiToJson();
            else
                EditorUtility.DisplayDialog("Error", "Please select a MIDI file!", "OK");
        }

        EditorGUILayout.EndScrollView();
    }

    private float GetBeatIntervalInSeconds(BeatInterval interval, float bpm)
    {
        float quarterNoteDuration = 60f / bpm;
        switch (interval)
        {
            case BeatInterval.WholeBeat: return quarterNoteDuration * 4f;
            case BeatInterval.HalfBeat: return quarterNoteDuration * 2f;
            case BeatInterval.QuarterBeat: return quarterNoteDuration;
            case BeatInterval.EighthBeat: return quarterNoteDuration / 2f;
            case BeatInterval.SixteenthBeat: return quarterNoteDuration / 4f;
            case BeatInterval.ThirtySecondBeat: return quarterNoteDuration / 8f;
            default: return quarterNoteDuration;
        }
    }

    private void ConvertMidiToJson()
    {
        string midiPath = AssetDatabase.GetAssetPath(midiFile);
        randomGen = new System.Random(midiPath.GetHashCode());

        try
        {
            var midiFile = MidiFile.Read(midiPath);
            var tempoMap = midiFile.GetTempoMap();

            // BPM 설정
            float bpm;
            if (autoDetectBPM)
            {
                var tempo = tempoMap.GetTempoAtTime(new MetricTimeSpan(0));
                bpm = (float)(60000000.0 / tempo.MicrosecondsPerQuarterNote);
                Debug.Log($"Detected BPM: {bpm}");
            }
            else
            {
                bpm = manualBPM;
            }

            // MIDI 노트 추출 (기존 방식과 동일)
            var chartNotes = ExtractNotesFromMIDI(midiFile, tempoMap, bpm, out float songDuration, out float firstNoteOffset);

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

            // JSON 생성
            var chart = new PatternData
            {
                songName = string.IsNullOrEmpty(songName) ? Path.GetFileNameWithoutExtension(midiPath) : songName,
                bpm = bpm,
                offset = firstNoteOffset,
                numberOfLanes = numberOfLanes,
                noteSpeed = noteSpeed,
                notes = chartNotes
            };

            string json = JsonUtility.ToJson(chart, true);

            // 파일 저장
            if (!Directory.Exists(outputPath))
                Directory.CreateDirectory(outputPath);

            string fileName = $"{chart.songName}_rapid_chart.json";
            string fullPath = Path.Combine(outputPath, fileName);
            File.WriteAllText(fullPath, json);

            AssetDatabase.Refresh();

            int rapidCount = chartNotes.Count(n => n.type == "rapid");
            EditorUtility.DisplayDialog(
                "Success",
                $"Chart generated with Rapid Notes!\n\n" +
                $"Total Notes: {chartNotes.Count}\n" +
                $"Rapid Notes: {rapidCount}\n" +
                $"Saved to: {fullPath}",
                "OK"
            );

            Debug.Log($"Chart conversion complete: {chartNotes.Count} notes ({rapidCount} rapid)");
        }
        catch (System.Exception e)
        {
            EditorUtility.DisplayDialog("Error", $"Failed to convert MIDI:\n{e.Message}", "OK");
            Debug.LogError(e);
        }
    }

    /// <summary>
    /// MIDI에서 노트 추출 (ProgressiveDifficultyMIDIGenerator와 동일)
    /// </summary>
    private List<NoteData> ExtractNotesFromMIDI(MidiFile midi, TempoMap tempoMap, float bpm, out float songDuration, out float firstNoteOffset)
    {
        var notes = midi.GetNotes();
        var chartNotes = new List<NoteData>();

        // 첫 노트 offset 계산
        firstNoteOffset = 0f;
        if (autoAlignFirstNote && notes.Any())
        {
            var firstNote = notes.OrderBy(n => n.Time).First();
            var firstMetricTime = TimeConverter.ConvertTo<MetricTimeSpan>(firstNote.Time, tempoMap);
            firstNoteOffset = -(float)firstMetricTime.TotalSeconds;
            Debug.Log($"Auto-detected first note offset: {firstNoteOffset}s");
        }
        else
        {
            firstNoteOffset = offset;
        }

        // 곡 전체 길이
        songDuration = 0f;
        if (notes.Any())
        {
            var lastNote = notes.OrderBy(n => n.Time).Last();
            var lastMetricTime = TimeConverter.ConvertTo<MetricTimeSpan>(lastNote.Time, tempoMap);
            songDuration = (float)lastMetricTime.TotalSeconds;
        }

        Debug.Log($"[RapidGenerator] Song duration: {songDuration:F2}s");

        // Progressive 모드 처리 (ProgressiveDifficultyMIDIGenerator 전체 복사)
        if (useProgressiveDifficulty)
        {
            Dictionary<float, List<Melanchall.DryWetMidi.Interaction.Note>> allTimeBasedNotes =
                new Dictionary<float, List<Melanchall.DryWetMidi.Interaction.Note>>();

            var sortedSections = difficultySections.OrderBy(s => s.startPercentage).ToList();

            // 각 섹션마다 해당 구간의 노트들을 처리
            for (int sectionIdx = 0; sectionIdx < sortedSections.Count; sectionIdx++)
            {
                var section = sortedSections[sectionIdx];
                float sectionStartPercent = section.startPercentage;
                float sectionEndPercent = (sectionIdx + 1 < sortedSections.Count)
                    ? sortedSections[sectionIdx + 1].startPercentage
                    : 1.0f;

                float sectionStartTime = sectionStartPercent * songDuration;
                float sectionEndTime = sectionEndPercent * songDuration;

                BeatInterval newInterval = section.beatInterval;
                int addCount = section.addNoteCount;

                Debug.Log($"[RapidGenerator] Section {sectionIdx}: {sectionStartPercent:P0}-{sectionEndPercent:P0}, Interval: {newInterval}, Count: {addCount}");

                // 이 Beat Interval에 해당하는 그리드 시간 수집
                Dictionary<float, List<Melanchall.DryWetMidi.Interaction.Note>> sectionIntervalNotes =
                    new Dictionary<float, List<Melanchall.DryWetMidi.Interaction.Note>>();

                float beatIntervalSeconds = GetBeatIntervalInSeconds(newInterval, bpm);

                foreach (var note in notes)
                {
                    var metricTime = TimeConverter.ConvertTo<MetricTimeSpan>(note.Time, tempoMap);
                    float originalTime = (float)metricTime.TotalSeconds;

                    if (originalTime < sectionStartTime || originalTime >= sectionEndTime)
                        continue;

                    int gridIndex = Mathf.RoundToInt(originalTime / beatIntervalSeconds);
                    float gridTime = gridIndex * beatIntervalSeconds;
                    float distance = Mathf.Abs(originalTime - gridTime);

                    if (distance <= quantizeTolerance)
                    {
                        if (!sectionIntervalNotes.ContainsKey(gridTime))
                            sectionIntervalNotes[gridTime] = new List<Melanchall.DryWetMidi.Interaction.Note>();
                        sectionIntervalNotes[gridTime].Add(note);
                    }
                }

                // 골고루 샘플링
                var gridTimes = sectionIntervalNotes.Keys.OrderBy(t => t).ToList();
                List<float> selectedGridTimes = new List<float>();

                if (addCount <= 0 || addCount >= gridTimes.Count)
                {
                    selectedGridTimes = gridTimes;
                }
                else
                {
                    float step = (float)gridTimes.Count / addCount;
                    for (int i = 0; i < addCount; i++)
                    {
                        int index = Mathf.RoundToInt(i * step);
                        if (index >= gridTimes.Count) index = gridTimes.Count - 1;
                        selectedGridTimes.Add(gridTimes[index]);
                    }
                }

                // 전체 노트 목록에 추가
                foreach (var gridTime in selectedGridTimes)
                {
                    if (!allTimeBasedNotes.ContainsKey(gridTime))
                        allTimeBasedNotes[gridTime] = new List<Melanchall.DryWetMidi.Interaction.Note>();
                    allTimeBasedNotes[gridTime].AddRange(sectionIntervalNotes[gridTime]);
                }
            }

            Debug.Log($"[RapidGenerator] Found {allTimeBasedNotes.Count} total grid points");

            // 각 그리드 시간마다 노트 생성
            foreach (var kvp in allTimeBasedNotes.OrderBy(x => x.Key))
            {
                float gridTime = kvp.Key;
                var notesInGrid = kvp.Value;

                var closestNote = notesInGrid.OrderBy(n =>
                {
                    var mt = TimeConverter.ConvertTo<MetricTimeSpan>(n.Time, tempoMap);
                    float ot = (float)mt.TotalSeconds;
                    return Mathf.Abs(ot - gridTime);
                }).First();

                int midiNoteNumber = closestNote.NoteNumber;
                int lane = ignoreLanes ? randomGen.Next(numberOfLanes) : (midiNoteNumber - lowestMidiNote);

                if (!ignoreLanes && (lane < 0 || lane >= numberOfLanes))
                    continue;

                var metricTime = TimeConverter.ConvertTo<MetricTimeSpan>(closestNote.Time, tempoMap);
                float originalTime = (float)metricTime.TotalSeconds;
                float time = snapToGrid ? (gridTime + firstNoteOffset) : (originalTime + firstNoteOffset);

                var metricLength = LengthConverter.ConvertTo<MetricTimeSpan>(closestNote.Length, closestNote.Time, tempoMap);
                float duration = (float)metricLength.TotalSeconds;

                string noteType = (includeHoldNotes && duration >= holdThreshold) ? "hold" : "tap";

                var chartNote = new NoteData
                {
                    time = Mathf.Round(time * 1000f) / 1000f,
                    lane = lane,
                    type = noteType,
                    arrow = "",
                    duration = (noteType == "hold") ? Mathf.Round(duration * 1000f) / 1000f : 0
                };

                chartNotes.Add(chartNote);
            }
        }

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

        // 연타 노트 추가
        chartNotes.AddRange(rapidNotes);
        Debug.Log($"[RapidNoteMIDIGenerator] Generated {rapidNotes.Count} rapid notes (offset: {firstNoteOffset:F2}s)");
    }

    /// <summary>
    /// Manual 모드: 기존 MIDI 노트 중 랜덤으로 N개 선택하여 rapid note로 변환
    /// </summary>
    private List<NoteData> GenerateManualRapidNotes(List<NoteData> existingNotes, float songDuration, float beatDuration)
    {
        List<NoteData> rapidNotes = new List<NoteData>();
        int count = Mathf.Max(1, rapidNoteCount);

        // offset이 적용되지 않은 MIDI 노트들만 필터링 (tap/hold)
        var candidateNotes = existingNotes
            .Where(n => n.type == "tap" || n.type == "hold")
            .OrderBy(n => n.time)
            .ToList();

        if (candidateNotes.Count == 0)
        {
            Debug.LogWarning("[RapidGenerator] No MIDI notes available for Manual mode");
            return rapidNotes;
        }

        // 랜덤으로 N개 선택
        int actualCount = Mathf.Min(count, candidateNotes.Count);
        var selectedNotes = candidateNotes.OrderBy(x => Random.value).Take(actualCount).ToList();

        foreach (var selectedNote in selectedNotes)
        {
            var rapidNote = CreateRapidNote(selectedNote.time, beatDuration);
            rapidNotes.Add(rapidNote);
            Debug.Log($"[RapidGenerator] Manual mode: Created rapid note at {selectedNote.time:F2}s (from MIDI note)");
        }

        return rapidNotes;
    }

    /// <summary>
    /// EveryNBeats 모드: N 박자마다 MIDI 노트가 있으면 rapid note 배치 (없으면 스킵)
    /// </summary>
    private List<NoteData> GenerateEveryNBeatsRapidNotes(List<NoteData> existingNotes, float songDuration, float beatDuration)
    {
        List<NoteData> rapidNotes = new List<NoteData>();
        float interval = beatDuration * rapidEveryNBeats;
        float nextRapidTime = interval;

        // MIDI 노트만 필터링 (tap/hold)
        var sortedNotes = existingNotes
            .Where(n => n.type == "tap" || n.type == "hold")
            .OrderBy(n => n.time)
            .ToList();

        while (nextRapidTime < songDuration - interval)
        {
            // 이 시간대 근처에 있는 MIDI 노트 찾기
            var nearbyNote = sortedNotes
                .Where(n => Mathf.Abs(n.time - nextRapidTime) <= beatDuration)
                .OrderBy(n => Mathf.Abs(n.time - nextRapidTime))
                .FirstOrDefault();

            if (nearbyNote != null)
            {
                // 가까운 MIDI 노트 위치에 rapid note 생성
                var rapidNote = CreateRapidNote(nearbyNote.time, beatDuration);
                rapidNotes.Add(rapidNote);

                Debug.Log($"[RapidGenerator] EveryNBeats: Created rapid note at {nearbyNote.time:F2}s (near interval {nextRapidTime:F2}s)");
            }
            else
            {
                // 근처에 MIDI 노트가 없으면 스킵
                Debug.Log($"[RapidGenerator] EveryNBeats: Skipped interval {nextRapidTime:F2}s (no nearby MIDI note)");
            }

            nextRapidTime += interval;
        }

        return rapidNotes;
    }

    /// <summary>
    /// ProgressiveSections 모드: 각 섹션 내 MIDI 노트 중 랜덤으로 선택하여 배치
    /// </summary>
    private List<NoteData> GenerateProgressiveRapidNotes(List<NoteData> existingNotes, float songDuration, float beatDuration)
    {
        List<NoteData> rapidNotes = new List<NoteData>();

        // MIDI 노트만 필터링 (tap/hold)
        var candidateNotes = existingNotes
            .Where(n => n.type == "tap" || n.type == "hold")
            .OrderBy(n => n.time)
            .ToList();

        if (candidateNotes.Count == 0)
        {
            Debug.LogWarning("[RapidGenerator] No MIDI notes available for ProgressiveSections mode");
            return rapidNotes;
        }

        // 섹션별로 연타 노트 증가
        for (int sectionIdx = 0; sectionIdx < difficultySections.Count; sectionIdx++)
        {
            var section = difficultySections[sectionIdx];
            float sectionStartTime = section.startPercentage * songDuration;
            float sectionEndTime = (sectionIdx + 1 < difficultySections.Count)
                ? difficultySections[sectionIdx + 1].startPercentage * songDuration
                : songDuration;

            // 이 섹션 내 MIDI 노트들만 필터링
            var sectionNotes = candidateNotes
                .Where(n => n.time >= sectionStartTime && n.time < sectionEndTime)
                .ToList();

            if (sectionNotes.Count == 0)
            {
                Debug.LogWarning($"[RapidGenerator] No MIDI notes in section {sectionIdx} ({sectionStartTime:F2}s ~ {sectionEndTime:F2}s)");
                continue;
            }

            // 섹션마다 연타 노트 개수 증가 (섹션 1: 1개, 섹션 2: 2개, ...)
            int rapidCountInSection = sectionIdx + 1;
            int actualCount = Mathf.Min(rapidCountInSection, sectionNotes.Count);

            // 랜덤으로 선택
            var selectedNotes = sectionNotes.OrderBy(x => Random.value).Take(actualCount).ToList();

            foreach (var selectedNote in selectedNotes)
            {
                var rapidNote = CreateRapidNote(selectedNote.time, beatDuration);
                rapidNotes.Add(rapidNote);
                Debug.Log($"[RapidGenerator] ProgressiveSections: Created rapid note at {selectedNote.time:F2}s (Section {sectionIdx})");
            }
        }

        return rapidNotes;
    }

    /// <summary>
    /// 연타 노트 생성
    /// </summary>
    private NoteData CreateRapidNote(float time, float beatDuration)
    {
        int hitCount = Random.Range(rapidHitCountRange.x, rapidHitCountRange.y + 1);
        float duration = Random.Range(rapidDurationRange.x, rapidDurationRange.y);

        // 난이도 자동 조절
        if (autoScaleRapidByDifficulty)
        {
            float progress = time / 100f; // 곡 진행도 (간단화)
            hitCount = Mathf.RoundToInt(Mathf.Lerp(rapidHitCountRange.x, rapidHitCountRange.y, progress));
            duration = Mathf.Lerp(rapidDurationRange.y, rapidDurationRange.x, progress); // 시간은 줄어듦
        }

        return new NoteData
        {
            time = Mathf.Round(time * 1000f) / 1000f,
            lane = 0,
            type = "rapid",
            arrow = "", // 나중에 AssignArrowKeys에서 할당
            rapidCount = hitCount,
            rapidDuration = duration
        };
    }

    /// <summary>
    /// 방향키 할당 (기존 방식)
    /// </summary>
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

    /// <summary>
    /// 연타 노트와 충돌하는 노트 제거
    /// 연타 노트 HitLine 도달 ~ rapidDuration 종료 구간에 다른 노트가 HitLine에 도달하면 제거
    /// </summary>
    private List<NoteData> RemoveRapidNoteConflicts(List<NoteData> notes, float noteSpeed)
    {
        // 각 연타 노트의 보호 구간 계산
        List<(float hitLineTime, float endTime)> rapidZones = new List<(float, float)>();

        foreach (var note in notes)
        {
            if (note.type == "rapid")
            {
                // 노트가 HitLine에 도달하는 시간 = noteTime
                float hitLineTime = note.time;

                // 연타 종료 시간 = HitLine 도달 시간 + rapidDuration
                float endTime = hitLineTime + note.rapidDuration;

                rapidZones.Add((hitLineTime, endTime));
                Debug.Log($"[RapidGenerator] Rapid zone protection: HitLine {hitLineTime:F2} ~ End {endTime:F2}");
            }
        }

        // 충돌하는 노트 제거
        List<NoteData> result = new List<NoteData>();

        foreach (var note in notes)
        {
            // 연타 노트 자신은 유지
            if (note.type == "rapid")
            {
                result.Add(note);
                continue;
            }

            bool inConflict = false;

            // 이 노트가 HitLine에 도달하는 시간
            float noteHitLineTime = note.time;

            // 각 연타 구간과 충돌 검사
            foreach (var zone in rapidZones)
            {
                // 이 노트가 연타 진행 중에 HitLine에 도달하는지 체크
                if (noteHitLineTime >= zone.hitLineTime && noteHitLineTime < zone.endTime)
                {
                    inConflict = true;
                    Debug.Log($"[RapidGenerator] Removed {note.arrow} at {note.time:F2} - conflicts with rapid zone");
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
            Debug.Log($"[RapidGenerator] Removed {removedCount} notes due to rapid conflicts");
        }

        return result;
    }
}
