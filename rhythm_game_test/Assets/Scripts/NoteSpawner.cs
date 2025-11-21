using UnityEngine;
using System.Collections.Generic;

public class NoteSpawner : MonoBehaviour
{
    public RectTransform notePrefab;
    public RectTransform spawnPoint;
    public RectTransform notesParent;
    public float noteSpeed = 500f;
    public float spawnLeadTime = 0.45f;

    List<NoteData> notes = new List<NoteData>();
    int nextIndex = 0;
    public double songStartDspTime;
    private bool songStarted = false;  // 🔥 추가

    public void LoadPattern(PatternData pattern)
    {
        notes = pattern.notes;
        nextIndex = 0;
        songStarted = false;  // 🔥 추가
    }

    public void StartSong(AudioSource audio)
    {
        songStartDspTime = AudioSettings.dspTime;
        audio.PlayScheduled(songStartDspTime);
        songStarted = true;  // 🔥 추가
    }

    void Update()
    {
        if (!songStarted || notes == null || notes.Count == 0) return;  // 🔥 수정

        double songTime = AudioSettings.dspTime - songStartDspTime;

        while (nextIndex < notes.Count &&
               notes[nextIndex].time - spawnLeadTime <= songTime)
        {
            Spawn(notes[nextIndex]);
            nextIndex++;
        }
    }

    void Spawn(NoteData data)
    {
        RectTransform n = Instantiate(notePrefab, spawnPoint.position, Quaternion.identity, notesParent);
        NoteMovement mv = n.GetComponent<NoteMovement>();
        NoteVisual visual = n.GetComponent<NoteVisual>();

        mv.Init(noteSpeed, data.time, data.type);
        visual.SetType(data.type);
    }
}