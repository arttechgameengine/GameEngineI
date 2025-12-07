using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;

// MIDI 파싱을 위해 DryWetMIDI 라이브러리 필요
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;

/// <summary>
/// 누락 비율을 조절할 수 있는 MIDI Generator
/// 첫 노트와 끝 노트는 항상 포함 보장
/// </summary>
public class SamplingMIDIGenerator : EditorWindow
{
    private DefaultAsset midiFile;
    private string outputPath = "Assets/Charts/";
    private string songName = "";

    [Header("Chart Settings")]
    private int numberOfLanes = 4;
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

    [Header("MIDI Mapping")]
    private bool ignoreLanes = false;
    private int lowestMidiNote = 60;

    [Header("Sampling Settings")]
    private float noteSpeed = 500f;

    [Tooltip("노트 샘플링 비율 (0.0 ~ 1.0) - 1.0 = 모든 노트, 0.5 = 50%만, 0.1 = 10%만")]
    [Range(0.01f, 1.0f)]
    private float samplingRate = 0.5f;

    [Tooltip("첫 노트와 끝 노트 항상 포함")]
    private bool guaranteeFirstAndLast = true;

    private enum BeatInterval
    {
        WholeBeat,
        HalfBeat,
        QuarterBeat,
        EighthBeat,
        SixteenthBeat,
        ThirtySecondBeat
    }

    private BeatInterval beatInterval = BeatInterval.QuarterBeat;
    private float quantizeTolerance = 0.1f;
    private bool snapToGrid = true;

    [MenuItem("Tools/Sampling MIDI Generator")]
    public static void ShowWindow()
    {
        GetWindow<SamplingMIDIGenerator>("Sampling MIDI Generator");
    }

    private Vector2 scrollPosition;

    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        GUILayout.Label("Sampling MIDI Generator", EditorStyles.boldLabel);
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
        GUILayout.Label("Sampling Settings", EditorStyles.boldLabel);

        beatInterval = (BeatInterval)EditorGUILayout.EnumPopup("Beat Interval", beatInterval);
        quantizeTolerance = EditorGUILayout.Slider("Snap Tolerance (seconds)", quantizeTolerance, 0.01f, 0.2f);
        snapToGrid = EditorGUILayout.Toggle("Snap to Perfect Grid", snapToGrid);
        noteSpeed = EditorGUILayout.FloatField("Note Speed", noteSpeed);

        EditorGUILayout.Space();
        samplingRate = EditorGUILayout.Slider("Sampling Rate (노트 추출 비율)", samplingRate, 0.01f, 1.0f);
        guaranteeFirstAndLast = EditorGUILayout.Toggle("Guarantee First & Last Notes", guaranteeFirstAndLast);

        EditorGUILayout.HelpBox(
            $"샘플링 비율: {samplingRate:P0}\n" +
            $"• 1.0 (100%) = 모든 노트 포함\n" +
            $"• 0.5 (50%) = 절반만 골고루 추출\n" +
            $"• 0.1 (10%) = 10%만 추출\n\n" +
            (guaranteeFirstAndLast ? "첫 노트와 끝 노트는 항상 포함됩니다." : "첫/끝 노트도 샘플링 대상입니다."),
            MessageType.Info
        );

        EditorGUILayout.Space();

        if (GUILayout.Button("Generate Sampled Chart", GUILayout.Height(40)))
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

            // MIDI 노트 추출
            var notes = midiFile.GetNotes();
            var chartNotes = new List<NoteData>();

            // 첫 노트 offset 계산
            float firstNoteOffset = 0f;
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

            // Beat Interval 그리드에 스냅
            float beatIntervalSeconds = GetBeatIntervalInSeconds(beatInterval, bpm);
            Dictionary<float, List<Melanchall.DryWetMidi.Interaction.Note>> gridNoteGroups =
                new Dictionary<float, List<Melanchall.DryWetMidi.Interaction.Note>>();

            foreach (var note in notes)
            {
                var metricTime = TimeConverter.ConvertTo<MetricTimeSpan>(note.Time, tempoMap);
                float originalTime = (float)metricTime.TotalSeconds;

                int gridIndex = Mathf.RoundToInt(originalTime / beatIntervalSeconds);
                float gridTime = gridIndex * beatIntervalSeconds;
                float distance = Mathf.Abs(originalTime - gridTime);

                if (distance <= quantizeTolerance)
                {
                    if (!gridNoteGroups.ContainsKey(gridTime))
                        gridNoteGroups[gridTime] = new List<Melanchall.DryWetMidi.Interaction.Note>();
                    gridNoteGroups[gridTime].Add(note);
                }
            }

            Debug.Log($"[SamplingGenerator] Found {gridNoteGroups.Count} grid points with notes");

            // 그리드 시간 정렬
            var sortedGridTimes = gridNoteGroups.Keys.OrderBy(t => t).ToList();

            // 샘플링할 그리드 선택
            List<float> sampledGridTimes = new List<float>();

            if (samplingRate >= 0.99f)
            {
                // 100% - 모두 포함
                sampledGridTimes = sortedGridTimes;
            }
            else
            {
                // 샘플링 개수 계산
                int targetCount = Mathf.Max(2, Mathf.RoundToInt(sortedGridTimes.Count * samplingRate));

                // 첫/끝 노트 보장
                if (guaranteeFirstAndLast && sortedGridTimes.Count > 0)
                {
                    sampledGridTimes.Add(sortedGridTimes[0]); // 첫 노트

                    // 중간 노트 골고루 샘플링
                    int middleCount = targetCount - 2;
                    if (middleCount > 0 && sortedGridTimes.Count > 2)
                    {
                        float step = (float)(sortedGridTimes.Count - 1) / (targetCount - 1);
                        for (int i = 1; i < targetCount - 1; i++)
                        {
                            int index = Mathf.RoundToInt(i * step);
                            if (index >= sortedGridTimes.Count) index = sortedGridTimes.Count - 1;
                            if (!sampledGridTimes.Contains(sortedGridTimes[index]))
                                sampledGridTimes.Add(sortedGridTimes[index]);
                        }
                    }

                    sampledGridTimes.Add(sortedGridTimes[sortedGridTimes.Count - 1]); // 끝 노트
                }
                else
                {
                    // 균등 샘플링 (첫/끝 보장 없음)
                    float step = (float)sortedGridTimes.Count / targetCount;
                    for (int i = 0; i < targetCount; i++)
                    {
                        int index = Mathf.RoundToInt(i * step);
                        if (index >= sortedGridTimes.Count) index = sortedGridTimes.Count - 1;
                        sampledGridTimes.Add(sortedGridTimes[index]);
                    }
                }
            }

            Debug.Log($"[SamplingGenerator] Sampled {sampledGridTimes.Count} / {sortedGridTimes.Count} grids (rate: {samplingRate:P0})");

            // 샘플링된 그리드에서 노트 생성
            foreach (var gridTime in sampledGridTimes.OrderBy(t => t))
            {
                var notesInGrid = gridNoteGroups[gridTime];

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

            // 시간순 정렬
            chartNotes = chartNotes.OrderBy(n => n.time).ToList();

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

            string fileName = $"{chart.songName}_sampled_{samplingRate:P0}_chart.json";
            string fullPath = Path.Combine(outputPath, fileName);
            File.WriteAllText(fullPath, json);

            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "Success",
                $"Sampled chart generated!\n\n" +
                $"Sampling Rate: {samplingRate:P0}\n" +
                $"Total Notes: {chartNotes.Count}\n" +
                $"Saved to: {fullPath}",
                "OK"
            );

            Debug.Log($"Sampled chart conversion complete: {chartNotes.Count} notes generated (rate: {samplingRate:P0})");
        }
        catch (System.Exception e)
        {
            EditorUtility.DisplayDialog("Error", $"Failed to convert MIDI:\n{e.Message}", "OK");
            Debug.LogError(e);
        }
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
                notes[i].arrow = "SPACE";
            else
                notes[i].arrow = arrowKeys[randomGen.Next(arrowKeys.Length)];
        }
    }
}
