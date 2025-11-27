using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SlideWallDown : WallBase
{
    public override void ExecuteOnHit(PlayerController player)
    {
        // This wall redirects player downward
        // The actual redirection logic is handled in PlayerController
    }

    public int GetRedirectDirection()
    {
        return 2; // Down
    }
}
