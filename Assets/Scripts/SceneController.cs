using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneController : MonoBehaviour
{

    Animator animator;
    
    [SerializeField]
    Button exit;
    [SerializeField]
    Button start;
    [SerializeField]
    Button settings;

    private void Awake()
    {
        animator = GameObject.FindGameObjectWithTag("Character").GetComponent<Animator>();
        animator.Play("Dance Booty");
    }
    public void StartGame()
    {
        SceneManager.LoadScene(1);
    }
    public void Exit()
    {
        Application.Quit();
    }

}
