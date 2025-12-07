using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 하나의 패턴 JSON에서 난이도별로 노트를 필터링하는 시스템
/// Easy/Normal/Hard 등 다양한 난이도를 동적으로 생성 가능
/// </summary>
public static class DifficultyFilter
{
    public enum Difficulty
    {
        Easy,       // 쉬움 - 기본 박자만, 밀도 낮음
        Normal,     // 보통 - 중간 밀도, 복잡한 패턴 일부 제거
        Hard        // 어려움 - 원본 그대로 또는 강화
    }

    /// <summary>
    /// 노트 필터링 설정 (난이도별 커스터마이징 가능)
    /// </summary>
    public class FilterSettings
    {
        // 노트 간 최소 시간 간격 (이보다 짧으면 "밀도 높음"으로 간주)
        public float minNoteInterval = 0.1f;

        // 연타 판정 기준 (이 시간 안에 여러 노트가 있으면 연타로 간주)
        public float rapidFireWindow = 0.15f;

        // 홀드 노트 제거 여부
        public bool removeHoldNotes = false;

        // SPACE 노트 제거 여부
        public bool removeSpaceNotes = false;

        // 연타 노트 제거 여부
        public bool removeRapidNotes = false;

        // 밀도 높은 구간 제거 비율 (0.0 ~ 1.0)
        // 예: 0.5 = 밀도 높은 노트 중 50%를 랜덤 제거
        public float densityReductionRatio = 0.0f;

        // 연타 패턴 제거 비율 (0.0 ~ 1.0)
        // 예: 0.7 = 연타 패턴 중 70%를 제거
        public float rapidFireReductionRatio = 0.0f;

        // 특정 lane만 유지 (null이면 모든 lane 허용)
        public List<int> allowedLanes = null;

        // 노트 간격 기반 필터링: N번째 노트만 유지 (예: 2 = 짝수번째만)
        public int keepEveryNthNote = 1;
    }

    /// <summary>
    /// 난이도에 맞게 노트 리스트 필터링
    /// </summary>
    public static List<NoteData> ApplyFilter(List<NoteData> sourceNotes, Difficulty difficulty)
    {
        if (sourceNotes == null || sourceNotes.Count == 0)
        {
            Debug.LogWarning("[DifficultyFilter] Source notes is empty!");
            return new List<NoteData>();
        }

        // 난이도별 설정 생성
        FilterSettings settings = GetFilterSettings(difficulty);

        // 필터링 적용
        List<NoteData> filtered = FilterNotes(sourceNotes, settings);

        Debug.Log($"[DifficultyFilter] {difficulty} - Original: {sourceNotes.Count}, Filtered: {filtered.Count}");

        return filtered;
    }

    /// <summary>
    /// 난이도별 기본 필터 설정 (커스터마이징 가능)
    /// </summary>
    private static FilterSettings GetFilterSettings(Difficulty difficulty)
    {
        FilterSettings settings = new FilterSettings();

        switch (difficulty)
        {
            case Difficulty.Easy:
                // 쉬움: 밀도 낮음, 연타 제거, 홀드/스페이스/연타노트 제거
                settings.minNoteInterval = 0.2f;        // 200ms보다 짧은 간격 제거
                settings.rapidFireWindow = 0.15f;
                settings.rapidFireReductionRatio = 0.8f; // 연타의 80% 제거
                settings.densityReductionRatio = 0.6f;   // 밀도 높은 구간 60% 제거
                settings.removeHoldNotes = true;         // 홀드 노트 제거
                settings.removeSpaceNotes = true;        // SPACE 노트 제거
                settings.removeRapidNotes = true;        // 연타 노트 제거
                settings.keepEveryNthNote = 2;           // 2번째 노트마다 유지 (전체의 50%)
                break;

            case Difficulty.Normal:
                // 보통: 중간 밀도, 일부 복잡한 패턴만 제거, 연타노트 유지
                settings.minNoteInterval = 0.12f;
                settings.rapidFireWindow = 0.12f;
                settings.rapidFireReductionRatio = 0.4f; // 연타의 40% 제거
                settings.densityReductionRatio = 0.3f;   // 밀도 높은 구간 30% 제거
                settings.removeHoldNotes = false;
                settings.removeSpaceNotes = false;
                settings.removeRapidNotes = false;       // 연타 노트 유지
                settings.keepEveryNthNote = 1;           // 모든 노트 유지
                break;

            case Difficulty.Hard:
                // 어려움: 원본 그대로 (필터링 최소), 모든 노트 유지
                settings.minNoteInterval = 0.05f;
                settings.rapidFireWindow = 0.08f;
                settings.rapidFireReductionRatio = 0.0f;
                settings.densityReductionRatio = 0.0f;
                settings.removeHoldNotes = false;
                settings.removeSpaceNotes = false;
                settings.removeRapidNotes = false;       // 연타 노트 유지
                settings.keepEveryNthNote = 1;
                break;
        }

        return settings;
    }

    /// <summary>
    /// 필터링 로직 실행
    /// </summary>
    private static List<NoteData> FilterNotes(List<NoteData> sourceNotes, FilterSettings settings)
    {
        // 1단계: 시간순 정렬 (이미 정렬되어 있어야 하지만 안전을 위해)
        List<NoteData> sorted = sourceNotes.OrderBy(n => n.time).ToList();

        // 2단계: 타입 기반 필터링 (홀드, SPACE 제거)
        List<NoteData> typeFiltered = FilterByType(sorted, settings);

        // 3단계: 연타 노트 구간 충돌 방지 (다른 노트 제거)
        List<NoteData> rapidConflictFiltered = FilterRapidNoteConflicts(typeFiltered);

        // 4단계: Lane 필터링
        List<NoteData> laneFiltered = FilterByLane(rapidConflictFiltered, settings);

        // 5단계: 밀도 기반 필터링 (너무 가까운 노트 제거)
        List<NoteData> densityFiltered = FilterByDensity(laneFiltered, settings);

        // 6단계: 연타 패턴 필터링
        List<NoteData> rapidFireFiltered = FilterRapidFire(densityFiltered, settings);

        // 7단계: N번째마다 유지 (간격 조절)
        List<NoteData> finalFiltered = KeepEveryNth(rapidFireFiltered, settings);

        return finalFiltered;
    }

    /// <summary>
    /// 연타 노트 구간 충돌 방지 (연타 노트 진행 중 다른 노트 제거)
    /// </summary>
    private static List<NoteData> FilterRapidNoteConflicts(List<NoteData> notes)
    {
        List<NoteData> result = new List<NoteData>();

        // 모든 연타 노트의 보호 구간 수집
        List<(float startTime, float endTime, string arrow)> rapidZones = new List<(float, float, string)>();

        foreach (var note in notes)
        {
            if (note.type == "rapid" && note.rapidDuration > 0)
            {
                float startTime = note.time;
                float endTime = note.time + note.rapidDuration;
                rapidZones.Add((startTime, endTime, note.arrow));

                Debug.Log($"[DifficultyFilter] Rapid zone protection: {startTime:F2} ~ {endTime:F2} ({note.arrow})");
            }
        }

        // 각 노트 체크
        foreach (var note in notes)
        {
            bool inConflict = false;

            // 연타 노트 자신은 제외
            if (note.type == "rapid")
            {
                result.Add(note);
                continue;
            }

            // 다른 노트가 연타 구간과 겹치는지 체크
            foreach (var zone in rapidZones)
            {
                // 같은 arrow 키의 연타 구간과 겹치는지 확인
                if (note.arrow == zone.arrow && note.time >= zone.startTime && note.time < zone.endTime)
                {
                    inConflict = true;
                    Debug.Log($"[DifficultyFilter] Removed note at {note.time:F2} ({note.arrow}) - conflicts with rapid zone");
                    break;
                }
            }

            if (!inConflict)
            {
                result.Add(note);
            }
        }

        return result;
    }

    /// <summary>
    /// 타입 기반 필터링 (홀드, SPACE, 연타 노트 제거)
    /// </summary>
    private static List<NoteData> FilterByType(List<NoteData> notes, FilterSettings settings)
    {
        List<NoteData> result = new List<NoteData>();

        foreach (var note in notes)
        {
            // 홀드 노트 제거
            if (settings.removeHoldNotes && note.type == "hold")
                continue;

            // SPACE 노트 제거
            if (settings.removeSpaceNotes && note.arrow == "SPACE")
                continue;

            // 연타 노트 제거
            if (settings.removeRapidNotes && note.type == "rapid")
                continue;

            result.Add(note);
        }

        return result;
    }

    /// <summary>
    /// Lane 기반 필터링
    /// </summary>
    private static List<NoteData> FilterByLane(List<NoteData> notes, FilterSettings settings)
    {
        if (settings.allowedLanes == null || settings.allowedLanes.Count == 0)
            return notes;

        return notes.Where(n => settings.allowedLanes.Contains(n.lane)).ToList();
    }

    /// <summary>
    /// 밀도 기반 필터링 (너무 가까운 노트들 제거)
    /// </summary>
    private static List<NoteData> FilterByDensity(List<NoteData> notes, FilterSettings settings)
    {
        if (settings.densityReductionRatio <= 0f)
            return notes;

        List<NoteData> result = new List<NoteData>();
        List<NoteData> denseNotes = new List<NoteData>();

        for (int i = 0; i < notes.Count; i++)
        {
            bool isDense = false;

            // 이전 노트와의 간격 체크
            if (i > 0)
            {
                float timeDiff = notes[i].time - notes[i - 1].time;
                if (timeDiff < settings.minNoteInterval)
                    isDense = true;
            }

            // 다음 노트와의 간격 체크
            if (i < notes.Count - 1)
            {
                float timeDiff = notes[i + 1].time - notes[i].time;
                if (timeDiff < settings.minNoteInterval)
                    isDense = true;
            }

            if (isDense)
                denseNotes.Add(notes[i]);
            else
                result.Add(notes[i]);
        }

        // 밀도 높은 노트 중 일부만 다시 추가
        int keepCount = Mathf.RoundToInt(denseNotes.Count * (1f - settings.densityReductionRatio));
        for (int i = 0; i < keepCount && i < denseNotes.Count; i++)
        {
            result.Add(denseNotes[i]);
        }

        return result.OrderBy(n => n.time).ToList();
    }

    /// <summary>
    /// 연타 패턴 필터링
    /// </summary>
    private static List<NoteData> FilterRapidFire(List<NoteData> notes, FilterSettings settings)
    {
        if (settings.rapidFireReductionRatio <= 0f)
            return notes;

        List<NoteData> result = new List<NoteData>();
        int i = 0;

        while (i < notes.Count)
        {
            // 현재 노트부터 시작하는 연타 그룹 찾기
            List<NoteData> rapidGroup = new List<NoteData> { notes[i] };
            int j = i + 1;

            while (j < notes.Count && (notes[j].time - notes[j - 1].time) < settings.rapidFireWindow)
            {
                rapidGroup.Add(notes[j]);
                j++;
            }

            // 연타 그룹이 3개 이상이면 필터링 적용
            if (rapidGroup.Count >= 3)
            {
                // 유지할 노트 개수 계산
                int keepCount = Mathf.Max(1, Mathf.RoundToInt(rapidGroup.Count * (1f - settings.rapidFireReductionRatio)));

                // 균등하게 분포된 노트만 유지
                for (int k = 0; k < keepCount; k++)
                {
                    int index = Mathf.RoundToInt(k * (rapidGroup.Count - 1) / (float)(keepCount - 1));
                    result.Add(rapidGroup[index]);
                }
            }
            else
            {
                // 연타가 아니면 모두 유지
                result.AddRange(rapidGroup);
            }

            i = j;
        }

        return result;
    }

    /// <summary>
    /// N번째 노트마다 유지 (간격 조절)
    /// </summary>
    private static List<NoteData> KeepEveryNth(List<NoteData> notes, FilterSettings settings)
    {
        if (settings.keepEveryNthNote <= 1)
            return notes;

        List<NoteData> result = new List<NoteData>();

        for (int i = 0; i < notes.Count; i++)
        {
            if (i % settings.keepEveryNthNote == 0)
            {
                result.Add(notes[i]);
            }
        }

        return result;
    }

    /// <summary>
    /// 커스텀 필터 설정으로 필터링 (고급 사용자용)
    /// </summary>
    public static List<NoteData> ApplyCustomFilter(List<NoteData> sourceNotes, FilterSettings customSettings)
    {
        return FilterNotes(sourceNotes, customSettings);
    }
}
