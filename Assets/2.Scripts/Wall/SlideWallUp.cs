using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SlideWallUp : WallBase
{
    public override void ExecuteOnHit(PlayerController player)
    {

    }

    public int GetRedirectDirection()
    {
        return 0; // Up
    }
}
