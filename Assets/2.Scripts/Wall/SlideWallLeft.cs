using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SlideWallLeft : WallBase
{
    public override void ExecuteOnHit(PlayerController player)
    {
        // This wall redirects player to the left
        // The actual redirection logic is handled in PlayerController
    }

    public int GetRedirectDirection()
    {
        return 3; // Left
    }
}
