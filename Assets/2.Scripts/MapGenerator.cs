using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public static class MapGenerator
{
    public static MapData Generate(Vector2Int playerStartPos, Vector2Int size, int wallCount)
    {
        MapData newMap = new MapData { MapSize = size, MapObjects = new List<MapObject>() };
        HashSet<Vector2Int> occupied = new HashSet<Vector2Int>();

        // 1. 플레이어 배치
        newMap.MapObjects.Add(new MapObject { Type = EMapObjectType.Player, X = playerStartPos.x, Y = playerStartPos.y });
        occupied.Add(playerStartPos);

        // 2. 도착 지점 랜덤 배치
        Vector2Int goalPos;
        do { goalPos = new Vector2Int(Random.Range(0, size.x), Random.Range(0, size.y)); }
        while (occupied.Contains(goalPos));

        // [BUG FIX] EMapObjectType.Player -> EMapObjectType.Goal 로 수정
        newMap.MapObjects.Add(new MapObject { Type = EMapObjectType.Goal, X = goalPos.x, Y = goalPos.y });
        occupied.Add(goalPos);

        // 3. 해법 경로 '뼈대' 생성
        HashSet<Vector2Int> scaffold = CreateSolutionScaffold(playerStartPos, goalPos, size);
        foreach (var pos in scaffold) occupied.Add(pos);

        // 4. 벽 배치
        List<Vector2Int> availableSpaces = new List<Vector2Int>();
        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                Vector2Int pos = new Vector2Int(x, y);
                if (!occupied.Contains(pos)) availableSpaces.Add(pos);
            }
        }

        availableSpaces = availableSpaces.OrderBy(a => Random.value).ToList();
        int wallsToPlace = Mathf.Min(wallCount, availableSpaces.Count);

        for (int i = 0; i < wallsToPlace; i++)
        {
            newMap.MapObjects.Add(new MapObject { Type = EMapObjectType.Wall, X = availableSpaces[i].x, Y = availableSpaces[i].y });
        }

        // 5. 최종 맵으로 해법 계산
        /*SolverResult result = new PuzzleSolver(newMap).Solve();
        if (result.isSolvable)
        {
            newMap.OptimalPath = result.optimalPath;
            return newMap;
        }*/
        return null;
    }

    private static HashSet<Vector2Int> CreateSolutionScaffold(Vector2Int start, Vector2Int end, Vector2Int size)
    {
        HashSet<Vector2Int> path = new HashSet<Vector2Int>();
        Vector2Int current = start;
        while (current != end)
        {
            path.Add(current);
            Vector2Int dir = end - current;
            if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y)) current.x += (int)Mathf.Sign(dir.x);
            else current.y += (int)Mathf.Sign(dir.y);
        }
        path.Add(end);
        return path;
    }
}
