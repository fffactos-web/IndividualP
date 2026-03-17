using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Movement : MonoBehaviour
{
    private float currentStamina;
    private float staminaRegenTimer;
    private float runBoost;
    public float jumpForce = 75 * 4f;
    public float dashTime;
    public float dashPower;
    public bool inAir;
    public bool isRunning;
    public bool isWalking;
    public bool isFiring;
    public bool onTop;
    public bool isDashing;
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
    [SerializeField] float bunnyHopBoost = 2.5f;     // ñèëà áóñòà
    [SerializeField] float bunnyHopWindow = 0.15f;   // îêíî òàéìèíãà ïîñëå ïðèçåìëåíèÿ
    [SerializeField] float goundCheckDistance;
    float lastJumpPressedTime;

    [Header("Ground check (robust)")]
    [SerializeField] LayerMask groundMask = ~0;
    [SerializeField] float minGroundDot = 0.65f; // ìèíèìàëüíàÿ dot( normal, up ) ÷òîáû ñ÷èòàòü ïîâåðõíîñòü "çåìëåé"
    [SerializeField] float ignoreGroundAfterJump = 0.06f; // âðåìÿ, â òå÷åíèå êîòîðîãî èãíîðèðóåì ìîìåíòàëüíûå êîëëèçèè ïîñëå ïðûæêà

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
        maxStamina = heroStats.maxStamina;
        staminaBar.maxValue = maxStamina;
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


        if (Input.GetKeyDown(KeyCode.LeftControl))
            Dash();

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

        // --- â Update(), ãäå íóæíî çíàòü grounded ---
        bool rawGrounded = groundContacts.Count > 0;

        // äîïîëíèòåëüíî: èãíîðèðóåì "ïîäëåòàþùèå" ñðàáàòûâàíèÿ ñðàçó ïîñëå ïðûæêà
        bool grounded = rawGrounded && Time.time > ignoreGroundUntil;

        // çàùèòà: åñëè ìû âñ¸ åù¸ äâèæåìñÿ ââåðõ — íå ñ÷èòàòü çåìëþ (ïðåäîòâðàùàåò ìãíîâåííîå ðåñåò â ïðûæêå)
        if (grounded && rb.velocity.y > 1.0f) grounded = false;

        isWalking = moveHorizontal != 0 || moveVertical != 0;
        animatorSpeed = horizontal.magnitude;

            animator.SetBool("Walk", isWalking);
        if (grounded)
        {
            if (inAir) lastGroundedTime = Time.time; // ôèêñèì ìîìåíò ïðèçåìëåíèÿ
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
        if(!isDashing)
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
        // â íà÷àëå Jump() ïîñëå ïðîâåðêè maxJumps è ïåðåä èçìåíåíèåì velocity:
        ignoreGroundUntil = Time.time + ignoreGroundAfterJump;

        int maxJumps = characterStats != null ? characterStats.GetStats().jumpCount : 1;
        if (jumpsUsed >= maxJumps)
            return;

        // ïðîâåðÿåì òàéìèíã
        bool timedBhop = (Time.time - lastGroundedTime) <= bunnyHopWindow;

        Vector3 horizontal = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

        // åñëè ýòî ÍÅ òàéìëåííûé ïðûæîê — ñëåãêà ðåæåì ñêîðîñòü
        if (!timedBhop)
            horizontal *= 0.85f;   // îáû÷íûé ïðûæîê íåìíîãî ãàñèò ñêîðîñòü

        // îáíóëÿåì âåðòèêàëü
        rb.velocity = new Vector3(horizontal.x, 0f, horizontal.z);

        // îáû÷íûé ïðûæîê ââåðõ
        rb.AddForce(Vector3.up * jumpForce, ForceMode.VelocityChange);

        // åñëè ýòî bunny hop — äîáàâëÿåì èìïóëüñ âïåð¸ä
        if (timedBhop && horizontal.magnitude > 0.1f)
        {
            Vector3 boostDir = horizontal.normalized;
            rb.AddForce(boostDir * bunnyHopBoost, ForceMode.VelocityChange);
        }

        inAir = true;
        jumpsUsed++;
    }



    IEnumerator Dash()
    {
        isDashing = true;
        Vector3 dashToVector = camera.transform.forward;
        rb.velocity = dashToVector * dashPower;
        yield return new WaitForSeconds(dashTime);
        Debug.Log("Dashed");
        isDashing = false;
        dashCooldownTimer = 1f;
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
        // ïðè âûõîäå — óäàëÿåì êîëëàéäåð èç ìíîæåñòâà
        if (groundContacts.Contains(collision.collider))
            groundContacts.Remove(collision.collider);
    }

    void EvaluateCollisionForGround(Collision collision)
    {
        // ïðîâåðÿåì, ÷òî ñëîé collision âõîäèò â groundMask
        if (((1 << collision.gameObject.layer) & groundMask) == 0)
            return;

        // ïðîâåðÿåì âñå êîíòàêòû — åñëè åñòü êîíòàêò ñ íîðìàëüþ äîñòàòî÷íî "ââåðõ"
        foreach (ContactPoint cp in collision.contacts)
        {
            float dot = Vector3.Dot(cp.normal, Vector3.up);
            if (dot >= minGroundDot)
            {
                groundContacts.Add(collision.collider);
                return; // äîñòàòî÷íî îäíîãî ïîäõîäÿùåãî êîíòàêòà
            }
        }
    }

}