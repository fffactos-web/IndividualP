using DG.Tweening;
using UnityEngine;

public class DOTweenManager : MonoBehaviour
{
    public static DOTweenManager Instance;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            DOTween.Init(false, false, LogBehaviour.Default);
        }
        else Destroy(gameObject);
    }

    public static void SafeKill(GameObject target)
    {
        if (target != null)
            DOTween.Kill(target);
    }
}
