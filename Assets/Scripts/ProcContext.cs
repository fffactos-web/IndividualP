using UnityEngine;

// Lightweight data passed from hit event to effect actions.
public struct ProcContext
{
    public float damageDone;
    public bool isCrit;
    public int hitLayer;

    public bool targetWasKilled;
    public Vector3 hitPosition;
}
