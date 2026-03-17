using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RecentRewardsFeedUI : MonoBehaviour
{
    public static RecentRewardsFeedUI I;

    [SerializeField] RectTransform messagesRoot;
    [SerializeField] TextMeshProUGUI messagePrefab;
    [SerializeField] float verticalStep = 36f;
    [SerializeField] float shiftDuration = 0.2f;
    [SerializeField] float visibleDuration = 3f;
    [SerializeField] float fadeDuration = 1f;
    [SerializeField] int maxMessages = 6;

    readonly List<FeedMessage> messages = new List<FeedMessage>();

    class FeedMessage
    {
        public RectTransform Rect;
        public CanvasGroup Group;
        public Coroutine LifeRoutine;
    }

    void Awake()
    {
        I = this;
    }

    public void ShowModifier(HeroStatModifier modifier)
    {
        PushMessage(BuildModifierText(modifier));
    }

    public void ShowItem(HeroItemDefinition item)
    {
        if (item == null)
            return;

        PushMessage($"Получен предмет: {item.ItemName}");
    }

    public void PushMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message) || messagePrefab == null || messagesRoot == null)
            return;

        bool useVerticalLayout = messagesRoot.GetComponent<VerticalLayoutGroup>() != null;

        if (!useVerticalLayout)
        {
            for (int i = 0; i < messages.Count; i++)
            {
                FeedMessage existing = messages[i];
                if (existing?.Rect == null)
                    continue;

                existing.Rect.DOKill();
                existing.Rect.DOAnchorPos(existing.Rect.anchoredPosition + Vector2.up * verticalStep, shiftDuration).SetUpdate(true);
            }
        }

        TextMeshProUGUI label = Instantiate(messagePrefab, messagesRoot);
        label.text = message;

        RectTransform rect = label.rectTransform;

        if (useVerticalLayout)
        {
            rect.SetAsFirstSibling();
            LayoutRebuilder.ForceRebuildLayoutImmediate(messagesRoot);
        }
        else
        {
            rect.anchoredPosition = Vector2.zero;
        }

        CanvasGroup group = label.GetComponent<CanvasGroup>();
        if (group == null)
            group = label.gameObject.AddComponent<CanvasGroup>();
        group.alpha = 1f;

        FeedMessage entry = new FeedMessage
        {
            Rect = rect,
            Group = group
        };

        messages.Insert(0, entry);
        entry.LifeRoutine = StartCoroutine(FadeAndRemove(entry));

        if (messages.Count > maxMessages)
            RemoveEntry(messages[messages.Count - 1]);
    }

    IEnumerator FadeAndRemove(FeedMessage entry)
    {
        yield return new WaitForSecondsRealtime(visibleDuration);

        if (entry?.Group != null)
        {
            entry.Group.DOKill();
            entry.Group.DOFade(0f, fadeDuration).SetUpdate(true);
            yield return new WaitForSecondsRealtime(fadeDuration);
        }

        RemoveEntry(entry);
    }

    void RemoveEntry(FeedMessage entry)
    {
        if (entry == null)
            return;

        if (entry.LifeRoutine != null)
            StopCoroutine(entry.LifeRoutine);

        messages.Remove(entry);

        if (entry.Rect != null)
        {
            entry.Rect.DOKill();
            Destroy(entry.Rect.gameObject);
        }
    }

    string BuildModifierText(HeroStatModifier modifier)
    {
        string statName;
        switch (modifier.stat)
        {
            case HeroStatType.Damage: statName = "Урон"; break;
            case HeroStatType.AttackSpeed: statName = "Скорость атаки"; break;
            case HeroStatType.CritChance: statName = "Шанс крита"; break;
            case HeroStatType.CritDamageMultiplier: statName = "Крит. урон"; break;
            case HeroStatType.ArmorPenetration: statName = "Пробитие брони"; break;
            case HeroStatType.GlobalDamageMultiplier: statName = "Множитель урона"; break;
            case HeroStatType.GlobalAttackSpeed: statName = "Глобальная скорость атаки"; break;
            case HeroStatType.MaxHealth: statName = "Макс. здоровье"; break;
            case HeroStatType.HealthRegen: statName = "Регенерация"; break;
            case HeroStatType.Shield: statName = "Щит"; break;
            case HeroStatType.MaxShield: statName = "Макс. щит"; break;
            case HeroStatType.Armor: statName = "Броня"; break;
            case HeroStatType.Resistance: statName = "Сопротивление"; break;
            case HeroStatType.MoveSpeed: statName = "Скорость движения"; break;
            case HeroStatType.MaxStamina: statName = "Макс. выносливость"; break;
            case HeroStatType.DashSpeed: statName = "Скорость рывка"; break;
            case HeroStatType.JumpCount: statName = "Прыжки"; break;
            case HeroStatType.AirControl: statName = "Контроль в воздухе"; break;
            case HeroStatType.GlobalAcceleration: statName = "Ускорение"; break;
            case HeroStatType.ProcChance: statName = "Шанс эффекта"; break;
            case HeroStatType.ProcPower: statName = "Сила эффекта"; break;
            case HeroStatType.Luck: statName = "Удача"; break;
            case HeroStatType.AttackRadius: statName = "Радиус атаки"; break;
            case HeroStatType.Expirience: statName = "Опыт"; break;
            case HeroStatType.Gold: statName = "Золото"; break;
            default: statName = modifier.stat.ToString(); break;
        }

        string sign = modifier.value >= 0f ? "+" : "";
        return string.Format("{0} {1}{2:0.#}", statName, sign, modifier.value);
    }

}
