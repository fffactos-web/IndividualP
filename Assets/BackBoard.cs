using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class BackBoard : MonoBehaviour
{
    private const int MaxLevel = 6;

    [SerializeField] private Button[] answers;
    [SerializeField] private TextMeshProUGUI exercise;

    private int level = 1;
    private int correctAnswer;
    private bool answerLocked;

    private Character_Properties playerProperties;
    private UnityAction[] answerHandlers;

    private void Awake()
    {
        Canvas canvas = GetComponent<Canvas>();
        if (canvas != null)
            canvas.worldCamera = Camera.main;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            playerProperties = player.GetComponent<Character_Properties>();
    }

    private void OnEnable()
    {
        answerHandlers = new UnityAction[answers.Length];

        for (int i = 0; i < answers.Length; i++)
        {
            Button button = answers[i];
            UnityAction handler = () => OnAnswerClicked(button);
            answerHandlers[i] = handler;
            button.onClick.AddListener(handler);
        }

        StartChallenge();
    }

    private void OnDisable()
    {
        if (answerHandlers == null)
            return;

        for (int i = 0; i < answers.Length; i++)
        {
            if (answerHandlers[i] == null)
                continue;

            answers[i].onClick.RemoveListener(answerHandlers[i]);
        }
    }

    private void StartChallenge()
    {
        answerLocked = false;
        SetAnswersInteractable(true);

        GenerateExercise();
        FillAnswers();
    }

    private void GenerateExercise()
    {
        switch (level)
        {
            case 1:
            {
                int a = UnityEngine.Random.Range(100, 999);
                int b = UnityEngine.Random.Range(100, 999);
                correctAnswer = a + b;
                exercise.text = $"{a} + {b} =";
                break;
            }
            case 2:
            {
                int a = UnityEngine.Random.Range(100, 200);
                int b = UnityEngine.Random.Range(3, 6);
                correctAnswer = a * b;
                exercise.text = $"{a} * {b} =";
                break;
            }
            case 3:
            {
                int a = UnityEngine.Random.Range(11, 30);
                correctAnswer = a * a;
                exercise.text = $"{a}^2 =";
                break;
            }
            case 4:
            {
                int a = UnityEngine.Random.Range(11, 30);
                int square = a * a;
                correctAnswer = a + square;
                exercise.text = $"{a} + {a}^2 =";
                break;
            }
            case 5:
            {
                int divisor = UnityEngine.Random.Range(100, 200);
                int multiplier = UnityEngine.Random.Range(3, 6);
                int dividend = divisor * multiplier;
                correctAnswer = dividend / divisor;
                exercise.text = $"{dividend} : {divisor} =";
                break;
            }
            case 6:
            {
                int x1 = UnityEngine.Random.Range(3, 9);
                int x2 = UnityEngine.Random.Range(3, 9);
                int sum = x1 + x2;
                correctAnswer = sum * sum;
                exercise.text = $"{x1}^2 + 2 * {x1}*{x2} + {x2}^2 =";
                break;
            }
            default:
                Destroy(gameObject);
                break;
        }
    }

    private void FillAnswers()
    {
        int rightButtonIndex = UnityEngine.Random.Range(0, answers.Length);
        System.Collections.Generic.HashSet<int> usedOffsets = new System.Collections.Generic.HashSet<int>();

        for (int i = 0; i < answers.Length; i++)
        {
            TextMeshProUGUI answerLabel = answers[i].GetComponentInChildren<TextMeshProUGUI>();
            if (answerLabel == null)
                continue;

            if (i == rightButtonIndex)
            {
                answerLabel.text = correctAnswer.ToString();
                continue;
            }

            int offset = UnityEngine.Random.Range(-10, 10);
            while (offset == 0 || usedOffsets.Contains(offset))
                offset = UnityEngine.Random.Range(-10, 10);

            usedOffsets.Add(offset);
            answerLabel.text = (correctAnswer + offset).ToString();
        }
    }

    private void OnAnswerClicked(Button button)
    {
        if (answerLocked)
            return;

        answerLocked = true;
        SetAnswersInteractable(false);

        bool isCorrect = IsCorrectAnswer(button);
        if (!isCorrect)
        {
            Destroy(gameObject);
            return;
        }

        GiveRewardForCurrentLevel();

        level++;
        if (level > MaxLevel)
        {
            Destroy(gameObject);
            return;
        }

        StartChallenge();
    }

    private bool IsCorrectAnswer(Button button)
    {
        TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>();
        if (label == null)
            return false;

        return int.TryParse(label.text, out int clickedValue) && clickedValue == correctAnswer;
    }

    private void GiveRewardForCurrentLevel()
    {
        ItemRarity rarity = level switch
        {
            1 => ItemRarity.Common,
            2 => ItemRarity.Uncommon,
            3 => ItemRarity.Rare,
            4 => ItemRarity.Epic,
            5 => ItemRarity.Mythic,
            6 => ItemRarity.Legendary,
            _ => ItemRarity.Common
        };

        HeroItemDefinition item = Shop.I.GetRandomItemByRarity(rarity);
        if (item == null)
            return;

        if (playerProperties != null)
            playerProperties.ApplyItem(item);

        if (Character_StatusBar.I != null)
            Character_StatusBar.I.AddItem(item);
    }

    private void SetAnswersInteractable(bool isInteractable)
    {
        for (int i = 0; i < answers.Length; i++)
            answers[i].interactable = isInteractable;
    }
}
