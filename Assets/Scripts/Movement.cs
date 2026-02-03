using UnityEngine;

public class Movement : MonoBehaviour
{

    public float walkSpeed = 5f;
    public float runBoost = 2;
    public float jumpForce = 100f;
    public float currentStamina;
    public float staminaRegenTimer;
    public bool inAir;
    public bool isRunning;
    public bool isFiring;
    public bool onTop;
    public Camera camera;
    public Rigidbody rb;
    public Transform feetPos;

    public float animatorSpeed;

    Animator animator;
    GameObject character;
    GameObject characterHolder;

    [Header("Stamina")]
    public float maxStamina = 100f;
    public float staminaDrainPerSecond = 25f;
    public float staminaRegenPerSecond = 20f;
    public float staminaRegenDelay = 0.5f;
    public UnityEngine.UI.Slider staminaBar;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        camera = Camera.main.GetComponent<Camera>();

        character = GameObject.FindWithTag("Character");

        characterHolder = GameObject.FindWithTag("Controller");

        animator = character.GetComponent<Animator>();

        UnityEngine.Cursor.lockState = CursorLockMode.Locked;

        staminaBar.maxValue = maxStamina;
    }

    void Update()
    {
        float moveHorizontal = Input.GetAxis("Horizontal");
        float moveVertical = Input.GetAxis("Vertical");

        Vector3 camForward = camera.transform.forward;
        Vector3 camRight = camera.transform.right;

        camForward.y = 0f;
        camRight.y = 0f;

        camForward.Normalize();
        camRight.Normalize();

        Vector3 movement = (camForward * moveVertical + camRight * moveHorizontal).normalized * walkSpeed;
        Vector3 moveDir = new Vector3(movement.x, 0f, movement.z);

        animator.SetBool("Run", isRunning);

        HandleStamina();

        if (Input.GetKeyDown(KeyCode.L))
            UnityEngine.Cursor.lockState = CursorLockMode.Locked;
        if (Input.GetKeyDown(KeyCode.U))
            UnityEngine.Cursor.lockState = CursorLockMode.None;

        if (Input.GetKeyDown(KeyCode.LeftShift) && currentStamina > 0)
        {
            walkSpeed *= runBoost;
            if (animator.gameObject.active)
            {
                animatorSpeed = animator.GetFloat("Speed") * runBoost;
                animator.SetBool("Run", true);
                animator.SetFloat("Speed", animatorSpeed);
            }
            else
                animatorSpeed *= runBoost;
            isRunning = true;
        }
        else if ((Input.GetKeyUp(KeyCode.LeftShift)) && isRunning)
        {
            walkSpeed /= runBoost;
            if (animator.gameObject.active)
            {
                animatorSpeed = animator.GetFloat("Speed") / runBoost;
                animator.SetBool("Run", true);
                animator.SetFloat("Speed", animatorSpeed);
            }
            else
                animatorSpeed /= runBoost;
            isRunning = false;
        }
        if((currentStamina <= 0 && isRunning))
        {
            walkSpeed /= runBoost;
            if (animator.gameObject.active)
            {
                animatorSpeed = animator.GetFloat("Speed") / runBoost;
                animator.SetBool("Run", true);
                animator.SetFloat("Speed", animatorSpeed);
            }
            else
                animatorSpeed /= runBoost;
            isRunning = false;
        }

        if (Input.GetMouseButtonDown(0))
        {
            animator.SetBool("Fire", true);
            isFiring = true;
        }
        if (Input.GetMouseButtonUp(0))
        {
            animator.SetBool("Fire", false);
            isFiring = false;
        }

        if (moveDir.sqrMagnitude > 0.001f && !isFiring)
        {
            Quaternion targetRott = Quaternion.LookRotation(moveDir, Vector3.up);
            targetRott *= Quaternion.Euler(-90f, 0f, 0f);
            characterHolder.transform.rotation = Quaternion.Slerp(characterHolder.transform.rotation, targetRott, 10 * Time.deltaTime);
        }
        if (isFiring)
        {
            Quaternion targetRot = Quaternion.LookRotation(camForward, Vector3.up);
            targetRot *= Quaternion.Euler(-90f, 0f, 0f);
            targetRot *= Quaternion.AngleAxis(-25f, Vector3.forward);
            characterHolder.transform.rotation = Quaternion.Slerp(
                characterHolder.transform.rotation,
                targetRot,
                10 * Time.deltaTime);
        }

        Ray rRay = new Ray(feetPos.position, -transform.up);

        if (Physics.Raycast(rRay, 0.4f) && onTop)
        {
            onTop = false;
            inAir = false;
        }

        if (inAir && rb.velocity.y > 0.3f)
            onTop = true;

        if (!inAir)
            rb.velocity = new Vector3(movement.x, rb.velocity.y, movement.z);

        if (animator.gameObject.active)
        {
            if (moveHorizontal != 0 || moveVertical != 0)
                animator.SetBool("Walk", true);
            else
                animator.SetBool("Walk", false);

            animator.SetFloat("Vertical", moveVertical);
            animator.SetFloat("Horizontal", moveHorizontal);

            if (Mathf.Abs(rb.velocity.y) > 0.05f && !Physics.Raycast(rRay, 0.4f))
                animator.SetBool("Jump", true);
            else
                animator.SetBool("Jump", false);
        }

        if (Input.GetKeyDown(KeyCode.Space))
            Jump();
    }

    void Jump()
    {
        RaycastHit hit;
        Ray ray = new Ray(feetPos.position, -transform.up);

        Physics.Raycast(ray, out hit, 0.13f);

        if (hit.collider != null)
        {
            if (hit.collider.transform.CompareTag("Terrain"))
            {
                rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
                rb.AddForce(0f, jumpForce, 0f);
                inAir = true;
            }
        }
    }

    void HandleStamina()
    {
        if (isRunning && currentStamina > 0)
        {
            currentStamina -= staminaDrainPerSecond * Time.deltaTime;
            staminaRegenTimer = 0f;
        }
        else if (currentStamina < maxStamina)
        {
            staminaRegenTimer += Time.deltaTime;

            if (staminaRegenTimer >= staminaRegenDelay)
            {
                currentStamina += staminaRegenPerSecond * Time.deltaTime;
            }
        }

        staminaBar.value = currentStamina;
    }
}
