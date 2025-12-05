using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class MapData
{
    public Vector2Int MapSize = new Vector2Int(15, 15);
    public List<int> OptimalPath = new List<int>();
    public List<MapObject> MapObjects;
    public int Seed;

    public int GetMinMoves() => OptimalPath.Count;
}

[System.Serializable]
public class MapObject
{
    public EMapObjectType Type;
    public int X;
    public int Y;
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

    SlideWallUp = 10,
    SlideWallRight = 11,
    SlideWallDown = 12,
    SlideWallLeft = 13,
}