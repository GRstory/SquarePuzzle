using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SlideWallUp : WallBase
{
    public override void ExecuteOnHit(PlayerController player)
    {
        // This wall redirects player upward
        // The actual redirection logic is handled in PlayerController
    }

    public int GetRedirectDirection()
    {
        return 0; // Up
    }
}
