using UnityEngine;

public class PoolManager : MonoBehaviour
{
    public static PoolManager I;

    public ObjectPool followerZombiePool;
    public ObjectPool deathEffectPool;
    public ObjectPool shotEffectPool;
    public ObjectPool popupPool;
    public ObjectPool pierceShotPool;
    public ObjectPool rocketsPool;
    public ObjectPool explosionPool;

    void Awake()
    {
        if (I == null) I = this;
        else Destroy(gameObject);
    }
}
