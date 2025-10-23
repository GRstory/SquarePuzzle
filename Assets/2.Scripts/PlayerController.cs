using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 10f;
    private bool isMoving = false;
    private Vector2Int currentGridPosition;
    private readonly Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
    private int tryCount = 0;

    public void SetInitialPosition(Vector2Int startPos)
    {
        currentGridPosition = startPos;
    }

    void Update()
    {
        if (isMoving) return;

        if (Input.GetKeyDown(KeyCode.W)) StartCoroutine(Move(0));
        else if (Input.GetKeyDown(KeyCode.S)) StartCoroutine(Move(1));
        else if (Input.GetKeyDown(KeyCode.A)) StartCoroutine(Move(2));
        else if (Input.GetKeyDown(KeyCode.D)) StartCoroutine(Move(3));
    }

    private IEnumerator Move(int directionIndex)
    {
        tryCount++;
        UI_HUD.Instance.UpdateTryCount(tryCount);

        isMoving = true;
        Vector2Int moveDirection = directions[directionIndex];
        Vector2Int targetGridPos = currentGridPosition;
        GameObject wallObjectHit = null;

        // MapManager로부터 현재 맵의 사이즈 정보를 가져옵니다.
        Vector2Int mapSize = MapManager.Instance.MapSize;

        while (true)
        {
            Vector2Int nextPos = targetGridPos + moveDirection;

            // --- 무한 루프 방지 로직 ---
            // 1. 맵 경계를 벗어나는지 확인합니다.

            if (mapSize != null && (nextPos.x < 0 || nextPos.x >= mapSize.x || nextPos.y < 0 || nextPos.y >= mapSize.y))
            {
                break; // 맵 경계에 도달하면 루프를 탈출합니다.
            }

            GameObject objectAtNextPos = MapManager.Instance.GetObjectAt(nextPos);

            // 2. 벽을 만나는지 확인합니다.
            if (objectAtNextPos != null)
            {
                wallObjectHit = objectAtNextPos;
                wallObjectHit.GetComponent<WallBase>()?.ExecuteOnHit(this);
                break; // 벽을 만나면 루프를 탈출합니다.
            }

            targetGridPos = nextPos;
        }

        Vector3 targetWorldPos = new Vector3(targetGridPos.x, targetGridPos.y, 0);

        // 부드러운 이동 애니메이션
        while (Vector3.Distance(transform.position, targetWorldPos) > 0.01f)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetWorldPos, moveSpeed * Time.deltaTime);
            yield return null;
        }

        transform.position = targetWorldPos;
        currentGridPosition = targetGridPos;

        // 벽 상호작용 실행
        if (wallObjectHit != null)
        {
            wallObjectHit.GetComponent<WallBase>()?.ExecuteOnHit(this);
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
