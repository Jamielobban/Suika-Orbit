using UnityEngine;

public abstract class AimStrategy : ScriptableObject
{
    public abstract AimResult Evaluate(Vector2 muzzlePos, Vector2 pointerWorld, bool isAiming, int heldLevel);
}
