using UnityEngine;

/// <summary>
/// DifficultyFilter 사용 예제
///
/// 하나의 JSON 패턴 파일로 Easy/Normal/Hard 난이도를 구현하는 방법
/// </summary>
public class DifficultyFilterUsageExample : MonoBehaviour
{
    [Header("Pattern Settings")]
    public string patternFileName = "Charts/my_pattern_chart.json";

    [Header("Difficulty Settings")]
    public DifficultyFilter.Difficulty currentDifficulty = DifficultyFilter.Difficulty.Normal;

    private NoteSpawner noteSpawner;

    void Start()
    {
        noteSpawner = FindObjectOfType<NoteSpawner>();

        // 예제 1: 기본 난이도 설정으로 패턴 로드
        LoadPatternWithDifficulty(currentDifficulty);
    }

    /// <summary>
    /// 난이도를 지정하여 패턴 로드
    /// </summary>
    void LoadPatternWithDifficulty(DifficultyFilter.Difficulty difficulty)
    {
        // PatternLoader가 자동으로 난이도 필터를 적용함
        PatternData pattern = PatternLoader.LoadPattern(patternFileName, difficulty);

        if (noteSpawner != null)
        {
            noteSpawner.LoadPattern(pattern);
            Debug.Log($"[Example] Loaded pattern with {difficulty} difficulty - {pattern.notes.Count} notes");
        }
    }

    /// <summary>
    /// 예제 2: 라운드별로 다른 난이도 적용
    /// </summary>
    public void LoadRound(int roundNumber)
    {
        DifficultyFilter.Difficulty difficulty = DifficultyFilter.Difficulty.Normal;

        switch (roundNumber)
        {
            case 1:
                difficulty = DifficultyFilter.Difficulty.Easy;
                Debug.Log("Round 1: Easy mode");
                break;
            case 2:
                difficulty = DifficultyFilter.Difficulty.Normal;
                Debug.Log("Round 2: Normal mode");
                break;
            case 3:
                difficulty = DifficultyFilter.Difficulty.Hard;
                Debug.Log("Round 3: Hard mode");
                break;
        }

        LoadPatternWithDifficulty(difficulty);
    }

    /// <summary>
    /// 예제 3: 커스텀 필터 설정 (고급)
    /// </summary>
    void LoadPatternWithCustomFilter()
    {
        // JSON 텍스트 로드
        string jsonPath = System.IO.Path.Combine(Application.dataPath, patternFileName);
        string jsonText = System.IO.File.ReadAllText(jsonPath);
        PatternData pattern = JsonUtility.FromJson<PatternData>(jsonText);

        // 커스텀 필터 설정 생성
        DifficultyFilter.FilterSettings customSettings = new DifficultyFilter.FilterSettings
        {
            minNoteInterval = 0.15f,              // 150ms보다 짧은 간격 필터링
            rapidFireReductionRatio = 0.5f,       // 연타의 50% 제거
            densityReductionRatio = 0.3f,         // 밀도 높은 구간 30% 제거
            removeHoldNotes = false,              // 홀드 노트 유지
            removeSpaceNotes = true,              // SPACE 노트 제거
            keepEveryNthNote = 1                  // 모든 노트 유지
        };

        // 커스텀 필터 적용
        pattern.notes = DifficultyFilter.ApplyCustomFilter(pattern.notes, customSettings);

        if (noteSpawner != null)
        {
            noteSpawner.LoadPattern(pattern);
            Debug.Log($"[Example] Loaded pattern with custom filter - {pattern.notes.Count} notes");
        }
    }

    /// <summary>
    /// 예제 4: 특정 lane만 사용 (2-lane 모드)
    /// </summary>
    void LoadTwoLaneMode()
    {
        string jsonPath = System.IO.Path.Combine(Application.dataPath, patternFileName);
        string jsonText = System.IO.File.ReadAllText(jsonPath);
        PatternData pattern = JsonUtility.FromJson<PatternData>(jsonText);

        // Lane 0과 1만 사용
        DifficultyFilter.FilterSettings settings = new DifficultyFilter.FilterSettings
        {
            allowedLanes = new System.Collections.Generic.List<int> { 0, 1 }
        };

        pattern.notes = DifficultyFilter.ApplyCustomFilter(pattern.notes, settings);

        if (noteSpawner != null)
        {
            noteSpawner.LoadPattern(pattern);
            Debug.Log($"[Example] Two-lane mode - {pattern.notes.Count} notes");
        }
    }

    // 런타임에서 난이도 변경 테스트 (키보드 입력)
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            LoadRound(1); // Easy
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            LoadRound(2); // Normal
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            LoadRound(3); // Hard
        }
    }
}

/*
 * 사용 방법:
 *
 * 1. 기본 사용법:
 *    - PatternLoader.LoadPattern("Charts/song.json", DifficultyFilter.Difficulty.Easy);
 *    - 하나의 JSON으로 Easy/Normal/Hard 모두 생성됨
 *
 * 2. 난이도별 특징:
 *    Easy:
 *      - 노트 개수: 원본의 약 40-50%
 *      - 연타 제거: 80%
 *      - 홀드/SPACE 노트 제거
 *      - 간격이 넓어서 여유로움
 *
 *    Normal:
 *      - 노트 개수: 원본의 약 70-80%
 *      - 연타 일부 제거: 40%
 *      - 홀드/SPACE 노트 유지
 *      - 적당한 난이도
 *
 *    Hard:
 *      - 노트 개수: 원본 그대로 (100%)
 *      - 모든 패턴 유지
 *      - 최고 난이도
 *
 * 3. 커스터마이징:
 *    - DifficultyFilter.cs의 GetFilterSettings() 함수에서
 *      각 난이도별 설정을 원하는 대로 조정 가능
 *
 * 4. 속도 조절과 함께 사용:
 *    - JSON에서 noteSpeed 설정
 *    - Easy: noteSpeed = 300 (느림)
 *    - Normal: noteSpeed = 500 (보통)
 *    - Hard: noteSpeed = 700 (빠름)
 *    - 필터링 + 속도 조절 = 완벽한 난이도 분리
 *
 * 5. 주의사항:
 *    - 음악 타이밍(note.time)은 절대 변하지 않음
 *    - 필터링은 노트 개수만 줄임 (타이밍 유지)
 *    - 속도 조절은 시각적 체감만 변화 (타이밍 유지)
 */
