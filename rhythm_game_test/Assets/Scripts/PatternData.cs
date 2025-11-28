using System.Collections.Generic; 

[System.Serializable]
public class PatternData
{
    public List<NoteData> notes;
}

[System.Serializable]
public class NoteData
{
    public float time;
    public string type;

    // 롱노트 타입: "NORMAL", "LONG_START", "LONG_HOLD", "LONG_END"
    public string noteSubType = "NORMAL";

    // 롱노트 그룹 ID (같은 롱노트는 같은 ID)
    public int longNoteGroupId = -1;

    // 롱노트 지속시간 (LONG_START에만 사용, 시각적 막대 길이 계산용)
    public float longNoteDuration = 0f;
}
