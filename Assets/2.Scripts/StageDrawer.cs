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
    [SerializeField] private Image _breakableWallPrefab;
    [SerializeField] private Image _slideWallUpPrefab;
    [SerializeField] private Image _slideWallRightPrefab;
    [SerializeField] private Image _slideWallDownPrefab;
    [SerializeField] private Image _slideWallLeftPrefab;
    [SerializeField] private Color _pathLineColor = new Color(0.2f, 0.8f, 1f, 0.5f); // Cyan with transparency
    [SerializeField] private float _pathLineWidth = 3f;
    [SerializeField] private Color _indexLabelColor = Color.white;
    [SerializeField] private int _indexLabelFontSize = 24;

    private List<GameObject> _visualElementList = new List<GameObject>();

    public void DrawMap(MapData mapData, int moveIndex, bool showPathLine = true, bool showIndexLabel = false, int indexNumber = -1)
    {
        ClearAll();

        var parentRect = GetComponent<RectTransform>();
        float cellSizeBasedOnWidth = parentRect.rect.width / mapData.MapSize.x;
        float cellSizeBasedOnHeight = parentRect.rect.height / mapData.MapSize.y;
        float cellSize = Mathf.Min(cellSizeBasedOnWidth, cellSizeBasedOnHeight);

        Vector2Int playerPos = Vector2Int.zero;
        Dictionary<Vector2Int, MapObject> objectMap = mapData.MapObjects.ToDictionary(o => new Vector2Int(o.X, o.Y));
        HashSet<int> brokenWallIds = new HashSet<int>(); // Track broken walls by ID

        // Draw all walls and goals
        foreach (var objInfo in mapData.MapObjects)
        {
            if (objInfo.Type == EMapObjectType.Player)
            {
                playerPos = new Vector2Int(objInfo.X, objInfo.Y);
            }
            else if (objInfo.Type == EMapObjectType.Wall || objInfo.Type == EMapObjectType.Goal ||
                     objInfo.Type == EMapObjectType.BreakableWall ||
                     objInfo.Type == EMapObjectType.SlideWallUp || objInfo.Type == EMapObjectType.SlideWallRight ||
                     objInfo.Type == EMapObjectType.SlideWallDown || objInfo.Type == EMapObjectType.SlideWallLeft)
            {
                Image prefab = GetPrefabByType(objInfo.Type);

                if (prefab != null)
                {
                    var instance = Instantiate(prefab, transform);
                    var rectTransform = instance.GetComponent<RectTransform>();
                    rectTransform.sizeDelta = new Vector2(cellSize, cellSize);
                    rectTransform.anchoredPosition = new Vector2(objInfo.X * cellSize, objInfo.Y * cellSize);
                    _visualElementList.Add(instance.gameObject);
                }
            }
        }

        // Build complete path for visualization
        List<Vector2> pathPoints = new List<Vector2>();
        Vector2Int currentPos = playerPos;
        Vector2Int startPos = playerPos;
        HashSet<Vector2Int> pathBrokenWalls = new HashSet<Vector2Int>();
        Vector2Int? wallInFront = null;
        
        // Add starting position (center of cell)
        pathPoints.Add(new Vector2(startPos.x * cellSize + cellSize / 2f, startPos.y * cellSize + cellSize / 2f));

        // Simulate full path to get all positions
        Vector2Int[] directions = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };
        for (int i = 0; i < mapData.OptimalPath.Count; i++)
        {
            int dirIndex = mapData.OptimalPath[i];
            Vector2Int prevPos = currentPos;
            Vector2Int? previousWallInFront = wallInFront; // Store wall from PREVIOUS move
            Vector2Int nextPos = SimulateMove(mapData, objectMap, currentPos, dirIndex, pathBrokenWalls, directions, ref wallInFront);
            
            // Only break wall if it was marked in PREVIOUS move and we moved this turn
            if (previousWallInFront.HasValue && nextPos != prevPos)
            {
                pathBrokenWalls.Add(previousWallInFront.Value);
            }
            
            // Add center of cell position
            pathPoints.Add(new Vector2(nextPos.x * cellSize + cellSize / 2f, nextPos.y * cellSize + cellSize / 2f));
            currentPos = nextPos;
        }

        // Draw path line segments (behind other elements) - only if enabled
        if (showPathLine && pathPoints.Count >= 2)
        {
            for (int i = 0; i < pathPoints.Count - 1; i++)
            {
                CreateLineSegment(pathPoints[i], pathPoints[i + 1]);
            }
        }

        // Simulate player movement through optimal path up to moveIndex
        currentPos = playerPos;
        HashSet<Vector2Int> brokenWallPositions = new HashSet<Vector2Int>();
        wallInFront = null;
        for (int i = 0; i <= moveIndex; i++)
        {
            if (i >= mapData.OptimalPath.Count) break;

            int dirIndex = mapData.OptimalPath[i];
            Vector2Int prevPos = currentPos;
            Vector2Int? previousWallInFront = wallInFront; // Store wall from PREVIOUS move
            Vector2Int nextPos = SimulateMove(mapData, objectMap, currentPos, dirIndex, brokenWallPositions, directions, ref wallInFront);
            
            // Only break wall if it was marked in PREVIOUS move and we moved this turn
            if (previousWallInFront.HasValue && nextPos != prevPos)
            {
                brokenWallPositions.Add(previousWallInFront.Value);
            }
            
            currentPos = nextPos;
        }

        // Remove broken walls from visualization
        foreach (var brokenWallPos in brokenWallPositions)
        {
            // Find and remove the visual element at this position
            for (int i = _visualElementList.Count - 1; i >= 0; i--)
            {
                var elem = _visualElementList[i];
                if (elem != null)
                {
                    // Check if this element matches the broken wall position
                    var rectTransform = elem.GetComponent<RectTransform>();
                    Vector2 expectedPos = new Vector2(brokenWallPos.x * cellSize, brokenWallPos.y * cellSize);
                    if (Vector2.Distance(rectTransform.anchoredPosition, expectedPos) < 0.1f)
                    {
                        Destroy(elem);
                        _visualElementList.RemoveAt(i);
                        break;
                    }
                }
            }
        }

        // Draw player at final position
        var playerInstance = Instantiate(_playerPrefab, transform);
        var playerRectTransform = playerInstance.GetComponent<RectTransform>();
        playerRectTransform.sizeDelta = new Vector2(cellSize, cellSize);
        playerRectTransform.anchoredPosition = new Vector2(currentPos.x * cellSize, currentPos.y * cellSize);
        
        // Disable PlayerController if present (visualization only)
        var playerController = playerInstance.GetComponent<PlayerController>();
        if (playerController != null)
        {
            playerController.SetControlEnabled(false);
        }
        
        _visualElementList.Add(playerInstance.gameObject);

        // Draw index label if enabled
        if (showIndexLabel && indexNumber >= 0)
        {
            DrawIndexLabel(indexNumber, parentRect);
        }
    }

    private void CreateLineSegment(Vector2 start, Vector2 end)
    {
        GameObject lineObj = new GameObject("PathSegment");
        lineObj.transform.SetParent(transform, false);
        lineObj.transform.SetAsFirstSibling(); // Draw behind other elements

        Image lineImage = lineObj.AddComponent<Image>();
        lineImage.color = _pathLineColor;

        RectTransform rectTransform = lineObj.GetComponent<RectTransform>();
        
        // Set anchor and pivot to center
        rectTransform.anchorMin = new Vector2(0, 0);
        rectTransform.anchorMax = new Vector2(0, 0);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        
        Vector2 direction = end - start;
        float distance = direction.magnitude;
        
        // Set size and position
        rectTransform.sizeDelta = new Vector2(distance, _pathLineWidth);
        rectTransform.anchoredPosition = (start + end) / 2f;
        
        // Rotate to align with direction
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        rectTransform.localRotation = Quaternion.Euler(0, 0, angle);

        _visualElementList.Add(lineObj);
    }

    private void ClearAll()
    {
        foreach(GameObject element in _visualElementList)
        {
            Destroy(element);
        }
        _visualElementList.Clear();
    }

    private Image GetPrefabByType(EMapObjectType type)
    {
        switch (type)
        {
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

    private Vector2Int SimulateMove(MapData data, Dictionary<Vector2Int, MapObject> objectMap, 
        Vector2Int startPos, int initialDirection, HashSet<Vector2Int> brokenWallPositions, Vector2Int[] directions, ref Vector2Int? wallInFront)
    {
        Vector2Int currentPos = startPos;
        int currentDirection = initialDirection;
        
        // Keep moving until we can't move anymore
        while (true)
        {
            Vector2Int moveDir = directions[currentDirection];
            Vector2Int targetPos = currentPos;
            bool shouldContinue = false;
            bool reachedGoal = false;

            // Slide in current direction
            while (true)
            {
                Vector2Int nextPos = targetPos + moveDir;

                // Check map boundaries
                if (nextPos.x < 0 || nextPos.x >= data.MapSize.x || nextPos.y < 0 || nextPos.y >= data.MapSize.y)
                {
                    break;
                }

                // Check if there's an object at next position
                if (objectMap.ContainsKey(nextPos))
                {
                    MapObject obj = objectMap[nextPos];

                    // Skip if this wall is already broken
                    if (brokenWallPositions.Contains(nextPos))
                    {
                        targetPos = nextPos;
                        continue;
                    }

                    if (obj.Type == EMapObjectType.BreakableWall)
                    {
                        // BreakableWall: stop here, mark for breaking on next successful move
                        wallInFront = nextPos;
                        break;
                    }
                    else if (obj.Type == EMapObjectType.SlideWallUp)
                    {
                        // Pass through and redirect upward
                        targetPos = nextPos;
                        currentDirection = 0; // Up
                        shouldContinue = true;
                        break;
                    }
                    else if (obj.Type == EMapObjectType.SlideWallRight)
                    {
                        // Pass through and redirect right
                        targetPos = nextPos;
                        currentDirection = 1; // Right
                        shouldContinue = true;
                        break;
                    }
                    else if (obj.Type == EMapObjectType.SlideWallDown)
                    {
                        // Pass through and redirect downward
                        targetPos = nextPos;
                        currentDirection = 2; // Down
                        shouldContinue = true;
                        break;
                    }
                    else if (obj.Type == EMapObjectType.SlideWallLeft)
                    {
                        // Pass through and redirect left
                        targetPos = nextPos;
                        currentDirection = 3; // Left
                        shouldContinue = true;
                        break;
                    }
                    else if (obj.Type == EMapObjectType.Goal)
                    {
                        // Move INTO the goal position
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
                else
                {
                    targetPos = nextPos;
                }
            }

            currentPos = targetPos;

            if (reachedGoal || !shouldContinue)
            {
                break;
            }
        }

        return currentPos;
    }

    private void DrawIndexLabel(int index, RectTransform parentRect)
    {
        // Create text object for index label
        GameObject labelObj = new GameObject("IndexLabel");
        labelObj.transform.SetParent(transform, false);

        TMPro.TextMeshProUGUI textComponent = labelObj.AddComponent<TMPro.TextMeshProUGUI>();
        textComponent.text = index.ToString();
        textComponent.fontSize = _indexLabelFontSize;
        textComponent.color = _indexLabelColor;
        textComponent.alignment = TMPro.TextAlignmentOptions.TopLeft;
        textComponent.fontStyle = TMPro.FontStyles.Bold;

        RectTransform labelRect = labelObj.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0, 1);
        labelRect.anchorMax = new Vector2(0, 1);
        labelRect.pivot = new Vector2(0, 1);
        labelRect.anchoredPosition = new Vector2(5, -5); // Top-left corner with small padding
        labelRect.sizeDelta = new Vector2(50, 50);

        _visualElementList.Add(labelObj);
    }
}
