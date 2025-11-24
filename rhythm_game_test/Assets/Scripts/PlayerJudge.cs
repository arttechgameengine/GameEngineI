using UnityEngine; 
using System.Linq;

public class PlayerJudge : MonoBehaviour
{
    public float perfectRange = 0.08f;
    public float greatRange = 0.15f;
    public float goodRange = 0.25f;

    public NoteSpawner spawner;
    public JudgePopup judgePopup;

    void Update()
    {
        // 일시정지 중에는 입력 무시
        if (PauseManager.IsPaused) return;

        if (Input.GetKeyDown(KeyCode.LeftArrow)) TryHit("LEFT");
        if (Input.GetKeyDown(KeyCode.RightArrow)) TryHit("RIGHT");
        if (Input.GetKeyDown(KeyCode.UpArrow)) TryHit("UP");
        if (Input.GetKeyDown(KeyCode.DownArrow)) TryHit("DOWN");
        if (Input.GetKeyDown(KeyCode.Space)) TryHit("SPACE");

        CheckMissedNotes();
    }

    void CheckMissedNotes()
    {
        double songTime = AudioSettings.dspTime - spawner.songStartDspTime;

        NoteMovement[] allNotes = FindObjectsOfType<NoteMovement>();

        foreach (var n in allNotes)
        {
            // 이미 판정된 노트는 무시
            if (n.isJudged) continue;

            // 노트의 판정 시간이 지나고 goodRange까지 벗어났으면 MISS
            if (songTime > n.noteTime + goodRange)
            {
                Debug.Log($"[MISS] songTime: {songTime:F2}, noteTime: {n.noteTime:F2}, diff: {(songTime - n.noteTime):F2}");
                Miss(n);
            }
        }
    }

    void TryHit(string keyType)
    {
        double songTime = AudioSettings.dspTime - spawner.songStartDspTime;

        NoteMovement[] allNotes = FindObjectsOfType<NoteMovement>();

        NoteMovement target = null;
        float minTimeDelta = float.MaxValue;

        foreach (var n in allNotes)
        {
            if (n.noteType != keyType) continue; // 타입이 다른 노트는 무시
            if (n.isJudged) continue; // 이미 판정된 노트는 무시

            // 시간 차이로 가장 가까운 노트 찾기 (시간 기반 판정과 일치)
            float timeDelta = Mathf.Abs((float)(songTime - n.noteTime));
            if (timeDelta < minTimeDelta)
            {
                minTimeDelta = timeDelta;
                target = n;
            }
        }

        if (target == null) return;

        // 판정 범위 내에 있으면 히트, 아니면 미스
        if (minTimeDelta <= perfectRange) Hit("PERFECT", target);
        else if (minTimeDelta <= greatRange) Hit("GREAT", target);
        else if (minTimeDelta <= goodRange) Hit("GOOD", target);
        else Miss();
    }

    void Hit(string judge, NoteMovement n)
    {
        n.isJudged = true;  // 판정 완료 표시

        Debug.Log($"{judge} ({n.noteType})");
        judgePopup.ShowJudge(judge);
        ScoreManager.Instance.AddJudge(judge);

        // 요리 효과 재생
        if (CookingEffect.Instance != null)
        {
            CookingEffect.Instance.PlayCookingEffect(n.noteType);
        }

        // 히트 효과 재생 후 파괴
        NoteEffect effect = n.GetComponent<NoteEffect>();
        if (effect != null)
        {
            effect.PlayHitEffect(() => Destroy(n.gameObject));
        }
        else
        {
            Destroy(n.gameObject);
        }
    }

    // 키를 눌렀지만 범위 밖인 경우 (노트 파괴 안함)
    void Miss()
    {
        Debug.Log("MISS");
        judgePopup.ShowJudge("MISS");
        ScoreManager.Instance.AddJudge("MISS");
    }

    // 노트를 놓친 경우 (노트 파괴)
    void Miss(NoteMovement n)
    {
        n.isJudged = true;  // 판정 완료 표시

        Debug.Log($"MISS ({n.noteType})");
        judgePopup.ShowJudge("MISS");
        ScoreManager.Instance.AddJudge("MISS");

        // 미스 효과 재생 후 파괴
        NoteEffect effect = n.GetComponent<NoteEffect>();
        if (effect != null)
        {
            effect.PlayMissEffect(() => Destroy(n.gameObject));
        }
        else
        {
            Destroy(n.gameObject);
        }
    }
}
