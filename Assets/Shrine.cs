using UnityEngine;

public class Shrine : MonoBehaviour
{
    [SerializeField] private Material material;
    [SerializeField] private float timeToComplete = 3f;
    [SerializeField] ShrineType type;

    private float progress;
    private bool isPlayerInside;

    Character_Properties character_properties;

    private static readonly Color baseColor = new Color(0.3677607f, 0.3302563f, 0.8679245f, 1f);

    enum ShrineType
    {
        Russian,
        Math,
        Physics,
        Literature,
        Chemistry,
        History
    }

    void Update()
    {
        if (isPlayerInside)
            progress += Time.deltaTime / timeToComplete;
        else
            progress -= Time.deltaTime / timeToComplete;

        progress = Mathf.Clamp01(progress);

        UpdateVisual();

        // Завершение
        if (progress >= 1f)
        {
            switch (type)
            {
                case ShrineType.Russian:
                    character_properties.ApplyModifier(new HeroStatModifier { stat = HeroStatType.CritDamageMultiplier, value = Random.Range(5, 15) + (int)character_properties.GetStats().luck / 2 });
                    break;
                case ShrineType.Math:
                    character_properties.ApplyModifier(new HeroStatModifier { stat = HeroStatType.Damage, value = Random.Range(5, 15) + (int)character_properties.GetStats().luck / 2 });
                    break;
                case ShrineType.Physics:
                    character_properties.ApplyModifier(new HeroStatModifier { stat = HeroStatType.AttackSpeed, value = Random.Range(5, 15) + (int)character_properties.GetStats().luck / 2 });
                    break;
                case ShrineType.Literature:
                    character_properties.ApplyModifier(new HeroStatModifier { stat = HeroStatType.Gold, value = Random.Range(5, 15) + (int)character_properties.GetStats().luck / 2 });
                    break;
                case ShrineType.Chemistry:
                    character_properties.ApplyModifier(new HeroStatModifier { stat = HeroStatType.HealthRegen, value = Random.Range(5, 15) + (int)character_properties.GetStats().luck / 2 });
                    break;
                case ShrineType.History:
                    character_properties.ApplyModifier(new HeroStatModifier { stat = HeroStatType.Expirience, value = Random.Range(5, 15) + (int)character_properties.GetStats().luck / 2 });
                    break;
                default:
                    break;
            }
            
        }
    }

    private void UpdateVisual()
    {
        float alpha = progress * 0.8f;
        material.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);

        float scale = 30f + progress * 10f;
        transform.localScale = Vector3.one * scale;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player") && other.gameObject.GetComponent<Character_Properties>() != null)
            character_properties = other.gameObject.GetComponent<Character_Properties>();
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInside = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInside = false;
        }
    }
}