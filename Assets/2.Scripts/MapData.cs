using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class MapData
{
    public int MapSeed;
    public Vector2Int MapSize = new Vector2Int(36, 18);
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
    Player,
    Wall,
    Goal
}