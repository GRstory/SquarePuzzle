# SquarePuzzle

2D 그리드 기반 슬라이딩 퍼즐 게임입니다. 플레이어는 방향키를 입력하면 벽이나 맵 경계에 부딪힐 때까지 한 방향으로 끝까지 미끄러지며, **최소 이동 횟수 안에 목표 지점(Goal)에 도달**하는 것이 목표입니다.

모든 레벨은 절차적 생성기(MapGenerator)와 BFS 솔버(PuzzleSolver)를 통해 자동으로 생성·검증되며, **"정확히 N번의 이동으로 풀 수 있는 퍼즐"만 채택**하는 솔버 검증형 레벨 파이프라인이 이 프로젝트의 핵심입니다.

## 개발 환경

| 항목 | 내용 |
|---|---|
| 엔진 | Unity **2022.3.62f2** (LTS) |
| 언어 | C# |
| 주요 패키지 | 2D Feature Set, TextMeshPro, uGUI, Test Framework |
| 렌더링 | uGUI(Canvas/RectTransform) 기반, 월드 좌표 렌더링 병행 지원 |
| 플랫폼 | PC (키보드 입력 기준) |

### 프로젝트 구조

```
Assets/
├── 0.Settings/          # 프로젝트 설정 에셋
├── 1.Scenes/            # GameScene.unity (단일 씬)
├── 2.Scripts/
│   ├── MapData.cs           # 레벨 데이터 모델 (맵 크기, 오브젝트, 최적 경로, 시드)
│   ├── MapGenerator.cs      # 절차적 맵 생성기
│   ├── PuzzleSolver.cs      # BFS 기반 최적해 솔버
│   ├── MapManager.cs        # 그리드 오브젝트 관리 (싱글톤)
│   ├── PlayerController.cs  # 플레이어 이동/충돌 로직
│   ├── MapDataVisualizer.cs # 맵 시각화 및 플레이 모드 관리
│   ├── StageDrawer.cs       # 스테이지 렌더링
│   ├── UI/                  # HUD, 월드 목록, 테마, 맵 생성기 UI
│   └── Wall/                # 벽 기믹 (WallBase 상속 구조)
├── 3.Prefabs/           # 프리팹
├── 4.Textures/          # 텍스처
├── StreamingAssets/Maps # 생성된 레벨 JSON (generated_map_N moves_*.json)
└── data_0~9.json        # 레벨 데이터
```

## 실행 방법

1. Unity Hub에서 **Unity 2022.3.62f2** (또는 호환되는 2022.3 LTS) 버전을 설치합니다.
2. 이 저장소를 클론한 뒤, Unity Hub에서 프로젝트 루트 폴더를 열어줍니다.
   ```
   git clone https://github.com/GRstory/SquarePuzzle.git
   ```
3. `Assets/1.Scenes/GameScene.unity` 씬을 엽니다.
4. 에디터에서 Play 버튼을 눌러 실행합니다.

### 조작법

| 키 | 동작 |
|---|---|
| `W` / `A` / `S` / `D` | 상 / 좌 / 하 / 우 방향으로 미끄러지기 |

- 한 번 입력하면 벽·기믹·맵 경계에 막힐 때까지 직선으로 이동합니다.
- 화면 상단의 `Tries: 현재/최대` 표시 안에 Goal에 도달하면 클리어입니다.
- 맵 경계 밖으로 미끄러지거나 슬라이드 벽에서 빠져나갈 수 없으면 스테이지가 리셋됩니다.

### 기믹 (벽 종류)

| 기믹 | 설명 |
|---|---|
| StandardWall | 일반 벽. 부딪히면 그 앞 칸에서 멈춤 |
| GoalWall | 도착 지점. 닿으면 클리어 |
| BreakableWall | 부술 수 있는 벽. 첫 충돌 시 마킹되고, 다음 이동에 성공하면 파괴됨 |
| SlideWallUp/Right/Down/Left | 부딪히면 진행 방향을 해당 방향으로 강제 전환. 출구가 막혀 있으면 리셋 |

## 방법론: 솔버 검증형 레벨 생성 파이프라인

이 프로젝트는 레벨을 손으로 제작하지 않고, **생성 → 검증 → 채택** 파이프라인으로 난이도가 보장된 퍼즐을 자동 생산합니다.

### 1. 절차적 생성 (MapGenerator)

- 15×15 그리드에 플레이어, Goal, 벽·기믹을 시드 기반으로 랜덤 배치합니다.
- 시드(`Seed`)를 저장하므로 동일한 맵을 재현할 수 있습니다.

### 2. BFS 최적해 검증 (PuzzleSolver)

- `PlayerController`의 실제 이동 로직(미끄러짐, 벽 파괴 2단계 메커니즘, 슬라이드 벽 방향 전환, 경계 이탈)을 그대로 재현한 BFS 탐색을 수행합니다.
- 탐색 상태는 `(위치, 파괴된 벽 집합, 마킹된 벽)`으로 구성되어 기믹 상호작용까지 정확히 시뮬레이션합니다.
- 결과로 **풀이 가능 여부(isSolvable)**, **최소 이동 횟수(minMoves)**, **최적 경로(optimalPath)** 를 산출합니다.

### 3. 목표 난이도 필터링

- 생성된 맵이 풀 수 없거나, 최소 이동 횟수가 목표치(`targetMoves`)와 다르면 폐기하고 재생성합니다 (최대 10,000회 재시도).
- 조건을 만족한 맵만 최적 경로·시드와 함께 JSON으로 직렬화되어 `Assets/StreamingAssets/Maps/`에 저장됩니다.

```json
{
  "MapSize": { "x": 15, "y": 15 },
  "OptimalPath": [1, 0, 3, ...],
  "MapObjects": [ { "Type": 0, "X": 5, "Y": 7 }, ... ],
  "Seed": 123456789
}
```

이 구조 덕분에 **게임 로직(PlayerController)과 솔버(PuzzleSolver)가 항상 동일하게 동작해야 한다**는 제약이 생기며, 이동 규칙을 수정할 때는 두 곳을 함께 갱신해야 합니다.

### 4. 레벨 선택 및 도구

- **월드 목록 UI**: 생성된 레벨을 미리보기 그리드로 표시하고, 이동 횟수별 필터와 정렬(순서/기물 개수)을 제공합니다.
- **맵 생성기 UI / 시각화 도구**: 에디터 상에서 목표 이동 횟수를 지정해 맵을 생성하고, 결과를 즉시 시각화·플레이 테스트할 수 있습니다.
- **테마 시스템**: 색상 테마 전환을 지원합니다.
