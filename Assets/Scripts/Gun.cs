using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class Gun : MonoBehaviour
{
    [Header("Weapon")]
    public float fireRate = 2f;

    [Header("Visual")]
    [SerializeField] Transform firePoint;
    [SerializeField] float cdOffset = 1f;

    [Header("Raycast")]
    [SerializeField] LayerMask hitMask = ~0;

    RectTransform crosshair;
    Slider visualCooldown;
    Tween cooldownTween;

    Character_Properties owner;
    public EffectData effectData;

    float CooldownDuration =>
        fireRate / Mathf.Max(0.01f, owner.attackSpeed);

    public void SetOwner(Character_Properties character)
    {
        owner = character;
    }

    void Start()
    {
        crosshair = GameObject.FindGameObjectWithTag("Crosshair")
            .GetComponent<RectTransform>();

        visualCooldown = GameObject.FindGameObjectWithTag("VisualCooldown")
            .GetComponent<Slider>();

        visualCooldown.minValue = 0f;
        visualCooldown.maxValue = 1f;

        LayerMask uiMask = LayerMask.GetMask("UI");
        if (uiMask != 0)
            hitMask &= ~uiMask;
    }

    void Update()
    {
        if (Input.GetMouseButton(0) && visualCooldown.value >= 1f)
            Shoot();
    }

    void Shoot()
    {
        StartCooldown();

        PoolManager.I.shotEffectPool
            .Spawn(firePoint.position, firePoint.rotation);

        Ray ray = Camera.main.ScreenPointToRay(crosshair.position);

        if (!Physics.Raycast(ray, out RaycastHit hit, 10000f, hitMask))
            return;

        var zombie = hit.collider.GetComponentInParent<Zombie_Properies>();
        if (zombie == null) return;

        bool isCrit = Random.value < owner.critChance;
        float damage = owner.damage;

        if (isCrit)
            damage *= owner.critDamage;

        float dealt = Mathf.Min(damage, zombie.currentHealth);
        zombie.TakeDamage(dealt, effectData, isCrit, owner, ProcDamageType.Direct);


        // lifesteal
        if (owner.lifeSteal > 0f)
            owner.currentHealth =
                Mathf.Min(owner.maxHealth, owner.currentHealth + dealt * owner.lifeSteal);
    }

    void StartCooldown()
    {
        cooldownTween?.Kill();
        visualCooldown.value = 0f;

        cooldownTween = visualCooldown
            .DOValue(1f, CooldownDuration * cdOffset)
            .SetEase(Ease.Linear);
    }
}
