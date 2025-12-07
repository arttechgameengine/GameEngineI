using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;

// MIDI 파싱을 위해 DryWetMIDI 라이브러리 필요
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;

public class NoteSpeedMIDIGenerator : EditorWindow
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
    private float noteSpeed = 500f;  // 노트 이동 속도 (느린 속도: 개수만 감소, 빠른 속도: 속도 증가)

    private enum BeatInterval {
        WholeBeat,
        HalfBeat,
        QuarterBeat,
        EighthBeat,
        SixteenthBeat,
        ThirtySecondBeat
    }

    // 구간별 설정
    [System.Serializable]
    private class DifficultySection
    {
        public float startPercentage; // 곡의 시작 지점 (0.0 ~ 1.0)
        public BeatInterval beatInterval;
        public int addNoteCount; // 이 Beat Interval에서 추가할 노트 개수 (0 = 모두 추가)
    }

    private List<DifficultySection> difficultySections = new List<DifficultySection>
    {
        new DifficultySection { startPercentage = 0.0f, beatInterval = BeatInterval.QuarterBeat, addNoteCount = 0 },
        new DifficultySection { startPercentage = 0.33f, beatInterval = BeatInterval.EighthBeat, addNoteCount = 10 },
        new DifficultySection { startPercentage = 0.66f, beatInterval = BeatInterval.SixteenthBeat, addNoteCount = 15 }
    };

    private float quantizeTolerance = 0.1f;
    private bool snapToGrid = true; // 기본적으로 스냅 활성화

    private Vector2 scrollPosition; // 스크롤 위치

    [MenuItem("Tools/Note Speed MIDI Generator")]
    public static void ShowWindow()
    {
        GetWindow<NoteSpeedMIDIGenerator>("Note Speed MIDI Generator");
    }

    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.ExpandWidth(false));

        GUILayout.Label("Note Speed MIDI Generator", EditorStyles.boldLabel);
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
        }

        EditorGUILayout.Space();
        GUILayout.Label("Arrow Key Assignment", EditorStyles.boldLabel);

        includeSpaceBar = EditorGUILayout.Toggle("Include SPACE Bar Notes", includeSpaceBar);
        if (includeSpaceBar)
        {
            spaceBarNoteCount = EditorGUILayout.IntField("Number of SPACE Notes", spaceBarNoteCount);
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

        EditorGUILayout.Space();
        GUILayout.Label("MIDI Mapping", EditorStyles.boldLabel);

        ignoreLanes = EditorGUILayout.Toggle("Ignore Pitch (Timing Only)", ignoreLanes);
        if (!ignoreLanes)
        {
            lowestMidiNote = EditorGUILayout.IntField("Lowest MIDI Note", lowestMidiNote);
        }

        EditorGUILayout.Space();
        GUILayout.Label("Progressive Difficulty Settings", EditorStyles.boldLabel);

        useProgressiveDifficulty = EditorGUILayout.Toggle("Use Progressive Difficulty", useProgressiveDifficulty);

        if (useProgressiveDifficulty)
        {
            EditorGUI.indentLevel++;

            quantizeTolerance = EditorGUILayout.Slider("Snap Tolerance (seconds)", quantizeTolerance, 0.01f, 0.2f);
            snapToGrid = EditorGUILayout.Toggle("Snap to Perfect Grid", snapToGrid);

            EditorGUILayout.Space();
            noteSpeed = EditorGUILayout.FloatField("Note Speed", noteSpeed);
            EditorGUILayout.HelpBox(
                "노트 속도 설정:\n" +
                "게임에서 시각적 스케일이 자동 조정됩니다.\n" +
                "• 느린 속도 (250): 화면 축소 → 체감상 느려짐\n" +
                "• 기준 속도 (500): 정상 크기\n" +
                "• 빠른 속도 (1000): 화면 확대 → 체감상 빨라짐\n" +
                "노트 개수는 속도와 무관하게 지정한 개수 그대로 생성됩니다.",
                MessageType.Info
            );

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Difficulty Sections", EditorStyles.boldLabel);

            for (int i = 0; i < difficultySections.Count; i++)
            {
                EditorGUILayout.BeginVertical("box");

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"Section {i + 1}:", GUILayout.Width(70));
                difficultySections[i].startPercentage = EditorGUILayout.Slider(
                    difficultySections[i].startPercentage, 0f, 1f, GUILayout.Width(150));
                EditorGUILayout.LabelField("%", GUILayout.Width(20));
                difficultySections[i].beatInterval = (BeatInterval)EditorGUILayout.EnumPopup(
                    difficultySections[i].beatInterval, GUILayout.Width(120));

                if (GUILayout.Button("-", GUILayout.Width(25)))
                {
                    difficultySections.RemoveAt(i);
                    break;
                }
                EditorGUILayout.EndHorizontal();

                // 노트 개수 설정 (첫 섹션이 아닌 경우만)
                if (i > 0)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("  Add Note Count:", GUILayout.Width(120));
                    difficultySections[i].addNoteCount = EditorGUILayout.IntField(
                        difficultySections[i].addNoteCount, GUILayout.Width(60));
                    EditorGUILayout.LabelField("(0 = 모두 추가)", GUILayout.Width(100));
                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.EndVertical();
            }

            if (GUILayout.Button("+ Add Section"))
            {
                difficultySections.Add(new DifficultySection
                {
                    startPercentage = 0.5f,
                    beatInterval = BeatInterval.QuarterBeat,
                    addNoteCount = 10
                });
            }

            EditorGUILayout.HelpBox(
                "Progressive 모드: 곡이 진행될수록 노트 밀도가 점진적으로 증가합니다.\n" +
                "각 섹션은 이전 섹션의 노트를 유지하면서 새로운 Beat Interval의 노트를 지정한 개수만큼 골고루 추가합니다.\n" +
                "Add Note Count: 추가할 노트 개수 (0 = 해당 Beat Interval의 모든 노트 추가)\n" +
                "예: Section 1 (QuarterBeat 모두) → Section 2 (+EighthBeat 10개) → Section 3 (+SixteenthBeat 15개)",
                MessageType.Info
            );
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("Generate Speed-Adjusted Chart", GUILayout.Height(40)))
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

    private BeatInterval GetBeatIntervalForTime(float normalizedTime)
    {
        // 섹션을 시작 지점 기준으로 정렬
        var sortedSections = difficultySections.OrderBy(s => s.startPercentage).ToList();

        // 현재 시간에 해당하는 섹션 찾기
        for (int i = sortedSections.Count - 1; i >= 0; i--)
        {
            if (normalizedTime >= sortedSections[i].startPercentage)
            {
                return sortedSections[i].beatInterval;
            }
        }

        return BeatInterval.QuarterBeat; // 기본값
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

            // 곡 전체 길이 계산
            float songDuration = 0f;
            if (notes.Any())
            {
                var lastNote = notes.OrderBy(n => n.Time).Last();
                var lastMetricTime = TimeConverter.ConvertTo<MetricTimeSpan>(lastNote.Time, tempoMap);
                songDuration = (float)lastMetricTime.TotalSeconds;
            }

            Debug.Log($"[Progressive] Song duration: {songDuration:F2}s");

            if (useProgressiveDifficulty)
            {
                // Progressive Difficulty 모드 - 점진적으로 노트 밀도 증가
                Dictionary<float, List<Melanchall.DryWetMidi.Interaction.Note>> allTimeBasedNotes =
                    new Dictionary<float, List<Melanchall.DryWetMidi.Interaction.Note>>();

                // 섹션별로 정렬
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

                    // 이 섹션에서 추가할 Beat Interval (새로 추가되는 것만)
                    BeatInterval newInterval = section.beatInterval;
                    int addCount = section.addNoteCount;

                    Debug.Log($"[Progressive] Section {sectionIdx}: {sectionStartPercent:P0}-{sectionEndPercent:P0}, New Interval: {newInterval}, Add Count: {addCount}");

                    // 이 Beat Interval에 해당하는 그리드 시간 수집 (이 섹션 범위 내에서만)
                    Dictionary<float, List<Melanchall.DryWetMidi.Interaction.Note>> sectionIntervalNotes =
                        new Dictionary<float, List<Melanchall.DryWetMidi.Interaction.Note>>();

                    float beatIntervalSeconds = GetBeatIntervalInSeconds(newInterval, bpm);

                    foreach (var note in notes)
                    {
                        var metricTime = TimeConverter.ConvertTo<MetricTimeSpan>(note.Time, tempoMap);
                        float originalTime = (float)metricTime.TotalSeconds;

                        // 이 노트가 현재 섹션 범위 내에 있는지 확인
                        if (originalTime < sectionStartTime || originalTime >= sectionEndTime)
                            continue;

                        // 이 Beat Interval의 그리드에 스냅
                        int gridIndex = Mathf.RoundToInt(originalTime / beatIntervalSeconds);
                        float gridTime = gridIndex * beatIntervalSeconds;
                        float distance = Mathf.Abs(originalTime - gridTime);

                        // 허용 오차 이내인 노트만
                        if (distance <= quantizeTolerance)
                        {
                            if (!sectionIntervalNotes.ContainsKey(gridTime))
                            {
                                sectionIntervalNotes[gridTime] = new List<Melanchall.DryWetMidi.Interaction.Note>();
                            }
                            sectionIntervalNotes[gridTime].Add(note);
                        }
                    }

                    // 수집된 그리드 시간 중에서 골고루 샘플링
                    var gridTimes = sectionIntervalNotes.Keys.OrderBy(t => t).ToList();
                    List<float> selectedGridTimes = new List<float>();

                    if (addCount <= 0 || addCount >= gridTimes.Count)
                    {
                        // 0이거나 전체 개수보다 많으면 모두 추가
                        selectedGridTimes = gridTimes;
                    }
                    else
                    {
                        // 골고루 분포시켜서 addCount개만 선택
                        float step = (float)gridTimes.Count / addCount;
                        for (int i = 0; i < addCount; i++)
                        {
                            int index = Mathf.RoundToInt(i * step);
                            if (index >= gridTimes.Count) index = gridTimes.Count - 1;
                            selectedGridTimes.Add(gridTimes[index]);
                        }
                    }

                    Debug.Log($"[Progressive] Section {sectionIdx}: Total grids: {gridTimes.Count}, Add count: {addCount}, Selected: {selectedGridTimes.Count}");

                    // 선택된 그리드 시간의 노트를 전체 노트 목록에 추가
                    foreach (var gridTime in selectedGridTimes)
                    {
                        if (!allTimeBasedNotes.ContainsKey(gridTime))
                        {
                            allTimeBasedNotes[gridTime] = new List<Melanchall.DryWetMidi.Interaction.Note>();
                        }
                        allTimeBasedNotes[gridTime].AddRange(sectionIntervalNotes[gridTime]);
                    }
                }

                Debug.Log($"[Progressive] Found {allTimeBasedNotes.Count} total grid points with notes");

                // 변수 이름 변경
                var timeBasedNotes = allTimeBasedNotes;

                // 각 그리드 시간마다 가장 가까운 노트 1개만 처리
                foreach (var kvp in timeBasedNotes.OrderBy(x => x.Key))
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
                            continue;
                        }
                    }

                    // 시간 계산
                    var metricTime = TimeConverter.ConvertTo<MetricTimeSpan>(closestNote.Time, tempoMap);
                    float originalTime = (float)metricTime.TotalSeconds;

                    // snapToGrid가 켜져있으면 정박에 완전히 스냅
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
                noteSpeed = noteSpeed,  // 입력한 속도 그대로 저장
                notes = chartNotes
            };

            string json = JsonUtility.ToJson(chart, true);

            // 파일 저장
            if (!Directory.Exists(outputPath))
            {
                Directory.CreateDirectory(outputPath);
            }

            string fileName = $"{chart.songName}_speed_chart.json";
            string fullPath = Path.Combine(outputPath, fileName);
            File.WriteAllText(fullPath, json);

            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "Success",
                $"Note Speed chart generated!\n\nNotes: {chartNotes.Count}\nNote Speed: {noteSpeed}\nSaved to: {fullPath}",
                "OK"
            );

            Debug.Log($"Note Speed chart conversion complete: {chartNotes.Count} notes generated, note speed: {noteSpeed}");
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
                {
                    tapNoteIndices.Add(i);
                }
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
}
