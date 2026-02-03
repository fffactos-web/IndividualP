using DG.Tweening;
using TMPro;
using UnityEngine;

public class DamagePopup : MonoBehaviour, IPoolable
{
    [Header("Lifetime")]
    [SerializeField] float lifeTime = 1.2f;
    [SerializeField] float moveUp = 1f;

    [Header("Visual")]
    [SerializeField] float maxVisualDamage = 300f;
    [SerializeField] float minScale = 1f;
    [SerializeField] float maxScale = 2.2f;
    [SerializeField] Color lowDamageColor = Color.yellow;
    [SerializeField] Color highDamageColor = Color.red;

    TextMeshPro text;
    Transform target;

    float totalDamage;
    bool finishing;

    Tween moveTween;
    Tween fadeTween;
    Tween punchTween;

    static Camera cachedCamera;

    // ===== UNITY =====

    void Awake()
    {
        text = GetComponent<TextMeshPro>();

        if (!cachedCamera)
            cachedCamera = Camera.main;
    }

    void LateUpdate()
    {
        // billboard ВСЕГДА
        if (cachedCamera)
            transform.forward = cachedCamera.transform.forward;

        // следуем за целью, только если она есть
        if (target)
            transform.position = target.position + Vector3.up * 5f;
    }

    // ===== POOL =====

    public void OnSpawn()
    {
        finishing = false;
        totalDamage = 0;

        target = null;

        text.alpha = 1f;
        transform.localScale = Vector3.one;
    }

    public void OnDespawn()
    {
        KillTweens();
        target = null;
    }

    // ===== API =====

    public void Attach(Transform targetTransform)
    {
        target = targetTransform;
        transform.position = target.position + Vector3.up * 5f;
    }

    public void AddDamage(float damage)
    {
        if (finishing)
            return;

        totalDamage += damage;

        UpdateVisuals();
        RestartLife();

        punchTween?.Kill();
        punchTween = transform.DOPunchScale(Vector3.one * 0.25f, 0.15f);
    }

    public void DetachAndFinish()
    {
        if (finishing)
            return;

        finishing = true;
        target = null;

        if (fadeTween == null || !fadeTween.IsActive())
            RestartLife();
    }

    // ===== INTERNAL =====

    void UpdateVisuals()
    {
        text.text = totalDamage.ToString();

        float t = Mathf.Clamp01(totalDamage / maxVisualDamage);

        transform.localScale = Vector3.one * Mathf.Lerp(minScale, maxScale, t);
        text.color = Color.Lerp(lowDamageColor, highDamageColor, t);
    }

    void RestartLife()
    {
        KillTweens();

        moveTween = transform.DOMoveY(transform.position.y + moveUp, lifeTime).SetEase(Ease.OutQuad);

        fadeTween = text.DOFade(0f, lifeTime).OnComplete(ReturnToPool);
    }

    void ReturnToPool()
    {
        PoolManager.I.popupPool.Despawn(gameObject);
    }

    void KillTweens()
    {
        moveTween?.Kill();
        fadeTween?.Kill();
        punchTween?.Kill();

        moveTween = null;
        fadeTween = null;
        punchTween = null;
    }
}
