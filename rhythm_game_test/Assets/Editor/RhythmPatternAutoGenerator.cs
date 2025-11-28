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
        GUILayout.Label("Basic Settings", EditorStyles.boldLabel);
        melodySensitivity = EditorGUILayout.Slider("Melody Sensitivity", melodySensitivity, 0.1f, 1.0f);
        noteSpeedFactor = EditorGUILayout.Slider("Note Speed Factor", noteSpeedFactor, 0.3f, 2.0f);
        minNoteInterval = EditorGUILayout.Slider("Min Note Interval", minNoteInterval, 0.1f, 1.0f);

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

        List<NoteData> notes = new List<NoteData>();

        float lastNoteTime = -999f;
        float lastParryTime = -999f; // 마지막 패링 노트 시간 추적
        float activeLongNoteEndTime = -999f; // 현재 진행 중인 롱노트의 종료 시간
        float step = 0.05f; // 20 FPS 분석 (빠른 분석)
        int longNoteGroupCounter = 0;

        // 노트 타입 배열 선택
        string[] noteTypePool = enableParryNotes ? allTypes : arrowTypes;

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

                    // 일반 노트
                    notes.Add(new NoteData()
                    {
                        time = t,
                        type = type,
                        noteSubType = "NORMAL",
                        longNoteGroupId = -1
                    });

                    lastNoteTime = t;

                    // 패링 노트면 패링 타임 업데이트
                    if (type == "SPACE")
                    {
                        lastParryTime = t;
                    }
                }
            }
        }

        string json = JsonHelper.ToJson(notes.ToArray(), true);
        File.WriteAllText(Application.dataPath + "/" + saveFileName, json);

        Debug.Log($"JSON created: {saveFileName} ({notes.Count} notes, {longNoteGroupCounter} long notes)");
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
