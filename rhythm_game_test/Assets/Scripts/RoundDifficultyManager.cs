using UnityEngine;
using UnityEngine.UI;
using System.Linq;

/// <summary>
/// 라운드별로 난이도를 관리하는 매니저
/// 하나의 JSON 패턴으로 여러 난이도 라운드를 구현
/// </summary>
public class RoundDifficultyManager : MonoBehaviour
{
    [System.Serializable]
    public class RoundSettings
    {
        [Header("Round Info")]
        public string roundName = "Round 1";
        public DifficultyFilter.Difficulty difficulty = DifficultyFilter.Difficulty.Easy;

        [Header("Pattern Settings")]
        public string patternFileName = "Charts/my_pattern_chart.json";
        public float noteSpeed = 500f; // 이 속도로 JSON의 noteSpeed 오버라이드

        [Header("Timing Settings")]
        public float prepareTime = 3f; // 음악 시작 후 첫 노트까지 준비 시간 (초)

        [Header("Visual Settings")]
        public float spawnDistance = 1000f; // spawnPoint와 hitLine 사이 거리

        [Header("Description")]
        [TextArea(2, 4)]
        public string description = "Easy mode - 기본 박자만, 느린 속도";
    }

    [Header("Round Configuration")]
    public RoundSettings[] rounds = new RoundSettings[]
    {
        new RoundSettings
        {
            roundName = "Round 1 - Easy",
            difficulty = DifficultyFilter.Difficulty.Easy,
            patternFileName = "Charts/my_pattern_chart.json",
            noteSpeed = 300f,
            spawnDistance = 1200f,
            description = "쉬움: 기본 박자만, 홀드/SPACE 제거, 느린 속도"
        },
        new RoundSettings
        {
            roundName = "Round 2 - Normal",
            difficulty = DifficultyFilter.Difficulty.Normal,
            patternFileName = "Charts/my_pattern_chart.json",
            noteSpeed = 500f,
            spawnDistance = 1000f,
            description = "보통: 중간 밀도, 적당한 속도"
        },
        new RoundSettings
        {
            roundName = "Round 3 - Hard",
            difficulty = DifficultyFilter.Difficulty.Hard,
            patternFileName = "Charts/my_pattern_chart.json",
            noteSpeed = 700f,
            spawnDistance = 800f,
            description = "어려움: 모든 패턴, 빠른 속도"
        }
    };

    [Header("References")]
    public NoteSpawner noteSpawner;
    public AudioSource bgmSource;
    public Text roundInfoText; // UI Text for round info (optional)

    private int currentRoundIndex = 0;

    void Start()
    {
        if (noteSpawner == null)
            noteSpawner = FindObjectOfType<NoteSpawner>();

        if (bgmSource == null)
            bgmSource = FindObjectOfType<AudioSource>();

        // 첫 번째 라운드 시작
        StartRound(0);
    }

    /// <summary>
    /// 특정 라운드 시작
    /// </summary>
    public void StartRound(int roundIndex)
    {
        if (roundIndex < 0 || roundIndex >= rounds.Length)
        {
            Debug.LogError($"[RoundDifficultyManager] Invalid round index: {roundIndex}");
            return;
        }

        currentRoundIndex = roundIndex;
        RoundSettings round = rounds[roundIndex];

        Debug.Log($"[RoundDifficultyManager] Starting {round.roundName} - {round.difficulty}");

        // 1. 패턴 로드 (난이도 필터 적용)
        PatternData pattern = PatternLoader.LoadPattern(round.patternFileName, round.difficulty);

        if (pattern == null || pattern.notes == null || pattern.notes.Count == 0)
        {
            Debug.LogError($"[RoundDifficultyManager] Failed to load pattern: {round.patternFileName}");
            return;
        }

        // 2. noteSpeed 오버라이드
        pattern.noteSpeed = round.noteSpeed;

        // 3. spawnPoint 위치 조정 (속도에 맞게 간격 유지)
        if (noteSpawner != null)
        {
            AdjustSpawnDistance(round.spawnDistance);
        }

        // 4. 준비 시간 적용 (모든 노트 시간에 prepareTime 더하기)
        ApplyPrepareTime(pattern, round.prepareTime);

        // 5. NoteSpawner에 패턴 로드
        noteSpawner.LoadPattern(pattern);

        // 6. UI 업데이트 (optional)
        if (roundInfoText != null)
        {
            roundInfoText.text = $"{round.roundName}\n{round.description}\nNotes: {pattern.notes.Count}";
        }

        // 7. 음악 즉시 시작 (준비 시간은 노트 타이밍에 이미 반영됨)
        StartMusic();
    }

    /// <summary>
    /// 준비 시간을 모든 노트에 적용 (음악은 즉시 시작, 노트는 prepareTime 후 시작)
    /// </summary>
    void ApplyPrepareTime(PatternData pattern, float prepareTime)
    {
        if (pattern == null || pattern.notes == null) return;

        Debug.Log($"[RoundDifficultyManager] Applying prepare time: {prepareTime}s");

        foreach (var note in pattern.notes)
        {
            note.time += prepareTime;
        }

        Debug.Log($"[RoundDifficultyManager] First note time after prepare: {pattern.notes.OrderBy(n => n.time).First().time:F3}s");
    }

    /// <summary>
    /// spawnPoint 위치를 동적으로 조정하여 노트 간격 유지
    /// </summary>
    void AdjustSpawnDistance(float desiredDistance)
    {
        if (noteSpawner == null || noteSpawner.spawnPoint == null || noteSpawner.hitLine == null)
            return;

        // hitLine 위치는 고정, spawnPoint만 이동
        float hitLineX = noteSpawner.hitLine.localPosition.x;
        float newSpawnX = hitLineX + desiredDistance;

        noteSpawner.spawnPoint.localPosition = new Vector3(newSpawnX, noteSpawner.spawnPoint.localPosition.y, 0);

        Debug.Log($"[RoundDifficultyManager] Adjusted spawnPoint to X={newSpawnX} (distance={desiredDistance})");
    }

    void StartMusic()
    {
        if (noteSpawner != null && bgmSource != null)
        {
            noteSpawner.StartSong(bgmSource);
        }
    }

    /// <summary>
    /// 다음 라운드로 진행
    /// </summary>
    public void NextRound()
    {
        int nextIndex = currentRoundIndex + 1;
        if (nextIndex < rounds.Length)
        {
            StartRound(nextIndex);
        }
        else
        {
            Debug.Log("[RoundDifficultyManager] All rounds completed!");
            // 여기에 게임 종료 또는 결과 화면 로직 추가
        }
    }

    /// <summary>
    /// 특정 난이도의 라운드로 점프
    /// </summary>
    public void JumpToRound(int index)
    {
        StartRound(index);
    }

    // 테스트용 키보드 입력
    void Update()
    {
        // 숫자 키 1, 2, 3으로 라운드 전환
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            StartRound(0); // Easy
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            StartRound(1); // Normal
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            StartRound(2); // Hard
        }
        else if (Input.GetKeyDown(KeyCode.N))
        {
            NextRound(); // 다음 라운드
        }
    }
}

/*
 * 설정 가이드:
 *
 * 1. Inspector에서 rounds 배열 설정:
 *    - Size = 3 (Easy, Normal, Hard)
 *    - 각 라운드마다:
 *      * roundName: 라운드 이름
 *      * difficulty: Easy/Normal/Hard
 *      * patternFileName: JSON 파일 경로 (같은 파일 사용 가능!)
 *      * noteSpeed: 속도 (Easy=300, Normal=500, Hard=700 권장)
 *      * spawnDistance: 간격 (Easy=1200, Normal=1000, Hard=800 권장)
 *
 * 2. noteSpeed와 spawnDistance 관계:
 *    - noteSpeed ↑ (빠름) → spawnDistance ↓ (가까이)
 *      이유: 빠르게 이동하므로 가까워도 타이밍 맞음
 *    - noteSpeed ↓ (느림) → spawnDistance ↑ (멀리)
 *      이유: 느리게 이동하므로 멀어야 간격 유지
 *
 * 3. 타이밍 보장:
 *    - note.time (음악 타이밍)은 절대 변하지 않음
 *    - spawnLeadTime = spawnDistance / noteSpeed 로 자동 계산
 *    - 어떤 설정이든 음악과 정확히 싱크 맞음
 *
 * 4. 난이도 차이:
 *    - Easy: 노트 개수 적음 + 느린 속도 + 넓은 간격 = 쉬움
 *    - Normal: 중간 노트 개수 + 보통 속도 + 보통 간격 = 적당함
 *    - Hard: 모든 노트 + 빠른 속도 + 좁은 간격 = 어려움
 *
 * 5. 사용 예:
 *    - 한 씬에서 여러 라운드 구현 가능
 *    - 같은 JSON 파일로 3가지 난이도 생성
 *    - Scene 분리 필요 없음
 */
