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
            gameObject.SetActive(false);
            Destroy(gameObject, 0.1f);
        }
    }

    public bool IsBroken()
    {
        return isBroken;
    }
}
