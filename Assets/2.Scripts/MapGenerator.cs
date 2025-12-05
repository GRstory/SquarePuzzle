using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Generates random solvable puzzle maps with specified move count
/// </summary>
public static class MapGenerator
{
    /// <summary>
    /// Generate a map with exact target move count
    /// </summary>
    /// <param name="targetMoves">Desired minimum move count (1-20 recommended)</param>
    /// <param name="seed">Optional random seed for reproducibility</param>
    /// <returns>Generated MapData or null if failed</returns>
    public static MapData GenerateMap(int targetMoves, int? seed = null)
    {
        return GenerateMapWithRetry(targetMoves, 10000, seed);
    }

    /// <summary>
    /// Generate a map with retry logic
    /// </summary>
    /// <param name="targetMoves">Desired minimum move count</param>
    /// <param name="maxRetries">Maximum generation attempts</param>
    /// <param name="seed">Optional random seed</param>
    /// <returns>Generated MapData or null if failed after max retries</returns>
    public static MapData GenerateMapWithRetry(int targetMoves, int maxRetries = 10000, int? seed = null)
    {
        int usedSeed;
        if (seed.HasValue)
        {
            usedSeed = seed.Value;
            Random.InitState(usedSeed);
        }
        else
        {
            usedSeed = (int)System.DateTime.Now.Ticks;
            Random.InitState(usedSeed);
        }

        int attempts = 0;
        while (attempts < maxRetries)
        {
            attempts++;
            
            MapData map = GenerateMapAttempt(targetMoves);
            
            if (map == null)
            {
                Debug.Log($"[MapGenerator] Attempt {attempts}: Failed to generate valid map structure");
                continue;
            }

            // Validate with solver
            PuzzleSolver solver = new PuzzleSolver(map);
            SolverResult result = solver.Solve();

            if (!result.isSolvable)
            {
                Debug.Log($"[MapGenerator] Attempt {attempts}: Map is unsolvable");
                continue;
            }

            if (result.minMoves != targetMoves)
            {
                Debug.Log($"[MapGenerator] Attempt {attempts}: Move count mismatch (target: {targetMoves}, actual: {result.minMoves})");
                continue;
            }

            // Success!
            map.OptimalPath = result.optimalPath;
            map.Seed = usedSeed;
            Debug.Log($"[MapGenerator] SUCCESS on attempt {attempts}: Generated map with {targetMoves} moves (seed: {usedSeed})");
            return map;
        }

        Debug.LogError($"[MapGenerator] FAILED after {maxRetries} attempts to generate map with {targetMoves} moves");
        return null;
    }

    /// <summary>
    /// Single attempt to generate a map
    /// </summary>
    private static MapData GenerateMapAttempt(int targetMoves)
    {
        Vector2Int mapSize = new Vector2Int(15, 15);
        MapData newMap = new MapData 
        { 
            MapSize = mapSize, 
            MapObjects = new List<MapObject>() 
        };
        
        HashSet<Vector2Int> occupied = new HashSet<Vector2Int>();

        // 1. Place Player
        Vector2Int playerPos = GetRandomPosition(mapSize, occupied);
        newMap.MapObjects.Add(new MapObject 
        { 
            Type = EMapObjectType.Player, 
            X = playerPos.x, 
            Y = playerPos.y 
        });
        occupied.Add(playerPos);

        // 2. Place Goal with appropriate distance
        Vector2Int goalPos = PlaceGoalForTargetMoves(mapSize, playerPos, targetMoves, occupied);
        if (goalPos == playerPos) // Failed to place goal
        {
            return null;
        }
        newMap.MapObjects.Add(new MapObject 
        { 
            Type = EMapObjectType.Goal, 
            X = goalPos.x, 
            Y = goalPos.y 
        });
        occupied.Add(goalPos);

        // 3. Calculate obstacle counts based on target moves
        int wallCount = CalculateWallCount(targetMoves);
        int breakableCount = CalculateBreakableWallCount(targetMoves);
        int slideCount = CalculateSlideWallCount(targetMoves);

        // 4. Place standard walls
        PlaceWalls(newMap, mapSize, occupied, wallCount);

        // 5. Place breakable walls
        PlaceBreakableWalls(newMap, mapSize, occupied, breakableCount);

        // 6. Place slide walls
        PlaceSlideWalls(newMap, mapSize, occupied, slideCount);

        return newMap;
    }

    /// <summary>
    /// Get a random unoccupied position
    /// </summary>
    private static Vector2Int GetRandomPosition(Vector2Int mapSize, HashSet<Vector2Int> occupied)
    {
        Vector2Int pos;
        int maxAttempts = 1000;
        int attempts = 0;
        
        do
        {
            pos = new Vector2Int(Random.Range(0, mapSize.x), Random.Range(0, mapSize.y));
            attempts++;
            if (attempts > maxAttempts)
            {
                // Fallback: find first unoccupied position
                for (int y = 0; y < mapSize.y; y++)
                {
                    for (int x = 0; x < mapSize.x; x++)
                    {
                        Vector2Int testPos = new Vector2Int(x, y);
                        if (!occupied.Contains(testPos))
                            return testPos;
                    }
                }
                return pos; // Should never reach here
            }
        } while (occupied.Contains(pos));
        
        return pos;
    }

    /// <summary>
    /// Place goal with appropriate distance for target moves
    /// </summary>
    private static Vector2Int PlaceGoalForTargetMoves(Vector2Int mapSize, Vector2Int playerPos, int targetMoves, HashSet<Vector2Int> occupied)
    {
        // Minimum Manhattan distance should be related to target moves
        int minDistance = Mathf.Max(2, targetMoves / 2);
        int maxDistance = Mathf.Min(mapSize.x + mapSize.y - 2, targetMoves * 2);

        List<Vector2Int> candidates = new List<Vector2Int>();
        
        for (int y = 0; y < mapSize.y; y++)
        {
            for (int x = 0; x < mapSize.x; x++)
            {
                Vector2Int pos = new Vector2Int(x, y);
                if (occupied.Contains(pos)) continue;
                
                int distance = Mathf.Abs(pos.x - playerPos.x) + Mathf.Abs(pos.y - playerPos.y);
                if (distance >= minDistance && distance <= maxDistance)
                {
                    candidates.Add(pos);
                }
            }
        }

        if (candidates.Count == 0)
        {
            // Fallback: any unoccupied position
            return GetRandomPosition(mapSize, occupied);
        }

        return candidates[Random.Range(0, candidates.Count)];
    }

    /// <summary>
    /// Calculate wall count based on target moves
    /// </summary>
    private static int CalculateWallCount(int targetMoves)
    {
        // More moves = more walls
        // Formula: targetMoves * (0.5 to 1.5)
        float multiplier = Random.Range(0.5f, 1.5f);
        return Mathf.RoundToInt(targetMoves * multiplier);
    }

    /// <summary>
    /// Calculate breakable wall count based on target moves
    /// </summary>
    private static int CalculateBreakableWallCount(int targetMoves)
    {
        if (targetMoves < 3) return 0;
        if (targetMoves < 6) return Random.Range(0, 2);
        if (targetMoves < 10) return Random.Range(0, 3);
        return Random.Range(1, 4);
    }

    /// <summary>
    /// Calculate slide wall count based on target moves
    /// </summary>
    private static int CalculateSlideWallCount(int targetMoves)
    {
        if (targetMoves < 4) return 0;
        if (targetMoves < 8) return Random.Range(0, 2);
        if (targetMoves < 12) return Random.Range(0, 3);
        return Random.Range(1, 4);
    }

    /// <summary>
    /// Place standard walls
    /// </summary>
    private static void PlaceWalls(MapData map, Vector2Int mapSize, HashSet<Vector2Int> occupied, int count)
    {
        for (int i = 0; i < count; i++)
        {
            if (occupied.Count >= mapSize.x * mapSize.y - 1)
                break;

            Vector2Int pos = GetRandomPosition(mapSize, occupied);
            map.MapObjects.Add(new MapObject 
            { 
                Type = EMapObjectType.Wall, 
                X = pos.x, 
                Y = pos.y 
            });
            occupied.Add(pos);
        }
    }

    /// <summary>
    /// Place breakable walls
    /// </summary>
    private static void PlaceBreakableWalls(MapData map, Vector2Int mapSize, HashSet<Vector2Int> occupied, int count)
    {
        for (int i = 0; i < count; i++)
        {
            if (occupied.Count >= mapSize.x * mapSize.y - 1)
                break;

            Vector2Int pos = GetRandomPosition(mapSize, occupied);
            map.MapObjects.Add(new MapObject 
            { 
                Type = EMapObjectType.BreakableWall, 
                X = pos.x, 
                Y = pos.y 
            });
            occupied.Add(pos);
        }
    }

    /// <summary>
    /// Place slide walls with random directions
    /// </summary>
    private static void PlaceSlideWalls(MapData map, Vector2Int mapSize, HashSet<Vector2Int> occupied, int count)
    {
        EMapObjectType[] slideTypes = 
        {
            EMapObjectType.SlideWallUp,
            EMapObjectType.SlideWallRight,
            EMapObjectType.SlideWallDown,
            EMapObjectType.SlideWallLeft
        };

        for (int i = 0; i < count; i++)
        {
            if (occupied.Count >= mapSize.x * mapSize.y - 1)
                break;

            Vector2Int pos = GetRandomPosition(mapSize, occupied);
            EMapObjectType slideType = slideTypes[Random.Range(0, slideTypes.Length)];
            
            map.MapObjects.Add(new MapObject 
            { 
                Type = slideType, 
                X = pos.x, 
                Y = pos.y 
            });
            occupied.Add(pos);
        }
    }
}
