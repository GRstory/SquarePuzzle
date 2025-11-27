using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BreakableWall : WallBase
{
    [SerializeField] private int wallId = -1;
    private bool isBroken = false;

    public void SetWallId(int id)
    {
        wallId = id;
    }

    public int GetWallId()
    {
        return wallId;
    }

    public override void ExecuteOnHit(PlayerController player)
    {
        if (!isBroken)
        {
            isBroken = true;
            // Destroy the wall game object
            Destroy(gameObject);
        }
    }

    public bool IsBroken()
    {
        return isBroken;
    }
}
