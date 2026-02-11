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
        Time.timeScale = 1f;
        UnityEngine.Cursor.lockState = CursorLockMode.None;
        SceneManager.LoadScene(1);
    }

    public void ExitToMenu()
    {
        Time.timeScale = 1f;
        UnityEngine.Cursor.lockState = CursorLockMode.None;
        SceneManager.LoadScene(0);
    }

}
