using Cinemachine;
using UnityEngine;

public class CameraBrains : MonoBehaviour
{

    bool isOn = true;
    public float crosshairY = 40;

    [SerializeField]
    CinemachineVirtualCamera firstPersonCam;

    [SerializeField]
    GameObject[] healthBars;

    [SerializeField]
    Animator animator;

    GameObject gunHolder;
    CinemachineVirtualCamera cam;
    GameObject character;
    RectTransform rect;
    Movement movement;

    void Awake()
    {
        cam = GetComponent<CinemachineVirtualCamera>();
        character = GameObject.FindGameObjectWithTag("Character");
        gunHolder = GameObject.FindGameObjectWithTag("Camera Gun");
        gunHolder.SetActive(false);
        movement = GameObject.FindGameObjectWithTag("Player").GetComponent<Movement>();
        rect = GameObject.FindGameObjectWithTag("Crosshair").GetComponent<RectTransform>();
        animator = character.GetComponent<Animator>();
    }

    void Update()
    {
        if (Input.GetAxis("Mouse ScrollWheel") < 0 && cam.GetCinemachineComponent<CinemachineFramingTransposer>().m_CameraDistance < 10 && isOn)
        {
            cam.GetCinemachineComponent<CinemachineFramingTransposer>().m_CameraDistance += 1f;
            rect.localPosition += new Vector3(0, -20, 0);
            crosshairY += -20f;
        }
        else if (Input.GetAxis("Mouse ScrollWheel") > 0 && cam.GetCinemachineComponent<CinemachineFramingTransposer>().m_CameraDistance > 2 && isOn)
        {
            cam.GetCinemachineComponent<CinemachineFramingTransposer>().m_CameraDistance += -1f;
            rect.localPosition += new Vector3(0, 20, 0);
            crosshairY += 20f;
        }

        if (Input.GetKeyDown(KeyCode.V))
        {
            if(isOn)
            {
                gunHolder.SetActive(true);
                rect.localPosition = new Vector3(0, 0, 0);
                firstPersonCam.GetCinemachineComponent<CinemachinePOV>().m_VerticalAxis = cam.GetCinemachineComponent<CinemachinePOV>().m_VerticalAxis;
                firstPersonCam.GetCinemachineComponent<CinemachinePOV>().m_HorizontalAxis = cam.GetCinemachineComponent<CinemachinePOV>().m_HorizontalAxis;
                cam.m_Priority += -2;
                character.SetActive(false);
                isOn = false;
                foreach (var bar in healthBars)
                    if (bar != null)
                        bar.SetActive(false);
                
            }
            else
            {
                gunHolder.SetActive(false);
                rect.localPosition = new Vector3(0, crosshairY, 0);
                cam.GetCinemachineComponent<CinemachinePOV>().m_VerticalAxis = firstPersonCam.GetCinemachineComponent<CinemachinePOV>().m_VerticalAxis;
                cam.GetCinemachineComponent<CinemachinePOV>().m_HorizontalAxis = firstPersonCam.GetCinemachineComponent<CinemachinePOV>().m_HorizontalAxis;
                cam.m_Priority += 2;
                character.SetActive(true);
                isOn = true;
                foreach (var bar in healthBars)
                    if(bar != null)
                        bar.SetActive(true);
                animator.SetBool("Run", movement.isRunning);
                animator.SetBool("Jump", movement.inAir);
                animator.SetBool("Fire", movement.isFiring); 
                animator.SetFloat("Speed", movement.animatorSpeed);
            }
        }
    }
}
