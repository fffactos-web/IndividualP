using UnityEngine;

public class LevelUpSelectionUI : MonoBehaviour
{
    [SerializeField] Character_Properties character;
    [SerializeField] GameObject panelRoot;

    void Awake()
    {
        if (character == null)
            character = GetComponent<Character_Properties>();

        if (character != null)
            character.OnLevelUp += ShowPanel;

        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    void OnDestroy()
    {
        if (character != null)
            character.OnLevelUp -= ShowPanel;
    }

    void ShowPanel()
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(true);
            Time.timeScale = 0f;
        }
    }

    public void HidePanel()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);

        Time.timeScale = 1f;
    }
}
