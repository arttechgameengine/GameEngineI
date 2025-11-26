using UnityEngine;
using UnityEngine.UI;

public class NoteMovement : MonoBehaviour
{
    public float speed;
    public float noteTime;
    public string noteType;
    public bool isJudged = false;  // 판정 완료 여부

    Transform trans;
    private Image image;
    private NoteVisual visual;

    // SPACE 노트 전용 설정
    private bool isSpaceNote = false;
    private bool hasAppeared = false;
    private float appearDistance;  // HitLine으로부터 이 거리에서 나타남
    private const float APPEAR_ANIM_DURATION = 0.3f;  // 등장 애니메이션 시간 (짧게 조정)

    public void Init(float s, float t, string type)
    {
        speed = s;
        noteTime = t;
        noteType = type;
        isJudged = false;

        isSpaceNote = (type == "SPACE");

        if (isSpaceNote)
        {
            // HitLine과 SpawnPoint 중간 지점에서 나타나도록 설정
            NoteSpawner spawner = FindObjectOfType<NoteSpawner>();
            if (spawner != null)
            {
                float totalDistance = Mathf.Abs(spawner.spawnPoint.localPosition.x - spawner.hitLineLocalX);

                // 중간 지점 기본 설정
                appearDistance = totalDistance * 0.5f;

                // 애니메이션 동안 이동하는 거리만큼 판정 시간 조정
                float distanceDuringAnim = speed * APPEAR_ANIM_DURATION;
                float timeDuringAnim = distanceDuringAnim / speed;

                // 판정 시간을 애니메이션 시간만큼 늦춤
                noteTime += timeDuringAnim;
            }

            // 처음엔 숨김
            SetVisibility(false);
        }
    }

    void Awake()
    {
        trans = transform;
        image = GetComponent<Image>();
        visual = GetComponent<NoteVisual>();
    }

    void Update()
    {
        // 일시정지 중에는 움직이지 않음
        if (PauseManager.IsPaused) return;

        // SPACE 노트: 중간 지점 도달 시 뾰로롱 나타남
        if (isSpaceNote && !hasAppeared)
        {
            NoteSpawner spawner = FindObjectOfType<NoteSpawner>();
            if (spawner != null)
            {
                float distanceFromHitLine = trans.localPosition.x - spawner.hitLineLocalX;

                // 나타날 거리에 도달하면 표시
                if (distanceFromHitLine <= appearDistance)
                {
                    SetVisibility(true);
                    hasAppeared = true;

                    // 아크 등장 연출 시작
                    StartCoroutine(PlayAppearAnimation());
                }
            }
        }

        // localPosition으로 이동 (NotesParent 기준)
        trans.localPosition += Vector3.left * speed * Time.deltaTime;

        if (trans.localPosition.x < -3000f)
            Destroy(gameObject);
    }

    System.Collections.IEnumerator PlayAppearAnimation()
    {
        // 비주얼 오브젝트만 조작 (실제 위치는 유지)
        if (image == null) yield break;

        float elapsed = 0f;

        Vector3 originalLocalPos = image.transform.localPosition;
        Quaternion originalRotation = image.transform.localRotation;

        // "적" 위치 가져오기
        NoteEffect effect = GetComponent<NoteEffect>();
        Vector3 enemyPos = effect != null ? effect.enemyPosition : new Vector3(0, -400f, 0);

        while (elapsed < APPEAR_ANIM_DURATION)
        {
            // 일시정지 중에는 애니메이션도 멈춤
            if (!PauseManager.IsPaused)
            {
                elapsed += Time.deltaTime;
            }

            float t = elapsed / APPEAR_ANIM_DURATION;

            // Ease-out 곡선
            float easeT = 1f - Mathf.Pow(1f - t, 3f);

            // "적" 위치에서 원래 위치로 아크 모션
            Vector3 startPos = originalLocalPos + enemyPos;
            image.transform.localPosition = Vector3.Lerp(startPos, originalLocalPos, easeT);

            // 회전 효과 (720도 회전으로 천천히)
            float rotation = Mathf.Lerp(720f, 0f, easeT);
            image.transform.localRotation = Quaternion.Euler(0, 0, rotation);

            yield return null;
        }

        // 최종 위치 보정
        image.transform.localPosition = originalLocalPos;
        image.transform.localRotation = originalRotation;
    }

    void SetVisibility(bool visible)
    {
        if (image != null)
        {
            Color c = image.color;
            c.a = visible ? 1f : 0f;
            image.color = c;
        }

        // 오버레이 이미지도 숨김
        NoteEffect effect = GetComponent<NoteEffect>();
        if (effect != null && effect.overlayImage != null)
        {
            Color c = effect.overlayImage.color;
            c.a = 0f;
            effect.overlayImage.color = c;
        }
    }
}