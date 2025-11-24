using UnityEngine;

public class NoteMovement : MonoBehaviour
{
    public float speed;
    public float noteTime;
    public string noteType;
    public bool isJudged = false;  // 판정 완료 여부

    Transform trans;

    public void Init(float s, float t, string type)
    {
        speed = s;
        noteTime = t;
        noteType = type;
        isJudged = false;
    }

    void Awake()
    {
        trans = transform;
    }

    void Update()
    {
        // 일시정지 중에는 움직이지 않음
        if (PauseManager.IsPaused) return;

        // localPosition으로 이동 (NotesParent 기준)
        trans.localPosition += Vector3.left * speed * Time.deltaTime;

        if (trans.localPosition.x < -3000f)
            Destroy(gameObject);
    }
}