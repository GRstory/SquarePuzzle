using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SlideWallRight : WallBase
{
    public override void ExecuteOnHit(PlayerController player)
    {
        // This wall redirects player to the right
        // The actual redirection logic is handled in PlayerController
    }

    public int GetRedirectDirection()
    {
        return 1; // Right
    }
}
