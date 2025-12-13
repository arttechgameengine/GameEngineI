using UnityEngine;
using System.Linq;

public class PlayerJudge : MonoBehaviour
{
    public float perfectRange = 0.08f;
    public float greatRange = 0.15f;
    public float goodRange = 0.25f;

    [Header("Long Note Settings")]
    public float longNoteEndRange = 0.5f; // 롱노트 끝 판정 범위 (더 관대함)

    public NoteSpawner spawner;
    public JudgePopup judgePopup;

    private CameraShakeManager cameraShake;
    private ParryMissEffectType currentParryMissEffect;

    // 롱노트 진행 상태
    private class LongNoteState
    {
        public int groupId;
        public string noteType;
        public string startJudge;  // START 노트의 판정 등급
        public bool isHolding;
        public bool isFailed;      // Miss 판정되었지만 bar는 계속 업데이트 중
    }
    private LongNoteState currentLongNote = null;

    void Start()
    {
        cameraShake = GetComponent<CameraShakeManager>();

        // NoteSpawner에서 패링 미스 효과 타입 가져오기
        if (spawner != null)
        {
            currentParryMissEffect = spawner.parryMissEffect;
            Debug.Log($"[PlayerJudge] 패링 미스 효과 설정: {currentParryMissEffect}");
        }
    }

    void Update()
    {
        // 일시정지 중에는 입력 무시
        if (PauseManager.IsPaused) return;

        // 롱노트 진행 중이면 키 홀딩 체크 (실패한 경우는 제외)
        if (currentLongNote != null && !currentLongNote.isFailed)
        {
            KeyCode keyCode = GetKeyCode(currentLongNote.noteType);
            currentLongNote.isHolding = Input.GetKey(keyCode);

            // 키를 떼면 롱노트 실패 처리
            if (!currentLongNote.isHolding)
            {
                FailLongNote();
            }
        }

        // 키 입력 처리
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

            // 롱노트 진행 중이면 현재 그룹의 노트가 아닌 것은 무시 (실패한 경우는 모두 무시)
            if (currentLongNote != null && !currentLongNote.isFailed && n.longNoteGroupId != currentLongNote.groupId)
            {
                continue;
            }

            // 롱노트가 실패 상태면 해당 그룹의 노트는 모두 무시 (이미 MISS 처리됨)
            if (currentLongNote != null && currentLongNote.isFailed && n.longNoteGroupId == currentLongNote.groupId)
            {
                continue;
            }

            // 연타 노트 자동 활성화 체크
            if (n.noteSubType == "RAPID")
            {
                RapidNoteJudge rapidJudge = n.GetComponent<RapidNoteJudge>();
                if (rapidJudge != null)
                {
                    // 이미 완료되었거나 실패한 경우 무시
                    if (rapidJudge.isCompleted || rapidJudge.isFailed)
                    {
                        continue;
                    }

                    float timeDelta = Mathf.Abs((float)(songTime - n.noteTime));

                    // HitLine 도달 시 자동 활성화 (perfectRange 이내)
                    if (!rapidJudge.isActive && timeDelta <= perfectRange)
                    {
                        Debug.Log($"[PlayerJudge] Auto-activating rapid note at HitLine");
                        rapidJudge.Activate();
                        // isJudged는 설정하지 않음 (키 입력을 받아야 하므로)
                    }
                    // 활성화도 못하고 지나가면 Miss
                    else if (!rapidJudge.isActive && songTime > n.noteTime + goodRange)
                    {
                        Debug.Log($"[PlayerJudge] Rapid note missed activation window");
                        rapidJudge.OnMissed();
                    }
                }
                continue;
            }

            // 노트가 판정 시간을 지났는지 확인
            // LONG_END 노트는 더 관대한 범위 사용
            float missRange = (n.noteSubType == "LONG_END") ? longNoteEndRange : goodRange;

            if (songTime > n.noteTime + missRange)
            {
                Debug.Log($"[MISS] songTime: {songTime:F2}, noteTime: {n.noteTime:F2}, diff: {(songTime - n.noteTime):F2}, missRange: {missRange:F2}");

                // LONG_END 노트를 놓쳤으면 롱노트 전체 파괴
                if (n.noteSubType == "LONG_END" && currentLongNote != null && n.longNoteGroupId == currentLongNote.groupId)
                {
                    Debug.Log($"[LONG_END MISS] Duration ended, destroying long note group {n.longNoteGroupId}");

                    // 롱노트 그룹 전체 파괴
                    DestroyLongNoteGroup(n.longNoteGroupId);

                    // 롱노트 상태 종료
                    currentLongNote = null;

                    // MISS 판정
                    judgePopup.ShowJudge("MISS");
                    ScoreManager.Instance.AddJudge("MISS");

                    // 화면 흔들림
                    if (cameraShake != null)
                    {
                        cameraShake.ShakeOnNormalMiss();
                    }
                }
                else
                {
                    Miss(n);
                }
            }
        }
    }

    void CheckLongNoteHold()
    {
        if (currentLongNote == null) return;

        double songTime = AudioSettings.dspTime - spawner.songStartDspTime;
        NoteMovement[] allNotes = FindObjectsOfType<NoteMovement>();

        // LONG_START 노트를 찾아서 막대 길이 업데이트 (실패 상태여도 계속 업데이트)
        NoteMovement startNote = null;
        NoteMovement endNote = null;

        foreach (var n in allNotes)
        {
            if (n.longNoteGroupId != currentLongNote.groupId) continue;

            if (n.noteSubType == "LONG_START")
            {
                startNote = n;
            }
            else if (n.noteSubType == "LONG_END")
            {
                endNote = n;
            }
        }

        // 막대 길이 업데이트 (START와 END 노트 사이의 거리로 계산)
        // 실패 상태여도 bar가 계속 짧아지도록 업데이트
        if (startNote != null && endNote != null && startNote.longNoteVisualBar != null)
        {
            UpdateLongNoteBarLength(startNote, endNote);
        }

        // Hold 노트 자동 판정 체크 (실패 상태면 스킵)
        if (!currentLongNote.isFailed)
        {
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
    }

    void UpdateLongNoteBarLength(NoteMovement startNote, NoteMovement endNote)
    {
        if (startNote.longNoteVisualBar == null) return;

        RectTransform barRect = startNote.longNoteVisualBar.GetComponent<RectTransform>();
        if (barRect == null) return;

        // START 노트와 END 노트 사이의 거리 계산
        float distance = endNote.transform.localPosition.x - startNote.transform.localPosition.x;

        // 거리가 음수면 0으로 (END가 START보다 왼쪽에 있으면)
        distance = Mathf.Max(0, distance);

        // 막대 길이 업데이트
        barRect.sizeDelta = new Vector2(distance, barRect.sizeDelta.y);
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

            // 롱노트가 실패 상태면 키 입력 무시
            if (currentLongNote != null && currentLongNote.isFailed)
            {
                continue;
            }

            // 일반 모드: 가장 가까운 노트 찾기
            if (timeDelta < minTimeDelta)
            {
                minTimeDelta = timeDelta;
                target = n;
            }
        }

        if (target == null) return;

        // 연타 노트 처리
        if (target.noteSubType == "RAPID")
        {
            HandleRapidNote(keyType, target);
            return;
        }

        // 판정 등급 계산
        // LONG_END 노트는 더 관대한 범위 사용
        float judgementRange = (target.noteSubType == "LONG_END") ? longNoteEndRange : goodRange;

        string judge = "";
        if (minTimeDelta <= perfectRange) judge = "PERFECT";
        else if (minTimeDelta <= greatRange) judge = "GREAT";
        else if (minTimeDelta <= judgementRange) judge = "GOOD";
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

    /// <summary>
    /// 연타 노트 처리 (키 입력만 전달, 활성화는 CheckMissedNotes에서 자동 처리)
    /// </summary>
    void HandleRapidNote(string keyType, NoteMovement target)
    {
        // RapidNoteJudge 컴포넌트 확인
        RapidNoteJudge rapidJudge = target.GetComponent<RapidNoteJudge>();
        if (rapidJudge == null)
        {
            Debug.LogWarning($"[PlayerJudge] Rapid note without RapidNoteJudge component!");
            return;
        }

        // 활성화된 상태에서만 키 입력 전달
        if (rapidJudge.isActive)
        {
            rapidJudge.OnKeyPressed(keyType);
        }
        // 활성화 안 됐으면 무시 (CheckMissedNotes에서 자동 활성화 대기 중)
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
            isHolding = true,
            isFailed = false
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

        // 롱노트 막대를 회색으로 변경 (파괴하지 않음)
        FadeLongNoteBar(currentLongNote.groupId);

        // 화면 흔들림
        if (cameraShake != null)
        {
            cameraShake.ShakeOnNormalMiss();
        }

        // 롱노트 상태를 실패로 표시 (null로 만들지 않고 bar 업데이트는 계속)
        currentLongNote.isFailed = true;
    }

    void FadeLongNoteBar(int groupId)
    {
        NoteMovement[] allNotes = FindObjectsOfType<NoteMovement>();
        foreach (var n in allNotes)
        {
            if (n.longNoteGroupId == groupId && n.noteSubType == "LONG_START")
            {
                // START 노트의 시각적 막대 찾기
                if (n.longNoteVisualBar != null)
                {
                    UnityEngine.UI.Image barImage = n.longNoteVisualBar.GetComponent<UnityEngine.UI.Image>();
                    if (barImage != null)
                    {
                        // 회색으로 변경 (잘 보이도록)
                        Color fadedColor = Color.gray;
                        fadedColor.a = 0.8f;
                        barImage.color = fadedColor;

                        // 롱노트 duration 이후에 막대 파괴
                        float duration = 0f;

                        // duration 정보 찾기 (LONG_END 노트의 시간 - LONG_START 노트의 시간)
                        double songTime = AudioSettings.dspTime - spawner.songStartDspTime;
                        NoteMovement endNote = System.Array.Find(allNotes, note =>
                            note.longNoteGroupId == groupId && note.noteSubType == "LONG_END");

                        if (endNote != null)
                        {
                            duration = Mathf.Max(0, endNote.noteTime - (float)songTime);
                        }

                        Debug.Log($"[LongNote Fail] Faded long note bar for group {groupId}, will destroy after {duration:F2}s");

                        // duration 후에 bar 파괴 + currentLongNote null 처리
                        StartCoroutine(DestroyBarAfterDuration(n.longNoteVisualBar, duration, groupId));
                    }
                }
                break;
            }
        }
    }

    System.Collections.IEnumerator DestroyBarAfterDuration(GameObject bar, float duration, int groupId)
    {
        yield return new WaitForSeconds(duration);

        if (bar != null)
        {
            Destroy(bar);
        }

        // duration이 끝나면 currentLongNote null 처리
        if (currentLongNote != null && currentLongNote.groupId == groupId)
        {
            Debug.Log($"[LongNote Fail] Duration ended, clearing currentLongNote for group {groupId}");
            currentLongNote = null;
        }
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

        Debug.Log($"MISS ({n.noteType}), subType: {n.noteSubType}");
        judgePopup.ShowJudge("MISS");
        ScoreManager.Instance.AddJudge("MISS");

        // LONG_START 노트를 놓쳤으면 롱노트 막대를 회색으로 변경
        if (n.noteSubType == "LONG_START")
        {
            Debug.Log($"[MISS] LONG_START missed! Fading bar for group {n.longNoteGroupId}");

            // currentLongNote 설정 (bar 업데이트를 위해)
            currentLongNote = new LongNoteState
            {
                groupId = n.longNoteGroupId,
                noteType = n.noteType,
                startJudge = "MISS",
                isHolding = false,
                isFailed = true
            };

            FadeLongNoteBar(n.longNoteGroupId);

            // 남은 LONG_HOLD와 LONG_END 노트들도 모두 MISS 처리
            NoteMovement[] allNotes = FindObjectsOfType<NoteMovement>();
            foreach (var note in allNotes)
            {
                if (note.longNoteGroupId == n.longNoteGroupId && !note.isJudged)
                {
                    note.isJudged = true;
                    ScoreManager.Instance.AddJudge("MISS");
                    Debug.Log($"[MISS] Auto-miss {note.noteSubType} for group {n.longNoteGroupId}");
                }
            }
        }

        // 패링 노트(SPACE) 미스 시 선택된 효과 적용
        bool isParryNote = (n.noteType == "SPACE");
        if (cameraShake != null)
        {
            if (isParryNote)
            {
                // 선택된 패링 미스 효과 재생
                cameraShake.PlayParryMissEffect(currentParryMissEffect);
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
