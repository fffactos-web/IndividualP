using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class Gun : MonoBehaviour
{
    [Header("Weapon")]
    public float fireRate = 2f;
    public float dmg = 4f;

    [Header("Combat stats")]
    public float attackSpeedMultiplier = 1f;
    public float critChance = 0.1f;
    public float critDmgMultiplier = 2f;
    public float armorPenetration;
    public float globalDamageMultiplier = 1f;
    public float statusChance;
    public float statusDuration = 1f;
    public float procChance;
    public float procPower = 1f;
    public int procCount = 1;

    [Header("Ability stats")]
    public float radius = 5f;
    public float skillRangeMultiplier = 1f;
    public float abilityHitboxSize = 1f;
    public float cooldownReduction;
    public float castSpeedMultiplier = 1f;

    [Header("Effects")]
    public bool isPiercing;

    [Header("Visual")]
    [SerializeField] Transform firePoint;
    [SerializeField] float cdOffset = 1f;

    [Header("Piercing Visual")]
    [SerializeField] LineRenderer pierceLine;
    [SerializeField] float pierceVisualTime = 0.07f;
    Tween pierceTween;

    [Header("Raycast")]
    [SerializeField] LayerMask hitMask = ~0;

    RectTransform crosshair;
    Character_Properties owner;
    Slider visualCooldown;
    Tween cooldownTween;

    float CooldownDuration => (1f / Mathf.Max(0.05f, fireRate)) / Mathf.Max(0.05f, attackSpeedMultiplier) * (1f - Mathf.Clamp(cooldownReduction, 0f, 0.8f)) / Mathf.Max(0.1f, castSpeedMultiplier);


    public enum Modifiers
    {
        nothing,
        piercing,
        explosing
    }
    public Modifiers modifiers;

    private void Start()
    {
        crosshair = GameObject.FindGameObjectWithTag("Crosshair")
            .GetComponent<RectTransform>();

        visualCooldown = GameObject.FindGameObjectWithTag("VisualCooldown")
            .GetComponent<Slider>();

        visualCooldown.minValue = 0f;
        visualCooldown.maxValue = 1f;

        owner = GetComponentInParent<Character_Properties>();

        LayerMask uiMask = LayerMask.GetMask("UI");
        if (uiMask != 0)
            hitMask &= ~uiMask;
    }

    private void Update()
    {
        if (Input.GetMouseButton(0) && CanShoot())
        {
            switch (modifiers)
            {
                case Modifiers.nothing:
                    Shoot();
                    break;
                case Modifiers.piercing:
                    PierceShot();
                    break;
                case Modifiers.explosing:
                    LaunchRocket();
                    break;
            }
        }
    }

    bool CanShoot()
    {
        return visualCooldown.value >= 1f;
    }

    void Shoot()
    {
        StartCooldown();

        PoolManager.I.shotEffectPool
            .Spawn(firePoint.position, firePoint.rotation);

        Ray camRay = Camera.main.ScreenPointToRay(crosshair.position);
        if (Physics.Raycast(camRay, out RaycastHit hit, 10000f * skillRangeMultiplier, hitMask, QueryTriggerInteraction.Ignore))
        {
            Zombie_Head head = hit.collider.GetComponent<Zombie_Head>();
            Zombie_Properies zombie = hit.collider.GetComponent<Zombie_Properies>();
            if (head != null)
                DealDamage(head.zombieProperies, BuildHitData(true));
            else if (zombie != null)
                DealDamage(zombie, BuildHitData(false));
        }
    }

    void PierceShot()
    {
        StartCooldown();

        PoolManager.I.shotEffectPool
            .Spawn(firePoint.position, firePoint.rotation);

        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(Camera.main, crosshair.position);
        Ray ray = Camera.main.ScreenPointToRay(screenPoint);

        RaycastHit[] hits = Physics.RaycastAll(ray, 10000f * skillRangeMultiplier, hitMask, QueryTriggerInteraction.Ignore);
        Vector3 endPoint = firePoint.position + ray.direction * 60f;

        if (hits.Length > 0)
        {
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            float currentDamage = dmg;
            foreach (var hit in hits)
            {
                Zombie_Head head = hit.collider.GetComponentInParent<Zombie_Head>();
                Zombie_Properies zombie = hit.collider.GetComponentInParent<Zombie_Properies>();
                if (head != null)
                {
                    var hd = BuildHitData(true);
                    hd.rawDamage = currentDamage;
                    DealDamage(head.zombieProperies, hd);
                }
                else if (zombie != null)
                {
                    var hd = BuildHitData(false);
                    hd.rawDamage = currentDamage;
                    DealDamage(zombie, hd);
                }
                else
                {
                    endPoint = hit.point;
                    break;
                }

                endPoint = hit.point;
                currentDamage *= 0.8f;
                if (currentDamage < 1f)
                    break;
            }
        }

        DrawPierceLine(firePoint.position, endPoint);
    }

    void LaunchRocket()
    {
        StartCooldown();
        Rocket rocket = PoolManager.I.rocketsPool.Spawn(firePoint.position, firePoint.rotation).GetComponent<Rocket>();
        rocket.dmg = dmg * globalDamageMultiplier;
        rocket.radius = radius * abilityHitboxSize;
    }

    Zombie_Properies.HitData BuildHitData(bool isHeadshot)
    {
        float missingHealthBonus = 1f;
        if (owner != null)
        {
            var ownerStats = owner.GetStats();
            float hpRatio = owner.GetCurrentHealthRatio();
            missingHealthBonus += ownerStats.missingHealthDamage * (1f - hpRatio);
            if (hpRatio <= 0.35f)
                missingHealthBonus += ownerStats.lowHealthPower;
        }

        bool isCrit = Random.value <= critChance;
        float critMultiplier = isCrit ? critDmgMultiplier : 1f;
        if (isHeadshot)
            critMultiplier *= critDmgMultiplier;

        return new Zombie_Properies.HitData
        {
            rawDamage = dmg * globalDamageMultiplier * missingHealthBonus,
            armorPenetration = armorPenetration,
            critMultiplier = critMultiplier,
            statusChance = statusChance,
            statusDuration = statusDuration,
            procChance = procChance,
            procPower = procPower,
            procCount = procCount
        };
    }

    Zombie_Properies.HitData BuildHitData(bool isHeadshot)
    {
        float missingHealthBonus = 1f;
        if (owner != null)
        {
            var ownerStats = owner.GetStats();
            float hpRatio = owner.GetCurrentHealthRatio();
            missingHealthBonus += ownerStats.missingHealthDamage * (1f - hpRatio);
            if (hpRatio <= 0.35f)
                missingHealthBonus += ownerStats.lowHealthPower;
        }

        bool isCrit = Random.value <= critChance;
        float critMultiplier = isCrit ? critDmgMultiplier : 1f;
        if (isHeadshot)
            critMultiplier *= critDmgMultiplier;

        return new Zombie_Properies.HitData
        {
            rawDamage = dmg * globalDamageMultiplier * missingHealthBonus,
            armorPenetration = armorPenetration,
            critMultiplier = critMultiplier,
            statusChance = statusChance,
            statusDuration = statusDuration,
            procChance = procChance,
            procPower = procPower,
            procCount = procCount
        };
    }

    void DealDamage(Zombie_Properies zombie, Zombie_Properies.HitData hitData)
    {
        float dealt = zombie.TakeHit(hitData);
        if (owner != null)
        {
            float heal = dealt * Mathf.Max(0f, owner.GetStats().lifesteal);
            owner.Heal(heal);
        }
    }

    void DealDamage(Zombie_Properies zombie, Zombie_Properies.HitData hitData)
    {
        float dealt = zombie.TakeHit(hitData);
        if (owner != null)
        {
            float heal = dealt * Mathf.Max(0f, owner.GetStats().lifesteal);
            owner.Heal(heal);
        }
    }

    void DrawPierceLine(Vector3 start, Vector3 end)
    {
        GameObject pierceShot = PoolManager.I.pierceShotPool.Spawn(start, firePoint.rotation);
        LineRenderer line = pierceShot.GetComponent<LineRenderer>();

        line.positionCount = 2;
        line.useWorldSpace = true;
        line.SetPosition(0, start);
        line.SetPosition(1, end);

        Color startColor = line.startColor;
        Color endColor = line.endColor;
        startColor.a = 1f;
        endColor.a = 1f;
        line.startColor = startColor;
        line.endColor = endColor;

        DOVirtual.Float(1f, 0f, 2f, a =>
        {
            startColor.a = a;
            endColor.a = a;
            line.startColor = startColor;
            line.endColor = endColor;
        })
        .OnComplete(() => { PoolManager.I.pierceShotPool.Despawn(pierceShot); });
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
