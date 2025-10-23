using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GoalWall : WallBase
{
    public override void ExecuteOnHit(PlayerController player)
    {
        UI_HUD.Instance.FinishGame();
    }
}
