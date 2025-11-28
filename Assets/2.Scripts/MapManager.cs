using System.Collections.Generic;
using UnityEngine;

// Lightweight grid manager for collision detection
// Used by PlayerController to check object positions
public class MapManager : MonoBehaviour
{
    public static MapManager Instance;

    private MapData _mapData;
    private GameObject[,] _mapGrid;
    private Dictionary<int, GameObject> _wallIdMap = new Dictionary<int, GameObject>();
    public Vector2Int MapSize => _mapData?.MapSize ?? Vector2Int.zero;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }
    
    // Initialize grid from MapData (called by MapDataVisualizer in PlayMode)
    public void ParseJsonData(TextAsset mapJson)
    {
        if (mapJson == null) return;

        _mapData = JsonUtility.FromJson<MapData>(mapJson.text);
        if (_mapData == null) return;

        _mapGrid = new GameObject[MapSize.x, MapSize.y];
        _wallIdMap.Clear();
    }

    // Grid access methods
    public GameObject GetObjectAt(Vector2Int position)
    {
        if (_mapGrid == null || position.x < 0 || position.x >= MapSize.x || position.y < 0 || position.y >= MapSize.y)
        {
            return null;
        }
        return _mapGrid[position.x, position.y];
    }

    public void SetObjectAt(Vector2Int position, GameObject obj)
    {
        if (_mapGrid != null && position.x >= 0 && position.x < MapSize.x && position.y >= 0 && position.y < MapSize.y)
        {
            _mapGrid[position.x, position.y] = obj;
        }
    }

    public void RemoveObjectAt(Vector2Int position)
    {
        if (_mapGrid != null && position.x >= 0 && position.x < MapSize.x && position.y >= 0 && position.y < MapSize.y)
        {
            _mapGrid[position.x, position.y] = null;
        }
    }

    // Wall ID map methods (for BreakableWall)
    public GameObject GetWallById(int wallId)
    {
        if (_wallIdMap.ContainsKey(wallId))
        {
            return _wallIdMap[wallId];
        }
        return null;
    }

    public void AddWallToIdMap(int wallId, GameObject wall)
    {
        _wallIdMap[wallId] = wall;
    }

    public void RemoveWallFromIdMap(int wallId)
    {
        if (_wallIdMap.ContainsKey(wallId))
        {
            _wallIdMap.Remove(wallId);
        }
    }

    public Vector2Int? GetWallPosition(int wallId)
    {
        GameObject wall = GetWallById(wallId);
        if (wall != null)
        {
            // For UI objects, try to get position from RectTransform first
            RectTransform rectTransform = wall.GetComponent<RectTransform>();
            if (rectTransform != null && wall.transform.parent != null)
            {
                PlayModeCellSizeHolder cellSizeHolder = wall.transform.parent.GetComponent<PlayModeCellSizeHolder>();
                if (cellSizeHolder != null)
                {
                    // UI mode - convert anchoredPosition back to grid coordinates
                    float cellSize = cellSizeHolder.cellSize;
                    int x = Mathf.RoundToInt(rectTransform.anchoredPosition.x / cellSize);
                    int y = Mathf.RoundToInt(rectTransform.anchoredPosition.y / cellSize);
                    return new Vector2Int(x, y);
                }
            }
            
            // Fallback to world position
            Vector3 pos = wall.transform.position;
            return new Vector2Int(Mathf.RoundToInt(pos.x), Mathf.RoundToInt(pos.y));
        }
        return null;
    }
    
    // Clear all data (called when stopping PlayMode)
    public void ClearGrid()
    {
        _mapGrid = null;
        _wallIdMap.Clear();
        _mapData = null;
    }
}