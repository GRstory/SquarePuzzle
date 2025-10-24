using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class StageDrawer : MonoBehaviour
{
    [SerializeField] private Image _wallPrefab;
    [SerializeField] private Image _goalPrefab;
    [SerializeField] private Image _playerPrefab;
    //[SerializeField] private LineRenderer _pathLinePrefab;

    private List<GameObject> _visualElementList = new List<GameObject>();

    public void DrawMap(MapData mapData, int moveIndex)
    {
        ClearAll();

        var parentRect = GetComponent<RectTransform>();
        float cellSizeBasedOnWidth = parentRect.rect.width / mapData.MapSize.x;
        float cellSizeBasedOnHeight = parentRect.rect.height / mapData.MapSize.y;
        float cellSize = Mathf.Min(cellSizeBasedOnWidth, cellSizeBasedOnHeight);

        /*float offsetX = mapData.MapSize.x * _cellSize / 2.0f - _cellSize / 2.0f;
        float offsetY = mapData.MapSize.y * _cellSize / 2.0f - _cellSize / 2.0f;*/

        Vector2Int playerPos = Vector2Int.zero;
        Dictionary<Vector2Int, MapObject> objectMap = mapData.MapObjects.ToDictionary(o => new Vector2Int(o.X, o.Y));
        foreach (var objInfo in mapData.MapObjects)
        {
            if (objInfo.Type == EMapObjectType.Player) playerPos = new Vector2Int(objInfo.X, objInfo.Y);
            if (objInfo.Type == EMapObjectType.Wall || objInfo.Type == EMapObjectType.Goal)
            {
                var prefab = (objInfo.Type == EMapObjectType.Wall) ? _wallPrefab : _goalPrefab;

                var instance = Instantiate(prefab, transform);
                var rectTransform = instance.GetComponent<RectTransform>();
                rectTransform.sizeDelta = new Vector2(cellSize, cellSize);
                rectTransform.anchoredPosition = new Vector2(objInfo.X * cellSize, objInfo.Y * cellSize);
                _visualElementList.Add(instance.gameObject);
            }
        }

        Vector2Int[] directions = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };
        for (int i = 0; i <= moveIndex; i++)
        {
            if (i >= mapData.OptimalPath.Count) break;

            int dirIndex = mapData.OptimalPath[i];
            Vector2Int moveDir = directions[dirIndex];
            Vector2Int nextPos = GetNextPosition(mapData, objectMap, playerPos, moveDir);

            /*LineRenderer line = Instantiate(_pathLinePrefab, transform);
            line.SetPositions(new Vector3[] { new Vector3(playerPos.x, playerPos.y, -1), new Vector3(nextPos.x, nextPos.y, -1) });
            _visualElementList.Add(line.gameObject);*/

            playerPos = nextPos;
        }

        var playerInstance = Instantiate(_playerPrefab, transform);
        var playerRectTransform = playerInstance.GetComponent<RectTransform>();
        playerRectTransform.sizeDelta = new Vector2(cellSize, cellSize);
        playerRectTransform.anchoredPosition = new Vector2(playerPos.x * cellSize, playerPos.y * cellSize);
        _visualElementList.Add(playerInstance.gameObject);
    }

    private void ClearAll()
    {
        foreach(GameObject element in _visualElementList)
        {
            Destroy(element);
        }
        _visualElementList.Clear();
    }

    private Vector2Int GetNextPosition(MapData data, Dictionary<Vector2Int, MapObject> objectMap, Vector2Int startPos, Vector2Int direction)
    {
        Vector2Int currentPos = startPos;
        while (true)
        {
            Vector2Int nextPos = currentPos + direction;

            // 맵 경계 체크
            if (nextPos.x < 0 || nextPos.x >= data.MapSize.x || nextPos.y < 0 || nextPos.y >= data.MapSize.y)
            {
                return currentPos;
            }
            // 다음 위치에 벽/골이 있는지 체크
            if (objectMap.ContainsKey(nextPos))
            {
                return currentPos;
            }
            currentPos = nextPos;
        }
    }
}
