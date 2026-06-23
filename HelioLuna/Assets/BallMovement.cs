using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class BallMovement : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference jumpAction;
    [SerializeField] private InputActionReference driftAction;

    [Header("Camera Reference")]
    [SerializeField] private Transform cameraTransform;

    [Header("Movement")]
    [SerializeField] private float torqueForce = 12f;
    [SerializeField] private float maxSpeed = 12f;
    [SerializeField] private float airControlMultiplier = 0.25f;

    [Header("Drift")]
    [SerializeField] private float driftGrip = 2f;
    [SerializeField] private float driftAmount = 0.7f;
    [SerializeField] private float normalGrip = 10f;

    [Header("Jump")]
    [SerializeField] private float jumpForce = 7f;
    [SerializeField] private float directionalJumpForce = 4f;
    [SerializeField] private float coyoteTime = 0.15f;
    [SerializeField] private float jumpBufferTime = 0.15f;

    [Header("Ground Check")]
    [SerializeField] private float groundCheckDistance = 0.65f;
    [SerializeField] private LayerMask groundLayer;

    private Rigidbody rb;
    private Vector2 moveInput;

    private float lastGroundedTime;
    private float lastJumpPressedTime;

    private bool isDrifting;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
    }

    private void OnEnable()
    {
        moveAction.action.Enable();
        jumpAction.action.Enable();
        driftAction.action.Enable();

        jumpAction.action.performed += OnJump;
    }

    private void OnDisable()
    {
        jumpAction.action.performed -= OnJump;

        moveAction.action.Disable();
        jumpAction.action.Disable();
        driftAction.action.Disable();
    }

    private void Update()
    {
        moveInput = moveAction.action.ReadValue<Vector2>();
        isDrifting = driftAction.action.IsPressed();

        if (IsGrounded())
            lastGroundedTime = Time.time;
    }

    private void FixedUpdate()
    {
        MoveBall();
        HandleJump();
    }

    private void MoveBall()
    {
        Vector3 moveDirection = GetCameraRelativeMoveDirection();

        if (moveDirection.sqrMagnitude < 0.01f)
            return;

        bool grounded = IsGrounded();
        float controlMultiplier = grounded ? 1f : airControlMultiplier;

        // Rolling torque
        if (rb.linearVelocity.magnitude < maxSpeed)
        {
            Vector3 torqueDirection = Vector3.Cross(Vector3.up, moveDirection);
            rb.AddTorque(torqueDirection * torqueForce * controlMultiplier, ForceMode.Acceleration);
        }

        if (grounded)
        {
            if (isDrifting)
                ApplyDrift(moveDirection);
            else
                ApplyGrip(moveDirection);
        }
        else
        {
            rb.AddForce(moveDirection * torqueForce * airControlMultiplier, ForceMode.Acceleration);
        }
    }

    private void ApplyGrip(Vector3 moveDirection)
    {
        Vector3 velocity = rb.linearVelocity;
        Vector3 horizontal = new Vector3(velocity.x, 0f, velocity.z);

        Vector3 desired = moveDirection * horizontal.magnitude;

        Vector3 corrected = Vector3.Lerp(
            horizontal,
            desired,
            normalGrip * Time.fixedDeltaTime
        );

        rb.linearVelocity = new Vector3(corrected.x, velocity.y, corrected.z);
    }

    private void ApplyDrift(Vector3 moveDirection)
    {
        Vector3 velocity = rb.linearVelocity;
        Vector3 horizontal = new Vector3(velocity.x, 0f, velocity.z);

        if (horizontal.sqrMagnitude < 0.1f)
            return;

        Vector3 currentDir = horizontal.normalized;

        Vector3 desiredDir = Vector3.Slerp(
            currentDir,
            moveDirection,
            driftGrip * Time.fixedDeltaTime
        );

        Vector3 drifted = Vector3.Lerp(
            desiredDir * horizontal.magnitude,
            horizontal,
            driftAmount
        );

        rb.linearVelocity = new Vector3(drifted.x, velocity.y, drifted.z);
    }

    private void OnJump(InputAction.CallbackContext ctx)
    {
        lastJumpPressedTime = Time.time;
    }

    private void HandleJump()
    {
        bool canJump =
            Time.time - lastGroundedTime <= coyoteTime &&
            Time.time - lastJumpPressedTime <= jumpBufferTime;

        if (!canJump)
            return;

        Vector3 moveDirection = GetCameraRelativeMoveDirection();

        Vector3 velocity = rb.linearVelocity;
        velocity.y = 0f;
        rb.linearVelocity = velocity;

        Vector3 jumpDir = Vector3.up;

        if (moveDirection.sqrMagnitude > 0.01f)
            jumpDir = (Vector3.up + moveDirection * 0.65f).normalized;

        rb.AddForce(jumpDir * jumpForce, ForceMode.Impulse);
        rb.AddForce(moveDirection * directionalJumpForce, ForceMode.Impulse);

        lastJumpPressedTime = -999f;
    }

    private Vector3 GetCameraRelativeMoveDirection()
    {
        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;

        forward.y = 0f;
        right.y = 0f;

        forward.Normalize();
        right.Normalize();

        return (forward * moveInput.y + right * moveInput.x).normalized;
    }

    private bool IsGrounded()
    {
        return Physics.Raycast(
            transform.position,
            Vector3.down,
            groundCheckDistance,
            groundLayer
        );
    }
}