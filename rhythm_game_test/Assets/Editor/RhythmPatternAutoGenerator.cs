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

    public string saveFileName = "pattern.json";

    private static readonly string[] noteTypes =
    {
        "LEFT", "RIGHT", "UP", "DOWN", "SPACE"
    };

    [MenuItem("Tools/Rhythm Pattern Auto Generator")]
    public static void OpenWindow()
    {
        GetWindow<RhythmPatternAutoGenerator>("Rhythm Pattern Auto Generator");
    }

    void OnGUI()
    {
        GUILayout.Label("Auto Rhythm Pattern Generator", EditorStyles.boldLabel);

        audioClip = (AudioClip)EditorGUILayout.ObjectField("Music", audioClip, typeof(AudioClip), false);

        melodySensitivity = EditorGUILayout.Slider("Melody Sensitivity", melodySensitivity, 0.1f, 1.0f);
        noteSpeedFactor = EditorGUILayout.Slider("Note Speed Factor", noteSpeedFactor, 0.3f, 2.0f);
        minNoteInterval = EditorGUILayout.Slider("Min Note Interval", minNoteInterval, 0.1f, 1.0f);

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
        float step = 0.05f; // 20 FPS 분석 (빠른 분석)

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
                    lastNoteTime = t;

                    string type = noteTypes[Random.Range(0, noteTypes.Length)];

                    notes.Add(new NoteData()
                    {
                        time = t,
                        type = type
                    });
                }
            }
        }

        string json = JsonHelper.ToJson(notes.ToArray(), true);
        File.WriteAllText(Application.dataPath + "/" + saveFileName, json);

        Debug.Log("JSON created: " + saveFileName);
    }
}

[System.Serializable]
public class NoteData
{
    public float time;
    public string type;
}

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
