using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;

// MIDI 파싱을 위해 DryWetMIDI 라이브러리 필요
// NuGet: Install-Package Melanchall.DryWetMidi
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;

public class MidiToJsonConverter : EditorWindow
{
    private DefaultAsset midiFile;
    private string outputPath = "Assets/Charts/";
    private string songName = "";
    
    [Header("Chart Settings")]
    private int numberOfLanes = 4;
    private bool includeHoldNotes = true;
    private float holdThreshold = 0.2f; // 이 시간(초) 이상이면 hold note

    [Header("Long Note Settings")]
    private float longNoteHoldInterval = 0.25f; // LONG_HOLD 노트 생성 간격 (초)
    
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
    private bool ignoreLanes = false; // 체크하면 음높이 무시하고 타이밍만 추출 (레인은 랜덤 배치)
    private int lowestMidiNote = 60; // C4 (Middle C)

    [Header("Beat Quantization")]
    private bool quantizeToGrid = false; // 정박에만 노트 추출
    private bool snapToGrid = false; // 체크 시 정박에 완전히 스냅 (원본 타이밍 무시)
    private enum BeatInterval {
        WholeBeat,     // 온음표 (4박)
        HalfBeat,      // 2분음표 (2박)
        QuarterBeat,   // 4분음표 (1박) - 기본 정박
        EighthBeat,    // 8분음표 (0.5박)
        SixteenthBeat, // 16분음표 (0.25박)
        ThirtySecondBeat // 32분음표 (0.125박)
    }
    private BeatInterval beatInterval = BeatInterval.QuarterBeat;
    private float quantizeTolerance = 0.1f; // 정박 판정 허용 오차 (초)

    [MenuItem("Tools/MIDI to JSON Converter")]
    public static void ShowWindow()
    {
        GetWindow<MidiToJsonConverter>("MIDI Chart Converter");
    }

    private void OnGUI()
    {
        GUILayout.Label("MIDI to JSON Chart Generator", EditorStyles.boldLabel);
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
                // Convert to relative path if inside Assets folder
                if (selectedPath.StartsWith(Application.dataPath))
                {
                    outputPath = "Assets" + selectedPath.Substring(Application.dataPath.Length);
                }
                else
                {
                    outputPath = selectedPath;
                }
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();
        GUILayout.Label("Chart Settings", EditorStyles.boldLabel);
        
        numberOfLanes = EditorGUILayout.IntSlider("Number of Lanes", numberOfLanes, 1, 8);
        
        includeHoldNotes = EditorGUILayout.Toggle("Include Hold Notes", includeHoldNotes);
        if (includeHoldNotes)
        {
            holdThreshold = EditorGUILayout.Slider("Hold Threshold (seconds)", holdThreshold, 0.1f, 2f);
            EditorGUILayout.HelpBox(
                $"MIDI notes longer than {holdThreshold}s will become hold notes.",
                MessageType.Info
            );
        }
        else
        {
            EditorGUILayout.HelpBox(
                "All notes will be tap notes regardless of MIDI note length.",
                MessageType.Info
            );
        }
        
        EditorGUILayout.Space();
        GUILayout.Label("Arrow Key Assignment", EditorStyles.boldLabel);
        
        includeSpaceBar = EditorGUILayout.Toggle("Include SPACE Bar Notes", includeSpaceBar);
        if (includeSpaceBar)
        {
            spaceBarNoteCount = EditorGUILayout.IntField("Number of SPACE Notes", spaceBarNoteCount);
            
            if (includeHoldNotes)
            {
                EditorGUILayout.HelpBox(
                    "SPACE notes will always be tap notes.\n" +
                    "Hold notes will only use arrow keys (UP, DOWN, LEFT, RIGHT).",
                    MessageType.Info
                );
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "Arrow keys (UP, DOWN, LEFT, RIGHT) will be randomly assigned.\n" +
                    $"{spaceBarNoteCount} notes will be randomly assigned as SPACE.",
                    MessageType.Info
                );
            }
        }
        else
        {
            EditorGUILayout.HelpBox(
                "Only arrow keys (UP, DOWN, LEFT, RIGHT) will be randomly assigned.",
                MessageType.Info
            );
        }
        
        EditorGUILayout.Space();
        GUILayout.Label("Timing Settings", EditorStyles.boldLabel);
        
        autoDetectBPM = EditorGUILayout.Toggle("Auto Detect BPM", autoDetectBPM);
        if (!autoDetectBPM)
        {
            manualBPM = EditorGUILayout.FloatField("Manual BPM", manualBPM);
        }
        
        autoAlignFirstNote = EditorGUILayout.Toggle("Auto Align First Note to 0", autoAlignFirstNote);
        if (!autoAlignFirstNote)
        {
            offset = EditorGUILayout.FloatField("Manual Offset (seconds)", offset);
        }
        
        EditorGUILayout.HelpBox(
            autoAlignFirstNote 
                ? "First MIDI note will automatically start at time 0" 
                : "Manually adjust offset to sync with audio",
            MessageType.Info
        );

        EditorGUILayout.Space();
        GUILayout.Label("MIDI Mapping", EditorStyles.boldLabel);

        ignoreLanes = EditorGUILayout.Toggle("Ignore Pitch (Timing Only)", ignoreLanes);

        if (ignoreLanes)
        {
            EditorGUILayout.HelpBox(
                "All MIDI notes will be extracted by timing only.\n" +
                $"Lanes will be randomly assigned from 0 to {numberOfLanes - 1}.",
                MessageType.Info
            );
        }
        else
        {
            lowestMidiNote = EditorGUILayout.IntField("Lowest MIDI Note", lowestMidiNote);
            EditorGUILayout.HelpBox(
                $"MIDI notes {lowestMidiNote} to {lowestMidiNote + numberOfLanes - 1} will be used for chart generation.",
                MessageType.Info
            );
        }

        EditorGUILayout.Space();
        GUILayout.Label("Beat Quantization (정박 필터링)", EditorStyles.boldLabel);

        quantizeToGrid = EditorGUILayout.Toggle("Quantize to Grid (정박만)", quantizeToGrid);

        if (quantizeToGrid)
        {
            EditorGUI.indentLevel++;
            beatInterval = (BeatInterval)EditorGUILayout.EnumPopup("Beat Interval", beatInterval);
            quantizeTolerance = EditorGUILayout.Slider("Snap Tolerance (seconds)", quantizeTolerance, 0.01f, 0.2f);

            snapToGrid = EditorGUILayout.Toggle("Snap to Perfect Grid", snapToGrid);

            // BPM 값 가져오기
            float currentBPM = autoDetectBPM ? 120f : manualBPM;
            float intervalSeconds = GetBeatIntervalInSeconds(beatInterval, currentBPM);

            if (snapToGrid)
            {
                EditorGUILayout.HelpBox(
                    $"정박 간격: {intervalSeconds:F3}초\n" +
                    $"허용 오차: ±{quantizeTolerance:F3}초\n" +
                    "노트 타이밍이 정박에 완전히 스냅됩니다 (원본 타이밍 무시).",
                    MessageType.Warning
                );
            }
            else
            {
                EditorGUILayout.HelpBox(
                    $"정박 간격: {intervalSeconds:F3}초\n" +
                    $"허용 오차: ±{quantizeTolerance:F3}초\n" +
                    "정박(BPM 그리드)에서 허용 오차 이내에 있는 노트만 추출됩니다 (원본 타이밍 유지).",
                    MessageType.Info
                );
            }
            EditorGUI.indentLevel--;
        }
        else
        {
            EditorGUILayout.HelpBox(
                "모든 MIDI 노트가 원본 타이밍 그대로 추출됩니다.",
                MessageType.Info
            );
        }

        EditorGUILayout.Space();
        
        if (GUILayout.Button("Generate JSON Chart", GUILayout.Height(40)))
        {
            if (midiFile != null)
            {
                ConvertMidiToJson();
            }
            else
            {
                EditorUtility.DisplayDialog("Error", "Please select a MIDI file!", "OK");
            }
        }

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "Tip: Create your chart in a DAW with each note as a MIDI note.\n" +
            "Arrow keys will be randomly assigned to all notes automatically.",
            MessageType.Info
        );
    }

    /// <summary>
    /// 비트 간격을 초 단위로 변환
    /// BPM의 1박 = 4분음표 (Quarter Note)
    /// </summary>
    private float GetBeatIntervalInSeconds(BeatInterval interval, float bpm)
    {
        // 1박 (4분음표)의 길이 (초) = 60초 / BPM
        float quarterNoteDuration = 60f / bpm;

        switch (interval)
        {
            case BeatInterval.WholeBeat: return quarterNoteDuration * 4f;    // 온음표 (4박)
            case BeatInterval.HalfBeat: return quarterNoteDuration * 2f;     // 2분음표 (2박)
            case BeatInterval.QuarterBeat: return quarterNoteDuration;       // 4분음표 (1박) - 기본 정박
            case BeatInterval.EighthBeat: return quarterNoteDuration / 2f;   // 8분음표 (0.5박)
            case BeatInterval.SixteenthBeat: return quarterNoteDuration / 4f; // 16분음표 (0.25박)
            case BeatInterval.ThirtySecondBeat: return quarterNoteDuration / 8f; // 32분음표 (0.125박)
            default: return quarterNoteDuration; // 기본값 4분음표
        }
    }

    /// <summary>
    /// 노트가 정박(그리드)에 가까운지 확인
    /// </summary>
    private bool IsNoteOnGrid(float noteTime, float beatIntervalSeconds, float offset, float tolerance)
    {
        float relativeTime = noteTime - offset;

        // 가장 가까운 그리드 포인트까지의 거리 계산
        float gridIndex = Mathf.Round(relativeTime / beatIntervalSeconds);
        float closestGridTime = gridIndex * beatIntervalSeconds + offset;
        float distance = Mathf.Abs(noteTime - closestGridTime);

        return distance <= tolerance;
    }

    private void ConvertMidiToJson()
    {
        string midiPath = AssetDatabase.GetAssetPath(midiFile);

        // 랜덤 시드 초기화 (재현 가능하도록 파일명 기반)
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
                Debug.Log($"Using manual BPM: {bpm}");
            }

            // MIDI 노트 추출
            var notes = midiFile.GetNotes();
            var chartNotes = new List<NoteData>();

            // 첫 노트 시간 자동 감지
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

            // 정박 필터링을 위한 BPM 그리드 간격 계산
            float beatIntervalSeconds = 0f;
            Dictionary<int, List<Melanchall.DryWetMidi.Interaction.Note>> gridNoteGroups = null;

            if (quantizeToGrid)
            {
                beatIntervalSeconds = GetBeatIntervalInSeconds(beatInterval, bpm);
                Debug.Log($"[Quantize] Beat interval: {beatIntervalSeconds:F3}s, Tolerance: {quantizeTolerance:F3}s");
                Debug.Log($"[Quantize] First note offset: {firstNoteOffset:F3}s (will check grid BEFORE applying offset)");

                // 각 정박(그리드)마다 가장 가까운 노트 1개만 선택
                gridNoteGroups = new Dictionary<int, List<Melanchall.DryWetMidi.Interaction.Note>>();

                foreach (var note in notes)
                {
                    var metricTime = TimeConverter.ConvertTo<MetricTimeSpan>(note.Time, tempoMap);
                    float originalTime = (float)metricTime.TotalSeconds;

                    // 가장 가까운 그리드 인덱스 찾기
                    int gridIndex = Mathf.RoundToInt(originalTime / beatIntervalSeconds);
                    float closestGridTime = gridIndex * beatIntervalSeconds;
                    float distance = Mathf.Abs(originalTime - closestGridTime);

                    // 허용 오차 이내인 노트만
                    if (distance <= quantizeTolerance)
                    {
                        if (!gridNoteGroups.ContainsKey(gridIndex))
                        {
                            gridNoteGroups[gridIndex] = new List<Melanchall.DryWetMidi.Interaction.Note>();
                        }
                        gridNoteGroups[gridIndex].Add(note);
                    }
                }

                Debug.Log($"[Quantize] Found {gridNoteGroups.Count} grid points with notes");
            }

            int totalNotes = quantizeToGrid ? notes.Count() : 0;
            int filteredNotes = 0;

            // 정박 필터링 모드: 각 그리드마다 가장 가까운 노트 1개만 처리
            if (quantizeToGrid && gridNoteGroups != null)
            {
                foreach (var kvp in gridNoteGroups.OrderBy(x => x.Key))
                {
                    int gridIndex = kvp.Key;
                    var notesInGrid = kvp.Value;

                    // 이 그리드에서 가장 가까운 노트 1개 선택
                    float gridTime = gridIndex * beatIntervalSeconds;
                    var closestNote = notesInGrid.OrderBy(n =>
                    {
                        var mt = TimeConverter.ConvertTo<MetricTimeSpan>(n.Time, tempoMap);
                        float ot = (float)mt.TotalSeconds;
                        return Mathf.Abs(ot - gridTime);
                    }).First();

                    filteredNotes += notesInGrid.Count - 1; // 선택되지 않은 노트들

                    // 선택된 노트 처리
                    int midiNoteNumber = closestNote.NoteNumber;
                    int lane;

                    if (ignoreLanes)
                    {
                        lane = randomGen.Next(numberOfLanes);
                    }
                    else
                    {
                        lane = midiNoteNumber - lowestMidiNote;
                        if (lane < 0 || lane >= numberOfLanes)
                        {
                            Debug.LogWarning($"MIDI note {midiNoteNumber} is outside lane range. Skipping.");
                            continue;
                        }
                    }

                    // 시간 계산
                    var metricTime = TimeConverter.ConvertTo<MetricTimeSpan>(closestNote.Time, tempoMap);
                    float originalTime = (float)metricTime.TotalSeconds;

                    // 정박에서 너무 멀리 떨어진 노트는 제외 (오프비트 노트 방지)
                    float distanceFromGrid = Mathf.Abs(originalTime - gridTime);
                    if (distanceFromGrid > quantizeTolerance * 0.5f) // 허용 오차의 절반 이상 떨어지면 스킵
                    {
                        Debug.Log($"[Quantize] Skipping note at {originalTime:F3}s - too far from grid {gridTime:F3}s (distance: {distanceFromGrid:F3}s)");
                        continue;
                    }

                    // snapToGrid가 켜져있으면 정박에 완전히 스냅, 아니면 원본 타이밍 유지
                    float time = snapToGrid ? (gridTime + firstNoteOffset) : (originalTime + firstNoteOffset);

                    // 노트 길이 계산
                    var metricLength = LengthConverter.ConvertTo<MetricTimeSpan>(closestNote.Length, closestNote.Time, tempoMap);
                    float duration = (float)metricLength.TotalSeconds;

                    // Hold note 판단
                    string noteType = "tap";
                    if (includeHoldNotes && duration >= holdThreshold)
                    {
                        noteType = "hold";
                    }

                    var chartNote = new NoteData
                    {
                        time = Mathf.Round(time * 1000f) / 1000f,
                        lane = lane,
                        type = noteType,
                        arrow = "",
                        duration = 0
                    };

                    if (noteType == "hold")
                    {
                        chartNote.duration = Mathf.Round(duration * 1000f) / 1000f;
                    }

                    chartNotes.Add(chartNote);
                }

                Debug.Log($"[Quantize] Filtered {filteredNotes} / {totalNotes} notes (kept {chartNotes.Count} on-grid notes, 1 per grid)");
            }
            // 일반 모드: 모든 노트 처리
            else
            {
                foreach (var note in notes)
                {
                    int midiNoteNumber = note.NoteNumber;
                    int lane;

                    if (ignoreLanes)
                    {
                        lane = randomGen.Next(numberOfLanes);
                    }
                    else
                    {
                        lane = midiNoteNumber - lowestMidiNote;
                        if (lane < 0 || lane >= numberOfLanes)
                        {
                            Debug.LogWarning($"MIDI note {midiNoteNumber} is outside lane range. Skipping.");
                            continue;
                        }
                    }

                    var metricTime = TimeConverter.ConvertTo<MetricTimeSpan>(note.Time, tempoMap);
                    float time = (float)metricTime.TotalSeconds + firstNoteOffset;

                    var metricLength = LengthConverter.ConvertTo<MetricTimeSpan>(note.Length, note.Time, tempoMap);
                    float duration = (float)metricLength.TotalSeconds;

                    string noteType = "tap";
                    if (includeHoldNotes && duration >= holdThreshold)
                    {
                        noteType = "hold";
                    }

                    var chartNote = new NoteData
                    {
                        time = Mathf.Round(time * 1000f) / 1000f,
                        lane = lane,
                        type = noteType,
                        arrow = "",
                        duration = 0
                    };

                    if (noteType == "hold")
                    {
                        chartNote.duration = Mathf.Round(duration * 1000f) / 1000f;
                    }

                    chartNotes.Add(chartNote);
                }
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
                noteSpeed = 500f,  // 기본 속도
                notes = chartNotes
            };

            string json = JsonUtility.ToJson(chart, true);

            // 파일 저장
            if (!Directory.Exists(outputPath))
            {
                Directory.CreateDirectory(outputPath);
            }

            string fileName = $"{chart.songName}_chart.json";
            string fullPath = Path.Combine(outputPath, fileName);
            File.WriteAllText(fullPath, json);

            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "Success", 
                $"Chart generated successfully!\n\nNotes: {chartNotes.Count}\nSaved to: {fullPath}", 
                "OK"
            );

            Debug.Log($"Chart conversion complete: {chartNotes.Count} notes generated");
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

        // SPACE 노트 선택 (tap 노트만 가능)
        if (includeSpaceBar && spaceBarNoteCount > 0)
        {
            // tap 노트들의 인덱스만 수집
            List<int> tapNoteIndices = new List<int>();
            for (int i = 0; i < notes.Count; i++)
            {
                if (notes[i].type == "tap")
                {
                    tapNoteIndices.Add(i);
                }
            }

            int actualSpaceCount = Mathf.Min(spaceBarNoteCount, tapNoteIndices.Count);

            // Fisher-Yates 셔플로 tap 노트 중에서 랜덤 선택
            for (int i = 0; i < actualSpaceCount; i++)
            {
                int randomIndex = randomGen.Next(i, tapNoteIndices.Count);
                int temp = tapNoteIndices[i];
                tapNoteIndices[i] = tapNoteIndices[randomIndex];
                tapNoteIndices[randomIndex] = temp;
            }

            spaceIndices = tapNoteIndices.Take(actualSpaceCount).ToList();

            if (includeHoldNotes)
            {
                Debug.Log($"Assigned {actualSpaceCount} SPACE notes (tap only). Hold notes will use arrow keys only.");
            }
            else
            {
                Debug.Log($"Assigned {actualSpaceCount} SPACE notes");
            }
        }

        // 모든 노트에 방향키 할당
        for (int i = 0; i < notes.Count; i++)
        {
            if (spaceIndices.Contains(i))
            {
                // SPACE는 tap 노트만 가능
                notes[i].arrow = "SPACE";
            }
            else
            {
                // hold 노트와 나머지 tap 노트는 방향키 사용
                notes[i].arrow = arrowKeys[randomGen.Next(arrowKeys.Length)];
            }
        }
    }
}