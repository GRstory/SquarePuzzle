using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SlideWallDown : WallBase
{
    public override void ExecuteOnHit(PlayerController player)
    {

    }

    public int GetRedirectDirection()
    {
        return 2; // Down
    }
}
