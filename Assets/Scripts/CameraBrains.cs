using Cinemachine;
using UnityEngine;

public class CameraBrains : MonoBehaviour
{
    bool isOn = true;
    public float crosshairY = 40;
    AnimatorCullingMode defaultAnimatorCullingMode;
    bool hasAnimatorCullingMode;

    [SerializeField] CinemachineVirtualCamera firstPersonCam;
    [SerializeField] GameObject[] healthBars;
    [SerializeField] Animator animator;

    GameObject firstPersonGunHolder;
    GameObject thirdPersonGunHolder;
    CinemachineVirtualCamera cam;
    GameObject character;
    Renderer[] characterRenderers;
    RectTransform rect;
    Movement movement;

    void Awake()
    {
        cam = GetComponent<CinemachineVirtualCamera>();
        character = GameObject.FindGameObjectWithTag("Character");
        if (character != null)
            characterRenderers = character.GetComponentsInChildren<Renderer>(true);

        TryResolveGunHolders();

        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            movement = player.GetComponent<Movement>();

        var crosshair = GameObject.FindGameObjectWithTag("Crosshair");
        if (crosshair != null)
            rect = crosshair.GetComponent<RectTransform>();

        if (character != null)
            animator = character.GetComponent<Animator>();

        if (animator != null)
        {
            defaultAnimatorCullingMode = animator.cullingMode;
            hasAnimatorCullingMode = true;
        }
    }

    void Update()
    {
        TryResolveGunHolders();

        if (Input.GetAxis("Mouse ScrollWheel") < 0 && cam.GetCinemachineComponent<CinemachineFramingTransposer>().m_CameraDistance < 10 && isOn)
        {
            cam.GetCinemachineComponent<CinemachineFramingTransposer>().m_CameraDistance += 1f;
            if (rect != null)
                rect.localPosition += new Vector3(0, -20, 0);
            crosshairY += -20f;
        }
        else if (Input.GetAxis("Mouse ScrollWheel") > 0 && cam.GetCinemachineComponent<CinemachineFramingTransposer>().m_CameraDistance > 2 && isOn)
        {
            cam.GetCinemachineComponent<CinemachineFramingTransposer>().m_CameraDistance += -1f;
            if (rect != null)
                rect.localPosition += new Vector3(0, 20, 0);
            crosshairY += 20f;
        }

        if (Input.GetKeyDown(KeyCode.V))
        {
            if (isOn)
            {
                if (firstPersonGunHolder != null)
                    firstPersonGunHolder.SetActive(true);
                if (thirdPersonGunHolder != null)
                    thirdPersonGunHolder.SetActive(false);

                if (rect != null)
                    rect.localPosition = new Vector3(0, 0, 0);

                firstPersonCam.GetCinemachineComponent<CinemachinePOV>().m_VerticalAxis = cam.GetCinemachineComponent<CinemachinePOV>().m_VerticalAxis;
                firstPersonCam.GetCinemachineComponent<CinemachinePOV>().m_HorizontalAxis = cam.GetCinemachineComponent<CinemachinePOV>().m_HorizontalAxis;
                cam.m_Priority += -2;
                SetCharacterVisible(false);
                SetAnimatorForFirstPerson();
                isOn = false;

                foreach (var bar in healthBars)
                    if (bar != null)
                        bar.SetActive(false);
            }
            else
            {
                if (firstPersonGunHolder != null)
                    firstPersonGunHolder.SetActive(false);
                if (thirdPersonGunHolder != null)
                    thirdPersonGunHolder.SetActive(true);

                if (rect != null)
                    rect.localPosition = new Vector3(0, crosshairY, 0);

                cam.GetCinemachineComponent<CinemachinePOV>().m_VerticalAxis = firstPersonCam.GetCinemachineComponent<CinemachinePOV>().m_VerticalAxis;
                cam.GetCinemachineComponent<CinemachinePOV>().m_HorizontalAxis = firstPersonCam.GetCinemachineComponent<CinemachinePOV>().m_HorizontalAxis;
                cam.m_Priority += 2;
                SetCharacterVisible(true);
                RestoreAnimatorAfterThirdPersonSwitch();
                isOn = true;

                foreach (var bar in healthBars)
                    if (bar != null)
                        bar.SetActive(true);

                if (animator != null && movement != null)
                {
                    animator.SetBool("Run", movement.isRunning);
                    animator.SetBool("Jump", movement.inAir);
                    animator.SetBool("Fire", movement.isFiring);
                    animator.SetFloat("Speed", movement.animatorSpeed);
                }
            }
        }
    }

    void SetCharacterVisible(bool isVisible)
    {
        if (characterRenderers == null)
            return;

        foreach (var item in characterRenderers)
        {
            if (item != null)
                item.enabled = isVisible;
        }
    }

    void TryResolveGunHolders()
    {
        if (thirdPersonGunHolder == null)
        {
            var worldGunHolder = GameObject.FindGameObjectWithTag("Gunn");
            if (worldGunHolder != null)
                thirdPersonGunHolder = worldGunHolder;
        }

        if (firstPersonGunHolder == null)
        {
            var cameraGun = GameObject.FindGameObjectWithTag("Camera Gun");
            if (cameraGun != null)
                firstPersonGunHolder = cameraGun;
        }
    }

    void SetAnimatorForFirstPerson()
    {
        if (animator == null)
            return;

        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
    }

    void RestoreAnimatorAfterThirdPersonSwitch()
    {
        if (animator == null)
            return;

        animator.Rebind();
        animator.Update(0f);

        if (hasAnimatorCullingMode)
            animator.cullingMode = defaultAnimatorCullingMode;
    }
}
