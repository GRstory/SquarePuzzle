using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class WallBase : MonoBehaviour
{
    public abstract void ExecuteOnHit(PlayerController player);
}
