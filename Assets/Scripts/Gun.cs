using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class Gun : MonoBehaviour
{
    [Header("Weapon")]
    public float fireRate = 2f;
    public float dmg = 4f;

    [Header("Multipliers")]
    public float dmgMultiplier = 1f;
    public float attackSpeedMultiplier = 1f;
    public float critDmgMultiplier = 2f;
    public float radius = 5f;

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
    Slider visualCooldown;
    Tween cooldownTween;

    float CooldownDuration => fireRate / attackSpeedMultiplier;

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
                default:
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

        Vector2 screenPoint =
            RectTransformUtility.WorldToScreenPoint(Camera.main, crosshair.position);
        Ray camRay = Camera.main.ScreenPointToRay(screenPoint);
        if (Physics.Raycast(camRay, out RaycastHit hit, 10000f, hitMask, QueryTriggerInteraction.Ignore))
        {
            Zombie_Head head = hit.collider.GetComponentInParent<Zombie_Head>();
            Zombie_Properies zombie = hit.collider.GetComponentInParent<Zombie_Properies>();
            if (head != null)
            {
                if (head.zombieProperies.currentHealth - dmg * dmgMultiplier * critDmgMultiplier > 0)
                    head.zombieProperies.GetDamage(dmg * dmgMultiplier * critDmgMultiplier);
                else
                    head.zombieProperies.GetDamage(head.zombieProperies.currentHealth);
            }
            else if (zombie != null)
            {
                if (zombie.currentHealth - dmg * dmgMultiplier > 0)
                    zombie.GetDamage(dmg * dmgMultiplier);
                else
                    zombie.GetDamage(zombie.currentHealth);
            }
        }
    }
    void PierceShot()
    {
        StartCooldown();

        PoolManager.I.shotEffectPool
            .Spawn(firePoint.position, firePoint.rotation);

        Vector2 screenPoint =
            RectTransformUtility.WorldToScreenPoint(Camera.main, crosshair.position);
        Ray ray = Camera.main.ScreenPointToRay(screenPoint);

        RaycastHit[] hits = Physics.RaycastAll(ray, 10000f, hitMask, QueryTriggerInteraction.Ignore);

        Vector3 endPoint =
            firePoint.position + ray.direction * 60f; // åñëè ïóñòî

        if (hits.Length > 0)
        {
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            float currentDamage = dmg * dmgMultiplier;

            foreach (var hit in hits)
            {
                Zombie_Head head = hit.collider.GetComponentInParent<Zombie_Head>();
                Zombie_Properies zombie = hit.collider.GetComponentInParent<Zombie_Properies>();
                if (head != null)
                {
                    DealDamage(head.zombieProperies, currentDamage * critDmgMultiplier);
                }
                else if (zombie != null)
                {
                    DealDamage(zombie, currentDamage);
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
        rocket.dmg = dmg;
        rocket.radius = radius;
    }

    void DealDamage(Zombie_Properies zombie, float damage)
    {
        float dmgToDeal = Mathf.Min(damage, zombie.currentHealth);
        zombie.GetDamage(dmgToDeal);
    }

    void DrawPierceLine(Vector3 start, Vector3 end)
    {
        GameObject pierceShot =
            PoolManager.I.pierceShotPool.Spawn(start, firePoint.rotation);

        LineRenderer line = pierceShot.GetComponent<LineRenderer>();

        line.positionCount = 2;
        line.useWorldSpace = true;
        line.SetPosition(0, start);
        line.SetPosition(1, end);

        // === FADE ===
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
        .OnComplete(() =>
        {
            PoolManager.I.pierceShotPool.Despawn(pierceShot);
        });
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
