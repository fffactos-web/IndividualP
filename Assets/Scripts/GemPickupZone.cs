using UnityEngine;

public class GemPickupZone : MonoBehaviour
{

    Character_Properties cc;
    private void Awake()
    {
        cc = GetComponentInParent<Character_Properties>();
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out Gem gem))
            gem.FlyTo(transform, cc);
        if (other.TryGetComponent(out Expirience exp))
            exp.FlyTo(transform, cc);
    }
}
