using System;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.TextCore.Text;

public class Zombie_Properies : MonoBehaviour, IPoolable
{
    [Header("Health")]
    public float maxHealth;
    public float currentHealth;

    [Header("Damage popup")]
    [SerializeField] float stackResetTime = 0.5f;
    DamagePopup damagePopup;
    float lastDamageTime;

    public MobSpawner spawner;
    public GameObject dieEffect;

    Quaternion initialLocalRotation;
    Vector3 initialLocalPosition;

    TextMeshProUGUI gemStatus;
    TextMeshProUGUI killsStatus;

    Character_Properties character;

    void Awake()
    {
        gemStatus = GameObject.FindGameObjectWithTag("Gem Status").GetComponent<TextMeshProUGUI>();
        killsStatus = GameObject.FindGameObjectWithTag("Kills Status").GetComponent<TextMeshProUGUI>();
        character = GameObject.FindGameObjectWithTag("Player").GetComponent<Character_Properties>();
        initialLocalRotation = transform.localRotation;
        initialLocalPosition = transform.localPosition;
    }

    // ===== POOL =====

    public void OnSpawn()
    {
        ResetProperties();
        damagePopup = null;
        lastDamageTime = 0f;
    }

    public void OnDespawn()
    {
        if (damagePopup != null)
        {
            PoolManager.I.popupPool.Despawn(damagePopup.gameObject);
            damagePopup = null;
        }
        ResetProperties();
    }

    // ===== HEALTH =====

    void ResetProperties()
    {
        currentHealth = maxHealth;
        transform.localRotation = initialLocalRotation;
        transform.localPosition = initialLocalPosition;
    }

    public void GetDamage(float damage)
    {
        currentHealth -= damage;

        ShowDamage(damage);

        if (currentHealth <= 0)
            Die();
    }

    // ===== DAMAGE STACK =====

    void ShowDamage(float damage)
    {
        if (PoolManager.I == null || PoolManager.I.popupPool == null)
        {
            Debug.LogError("PopupPool is NULL");
            return;
        }

        if (damagePopup == null || Time.time - lastDamageTime > stackResetTime)
        {
            var go = PoolManager.I.popupPool.Spawn(
                transform.position,
                Quaternion.identity
            );

            if (go == null)
            {
                Debug.LogError("Spawn returned NULL");
                return;
            }

            damagePopup = go.GetComponent<DamagePopup>();

            if (damagePopup == null)
            {
                Debug.LogError("DamagePopup component NOT FOUND on prefab");
                return;
            }

            damagePopup.Attach(transform);
        }

        damagePopup.AddDamage(damage);
        lastDamageTime = Time.time;
    }


    // ===== DEATH =====

    void Die()
    {
        if (damagePopup != null)
        {
            damagePopup.DetachAndFinish();
            damagePopup = null;
        }


        character.kills++;
        int gemPlus = Convert.ToInt32(UnityEngine.Random.Range(5, 20));
        character.gems += gemPlus;
        killsStatus.text = (character.kills + 1).ToString();
        gemStatus.text = (character.gems + gemPlus).ToString();
        ResetProperties();

        PoolManager.I.deathEffectPool.Spawn(
            transform.position + Vector3.up * 0.8f,
            Quaternion.identity
        );

        // Zombie_Properies lives on a child of the pooled prefab. We must return the pooled root object.
        PoolManager.I.followerZombiePool.Despawn(transform.root.gameObject);
    }

}
