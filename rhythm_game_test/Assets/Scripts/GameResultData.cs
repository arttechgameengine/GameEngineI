using UnityEngine;

// 씬 간 결과 데이터 전달용 static 클래스
public static class GameResultData
{
    public static int Score;
    public static int MaxCombo;
    public static int PerfectCount;
    public static int GreatCount;
    public static int GoodCount;
    public static int MissCount;
    public static int ParryCount;  // 패링 성공 횟수

    public static int TotalNotes => PerfectCount + GreatCount + GoodCount + MissCount;

    // 정확도 계산 (0~100%)
    public static float Accuracy
    {
        get
        {
            if (TotalNotes == 0) return 0f;
            // Perfect=100%, Great=75%, Good=50%, Miss=0%
            float total = (PerfectCount * 100f) + (GreatCount * 75f) + (GoodCount * 50f);
            return total / TotalNotes;
        }
    }

    // 등급 계산
    public static string GetRank()
    {
        float acc = Accuracy;
        if (acc >= 95f) return "S";
        if (acc >= 85f) return "A";
        if (acc >= 70f) return "B";
        if (acc >= 55f) return "C";
        if (acc >= 40f) return "D";
        return "F";
    }

    // ScoreManager에서 데이터 저장
    public static void SaveFromScoreManager(ScoreManager sm)
    {
        Score = sm.currentScore;
        MaxCombo = sm.maxCombo;
        PerfectCount = sm.perfectCount;
        GreatCount = sm.greatCount;
        GoodCount = sm.goodCount;
        MissCount = sm.missCount;
        ParryCount = sm.parryCount;
    }

    // 초기화
    public static void Reset()
    {
        Score = 0;
        MaxCombo = 0;
        PerfectCount = 0;
        GreatCount = 0;
        GoodCount = 0;
        MissCount = 0;
        ParryCount = 0;
    }
}
