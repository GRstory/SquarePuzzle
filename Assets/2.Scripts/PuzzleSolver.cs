using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// COMPLETELY REWRITTEN BFS-based solver that EXACTLY matches PlayerController logic
/// </summary>
public class PuzzleSolver
{
    private MapData _mapData;
    private Dictionary<Vector2Int, MapObject> _objectMap;
    private readonly Vector2Int[] _directions = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };

    public PuzzleSolver(MapData mapData)
    {
        _mapData = mapData;
        _objectMap = new Dictionary<Vector2Int, MapObject>();
        
        // Only add walls and goal to objectMap - NOT the player!
        foreach (var obj in mapData.MapObjects)
        {
            if (obj.Type != EMapObjectType.Player)
            {
                _objectMap[new Vector2Int(obj.X, obj.Y)] = obj;
            }
        }
    }

    public SolverResult Solve()
    {
        // Find player and goal positions
        Vector2Int playerPos = Vector2Int.zero;
        Vector2Int goalPos = Vector2Int.zero;
        
        foreach (var obj in _mapData.MapObjects)
        {
            if (obj.Type == EMapObjectType.Player)
                playerPos = new Vector2Int(obj.X, obj.Y);
            else if (obj.Type == EMapObjectType.Goal)
                goalPos = new Vector2Int(obj.X, obj.Y);
        }

        // BFS
        var queue = new Queue<SearchState>();
        var visited = new HashSet<string>();
        var parentMap = new Dictionary<string, (SearchState parent, int direction)>();

        // Check for adjacent BreakableWalls at start position (matches PlayerController.CheckAdjacentWalls)
        Vector2Int? initialWallInFront = null;
        for (int dir = 0; dir < 4; dir++)
        {
            Vector2Int checkPos = playerPos + _directions[dir];
            if (_objectMap.ContainsKey(checkPos) && _objectMap[checkPos].Type == EMapObjectType.BreakableWall)
            {
                initialWallInFront = checkPos;
                Debug.Log($"[Solver] Initial BreakableWall found at {checkPos}, marked for breaking");
                break; // Only mark one wall
            }
        }

        var startState = new SearchState(playerPos, new HashSet<Vector2Int>(), initialWallInFront);
        queue.Enqueue(startState);
        visited.Add(startState.GetKey());

        while (queue.Count > 0)
        {
            var currentState = queue.Dequeue();

            // Check if current position is out of bounds (should never reach goal if out of bounds)
            if (currentState.position.x < 0 || currentState.position.x >= _mapData.MapSize.x ||
                currentState.position.y < 0 || currentState.position.y >= _mapData.MapSize.y)
            {
                continue; // Skip this state - it's invalid
            }

            // Check if we reached the goal
            if (currentState.position == goalPos)
            {
                var path = ReconstructPath(parentMap, currentState);
                return new SolverResult
                {
                    isSolvable = true,
                    minMoves = path.Count,
                    optimalPath = path
                };
            }

            // Try all 4 directions
            for (int dir = 0; dir < 4; dir++)
            {
                var nextState = SimulateMove(currentState, dir);
                
                // Skip if SimulateMove returned null (invalid SlideWall exit)
                if (nextState == null)
                {
                    continue;
                }
                
                // Check if out of bounds (failure)
                if (nextState.position.x < 0 || nextState.position.x >= _mapData.MapSize.x ||
                    nextState.position.y < 0 || nextState.position.y >= _mapData.MapSize.y)
                {
                    continue; // Out of bounds = invalid move
                }
                
                // Skip if we didn't move
                if (nextState.position == currentState.position && 
                    nextState.brokenWalls.SetEquals(currentState.brokenWalls))
                    continue;

                string nextKey = nextState.GetKey();
                if (!visited.Contains(nextKey))
                {
                    visited.Add(nextKey);
                    queue.Enqueue(nextState);
                    parentMap[nextKey] = (currentState, dir);
                }
            }
        }

        return new SolverResult
        {
            isSolvable = false,
            minMoves = 0,
            optimalPath = new List<int>()
        };
    }

    /// <summary>
    /// Simulate move - EXACTLY matches PlayerController.Move logic
    /// </summary>
    private SearchState SimulateMove(SearchState state, int direction)
    {
        Vector2Int currentPos = state.position;
        Vector2Int moveDir = _directions[direction];
        Vector2Int targetPos = currentPos;
        bool reachedGoal = false;
        
        var newBrokenWalls = new HashSet<Vector2Int>(state.brokenWalls);
        Vector2Int? newWallInFront = null;
        
        // Break wall from previous move if marked
        if (state.wallInFront.HasValue)
        {
            newBrokenWalls.Add(state.wallInFront.Value);
        }

        // Slide in current direction until hitting something
        while (true)
        {
            Vector2Int nextPos = targetPos + moveDir;

            // 1. Check for objects FIRST (matches PlayerController line 76)
            if (_objectMap.ContainsKey(nextPos))
            {
                MapObject obj = _objectMap[nextPos];

                // Skip if this wall is already broken
                if (newBrokenWalls.Contains(nextPos))
                {
                    targetPos = nextPos;
                    continue;
                }

                if (obj.Type == EMapObjectType.BreakableWall)
                {
                    // Mark for breaking on NEXT successful move
                    newWallInFront = nextPos;
                    break;
                }
                else if (obj.Type == EMapObjectType.SlideWallUp || obj.Type == EMapObjectType.SlideWallRight ||
                         obj.Type == EMapObjectType.SlideWallDown || obj.Type == EMapObjectType.SlideWallLeft)
                {
                    // Move TO the slide wall position
                    targetPos = nextPos;
                    
                    // Get redirect direction
                    int exitDir = GetSlideWallDirection(obj.Type);
                    Vector2Int exitDirVec = _directions[exitDir];
                    
                    // Exit position is ONE STEP from SLIDE WALL position (matches PlayerController line 243)
                    Vector2Int exitPos = targetPos + exitDirVec;
                    
                    // Check if can exit
                    bool canExit = true;
                    
                    // Check bounds
                    if (exitPos.x < 0 || exitPos.x >= _mapData.MapSize.x || 
                        exitPos.y < 0 || exitPos.y >= _mapData.MapSize.y)
                    {
                        canExit = false;
                    }
                    
                    // Check for walls at exit
                    if (canExit && _objectMap.ContainsKey(exitPos))
                    {
                        MapObject exitObj = _objectMap[exitPos];
                        if (!newBrokenWalls.Contains(exitPos) && exitObj.Type != EMapObjectType.Goal)
                        {
                            canExit = false;
                        }
                    }
                    
                    // If cannot exit, this is an invalid state - return null
                    if (!canExit)
                    {
                        return null;
                    }
                    
                    // Can exit - move to exit position
                    targetPos = exitPos;
                    
                    break;
                }
                else if (obj.Type == EMapObjectType.Goal)
                {
                    // Move INTO goal
                    targetPos = nextPos;
                    reachedGoal = true;
                    break;
                }
                else
                {
                    // Standard wall - stop
                    break;
                }
            }

            // 2. Check bounds AFTER walls (matches PlayerController line 146-152)
            if (nextPos.x < 0 || nextPos.x >= _mapData.MapSize.x || 
                nextPos.y < 0 || nextPos.y >= _mapData.MapSize.y)
            {
                // Allow moving to out of bounds (matches PlayerController line 151)
                targetPos = nextPos;
                break;
            }
            
            // 3. No obstacle - continue moving
            targetPos = nextPos;
        }

        return new SearchState(targetPos, newBrokenWalls, newWallInFront);
    }

    private int GetSlideWallDirection(EMapObjectType wallType)
    {
        switch (wallType)
        {
            case EMapObjectType.SlideWallUp: return 0;
            case EMapObjectType.SlideWallRight: return 1;
            case EMapObjectType.SlideWallDown: return 2;
            case EMapObjectType.SlideWallLeft: return 3;
            default: return 0;
        }
    }

    private List<int> ReconstructPath(Dictionary<string, (SearchState parent, int direction)> parentMap, SearchState goalState)
    {
        var path = new List<int>();
        var currentKey = goalState.GetKey();

        while (parentMap.ContainsKey(currentKey))
        {
            var (parent, direction) = parentMap[currentKey];
            path.Insert(0, direction);
            currentKey = parent.GetKey();
        }

        return path;
    }

    private class SearchState
    {
        public Vector2Int position;
        public HashSet<Vector2Int> brokenWalls;
        public Vector2Int? wallInFront;

        public SearchState(Vector2Int pos, HashSet<Vector2Int> broken, Vector2Int? wall)
        {
            position = pos;
            brokenWalls = broken;
            wallInFront = wall;
        }

        public string GetKey()
        {
            var wallsKey = string.Join(",", brokenWalls);
            var wallFrontKey = wallInFront.HasValue ? $"{wallInFront.Value.x},{wallInFront.Value.y}" : "none";
            return $"{position.x},{position.y}|{wallsKey}|{wallFrontKey}";
        }
    }
}
