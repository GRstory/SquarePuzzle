using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 10f;
    private bool isMoving = false;
    private Vector2Int currentGridPosition;
    private readonly Vector2Int[] directions = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };
    private int tryCount = 0;
    
    private Vector2Int? _wallInFrontPosition = null;
    private bool _canBreakWall = false;
    private bool _controlEnabled = true;
    
    [SerializeField] private Sprite _successSprite;
    public System.Action<int> OnMoveCountChanged;

    public void SetInitialPosition(Vector2Int startPos)
    {
        currentGridPosition = startPos;
        CheckAdjacentWalls();
    }
    
    public void SetControlEnabled(bool enabled)
    {
        _controlEnabled = enabled;
    }

    void Update()
    {
        if (isMoving || !_controlEnabled) return;

        if (Input.GetKeyDown(KeyCode.W)) StartCoroutine(Move(0));
        else if (Input.GetKeyDown(KeyCode.D)) StartCoroutine(Move(1));
        else if (Input.GetKeyDown(KeyCode.S)) StartCoroutine(Move(2));
        else if (Input.GetKeyDown(KeyCode.A)) StartCoroutine(Move(3));
    }

    private IEnumerator Move(int directionIndex)
    {
        isMoving = true;
        int currentDirection = directionIndex;
        Vector2Int startPos = currentGridPosition;
        Vector2Int? previousWallInFront = _wallInFrontPosition;
        bool canBreakWallThisMove = _canBreakWall;
        
        _canBreakWall = false;
        _wallInFrontPosition = null;
        while (true)
        {
            Vector2Int moveDirection = directions[currentDirection];
            Vector2Int targetGridPos = currentGridPosition;
            GameObject wallObjectHit = null;
            bool shouldContinue = false;
            bool reachedGoal = false;

            Vector2Int mapSize = MapManager.Instance.MapSize;

            while (true)
            {
                Vector2Int nextPos = targetGridPos + moveDirection;
                GameObject objectAtNextPos = MapManager.Instance.GetObjectAt(nextPos);

                if (objectAtNextPos != null)
                {
                    WallBase wall = objectAtNextPos.GetComponent<WallBase>();
                    
                               // 컴포넌트 없으면 이름으로 추가
                    if (wall == null)
                    {
                        string objName = objectAtNextPos.name;
                        if (objName.Contains("BreakableWall")) wall = objectAtNextPos.AddComponent<BreakableWall>();
                        else if (objName.Contains("Goal")) wall = objectAtNextPos.AddComponent<GoalWall>();
                        else if (objName.Contains("SlideWallUp")) wall = objectAtNextPos.AddComponent<SlideWallUp>();
                        else if (objName.Contains("SlideWallRight")) wall = objectAtNextPos.AddComponent<SlideWallRight>();
                        else if (objName.Contains("SlideWallDown")) wall = objectAtNextPos.AddComponent<SlideWallDown>();
                        else if (objName.Contains("SlideWallLeft")) wall = objectAtNextPos.AddComponent<SlideWallLeft>();
                        else if (objName.Contains("Wall")) wall = objectAtNextPos.AddComponent<StandardWall>();
                        
                        if (wall != null)
                        {
                            Debug.LogWarning($"Recovered missing component for {objName}. Added {wall.GetType().Name}.");
                        }
                    }
                    
                    if (wall != null)
                    {
                        if (wall is BreakableWall)// 이미 마킹된 벽이면 즉시 파괴
                        {
                            if (previousWallInFront.HasValue && previousWallInFront.Value == nextPos && canBreakWallThisMove)
                            {
                                BreakableWall breakableWall = wall as BreakableWall;
                                breakableWall.ExecuteOnHit(this);
                                MapManager.Instance.RemoveObjectAt(nextPos);
                                MapManager.Instance.RemoveWallFromIdMap(breakableWall.GetWallId());
                                
                                previousWallInFront = null;
                                canBreakWallThisMove = false;
                                
                                targetGridPos = nextPos;
                                continue;
                            }
                            else // 첫 충돌: 마킹만
                            {
                                _wallInFrontPosition = nextPos;
                                _canBreakWall = true;
                                break;
                            }
                        }
                        else if (wall is SlideWallUp || wall is SlideWallRight || wall is SlideWallDown || wall is SlideWallLeft)
                        {
                            targetGridPos = nextPos;
                            wallObjectHit = objectAtNextPos;
                            shouldContinue = false;
                            break;
                        }
                        else if (wall is GoalWall)
                        {
                            targetGridPos = nextPos;
                            wallObjectHit = objectAtNextPos;
                            reachedGoal = true;
                            break;
                        }
                        else
                        {
                            wallObjectHit = objectAtNextPos;
                            break;
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"Object at {nextPos} has no WallBase component. Name: {objectAtNextPos.name}");
                        break;
                    }
                }

                if (mapSize.x > 0 && mapSize.y > 0 && (nextPos.x < 0 || nextPos.x >= mapSize.x || nextPos.y < 0 || nextPos.y >= mapSize.y))// 경계 체크
                {
                    targetGridPos = nextPos;
                    break;
                }
                targetGridPos = nextPos;
            }

                  // 이동 애니메이션
            RectTransform rectTransform = GetComponent<RectTransform>();
            float cellSize = 1f;
            
            if (transform.parent != null)
            {
                PlayModeCellSizeHolder cellSizeHolder = transform.parent.GetComponent<PlayModeCellSizeHolder>();
                if (cellSizeHolder != null)
                {
                    cellSize = cellSizeHolder.cellSize;
                }
            }
            
            if (rectTransform != null && cellSize != 1f)
            {
                Vector2 targetAnchoredPos = new Vector2(targetGridPos.x * cellSize, targetGridPos.y * cellSize);
                while (Vector2.Distance(rectTransform.anchoredPosition, targetAnchoredPos) > 0.01f)
                {
                    rectTransform.anchoredPosition = Vector2.MoveTowards(rectTransform.anchoredPosition, targetAnchoredPos, moveSpeed * cellSize * Time.deltaTime);
                    yield return null;
                }
                rectTransform.anchoredPosition = targetAnchoredPos;
            }
            else
            {
                Vector3 targetWorldPos = new Vector3(targetGridPos.x, targetGridPos.y, 0);
                while (Vector3.Distance(transform.position, targetWorldPos) > 0.01f)
                {
                    transform.position = Vector3.MoveTowards(transform.position, targetWorldPos, moveSpeed * Time.deltaTime);
                    yield return null;
                }
                transform.position = targetWorldPos;
            }
            
            currentGridPosition = targetGridPos;
            
                   // 경계 밖으로 나가면 리셋
            if (mapSize.x > 0 && mapSize.y > 0 && 
                (currentGridPosition.x < 0 || currentGridPosition.x >= mapSize.x || 
                 currentGridPosition.y < 0 || currentGridPosition.y >= mapSize.y))
            {
                MapDataVisualizer visualizer = FindObjectOfType<MapDataVisualizer>();
                if (visualizer != null)
                {
                    visualizer.ResetPlayMode();
                }
                
                isMoving = false;
                yield break;
            }

                   // 벽 상호작용
            if (wallObjectHit != null)
            {
                WallBase wall = wallObjectHit.GetComponent<WallBase>();
                
                if (wall is SlideWallUp || wall is SlideWallRight || wall is SlideWallDown || wall is SlideWallLeft)
                {
                    wall.ExecuteOnHit(this);
                    
                    int newDirection = 0;
                    if (wall is SlideWallUp) newDirection = 0;
                    else if (wall is SlideWallRight) newDirection = 1;
                    else if (wall is SlideWallDown) newDirection = 2;
                    else if (wall is SlideWallLeft) newDirection = 3;
                    
                    Vector2Int exitDirection = directions[newDirection];
                    Vector2Int exitPos = currentGridPosition + exitDirection;
                    
                    bool canExit = true;
                    
                    if (mapSize.x > 0 && mapSize.y > 0 && 
                        (exitPos.x < 0 || exitPos.x >= mapSize.x || exitPos.y < 0 || exitPos.y >= mapSize.y))
                    {
                        canExit = false;
                    }
                    
                    if (canExit)
                    {
                        GameObject objectAtExit = MapManager.Instance.GetObjectAt(exitPos);
                        if (objectAtExit != null)
                        {
                            WallBase exitWall = objectAtExit.GetComponent<WallBase>();
                            if (exitWall != null && !(exitWall is GoalWall))
                            {
                                canExit = false;
                            }
                        }
                    }
                    
                    if (!canExit)
                    {
                        // Cannot exit SlideWall - player dies
                        Debug.Log($"Player cannot exit SlideWall at {currentGridPosition}. Resetting...");
                        MapDataVisualizer visualizer = FindObjectOfType<MapDataVisualizer>();
                        if (visualizer != null)
                        {
                            visualizer.ResetPlayMode();
                        }
                        
                        isMoving = false;
                        yield break;
                    }
                    
                    // Can exit - animate to exit position
                    cellSize = 1f;
                    if (transform.parent != null)
                    {
                        PlayModeCellSizeHolder cellSizeHolder = transform.parent.GetComponent<PlayModeCellSizeHolder>();
                        if (cellSizeHolder != null)
                        {
                            cellSize = cellSizeHolder.cellSize;
                        }
                    }
                    
                    if (rectTransform != null && cellSize != 1f)
                    {
                        Vector2 targetAnchoredPos = new Vector2(exitPos.x * cellSize, exitPos.y * cellSize);
                        while (Vector2.Distance(rectTransform.anchoredPosition, targetAnchoredPos) > 0.01f)
                        {
                            rectTransform.anchoredPosition = Vector2.MoveTowards(rectTransform.anchoredPosition, targetAnchoredPos, moveSpeed * cellSize * Time.deltaTime);
                            yield return null;
                        }
                        rectTransform.anchoredPosition = targetAnchoredPos;
                    }
                    else
                    {
                        Vector3 targetWorldPos = new Vector3(exitPos.x, exitPos.y, 0);
                        while (Vector3.Distance(transform.position, targetWorldPos) > 0.01f)
                        {
                            transform.position = Vector3.MoveTowards(transform.position, targetWorldPos, moveSpeed * Time.deltaTime);
                            yield return null;
                        }
                        transform.position = targetWorldPos;
                    }
                    
                    currentGridPosition = exitPos;
                }
                else if (reachedGoal && _successSprite != null)
                {
                    Image image = GetComponent<Image>();
                    if (image != null)
                    {
                        image.sprite = _successSprite;
                    }
                    else
                    {
                        SpriteRenderer sr = GetComponent<SpriteRenderer>();
                        if (sr != null)
                        {
                            sr.sprite = _successSprite;
                        }
                    }
                }
                
                if (!(wall is SlideWallUp || wall is SlideWallRight || wall is SlideWallDown || wall is SlideWallLeft))
                {
                    wallObjectHit.GetComponent<WallBase>()?.ExecuteOnHit(this);
                }
            }
            else if (reachedGoal)
            {
                if (_successSprite != null)
                {
                    Image image = GetComponent<Image>();
                    if (image != null)
                    {
                        image.sprite = _successSprite;
                    }
                    else
                    {
                        SpriteRenderer sr = GetComponent<SpriteRenderer>();
                        if (sr != null)
                        {
                            sr.sprite = _successSprite;
                        }
                    }
                }
                
                UI_HUD.Instance.FinishGame();
            }

            if (reachedGoal || !shouldContinue)
            {
                break;
            }
        }
        
        bool playerMoved = (currentGridPosition != startPos);
        
            // 실제로 이동했을 때만 카운트 증가 및 벽 파괴
        if (playerMoved)
        {
            tryCount++;
            UI_HUD.Instance.UpdateTryCount(tryCount);
            OnMoveCountChanged?.Invoke(tryCount);
            
            if (canBreakWallThisMove && previousWallInFront.HasValue)
            {
                Vector2Int wallPos = previousWallInFront.Value;
                GameObject wallObj = MapManager.Instance.GetObjectAt(wallPos);
                if (wallObj != null)
                {
                    BreakableWall breakableWall = wallObj.GetComponent<BreakableWall>();
                    if (breakableWall != null)
                    {
                        breakableWall.ExecuteOnHit(this);
                        MapManager.Instance.RemoveObjectAt(wallPos);
                        MapManager.Instance.RemoveWallFromIdMap(breakableWall.GetWallId());
                    }
                }
            }
        }
        
        if (_isDying)
        {
            yield return StartCoroutine(HandleDeath("Out of Bounds"));
            yield break;
        }

        isMoving = false;
    }
   
    private bool _isDying = false;

    private IEnumerator HandleDeathAfterMove()
    {
        _isDying = true;
        yield return null;
    }

    private IEnumerator HandleDeath(string reason)
    {
        if (_isDying) yield break;
        _isDying = true;
        
        isMoving = true;
        _controlEnabled = false;
        
        yield return new WaitForSeconds(3f);
        
        MapDataVisualizer visualizer = FindObjectOfType<MapDataVisualizer>();
        if (visualizer != null)
        {
            visualizer.ResetPlayMode();
        }
        
        isMoving = false;
        _isDying = false;
    }

    private void CheckAdjacentWalls()
    {
        if (MapManager.Instance == null) return;

        for (int dir = 0; dir < 4; dir++)
        {
            Vector2Int checkPos = currentGridPosition + directions[dir];
            GameObject obj = MapManager.Instance.GetObjectAt(checkPos);
            
            if (obj != null)
            {
                BreakableWall wall = obj.GetComponent<BreakableWall>();
                if (wall != null)
                {
                    _wallInFrontPosition = checkPos;
                    _canBreakWall = true;
                    return;
                }
            }
        }
    }
}
