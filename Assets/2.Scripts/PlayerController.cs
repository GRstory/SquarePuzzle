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
    
    // Two-step wall breaking
    private Vector2Int? _wallInFrontPosition = null;
    private bool _canBreakWall = false;
    
    // Control enabled flag
    private bool _controlEnabled = true;
    
    [SerializeField] private Sprite _successSprite;

    public void SetInitialPosition(Vector2Int startPos)
    {
        currentGridPosition = startPos;
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
        tryCount++;
        UI_HUD.Instance.UpdateTryCount(tryCount);

        isMoving = true;
        int currentDirection = directionIndex;
        Vector2Int startPos = currentGridPosition;
        
        // Check if we should break a wall from previous move
        if (_canBreakWall && _wallInFrontPosition.HasValue)
        {
            Vector2Int wallPos = _wallInFrontPosition.Value;
            Debug.Log($"Processing BreakableWall at {wallPos}");
            
            GameObject wallObj = MapManager.Instance.GetObjectAt(wallPos);
            if (wallObj != null)
            {
                BreakableWall breakableWall = wallObj.GetComponent<BreakableWall>();
                if (breakableWall != null)
                {
                    Debug.Log($"Breaking wall at {wallPos}");
                    breakableWall.ExecuteOnHit(this);
                    MapManager.Instance.RemoveObjectAt(wallPos);
                    MapManager.Instance.RemoveWallFromIdMap(breakableWall.GetWallId());
                }
            }
        }
        
        // Reset wall breaking flags at start of new move
        _canBreakWall = false;
        _wallInFrontPosition = null;

        // Keep moving until we can't move anymore
        while (true)
        {
            Vector2Int moveDirection = directions[currentDirection];
            Vector2Int targetGridPos = currentGridPosition;
            GameObject wallObjectHit = null;
            bool shouldContinue = false;
            bool reachedGoal = false;

            // MapManager로부터 현재 맵의 사이즈 정보를 가져옵니다.
            Vector2Int mapSize = MapManager.Instance.MapSize;

            // Slide in current direction until hitting something
            while (true)
            {
                Vector2Int nextPos = targetGridPos + moveDirection;

                // 1. Check for walls FIRST (before boundary check)
                GameObject objectAtNextPos = MapManager.Instance.GetObjectAt(nextPos);

                // 2. 벽을 만나는지 확인합니다.
                // 2. 벽을 만나는지 확인합니다.
                if (objectAtNextPos != null)
                {
                    WallBase wall = objectAtNextPos.GetComponent<WallBase>();
                    
                    // Fallback: If component is missing, try to add it based on name
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
                        // Check wall type
                        if (wall is BreakableWall)
                        {
                            // BreakableWall: stop here, mark for breaking on next successful move
                            _wallInFrontPosition = nextPos;
                            _canBreakWall = true;
                            Debug.Log($"BreakableWall hit at {nextPos}, marked for breaking. _canBreakWall={_canBreakWall}");
                            break;
                        }
                        else if (wall is SlideWallUp || wall is SlideWallRight || wall is SlideWallDown || wall is SlideWallLeft)
                        {
                            // Pass through slide wall and change direction
                            targetGridPos = nextPos;
                            
                            // Execute gimmick (sound/anim)
                            wall.ExecuteOnHit(this);
                            
                            // Get redirect direction based on wall type
                            if (wall is SlideWallUp)
                                currentDirection = 0; // Up
                            else if (wall is SlideWallRight)
                                currentDirection = 1; // Right
                            else if (wall is SlideWallDown)
                                currentDirection = 2; // Down
                            else if (wall is SlideWallLeft)
                                currentDirection = 3; // Left
                            
                            shouldContinue = true; // Continue in new direction
                            break;
                        }
                        else if (wall is GoalWall)
                        {
                            Debug.Log($"GoalWall detected at {nextPos}");
                            // Move INTO the goal position and execute win
                            targetGridPos = nextPos;
                            wallObjectHit = objectAtNextPos; // Set this to call ExecuteOnHit
                            reachedGoal = true;
                            break;
                        }
                        else
                        {
                            // Standard wall - stop here
                            Debug.Log($"Standard Wall hit at {nextPos}");
                            wallObjectHit = objectAtNextPos;
                            break;
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"Object at {nextPos} has no WallBase component and could not recover. Name: {objectAtNextPos.name}");
                        break; // Treat as obstacle
                    }
                }
                
                // 3. 맵 경계를 벗어나는지 확인합니다 (벽 체크 이후)
                // This runs AFTER wall check, so edge walls work properly
                // 3. 맵 경계를 벗어나는지 확인합니다 (벽 체크 이후)
                // This runs AFTER wall check, so edge walls work properly
                if (mapSize.x > 0 && mapSize.y > 0 && 
                    (nextPos.x < 0 || nextPos.x >= mapSize.x || nextPos.y < 0 || nextPos.y >= mapSize.y))
                {
                    // Out of bounds - move there then die
                    Debug.Log($"Out of bounds! Moving to {nextPos} then dying. mapSize={mapSize}");
                    targetGridPos = nextPos; // Move to the out-of-bounds position
                    shouldContinue = false; // Stop sliding loop
                    
                    // Mark for death after movement
                    StartCoroutine(HandleDeathAfterMove());
                    break; // Break inner loop to start movement animation
                }
                
                // 4. No obstacle - continue moving
                targetGridPos = nextPos;
            }

            // Animate movement to target position
            // Check if using RectTransform (UI mode)
            RectTransform rectTransform = GetComponent<RectTransform>();
            float cellSize = 1f;
            
            // Get cellSize from parent if in PlayMode
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
                // UI mode - use anchoredPosition with cellSize
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
                // World mode - use transform.position
                Vector3 targetWorldPos = new Vector3(targetGridPos.x, targetGridPos.y, 0);
                while (Vector3.Distance(transform.position, targetWorldPos) > 0.01f)
                {
                    transform.position = Vector3.MoveTowards(transform.position, targetWorldPos, moveSpeed * Time.deltaTime);
                    yield return null;
                }
                transform.position = targetWorldPos;
            }
            
            currentGridPosition = targetGridPos;



            // Execute wall interaction if we hit a standard wall or goal
            if (wallObjectHit != null)
            {
                if (reachedGoal && _successSprite != null)
                {
                    // Change sprite to success sprite
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
                
                wallObjectHit.GetComponent<WallBase>()?.ExecuteOnHit(this);
            }
            else if (reachedGoal)
            {
                // Fallback for Goal tag without WallBase
                Debug.Log("Reached Goal (Fallback)");
                
                if (_successSprite != null)
                {
                    // Change sprite to success sprite
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

            // If we reached the goal or should not continue, stop
            if (reachedGoal || !shouldContinue)
            {
                break;
            }
        }
        
        // Check if we are dying (out of bounds)
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
        yield return null; // Wait for movement to start/finish in Update/Move coroutine
    }

    private IEnumerator HandleDeath(string reason)
    {
        if (_isDying) yield break; // Already dying
        _isDying = true;
        
        isMoving = true; // Prevent further input
        _controlEnabled = false;
        
        Debug.Log($"Player Died: {reason}");
        
        // Wait a moment
        yield return new WaitForSeconds(1f);
        
        // Auto-reset: Find MapDataVisualizer and call ResetPlayMode
        MapDataVisualizer visualizer = FindObjectOfType<MapDataVisualizer>();
        if (visualizer != null)
        {
            visualizer.ResetPlayMode();
        }
        
        isMoving = false;
        _isDying = false;
    }
}
