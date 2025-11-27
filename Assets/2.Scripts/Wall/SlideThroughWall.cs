using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SlideThroughWall : WallBase
{
    [SerializeField] private int wallId = -1;
    [SerializeField] private int targetWallId = -1;
    
    public void SetWallId(int id)
    {
        wallId = id;
    }

    public int GetWallId()
    {
        return wallId;
    }

    public void SetTargetWallId(int id)
    {
        targetWallId = id;
    }

    public int GetTargetWallId()
    {
        return targetWallId;
    }

    public override void ExecuteOnHit(PlayerController player)
    {
        // This wall doesn't stop the player, it teleports them
        // The actual teleportation logic will be handled in PlayerController
    }
}
