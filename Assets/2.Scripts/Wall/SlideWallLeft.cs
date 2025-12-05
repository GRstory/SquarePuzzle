using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SlideWallLeft : WallBase
{
    public override void ExecuteOnHit(PlayerController player)
    {

    }

    public int GetRedirectDirection()
    {
        return 3; // Left
    }
}
