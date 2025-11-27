using System.Collections.Generic;
using UnityEngine;

public class MapManager : MonoBehaviour
{
    public static MapManager Instance;

    [Header("Map Data")]
    public TextAsset _mapJson;

    private MapData _mapData;
    private GameObject[,] _mapGrid;
    private Dictionary<int, GameObject> _wallIdMap = new Dictionary<int, GameObject>(); // Map wall IDs to GameObjects
    public Vector2Int MapSize => _mapData.MapSize;

    [Header("Prefabs")]
    [SerializeField] private GameObject _playerPrefab;
    [SerializeField] private GameObject _wallPrefab;
    [SerializeField] private GameObject _goalPrefab;
    [SerializeField] private GameObject _breakableWallPrefab;
    [SerializeField] private GameObject _slideWallUpPrefab;
    [SerializeField] private GameObject _slideWallRightPrefab;
    [SerializeField] private GameObject _slideWallDownPrefab;
    [SerializeField] private GameObject _slideWallLeftPrefab;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        ParseJsonData(_mapJson);
        GenerateMap();
    }
    
    public void ParseJsonData(TextAsset mapJson)
    {
        if (mapJson == null) return;

        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }
        _mapData = JsonUtility.FromJson<MapData>(mapJson.text);
        if (_mapData == null) return;

        _mapGrid = new GameObject[MapSize.x, MapSize.y];
        _wallIdMap.Clear();
    }

    public void GenerateMap()
    {
        int breakableWallIdCounter = 0; // 자동 ID 생성

        foreach (var objInfo in _mapData.MapObjects)
        {
            Vector3 position = new Vector3(objInfo.X, objInfo.Y, 0);
            GameObject prefab = GetPrefabByType(objInfo.Type);

            if (prefab != null)
            {
                GameObject instance = Instantiate(prefab, position, Quaternion.identity, transform);
                Vector2Int gridPos = new Vector2Int(objInfo.X, objInfo.Y);

                if (objInfo.Type == EMapObjectType.Player)
                {
                    instance.GetComponent<PlayerController>()?.SetInitialPosition(gridPos);
                }
                else if (objInfo.Type == EMapObjectType.BreakableWall)
                {
                    // 자동으로 ID 부여
                    BreakableWall breakableWall = instance.GetComponent<BreakableWall>();
                    if (breakableWall != null)
                    {
                        breakableWall.SetWallId(breakableWallIdCounter);
                        _wallIdMap[breakableWallIdCounter] = instance;
                        breakableWallIdCounter++;
                    }
                }

                // Add to grid for collision detection
                if (objInfo.Type == EMapObjectType.Wall || objInfo.Type == EMapObjectType.Goal || 
                    objInfo.Type == EMapObjectType.BreakableWall ||
                    objInfo.Type == EMapObjectType.SlideWallUp || objInfo.Type == EMapObjectType.SlideWallRight ||
                    objInfo.Type == EMapObjectType.SlideWallDown || objInfo.Type == EMapObjectType.SlideWallLeft)
                {
                    _mapGrid[objInfo.X, objInfo.Y] = instance;
                }
            }
        }

        UI_HUD.Instance.SetMaxSolveCount(_mapData.OptimalPath.Count);
        UI_HUD.Instance.UpdateTryCount(0);

    }

    private GameObject GetPrefabByType(EMapObjectType type)
    {
        switch (type)
        {
            case EMapObjectType.Player: return _playerPrefab;
            case EMapObjectType.Wall: return _wallPrefab;
            case EMapObjectType.Goal: return _goalPrefab;
            case EMapObjectType.BreakableWall: return _breakableWallPrefab;
            case EMapObjectType.SlideWallUp: return _slideWallUpPrefab;
            case EMapObjectType.SlideWallRight: return _slideWallRightPrefab;
            case EMapObjectType.SlideWallDown: return _slideWallDownPrefab;
            case EMapObjectType.SlideWallLeft: return _slideWallLeftPrefab;
            default: return null;
        }
    }

    public GameObject GetObjectAt(Vector2Int position)
    {
        if (position.x < 0 || position.x >= MapSize.x || position.y < 0 || position.y >= MapSize.y)
        {
            return null;
        }
        return _mapGrid[position.x, position.y];
    }

    public GameObject GetWallById(int wallId)
    {
        if (_wallIdMap.ContainsKey(wallId))
        {
            return _wallIdMap[wallId];
        }
        return null;
    }

    public Vector2Int? GetWallPosition(int wallId)
    {
        GameObject wall = GetWallById(wallId);
        if (wall != null)
        {
            Vector3 pos = wall.transform.position;
            return new Vector2Int(Mathf.RoundToInt(pos.x), Mathf.RoundToInt(pos.y));
        }
        return null;
    }

    public void RemoveObjectAt(Vector2Int position)
    {
        if (position.x >= 0 && position.x < MapSize.x && position.y >= 0 && position.y < MapSize.y)
        {
            _mapGrid[position.x, position.y] = null;
        }
    }

    public void RemoveWallFromIdMap(int wallId)
    {
        if (_wallIdMap.ContainsKey(wallId))
        {
            _wallIdMap.Remove(wallId);
        }
    }
}