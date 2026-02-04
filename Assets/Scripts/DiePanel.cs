using UnityEngine;
using UnityEngine.SceneManagement;

public class DiePanel : MonoBehaviour
{
    SceneManager sceneManager;

    [SerializeField]
    GameObject panel;

    public void showDiePanel()
    {
        gameObject.SetActive(true);
        panel.SetActive(true);
    }

    public void Restart()
    {
        SceneManager.LoadScene(1);
    }

    public void ExitToMenu()
    {
        SceneManager.LoadScene(0);
    }

}
