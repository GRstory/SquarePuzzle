using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 10f;
    private bool isMoving = false;
    private Vector2Int currentGridPosition;
    private readonly Vector2Int[] directions = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };
    private int tryCount = 0;

    public void SetInitialPosition(Vector2Int startPos)
    {
        currentGridPosition = startPos;
    }

    void Update()
    {
        if (isMoving) return;

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

                // 1. 맵 경계를 벗어나는지 확인합니다.
                if (mapSize != null && (nextPos.x < 0 || nextPos.x >= mapSize.x || nextPos.y < 0 || nextPos.y >= mapSize.y))
                {
                    break; // 맵 경계에 도달하면 루프를 탈출합니다.
                }

                GameObject objectAtNextPos = MapManager.Instance.GetObjectAt(nextPos);

                // 2. 벽을 만나는지 확인합니다.
                if (objectAtNextPos != null)
                {
                    WallBase wall = objectAtNextPos.GetComponent<WallBase>();
                    
                    if (wall != null)
                    {
                        // Check wall type
                        if (wall is BreakableWall)
                        {
                            BreakableWall breakableWall = wall as BreakableWall;
                            // Break the wall and continue moving
                            wall.ExecuteOnHit(this);
                            MapManager.Instance.RemoveObjectAt(nextPos);
                            MapManager.Instance.RemoveWallFromIdMap(breakableWall.GetWallId());
                            targetGridPos = nextPos;
                            shouldContinue = false; // Continue in same direction in this loop
                        }
                        else if (wall is SlideWallUp || wall is SlideWallRight || wall is SlideWallDown || wall is SlideWallLeft)
                        {
                            // Pass through slide wall and change direction
                            targetGridPos = nextPos;
                            
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
                            // Move INTO the goal position
                            targetGridPos = nextPos;
                            wallObjectHit = objectAtNextPos;
                            reachedGoal = true;
                            break;
                        }
                        else
                        {
                            // Standard wall - stop here
                            wallObjectHit = objectAtNextPos;
                            break;
                        }
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    targetGridPos = nextPos;
                }
            }

            // Animate movement to target position
            Vector3 targetWorldPos = new Vector3(targetGridPos.x, targetGridPos.y, 0);
            while (Vector3.Distance(transform.position, targetWorldPos) > 0.01f)
            {
                transform.position = Vector3.MoveTowards(transform.position, targetWorldPos, moveSpeed * Time.deltaTime);
                yield return null;
            }

            transform.position = targetWorldPos;
            currentGridPosition = targetGridPos;

            // Execute wall interaction if we hit a standard wall or goal
            if (wallObjectHit != null)
            {
                wallObjectHit.GetComponent<WallBase>()?.ExecuteOnHit(this);
            }

            // If we reached the goal or should not continue, stop
            if (reachedGoal || !shouldContinue)
            {
                break;
            }
        }

        isMoving = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Goal"))
        {
            Debug.Log("Level Clear!");
            this.enabled = false;
        }
    }
}
