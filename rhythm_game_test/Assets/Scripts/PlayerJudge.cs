using UnityEngine;
using System.Linq;

public class PlayerJudge : MonoBehaviour
{
    public float perfectRange = 0.08f;
    public float greatRange = 0.15f;
    public float goodRange = 0.25f;

    public NoteSpawner spawner;
    public JudgePopup judgePopup;

    private CameraShakeManager cameraShake;

    // 롱노트 진행 상태
    private class LongNoteState
    {
        public int groupId;
        public string noteType;
        public string startJudge;  // START 노트의 판정 등급
        public bool isHolding;
    }
    private LongNoteState currentLongNote = null;

    void Start()
    {
        cameraShake = GetComponent<CameraShakeManager>();
    }

    void Update()
    {
        // 일시정지 중에는 입력 무시
        if (PauseManager.IsPaused) return;

        // 롱노트 진행 중이면 키 홀딩 체크
        if (currentLongNote != null)
        {
            KeyCode keyCode = GetKeyCode(currentLongNote.noteType);
            currentLongNote.isHolding = Input.GetKey(keyCode);

            // 키를 떼면 롱노트 실패 처리
            if (!currentLongNote.isHolding)
            {
                FailLongNote();
            }
        }

        if (Input.GetKeyDown(KeyCode.LeftArrow)) TryHit("LEFT");
        if (Input.GetKeyDown(KeyCode.RightArrow)) TryHit("RIGHT");
        if (Input.GetKeyDown(KeyCode.UpArrow)) TryHit("UP");
        if (Input.GetKeyDown(KeyCode.DownArrow)) TryHit("DOWN");
        if (Input.GetKeyDown(KeyCode.Space)) TryHit("SPACE");

        CheckMissedNotes();
        CheckLongNoteHold();
    }

    KeyCode GetKeyCode(string noteType)
    {
        switch (noteType)
        {
            case "LEFT": return KeyCode.LeftArrow;
            case "RIGHT": return KeyCode.RightArrow;
            case "UP": return KeyCode.UpArrow;
            case "DOWN": return KeyCode.DownArrow;
            case "SPACE": return KeyCode.Space;
            default: return KeyCode.None;
        }
    }

    void CheckMissedNotes()
    {
        double songTime = AudioSettings.dspTime - spawner.songStartDspTime;

        NoteMovement[] allNotes = FindObjectsOfType<NoteMovement>();

        foreach (var n in allNotes)
        {
            // 이미 판정된 노트는 무시
            if (n.isJudged) continue;

            // 롱노트 진행 중이면 현재 그룹의 노트가 아닌 것은 무시
            if (currentLongNote != null && n.longNoteGroupId != currentLongNote.groupId)
            {
                continue;
            }

            // 노트의 판정 시간이 지나고 goodRange까지 벗어났으면 MISS
            if (songTime > n.noteTime + goodRange)
            {
                Debug.Log($"[MISS] songTime: {songTime:F2}, noteTime: {n.noteTime:F2}, diff: {(songTime - n.noteTime):F2}");
                Miss(n);
            }
        }
    }

    void CheckLongNoteHold()
    {
        if (currentLongNote == null) return;

        double songTime = AudioSettings.dspTime - spawner.songStartDspTime;
        NoteMovement[] allNotes = FindObjectsOfType<NoteMovement>();

        foreach (var n in allNotes)
        {
            if (n.isJudged) continue;
            if (n.longNoteGroupId != currentLongNote.groupId) continue;
            if (n.noteSubType != "LONG_HOLD") continue;

            // Hold 노트가 판정 시간에 도달하면 자동 판정
            float timeDelta = Mathf.Abs((float)(songTime - n.noteTime));
            if (timeDelta <= goodRange)
            {
                // Start 노트의 판정 등급으로 자동 판정
                AutoJudgeLongHold(n, currentLongNote.startJudge);
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

            float timeDelta = Mathf.Abs((float)(songTime - n.noteTime));

            // 롱노트 진행 중이면 현재 그룹의 LONG_END 노트만 판정 가능
            if (currentLongNote != null)
            {
                if (n.longNoteGroupId == currentLongNote.groupId && n.noteSubType == "LONG_END")
                {
                    if (timeDelta < minTimeDelta)
                    {
                        minTimeDelta = timeDelta;
                        target = n;
                    }
                }
                continue; // 롱노트 진행 중에는 다른 노트 무시
            }

            // 일반 모드: 가장 가까운 노트 찾기
            if (timeDelta < minTimeDelta)
            {
                minTimeDelta = timeDelta;
                target = n;
            }
        }

        if (target == null) return;

        // 판정 등급 계산
        string judge = "";
        if (minTimeDelta <= perfectRange) judge = "PERFECT";
        else if (minTimeDelta <= greatRange) judge = "GREAT";
        else if (minTimeDelta <= goodRange) judge = "GOOD";
        else
        {
            Miss();
            return;
        }

        // 노트 타입별 처리
        if (target.noteSubType == "LONG_START")
        {
            HitLongStart(judge, target);
        }
        else if (target.noteSubType == "LONG_END")
        {
            HitLongEnd(judge, target);
        }
        else
        {
            Hit(judge, target);
        }
    }

    void Hit(string judge, NoteMovement n)
    {
        n.isJudged = true;  // 판정 완료 표시

        Debug.Log($"{judge} ({n.noteType})");
        judgePopup.ShowJudge(judge);
        ScoreManager.Instance.AddJudge(judge);

        // SPACE 노트 성공 시 패링 카운트 추가
        bool isParry = (n.noteType == "SPACE");
        if (isParry)
        {
            ScoreManager.Instance.AddParrySuccess();

            // 패링 성공 시 카메라 흔들림
            if (cameraShake != null)
            {
                cameraShake.ShakeOnParrySuccess();
            }
        }

        // 요리 효과 재생 (기존 시스템) - 주석 처리: CookingAreaManager 사용
        // if (CookingEffect.Instance != null)
        // {
        //     CookingEffect.Instance.PlayCookingEffect(n.noteType);
        // }

        // 고정 요리 스프라이트 애니메이션 재생 (새로운 시스템)
        bool shouldPlayCooking = (CookingAreaManager.Instance != null && n.noteType != "SPACE");
        Debug.Log($"[PlayerJudge Hit] noteType={n.noteType}, Instance={CookingAreaManager.Instance != null}, shouldPlay={shouldPlayCooking}");

        if (shouldPlayCooking)
        {
            CookingAreaManager.Instance.PlayCookingAnimation(n.noteType);
        }

        // 히트 효과 재생 후 파괴
        NoteEffect effect = n.GetComponent<NoteEffect>();
        if (effect != null)
        {
            // SPACE 노트는 적 위치로 날아가는 효과
            if (isParry)
            {
                effect.PlayParryReturnEffect(() => Destroy(n.gameObject));
            }
            else
            {
                effect.PlayHitEffect(() => Destroy(n.gameObject));
            }
        }
        else
        {
            Destroy(n.gameObject);
        }
    }

    void HitLongStart(string judge, NoteMovement n)
    {
        n.isJudged = true;

        Debug.Log($"[LongNote Start] {judge} ({n.noteType}), groupId: {n.longNoteGroupId}");
        judgePopup.ShowJudge(judge);
        ScoreManager.Instance.AddJudge(judge);

        // 롱노트 상태 시작
        currentLongNote = new LongNoteState
        {
            groupId = n.longNoteGroupId,
            noteType = n.noteType,
            startJudge = judge,
            isHolding = true
        };

        // 요리 효과 재생 (기존 시스템) - 주석 처리: CookingAreaManager 사용
        // if (CookingEffect.Instance != null)
        // {
        //     CookingEffect.Instance.PlayCookingEffect(n.noteType);
        // }

        // 고정 요리 스프라이트 애니메이션 재생 (새로운 시스템)
        if (CookingAreaManager.Instance != null && n.noteType != "SPACE")
        {
            CookingAreaManager.Instance.PlayCookingAnimation(n.noteType);
        }

        // Start 노트는 히트 효과만 재생, 파괴하지 않음 (End까지 유지)
        NoteEffect effect = n.GetComponent<NoteEffect>();
        if (effect != null)
        {
            effect.PlayHitEffect(null); // 파괴 콜백 없음
        }
    }

    void HitLongEnd(string judge, NoteMovement n)
    {
        if (currentLongNote == null || currentLongNote.groupId != n.longNoteGroupId)
        {
            Debug.LogWarning("[LongNote End] 롱노트 상태가 없거나 그룹 ID가 맞지 않음!");
            return;
        }

        n.isJudged = true;

        Debug.Log($"[LongNote End] {judge} ({n.noteType}), groupId: {n.longNoteGroupId}");
        judgePopup.ShowJudge(judge);
        ScoreManager.Instance.AddJudge(judge);

        // 요리 효과 재생 (기존 시스템) - 주석 처리: CookingAreaManager 사용
        // if (CookingEffect.Instance != null)
        // {
        //     CookingEffect.Instance.PlayCookingEffect(n.noteType);
        // }

        // 고정 요리 스프라이트 애니메이션 재생 (새로운 시스템)
        if (CookingAreaManager.Instance != null && n.noteType != "SPACE")
        {
            CookingAreaManager.Instance.PlayCookingAnimation(n.noteType);
        }

        // 같은 그룹의 모든 노트 파괴 (Start, Hold 포함)
        DestroyLongNoteGroup(n.longNoteGroupId);

        // 롱노트 상태 종료
        currentLongNote = null;
    }

    void AutoJudgeLongHold(NoteMovement n, string judge)
    {
        n.isJudged = true;

        Debug.Log($"[LongNote Hold] {judge} ({n.noteType}), groupId: {n.longNoteGroupId}");
        ScoreManager.Instance.AddJudge(judge);

        // Hold 노트는 조용히 파괴 (이펙트 없음)
        Destroy(n.gameObject);
    }

    void FailLongNote()
    {
        if (currentLongNote == null) return;

        Debug.Log($"[LongNote Fail] 키를 뗌! groupId: {currentLongNote.groupId}");

        // 남은 Hold 노트와 End 노트를 모두 MISS 처리
        NoteMovement[] allNotes = FindObjectsOfType<NoteMovement>();
        foreach (var n in allNotes)
        {
            if (n.longNoteGroupId == currentLongNote.groupId && !n.isJudged)
            {
                n.isJudged = true;
                ScoreManager.Instance.AddJudge("MISS");
                Debug.Log($"[LongNote Fail] MISS ({n.noteSubType}), groupId: {n.longNoteGroupId}");
            }
        }

        // 같은 그룹의 모든 노트 파괴
        DestroyLongNoteGroup(currentLongNote.groupId);

        // 화면 흔들림
        if (cameraShake != null)
        {
            cameraShake.ShakeOnNormalMiss();
        }

        // 롱노트 상태 종료
        currentLongNote = null;
    }

    void DestroyLongNoteGroup(int groupId)
    {
        NoteMovement[] allNotes = FindObjectsOfType<NoteMovement>();
        foreach (var n in allNotes)
        {
            if (n.longNoteGroupId == groupId)
            {
                // 시각적 막대도 파괴 (LONG_START 노트가 가지고 있음)
                if (n.longNoteVisualBar != null)
                {
                    Destroy(n.longNoteVisualBar);
                }
                Destroy(n.gameObject);
            }
        }
    }

    // 키를 눌렀지만 범위 밖인 경우 (노트 파괴 안함)
    void Miss()
    {
        Debug.Log("MISS");
        judgePopup.ShowJudge("MISS");
        ScoreManager.Instance.AddJudge("MISS");

        // 일반 미스 화면 흔들림
        if (cameraShake != null)
        {
            cameraShake.ShakeOnNormalMiss();
        }
    }

    // 노트를 놓친 경우 (노트 파괴)
    void Miss(NoteMovement n)
    {
        n.isJudged = true;  // 판정 완료 표시

        Debug.Log($"MISS ({n.noteType})");
        judgePopup.ShowJudge("MISS");
        ScoreManager.Instance.AddJudge("MISS");

        // 패링 노트(SPACE) 미스 시 강한 화면 흔들림
        bool isParryNote = (n.noteType == "SPACE");
        if (cameraShake != null)
        {
            if (isParryNote)
            {
                cameraShake.ShakeOnParryMiss();
            }
            else
            {
                cameraShake.ShakeOnNormalMiss();
            }
        }

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
