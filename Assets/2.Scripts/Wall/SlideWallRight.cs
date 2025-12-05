using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SlideWallRight : WallBase
{
    public override void ExecuteOnHit(PlayerController player)
    {

    }

    public int GetRedirectDirection()
    {
        return 1; // Right
    }
}
