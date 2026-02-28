using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BackBoard : MonoBehaviour
{
    [SerializeField]Button[] answers;
    [SerializeField]TextMeshProUGUI exercise;
    int level = 1;
    int ans;
    bool answerLocked;
    Canvas canvas;

    Shop shop;

    readonly List<RaycastResult> uiRaycastResults = new List<RaycastResult>();
    int lastAnswerFrame = -1;

    private void Start()
    {
        shop = GameObject.FindGameObjectWithTag("Shop").GetComponent<Shop>();
        canvas = GetComponent<Canvas>();
        canvas.worldCamera = Camera.main;
        StartChallange();
        for (int i = 0; i < answers.Length; i++)
        {
            Button btn = answers[i];
            btn.onClick.AddListener(() => CheckAnswer(btn));
        }
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
            TryClickCenterButton();
    }

    public void StartChallange()
    {
        answerLocked = false;
        SetAnswersInteractable(true);

        switch (level)
        {
            case 1:
                int a = Random.Range(100, 999);
                int b = Random.Range(100, 999);
                ans = a + b;
                exercise.text = a + " + " + b + " =";
                break;
            case 2:
                int c = Random.Range(100, 200);
                int d = Random.Range(3, 6);
                ans = c * d;
                exercise.text = c + " * " + d + " =";
                break;
            case 3:
                int e = Random.Range(11, 30);
                ans = (int)System.Math.Pow(e, 2);
                exercise.text = e + "^2" + " =";
                break;
            case 4:
                int f = Random.Range(11, 30);
                int g = (int)System.Math.Pow(f, 2);
                ans = f + g;
                exercise.text = f + " + " + f + "^2" + " =";
                break;
            case 5:
                int z = Random.Range(100, 200);
                int x = z * Random.Range(3, 6);
                ans = x/z;
                exercise.text = x + " : " + z + " =";
                break;
            case 6:
                int x1 = Random.Range(3, 9);
                int x2 = Random.Range(3, 9);
                ans = (int)Mathf.Pow((x1 + x2),2);
                exercise.text = x1 + "^2 + " + "2 * " + x1 + "*" + x2 + " + " + x2 + "^2" + " =";
                break;
            default:
                break;
        }
        
        int rightB = Random.Range(0, answers.Length);
        List<int> ints = new List<int>();
        for (int i = 0; i < answers.Length; i++)
        {
            if (i == rightB)
                answers[i].GetComponentInChildren<TextMeshProUGUI>().text = ans.ToString();
            else
            {
                int r = Random.Range(-10, 10);
                while (r == 0 || ints.Contains(r))
                    r = Random.Range(-10, 10);
                answers[i].GetComponentInChildren<TextMeshProUGUI>().text = (ans + r).ToString();
                ints.Add(r);
            }
        }
    }

    public void CheckAnswer(Button button)
    {
        if (lastAnswerFrame == Time.frameCount)
            return;

        lastAnswerFrame = Time.frameCount;

        if (answerLocked)
            return;

        answerLocked = true;
        SetAnswersInteractable(false);

        Debug.Log("Answer checked");
        if (button.GetComponentInChildren<TextMeshProUGUI>().text == ans.ToString())
        {
            switch (level)
            {
                case 1:
                    HeroItemDefinition item1 = Shop.I.GetRandomItemByRarity(ItemRarity.Common);
                    GameObject.FindGameObjectWithTag("Player").GetComponent<Character_Properties>().ApplyItem(item1);
                    Character_StatusBar.I.AddItem(item1);
                    break;
                case 2:
                    HeroItemDefinition item2 = Shop.I.GetRandomItemByRarity(ItemRarity.Uncommon);
                    GameObject.FindGameObjectWithTag("Player").GetComponent<Character_Properties>().ApplyItem(item2);
                    Character_StatusBar.I.AddItem(item2);
                    break;
                case 3:
                    HeroItemDefinition item3 = Shop.I.GetRandomItemByRarity(ItemRarity.Rare);
                    GameObject.FindGameObjectWithTag("Player").GetComponent<Character_Properties>().ApplyItem(item3);
                    Character_StatusBar.I.AddItem(item3);
                    break;
                case 4:
                    HeroItemDefinition item4 = Shop.I.GetRandomItemByRarity(ItemRarity.Epic);
                    GameObject.FindGameObjectWithTag("Player").GetComponent<Character_Properties>().ApplyItem(item4);
                    Character_StatusBar.I.AddItem(item4);
                    break;
                case 5:
                    HeroItemDefinition item5 = Shop.I.GetRandomItemByRarity(ItemRarity.Mythic);
                    GameObject.FindGameObjectWithTag("Player").GetComponent<Character_Properties>().ApplyItem(item5);
                    Character_StatusBar.I.AddItem(item5);
                    break;
                case 6:
                    HeroItemDefinition item6 = Shop.I.GetRandomItemByRarity(ItemRarity.Legendary);
                    GameObject.FindGameObjectWithTag("Player").GetComponent<Character_Properties>().ApplyItem(item6);
                    Character_StatusBar.I.AddItem(item6);
                    Destroy(gameObject);
                    break;
                default:
                    break;
            }
            level++;
            StartChallange();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void SetAnswersInteractable(bool isInteractable)
    {
        for (int i = 0; i < answers.Length; i++)
            answers[i].interactable = isInteractable;
    }

    void TryClickCenterButton()
    {
        if (EventSystem.current == null)
            return;

        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f)
        };

        uiRaycastResults.Clear();
        EventSystem.current.RaycastAll(pointerData, uiRaycastResults);

        for (int i = 0; i < uiRaycastResults.Count; i++)
        {
            Button btn = uiRaycastResults[i].gameObject.GetComponentInParent<Button>();
            if (btn != null && btn.interactable)
            {
                btn.onClick.Invoke();
                return;
            }
        }
    }
}
