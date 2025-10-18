using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D), typeof(Animator))]
public class CatController : MonoBehaviour
{
    // ===================== MOVEMENT SETTINGS =====================
    [Header("Movement Settings")]
    public float m_speed = 4f;
    public float m_sprint = 7.5f;
    public float jumpForce = 7.5f;
    public float idleIncrease = 1f;
    public float pushDown = 0.5f;

    // ===================== GRAVITY SETTINGS =====================
    [Header("Gravity Settings")]
    public float gravityStrength = 9.81f;
    public float rotationDuration = 0.25f;
    public LayerMask wallLayer;

    // ===================== SENSORS =====================
    [Header("Wall Sensors (Assign in Inspector)")]
    [SerializeField] private Collider2D topSensor;
    [SerializeField] private Collider2D leftSensor;
    [SerializeField] private Collider2D rightSensor;

    [Header("References")]
    public GameOverScript gameOverScript;

    // ===================== INTERNALS =====================
    private Rigidbody2D rb;
    private Animator animator;
    private InputAction jumpAction;
    private Vector2 localGravity;

    // ===================== STATE =====================
    private bool isGrounded;
    private bool isRotating;
    private int rotationIndex = 0;   // 0=down, 1=right, 2=up, 3=left
    private int yarnCount = 3;
    private int score;
    private float idleAmt;

    private float gravitySwitchCooldown = 0f;
    [SerializeField] private float switchCooldownTime = 0.25f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        rb.gravityScale = 0;
        localGravity = Vector2.down * gravityStrength; // start normal gravity
    }

    private void Start()
    {
        jumpAction = new InputAction("Jump", InputActionType.Button, "<Keyboard>/space");
        jumpAction.Enable();
    }

    private void FixedUpdate()
    {
        if (!isRotating)
            rb.AddForce(localGravity);
    }

    private void Update()
    {
        gravitySwitchCooldown -= Time.deltaTime;

        // ================= SENSOR-BASED ROTATION =================
        if (!isRotating && gravitySwitchCooldown <= 0f)
        {
            bool facingRight = transform.localScale.x > 0; // True if cat is facing right

            // When the cat is facing right, sensors behave normally
            // When facing left, we swap left/right logic
            Collider2D activeRightSensor = facingRight ? rightSensor : leftSensor;
            Collider2D activeLeftSensor = facingRight ? leftSensor : rightSensor;

            // RIGHT sensor behavior (relative to facing direction)
            if (activeRightSensor.IsTouchingLayers(wallLayer))
            {
                RotateRelative(+90f);
                gravitySwitchCooldown = switchCooldownTime;
            }
            // LEFT sensor behavior (relative to facing direction)
            else if (activeLeftSensor.IsTouchingLayers(wallLayer))
            {
                RotateRelative(-90f);
                gravitySwitchCooldown = switchCooldownTime;
            }
            // TOP sensor (always flip)
            else if (topSensor.IsTouchingLayers(wallLayer))
            {
                RotateRelative(180f);
                gravitySwitchCooldown = switchCooldownTime;
            }
        }


        // ================= MOVEMENT =================
        float inputX = Input.GetAxis("Horizontal");
        Vector2 vel = rb.linearVelocity;

        // Flip sprite visually
        if (inputX > 0) transform.localScale = new Vector3(5f, 5f, 1f);
        else if (inputX < 0) transform.localScale = new Vector3(-5f, 5f, 1f);

        // Move along local horizontal axis
        Vector2 moveDir = new Vector2(-localGravity.y, localGravity.x).normalized;
        rb.linearVelocity = moveDir * inputX * m_speed + Vector2.Dot(vel, localGravity.normalized) * localGravity.normalized;

        // Sprint
        if (Input.GetKey(KeyCode.LeftShift))
            rb.linearVelocity = moveDir * inputX * m_sprint + Vector2.Dot(vel, localGravity.normalized) * localGravity.normalized;

        // ================= JUMP =================
        if (jumpAction.triggered && isGrounded)
        {
            Vector2 jumpDir = -localGravity.normalized;
            rb.AddForce(jumpDir * jumpForce, ForceMode2D.Impulse);
            isGrounded = false;
        }

        // ================= ANIMATION (rotation-based) =================

        // Get the cat's current rotation (0, 90, 180, 270)
        float zRot = Mathf.Round(transform.eulerAngles.z) % 360;

        // Determine which axis represents forward movement
        float movementSpeed = 0f;

        // Bottom or top walls (0° or 180°): horizontal movement
        if (Mathf.Approximately(zRot, 0f) || Mathf.Approximately(zRot, 180f))
        {
            movementSpeed = Mathf.Abs(rb.linearVelocity.x);
        }
        // Right or left walls (90° or 270°): vertical movement
        else if (Mathf.Approximately(zRot, 90f) || Mathf.Approximately(zRot, 270f))
        {
            movementSpeed = Mathf.Abs(rb.linearVelocity.y);
        }

        // Apply smoothed speed value to animator
        animator.SetFloat("Speed", movementSpeed);

        // Handle AirSpeed (jump / fall state)
        if (!isGrounded)
        {
            animator.SetFloat("AirSpeed", 1);
        }
        else
        {
            animator.SetFloat("AirSpeed", 0);
        }

        // --- Idle Timer (for “bored” animation) ---
        bool isIdle = isGrounded && movementSpeed < 0.05f && rb.linearVelocity.magnitude < 0.1f;

        if (isIdle)
        {
            idleAmt = Mathf.Min(idleAmt + Time.deltaTime * idleIncrease, 11f);
        }
        else
        {
            idleAmt = 0f;
        }

        animator.SetFloat("IdleTime", idleAmt);

    }

    // ================= RELATIVE ROTATION LOGIC =================
    private void RotateRelative(float angleDelta)
    {
        // Convert rotationIndex (0-3) based on 90° increments
        int deltaIndex = Mathf.RoundToInt(angleDelta / 90f);
        rotationIndex = (rotationIndex + deltaIndex + 4) % 4; // wrap around 0-3

        // Convert index back to vector
        Vector2 newGravityDir = Vector2.down;
        switch (rotationIndex)
        {
            case 0: newGravityDir = Vector2.down; break;
            case 1: newGravityDir = Vector2.right; break;
            case 2: newGravityDir = Vector2.up; break;
            case 3: newGravityDir = Vector2.left; break;
        }

        // Start smooth rotation coroutine
        StartCoroutine(SwitchGravitySmooth(newGravityDir));
    }

    // ================= SMOOTH GRAVITY SWITCH =================
    private IEnumerator SwitchGravitySmooth(Vector2 newGravityDir)
    {
        isRotating = true;

        // Find target rotation in degrees
        float targetRotation = 0f;
        if (newGravityDir == Vector2.up) targetRotation = 180f;
        else if (newGravityDir == Vector2.right) targetRotation = 90f;
        else if (newGravityDir == Vector2.left) targetRotation = 270f;
        else targetRotation = 0f;

        // Smooth interpolate rotation & gravity
        float elapsed = 0f;
        Vector2 startGravity = localGravity;
        Quaternion startRot = transform.rotation;
        Quaternion endRot = Quaternion.Euler(0, 0, targetRotation);

        while (elapsed < rotationDuration)
        {
            float t = elapsed / rotationDuration;
            float eased = Mathf.SmoothStep(0f, 1f, t);

            localGravity = Vector2.Lerp(startGravity, newGravityDir * gravityStrength, t * 1.5f);
            transform.rotation = Quaternion.Lerp(startRot, endRot, eased);
            elapsed += Time.deltaTime;

            yield return null;
        }

        localGravity = newGravityDir * gravityStrength;
        transform.rotation = endRot;
        isRotating = false;
    }

    // ===================== YARN SYSTEM =====================
    public int getYarnCount() => yarnCount;
    public void useYarn() => yarnCount -= 1;

    // ================= COLLISION CHECK =================
    private void OnCollisionStay2D(Collision2D collision)
    {
        // Basic hazard handling
        if (collision.gameObject.CompareTag("Hazard"))
            gameOverScript.showGameOver(score);

        // Grounded if touching any solid layer (rough approximation)
        if (((1 << collision.gameObject.layer) & wallLayer) != 0)
            isGrounded = true;
    }
    private void OnCollisionExit2D(Collision2D collision)
    {
        // Not grounded if leaving any solid layer
        if (((1 << collision.gameObject.layer) & wallLayer) != 0)
            isGrounded = false;
    }
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Collect yarn
        if (collision.gameObject.CompareTag("Collect"))
        {
            if (yarnCount < 3)
                yarnCount += 1;
            Destroy(collision.gameObject);
        }
    }
}
