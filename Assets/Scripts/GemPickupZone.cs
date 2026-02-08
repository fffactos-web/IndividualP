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
        if (!other.TryGetComponent(out Gem gem)) return;

        gem.FlyTo(transform, cc);
    }
}
