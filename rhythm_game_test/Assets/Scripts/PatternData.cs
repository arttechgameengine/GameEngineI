using System.Collections.Generic;

[System.Serializable]
public class PatternData
{
    public string songName;
    public float bpm;
    public float offset;
    public int numberOfLanes;
    public List<NoteData> notes;
}

[System.Serializable]
public class NoteData
{
    public float time;
    public int lane;
    public string type;          // "tap" or "hold"
    public string arrow;         // "UP", "DOWN", "LEFT", "RIGHT", "SPACE"
    public float duration;       // hold note duration (only for hold type)

    // 런타임에서 롱노트 처리용 (JSON에는 없음, 로드 후 자동 생성)
    [System.NonSerialized] public string noteSubType = "NORMAL";    // "NORMAL", "LONG_START", "LONG_HOLD", "LONG_END"
    [System.NonSerialized] public int longNoteGroupId = -1;
    [System.NonSerialized] public float longNoteDuration = 0f;
}
