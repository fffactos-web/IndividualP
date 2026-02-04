using UnityEngine;

public class GemPickupZone : MonoBehaviour
{
    private IGemCollector collector;

    private void Awake()
    {
        collector = GetComponentInParent<IGemCollector>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.TryGetComponent(out Gem gem)) return;

        gem.FlyTo(transform, collector);
    }
}
