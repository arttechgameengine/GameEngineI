using UnityEngine;
using System.Linq;

public class PlayerJudge : MonoBehaviour
{
    public float perfectRange = 0.05f;
    public float greatRange = 0.10f;
    public float goodRange = 0.15f;

    public NoteSpawner spawner;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.LeftArrow)) TryHit("LEFT");
        if (Input.GetKeyDown(KeyCode.RightArrow)) TryHit("RIGHT");
        if (Input.GetKeyDown(KeyCode.UpArrow)) TryHit("UP");
        if (Input.GetKeyDown(KeyCode.DownArrow)) TryHit("DOWN");
        if (Input.GetKeyDown(KeyCode.Space)) TryHit("SPACE");
    }

    void TryHit(string keyType)
    {
        double songTime = AudioSettings.dspTime - spawner.songStartDspTime;

        NoteMovement[] allNotes = FindObjectsOfType<NoteMovement>();

        float judgeX = transform.position.x;

        NoteMovement target = null;
        float minDist = 999f;

        foreach (var n in allNotes)
        {
            if (n.noteType != keyType) continue; // 타입이 다른 노트는 무시

            float dist = Mathf.Abs(n.transform.position.x - judgeX);
            if (dist < minDist)
            {
                minDist = dist;
                target = n;
            }
        }

        if (target == null) return;

        float delta = Mathf.Abs((float)(songTime - target.noteTime));

        if (delta <= perfectRange) Hit("PERFECT", target);
        else if (delta <= greatRange) Hit("GREAT", target);
        else if (delta <= goodRange) Hit("GOOD", target);
        else Miss();
    }

    void Hit(string judge, NoteMovement n)
    {
        Debug.Log($"{judge} ({n.noteType})");
        Destroy(n.gameObject);
    }

    void Miss()
    {
        Debug.Log("MISS");
    }
}
