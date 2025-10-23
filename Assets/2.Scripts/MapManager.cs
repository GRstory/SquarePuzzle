using System.Collections.Generic;
using UnityEngine;

public class MapManager : MonoBehaviour
{
    public static MapManager Instance;

    [Header("Map Data")]
    public TextAsset _mapJson;

    private MapData _mapData;
    private GameObject[,] _mapGrid;
    public Vector2Int MapSize => _mapData.MapSize;

    [Header("Prefabs")]
    [SerializeField] private GameObject _playerPrefab;
    [SerializeField] private GameObject _wallPrefab;
    [SerializeField] private GameObject _goalPrefab;

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
    }

    public void GenerateMap()
    {
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
}