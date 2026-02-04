using DG.Tweening;
using UnityEngine;

public class Character_Properties : MonoBehaviour
{
    [SerializeField]
    GameObject[] dieEffect;
    [SerializeField]
    GameObject[] guns;

    [SerializeField]
    DiePanel diePanel;

    [SerializeField]
    UnityEngine.UI.Slider[] healthBars;

    [SerializeField]
    UnityEngine.UI.Slider[] healthBarForeground;

    [SerializeField]
    Transform gunHolder;
    [SerializeField]
    Transform camGunHolder;

    public float maxHealth;
    public float dmg;

    public float dmgMultiplier;
    public float attackSpeedMultiplier;
    public float critDmgMultiplier;

    public float maxHealthMultiplier;

    public float speedMultiplier;
    public float runSpeedMultiplier;

    float currentHealth;

    bool healthChanged;
    float timeWithoutHealthChanhges = 0f;

    public float difficulty = 1;
    public float kills;
    public float gems;

    public enum property
    {
        dmgMultiplier,
        attackSpeedMultiplier,
        critDmgMultiplier,

        maxHealthMultiplier,

        speedMultiplier,
        runSpeedMultiplier
    }

    private void Awake()
    {
        Instantiate(guns[0], camGunHolder);
        Instantiate(guns[0], gunHolder);

        ResetProperties();
        ChangeGunProperties();

        diePanel.gameObject.SetActive(false);

        foreach (var bar in healthBars)
        {
            bar.maxValue = maxHealth;
            bar.value = currentHealth;
        }
        foreach (var bar in healthBarForeground)
        {
            bar.maxValue = maxHealth;
            bar.value = currentHealth;
        }
    }

    public void ResetProperties()
    {
        maxHealth += maxHealthMultiplier;
        currentHealth = maxHealth;
    }

    public void ChangeGun(int id)
    {
        Destroy(gunHolder.GetChild(0));
        Instantiate(guns[id], gunHolder);

        Destroy(camGunHolder.GetChild(0));
        Instantiate(guns[id], camGunHolder);
    }

    public void ChangeGunProperties()
    {
        gunHolder.GetComponentInChildren<Gun>().critDmgMultiplier = critDmgMultiplier;
        gunHolder.GetComponentInChildren<Gun>().attackSpeedMultiplier = attackSpeedMultiplier;
        gunHolder.GetComponentInChildren<Gun>().dmgMultiplier = dmgMultiplier;
        gunHolder.GetComponentInChildren<Gun>().dmg = dmg;

        camGunHolder.GetComponentInChildren<Gun>().critDmgMultiplier = critDmgMultiplier;
        camGunHolder.GetComponentInChildren<Gun>().attackSpeedMultiplier = attackSpeedMultiplier;
        camGunHolder.GetComponentInChildren<Gun>().dmgMultiplier = dmgMultiplier;
        camGunHolder.GetComponentInChildren<Gun>().dmg = dmg;

    }

    public void ChangeProperty(float value, property property)
    {
        switch (property)
        {
            case property.dmgMultiplier:
                dmgMultiplier += value;
                break;
            case property.attackSpeedMultiplier:
                attackSpeedMultiplier += value;
                break;
            case property.maxHealthMultiplier:
                maxHealthMultiplier += value;
                break;
            case property.speedMultiplier:
                speedMultiplier += value;
                break;
            case property.critDmgMultiplier:
                critDmgMultiplier += value;
                break;
            default:
                break;
        }
    }

    private void Update()
    {
        timeWithoutHealthChanhges += Time.deltaTime;

        if(healthChanged && timeWithoutHealthChanhges > 1f)
        {
            foreach (var bar in healthBars)
            {
                bar.DOValue(currentHealth, 1f);
                timeWithoutHealthChanhges = 0f;
            }
        }
    }

    public void GetDamage(float damage)
    {
        currentHealth -= damage;
        healthChanged = true;
        foreach (var bar in healthBarForeground)
            bar.value = currentHealth;
        timeWithoutHealthChanhges = 0f;
        if (currentHealth <= 0)
            Die();
    }

    public void Die()
    {
        foreach (var effect in dieEffect)
            Instantiate(effect, transform.position + new Vector3(0, 1f, 0f), Quaternion.identity);
        ResetProperties();
        diePanel.showDiePanel();
        UnityEngine.Cursor.lockState = CursorLockMode.None;
        gameObject.SetActive(false);
    }
}
