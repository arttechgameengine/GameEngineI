# Claude Guide - 토너먼트 맵 시스템 가이드 모음

이 폴더에는 Claude가 작성한 모든 설명 및 가이드 문서가 포함되어 있습니다.

---

## 📚 가이드 목록

### 🎯 핵심 개념
1. **[MANAGER_DIFFERENCES.md](MANAGER_DIFFERENCES.md)**
   - GameModeManager vs TournamentMapManager 차이점
   - 싱글톤 vs 씬 전용 매니저 설명
   - 데이터 흐름 및 관계도

### 🏗️ Unity 씬 설정
2. **[UNITY_SCENE_LAYOUT_GUIDE.md](UNITY_SCENE_LAYOUT_GUIDE.md)**
   - GameStartScene 및 TournamentMapScene 전체 구조
   - 오브젝트 배치 및 좌표
   - Step-by-Step 설정 가이드

3. **[TOURNAMENT_MAP_SETUP_GUIDE.md](TOURNAMENT_MAP_SETUP_GUIDE.md)**
   - 토너먼트 맵 상세 설정 가이드
   - RoundData ScriptableObject 생성
   - 테스트 체크리스트

### 🎨 브래킷 레이아웃
4. **[CORRECT_BRACKET_LAYOUT.md](CORRECT_BRACKET_LAYOUT.md)** ⭐
   - 올바른 토너먼트 브래킷 구조 (계단식)
   - Round 1 → 2 → 3 → 4 → 5 진행 방식
   - 실제 좌표 및 더미 실루엣 배치

5. **[BRACKET_BOUNDARY_GUIDE.md](BRACKET_BOUNDARY_GUIDE.md)**
   - 토너먼트 브래킷 형태 카메라 경계 설정
   - PolygonCollider2D 좌표 예시
   - Progressive Boundaries 설정

6. **[SIMPLIFIED_LAYOUT_GUIDE.md](SIMPLIFIED_LAYOUT_GUIDE.md)**
   - RoundPosition과 RoundButton 통합 방법
   - World Space Canvas 사용법
   - 간소화된 구조

### 🎬 실루엣 시스템
7. **[FULL_BRACKET_SILHOUETTE_GUIDE.md](FULL_BRACKET_SILHOUETTE_GUIDE.md)** ⭐
   - 전체 토너먼트 브래킷 실루엣 시스템
   - 실제 5라운드 + 더미 실루엣 배치
   - 웅장한 연출 가이드

8. **[TOURNAMENT_SILHOUETTE_GUIDE.md](TOURNAMENT_SILHOUETTE_GUIDE.md)**
   - 줌아웃/줌인 시 실루엣 전환
   - 시각적 연출 아이디어
   - 애니메이션 효과

### 🎮 Free Mode 인터랙션
9. **[FREE_MODE_INTERACTION_GUIDE.md](FREE_MODE_INTERACTION_GUIDE.md)** ⭐
   - Free Mode 사용자 인터랙션 방식
   - 투명 버튼 클릭 방식
   - 빠른 선택 UI 구현

---

## 🚀 빠른 시작 가이드

### 1단계: 개념 이해
- [MANAGER_DIFFERENCES.md](MANAGER_DIFFERENCES.md) 읽기

### 2단계: 씬 설정
- [UNITY_SCENE_LAYOUT_GUIDE.md](UNITY_SCENE_LAYOUT_GUIDE.md) 따라하기
- [CORRECT_BRACKET_LAYOUT.md](CORRECT_BRACKET_LAYOUT.md) 좌표 참고

### 3단계: 실루엣 시스템
- [FULL_BRACKET_SILHOUETTE_GUIDE.md](FULL_BRACKET_SILHOUETTE_GUIDE.md) 구현

### 4단계: Free Mode
- [FREE_MODE_INTERACTION_GUIDE.md](FREE_MODE_INTERACTION_GUIDE.md) 구현

---

## 📋 주요 파일 경로

### Scripts
- `Assets/Scripts/GameStartManager.cs`
- `Assets/Scripts/GameModeManager.cs`
- `Assets/Scripts/TournamentMapManager.cs`
- `Assets/Scripts/RoundButton.cs`
- `Assets/Scripts/RoundData.cs`

### Scenes
- `Assets/Scenes/GameStartScene.unity`
- `Assets/Scenes/TournamentMapScene.unity`

---

## 🎯 추천 읽기 순서

**처음 시작하는 경우:**
1. MANAGER_DIFFERENCES.md (개념)
2. CORRECT_BRACKET_LAYOUT.md (구조)
3. UNITY_SCENE_LAYOUT_GUIDE.md (설정)
4. FULL_BRACKET_SILHOUETTE_GUIDE.md (연출)
5. FREE_MODE_INTERACTION_GUIDE.md (인터랙션)

**특정 기능만 찾는 경우:**
- 카메라 경계: BRACKET_BOUNDARY_GUIDE.md
- 실루엣 연출: TOURNAMENT_SILHOUETTE_GUIDE.md
- 레이아웃 간소화: SIMPLIFIED_LAYOUT_GUIDE.md

---

## 💡 업데이트 이력

- 2024.11.28: 전체 가이드 작성
- ClaudeGuide 폴더로 정리

---

궁금한 점이 있으면 각 가이드의 내용을 참고하세요! 🎮
