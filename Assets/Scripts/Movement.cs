using System.Collections.Generic;
using UnityEngine;

public class Movement : MonoBehaviour
{
    private float currentStamina;
    private float staminaRegenTimer;
    private float runBoost;
    public float jumpForce = 75 * 4f;
    public bool inAir;
    public bool isRunning;
    public bool isFiring;
    public bool onTop;
    private Camera camera;
    private Rigidbody rb;
    [SerializeField] Transform feetPos;

    public float animatorSpeed; 

    Animator animator;
    GameObject character;
    GameObject characterHolder;
    [SerializeField] Character_Properties characterStats;

    [SerializeField] float baseWalkSpeed;
    int jumpsUsed;
    float dashCooldownTimer;

    [Header("Stamina")]
    public float maxStamina = 100f;
    public float staminaDrainPerSecond = 25f;
    public float staminaRegenPerSecond = 20f;
    public float staminaRegenDelay = 0.5f;
    public UnityEngine.UI.Slider staminaBar;

    [Header("Bunny Hop")]
    [SerializeField] float bunnyHopBoost = 2.5f;     // сила буста
    [SerializeField] float bunnyHopWindow = 0.15f;   // окно тайминга после приземлени€
    [SerializeField] float goundCheckDistance;
    float lastJumpPressedTime;

    [Header("Ground check (robust)")]
    [SerializeField] LayerMask groundMask = ~0;
    [SerializeField] float minGroundDot = 0.65f; // минимальна€ dot( normal, up ) чтобы считать поверхность "землей"
    [SerializeField] float ignoreGroundAfterJump = 0.06f; // врем€, в течение которого игнорируем моментальные коллизии после прыжка

    HashSet<Collider> groundContacts = new HashSet<Collider>();
    float lastGroundedTime = -10f;
    float ignoreGroundUntil = -10f;


    void Start()
    {
        rb = GetComponent<Rigidbody>();
        camera = Camera.main.GetComponent<Camera>();
        character = GameObject.FindWithTag("Character");
        characterHolder = GameObject.FindWithTag("Controller");
        animator = character.GetComponent<Animator>();

        UnityEngine.Cursor.lockState = CursorLockMode.Locked;

        staminaBar.maxValue = maxStamina;
        currentStamina = maxStamina;
    }

    public void ResetProperties()
    {
        Character_Properties.HeroStats heroStats = characterStats.GetStats();
        runBoost = heroStats.runSpeed;
    }

    void Update()
    {
        dashCooldownTimer -= Time.deltaTime;

        float moveHorizontal = Input.GetAxis("Horizontal");
        float moveVertical = Input.GetAxis("Vertical");

        Vector3 camForward = camera.transform.forward;
        Vector3 camRight = camera.transform.right;
        camForward.y = 0f;
        camRight.y = 0f;
        camForward.Normalize();
        camRight.Normalize();

        isRunning = Input.GetKey(KeyCode.LeftShift) && currentStamina > 0f;
        HandleStamina();

        var stats = characterStats != null ? characterStats.GetStats() : null;
        float moveMultiplier = stats != null ? stats.moveSpeed : 20f;
        float acceleration = stats != null ? stats.globalAcceleration : 1f;
        float targetSpeed = baseWalkSpeed * moveMultiplier * (isRunning ? runBoost : 1f);

        Vector3 desiredVelocity = (camForward * moveVertical + camRight * moveHorizontal).normalized * targetSpeed;
        Vector3 moveDir = new Vector3(desiredVelocity.x, 0f, desiredVelocity.z);

        animator.SetBool("Run", isRunning);


        if (Input.GetKeyDown(KeyCode.LeftControl) && dashCooldownTimer <= 0f)
            Dash(camForward, camRight, moveHorizontal, moveVertical, stats);

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
            characterHolder.transform.rotation = Quaternion.Slerp(characterHolder.transform.rotation, targetRot, 10 * Time.deltaTime);
        }

        Ray rRay = new Ray(feetPos.position, -transform.up);

        // --- в Update(), где нужно знать grounded ---
        bool rawGrounded = groundContacts.Count > 0;

        // дополнительно: игнорируем "подлетающие" срабатывани€ сразу после прыжка
        bool grounded = rawGrounded && Time.time > ignoreGroundUntil;

        // защита: если мы всЄ ещЄ движемс€ вверх Ч не считать землю (предотвращает мгновенное ресет в прыжке)
        if (grounded && rb.velocity.y > 1.0f) grounded = false;

        // примен€ем состо€ние
        if (grounded)
        {
            if (inAir) lastGroundedTime = Time.time; // фиксим момент приземлени€
            onTop = false;
            inAir = false;
            jumpsUsed = 0;
        }



        if (inAir && rb.velocity.y > 0.3f)
            onTop = true;

        Vector3 velocity = rb.velocity;
        float airControl = stats != null ? stats.airControl : 0.35f;
        float control = inAir ? airControl : 1f;

        Vector3 horizontal = new Vector3(velocity.x, 0f, velocity.z);
        horizontal = Vector3.Lerp(horizontal, moveDir, Time.deltaTime * 10f * acceleration * control);
        //rb.AddForce(new Vector3(horizontal.x, 0, horizontal.z), ForceMode.VelocityChange);
        rb.velocity = new Vector3(horizontal.x, velocity.y, horizontal.z);

        if (animator.gameObject.active)
        {
            animator.SetBool("Walk", moveHorizontal != 0 || moveVertical != 0);
            animator.SetFloat("Vertical", moveVertical);
            animator.SetFloat("Horizontal", moveHorizontal);
            animator.SetBool("Jump", Mathf.Abs(rb.velocity.y) > 0.05f && !Physics.Raycast(rRay, 0.4f));
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            lastJumpPressedTime = Time.time;
            Jump();
        }
    }

    void Jump()
    {
        // в начале Jump() после проверки maxJumps и перед изменением velocity:
        ignoreGroundUntil = Time.time + ignoreGroundAfterJump;

        int maxJumps = characterStats != null ? characterStats.GetStats().jumpCount : 1;
        if (jumpsUsed >= maxJumps)
            return;

        // провер€ем тайминг
        bool timedBhop = (Time.time - lastGroundedTime) <= bunnyHopWindow;

        Vector3 horizontal = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

        // если это Ќ≈ таймленный прыжок Ч слегка режем скорость
        if (!timedBhop)
            horizontal *= 0.85f;   // обычный прыжок немного гасит скорость

        // обнул€ем вертикаль
        rb.velocity = new Vector3(horizontal.x, 0f, horizontal.z);

        // обычный прыжок вверх
        rb.AddForce(Vector3.up * jumpForce, ForceMode.VelocityChange);

        // если это bunny hop Ч добавл€ем импульс вперЄд
        if (timedBhop && horizontal.magnitude > 0.1f)
        {
            Vector3 boostDir = horizontal.normalized;
            rb.AddForce(boostDir * bunnyHopBoost, ForceMode.VelocityChange);
        }

        inAir = true;
        jumpsUsed++;
    }



    void Dash(Vector3 camForward, Vector3 camRight, float horizontal, float vertical, Character_Properties.HeroStats stats)
    {
        Vector3 dir = (camForward * vertical + camRight * horizontal);
        if (dir.sqrMagnitude < 0.0001f)
            dir = transform.forward;

        dir.Normalize();

        float dashMultiplier = stats != null ? stats.runSpeed : 1f;
        float accel = stats != null ? stats.globalAcceleration : 1f;
        float dashSpeed = baseWalkSpeed * runBoost * dashMultiplier * accel;

        rb.velocity = new Vector3(dir.x * dashSpeed, rb.velocity.y, dir.z * dashSpeed);
        dashCooldownTimer = 0.8f;
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
                currentStamina += staminaRegenPerSecond * Time.deltaTime;
        }

        currentStamina = Mathf.Clamp(currentStamina, 0f, maxStamina);
        staminaBar.value = currentStamina;
    }

    void OnCollisionEnter(Collision collision)
    {
        EvaluateCollisionForGround(collision);
    }

    void OnCollisionStay(Collision collision)
    {
        EvaluateCollisionForGround(collision);
    }

    void OnCollisionExit(Collision collision)
    {
        // при выходе Ч удал€ем коллайдер из множества
        if (groundContacts.Contains(collision.collider))
            groundContacts.Remove(collision.collider);
    }

    void EvaluateCollisionForGround(Collision collision)
    {
        // провер€ем, что слой collision входит в groundMask
        if (((1 << collision.gameObject.layer) & groundMask) == 0)
            return;

        // провер€ем все контакты Ч если есть контакт с нормалью достаточно "вверх"
        foreach (ContactPoint cp in collision.contacts)
        {
            float dot = Vector3.Dot(cp.normal, Vector3.up);
            if (dot >= minGroundDot)
            {
                groundContacts.Add(collision.collider);
                return; // достаточно одного подход€щего контакта
            }
        }
    }

}