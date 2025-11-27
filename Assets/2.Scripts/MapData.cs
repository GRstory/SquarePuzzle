using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class MapData
{
    public int MapSeed;
    public Vector2Int MapSize = new Vector2Int(15, 15); // Standard 15x15 world size
    public List<int> OptimalPath = new List<int>();
    public List<MapObject> MapObjects;

    public int GetMinMoves() => OptimalPath.Count;
}

[System.Serializable]
public class MapObject
{
    public EMapObjectType Type;
    public int X;
    public int Y;
    // Id 필드 제거 - 런타임에 자동 생성
}

[System.Serializable]
public class SolverResult
{
    public bool isSolvable;
    public int minMoves;
    public List<int> optimalPath;
}

public enum EMapObjectType
{
    Player = 0,
    Wall = 1,
    Goal = 2,
    BreakableWall = 3,
    // Directional Slide Walls (4-9 reserved for future use)
    SlideWallUp = 10,      // Slides player upward
    SlideWallRight = 11,   // Slides player to the right
    SlideWallDown = 12,    // Slides player downward
    SlideWallLeft = 13     // Slides player to the left
}