using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapSolver : MonoBehaviour
{
    private int[,] _mapDataArray;
    private Vector2Int _startNode;
    private Vector2Int _endNode;

    private readonly int[] _dx = { 0, 1, 0, -1 };
    private readonly int[] _dy = { 1, 0, -1, 0 };

    public bool ValidateMap(int[,]map, Vector2Int startPos, Vector2Int endPos)
    {
        _mapDataArray = map;
        _startNode = startPos;
        _endNode = endPos;

        Queue<Vector2Int> nodeQueue = new Queue<Vector2Int>();
        HashSet<(Vector2Int, int)> visitedPath = new HashSet<(Vector2Int, int)>();

        nodeQueue.Enqueue(startPos);
        while (nodeQueue.Count > 0)
        {
            Vector2Int currentNode = nodeQueue.Dequeue();

            for (int i = 0; i < 4; i++)
            {
                if (visitedPath.Contains((currentNode, i))) continue;

                visitedPath.Add((currentNode, i));
                Vector2Int next = currentNode;
                Vector2Int target = currentNode;

                while (true)
                {
                    next.x += _dx[i];
                    next.y += _dy[i];

                    if (next.x < -20 || next.y < -20 || next.x > 20 || next.y > 20) break;
                    int temptileType = _mapDataArray[next.y, next.x];
                    if (temptileType == 1) break;
                    else if (temptileType == 2) return true;

                    target = next;
                }

                nodeQueue.Enqueue(target);
            }
        }

        return false;
    }
}
