using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController_ThirdPerson : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference lookAction;
    [SerializeField] private InputActionReference sprintAction;
    [SerializeField] private InputActionReference crouchAction;
    [SerializeField] private InputActionReference jumpAction;
    [SerializeField] private InputActionReference pogoAction;
    [SerializeField] private InputActionReference dashAction;

    [Header("Camera")]
    [SerializeField] private Transform cameraRig;
    [SerializeField] private Transform cameraPitchPivot;
    [SerializeField] private Vector3 cameraFollowOffset = new Vector3(0f, 1.5f, 0f);
    [SerializeField] private float cameraFollowSpeed = 18f;
    [SerializeField] private float mouseSensitivity = 0.12f;
    [SerializeField] private float controllerSensitivity = 120f;
    [SerializeField] private float minPitch = -35f;
    [SerializeField] private float maxPitch = 70f;

    [Header("Movement")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float sprintSpeed = 8f;
    [SerializeField] private float crouchSpeed = 2.5f;
    [SerializeField] private float groundAcceleration = 35f;
    [SerializeField] private float groundDeceleration = 45f;
    [SerializeField] private float airAcceleration = 10f;
    [SerializeField] private float airControlMultiplier = 0.75f;
    [SerializeField] private float rotationSpeed = 16f;

    [Header("Jumping")]
    [SerializeField] private float jumpForce = 8f;
    [SerializeField] private float doubleJumpForce = 7.5f;
    [SerializeField] private float gravity = -28f;
    [SerializeField] private float fallGravityMultiplier = 1.7f;
    [SerializeField] private float lowJumpGravityMultiplier = 2.2f;
    [SerializeField] private float maxFallSpeed = -35f;
    [SerializeField] private float groundedGravity = -2f;
    [SerializeField] private float coyoteTime = 0.12f;
    [SerializeField] private float jumpBufferTime = 0.12f;

    [Header("Crouching")]
    [SerializeField] private float standingHeight = 2f;
    [SerializeField] private float crouchingHeight = 1f;
    [SerializeField] private float crouchTransitionSpeed = 12f;

    [Header("Air Dash")]
    [SerializeField] private float dashSpeed = 16f;
    [SerializeField] private float dashDuration = 0.18f;
    [SerializeField] private float dashCooldown = 0.25f;
    [SerializeField] private bool resetVerticalVelocityOnDash = true;

    [Header("Pogo")]
    [SerializeField] private LayerMask pogoLayer;
    [SerializeField] private float pogoDownSpeed = -24f;
    [SerializeField] private float pogoBounceForce = 10f;
    [SerializeField] private float pogoCheckDistance = 0.4f;
    [SerializeField] private float pogoCheckRadius = 0.35f;
    [SerializeField] private Transform pogoCheckPoint;

    [Header("Pogo Steering")]
    [SerializeField] private bool allowPogoSteering = true;
    [SerializeField] private bool pogoSteeringRequiresDash = true;
    [SerializeField] private float pogoSteerRotationSpeed = 14f;
    [SerializeField] private float pogoAimGravityMultiplier = 0.35f;
    [SerializeField] private float pogoLaunchSpeed = 28f;
    [SerializeField] private float pogoLaunchUpAmount = 0.1f;
    

    private CharacterController controller;

    private Vector2 moveInput;
    private Vector3 horizontalVelocity;
    private Vector3 dashDirection;
    private Vector3 pogoDirection = Vector3.down;

    private float verticalVelocity;
    private float cameraYaw;
    private float cameraPitch;

    private float coyoteCounter;
    private float jumpBufferCounter;
    private float dashTimer;
    private float dashCooldownTimer;

    private bool isGrounded;
    private bool isCrouching;
    private bool hasDoubleJumped;
    private bool hasAirDashed;
    private bool isDashing;
    private bool isPogoing;
    private bool isPogoAiming;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    private void OnEnable()
    {
        moveAction?.action.Enable();
        lookAction?.action.Enable();
        sprintAction?.action.Enable();
        crouchAction?.action.Enable();
        jumpAction?.action.Enable();
        pogoAction?.action.Enable();
        dashAction?.action.Enable();

        if (jumpAction != null)
            jumpAction.action.performed += OnJumpPressed;

        if (dashAction != null)
            dashAction.action.performed += OnDashPressed;

        if (pogoAction != null)
        {
            pogoAction.action.started += OnPogoStarted;
            pogoAction.action.canceled += OnPogoReleased;
        }
    }

    private void OnDisable()
    {
        if (jumpAction != null)
            jumpAction.action.performed -= OnJumpPressed;

        if (dashAction != null)
            dashAction.action.performed -= OnDashPressed;

        if (pogoAction != null)
        {
            pogoAction.action.started -= OnPogoStarted;
            pogoAction.action.canceled -= OnPogoReleased;
        }

        moveAction?.action.Disable();
        lookAction?.action.Disable();
        sprintAction?.action.Disable();
        crouchAction?.action.Disable();
        jumpAction?.action.Disable();
        pogoAction?.action.Disable();
        dashAction?.action.Disable();
    }

    private void Update()
    {
        ReadInput();
        CheckGrounded();

        HandleLook();
        FollowCameraRig();

        HandleCrouch();
        HandleTimers();
        HandleJumpBuffer();

        HandlePogoAiming();
        HandleMovement();
        HandleDash();
        HandleGravity();
        HandlePogoCheck();

        Vector3 finalMove = horizontalVelocity + Vector3.up * verticalVelocity;

        if (isDashing)
            finalMove = dashDirection * dashSpeed + Vector3.up * verticalVelocity;

        controller.Move(finalMove * Time.deltaTime);
    }

    private void ReadInput()
    {
        moveInput = moveAction != null ? moveAction.action.ReadValue<Vector2>() : Vector2.zero;
        isCrouching = crouchAction != null && crouchAction.action.IsPressed();
    }

    private void CheckGrounded()
    {
        isGrounded = controller.isGrounded;

        if (isGrounded)
        {
            coyoteCounter = coyoteTime;
            hasDoubleJumped = false;
            hasAirDashed = false;
            isPogoing = false;
            isPogoAiming = false;

            if (verticalVelocity < 0f)
                verticalVelocity = groundedGravity;
        }
        else
        {
            coyoteCounter -= Time.deltaTime;
        }
    }

    private void HandleLook()
    {
        if (lookAction == null || cameraRig == null || cameraPitchPivot == null)
            return;

        Vector2 lookInput = lookAction.action.ReadValue<Vector2>();

        bool usingMouse = Mouse.current != null && Mouse.current.delta.IsActuated();

        float lookX = usingMouse
            ? lookInput.x * mouseSensitivity
            : lookInput.x * controllerSensitivity * Time.deltaTime;

        float lookY = usingMouse
            ? lookInput.y * mouseSensitivity
            : lookInput.y * controllerSensitivity * Time.deltaTime;

        cameraYaw += lookX;
        cameraPitch -= lookY;
        cameraPitch = Mathf.Clamp(cameraPitch, minPitch, maxPitch);

        cameraRig.rotation = Quaternion.Euler(0f, cameraYaw, 0f);
        cameraPitchPivot.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
    }

    private void FollowCameraRig()
    {
        if (cameraRig == null)
            return;

        Vector3 targetPosition = transform.position + cameraFollowOffset;

        cameraRig.position = Vector3.Lerp(
            cameraRig.position,
            targetPosition,
            cameraFollowSpeed * Time.deltaTime
        );
    }

    private void HandleMovement()
    {
        if (isDashing || isPogoAiming)
            return;

        Vector3 desiredDirection = GetCameraRelativeMoveDirection();

        if (moveInput.sqrMagnitude < 0.01f)
            desiredDirection = Vector3.zero;

        float targetSpeed = walkSpeed;

        if (isCrouching)
            targetSpeed = crouchSpeed;
        else if (sprintAction != null && sprintAction.action.IsPressed())
            targetSpeed = sprintSpeed;

        Vector3 targetVelocity = desiredDirection * targetSpeed;

        float acceleration = isGrounded
            ? desiredDirection.sqrMagnitude > 0.01f ? groundAcceleration : groundDeceleration
            : airAcceleration * airControlMultiplier;

        horizontalVelocity = Vector3.MoveTowards(
            horizontalVelocity,
            targetVelocity,
            acceleration * Time.deltaTime
        );

        if (desiredDirection.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(desiredDirection);

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
        }
    }

    private void HandlePogoAiming()
    {
        if (!isPogoAiming)
            return;

        Vector3 aimDirection = GetCameraRelativeMoveDirection();

        if (moveInput.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(aimDirection, Vector3.up);

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                pogoSteerRotationSpeed * Time.deltaTime
            );

            pogoDirection = transform.forward;
        }
        else
        {
            pogoDirection = transform.forward;
        }
    }

    private void HandleDash()
    {
        if (!isDashing)
            return;

        dashTimer -= Time.deltaTime;

        horizontalVelocity = dashDirection * dashSpeed;

        if (dashTimer <= 0f)
        {
            isDashing = false;

            // Keeps dash momentum after the dash finishes.
            horizontalVelocity = dashDirection * dashSpeed;
        }
    }

    private void HandleGravity()
    {
        if (isPogoAiming)
        {
            verticalVelocity += gravity * pogoAimGravityMultiplier * Time.deltaTime;
            verticalVelocity = Mathf.Max(verticalVelocity, maxFallSpeed);
            return;
        }

        if (isDashing && resetVerticalVelocityOnDash)
            return;

        float gravityToUse = gravity;

        bool jumpHeld = jumpAction != null && jumpAction.action.IsPressed();

        if (verticalVelocity < 0f)
            gravityToUse *= fallGravityMultiplier;
        else if (verticalVelocity > 0f && !jumpHeld)
            gravityToUse *= lowJumpGravityMultiplier;

        verticalVelocity += gravityToUse * Time.deltaTime;
        verticalVelocity = Mathf.Max(verticalVelocity, maxFallSpeed);

        if (isPogoing && verticalVelocity > pogoDownSpeed && pogoDirection == Vector3.down)
            verticalVelocity = pogoDownSpeed;
    }

    private void HandleTimers()
    {
        if (jumpBufferCounter > 0f)
            jumpBufferCounter -= Time.deltaTime;

        if (dashCooldownTimer > 0f)
            dashCooldownTimer -= Time.deltaTime;
    }

    private void HandleJumpBuffer()
    {
        if (jumpBufferCounter <= 0f)
            return;

        if (coyoteCounter > 0f)
        {
            DoJump(jumpForce);
            jumpBufferCounter = 0f;
        }
        else if (!hasDoubleJumped)
        {
            DoJump(doubleJumpForce);
            hasDoubleJumped = true;
            jumpBufferCounter = 0f;
        }
    }

    private void OnJumpPressed(InputAction.CallbackContext context)
    {
        jumpBufferCounter = jumpBufferTime;
    }

    private void DoJump(float force)
    {
        verticalVelocity = force;
        isPogoing = false;
        isPogoAiming = false;
        isDashing = false;
        coyoteCounter = 0f;
    }

    private void OnDashPressed(InputAction.CallbackContext context)
    {
        if (isGrounded)
            return;

        if (hasAirDashed)
            return;

        if (dashCooldownTimer > 0f)
            return;

        Vector3 inputDirection = GetCameraRelativeMoveDirection();

        if (moveInput.sqrMagnitude > 0.01f)
            dashDirection = inputDirection;
        else
            dashDirection = transform.forward;

        dashDirection.y = 0f;
        dashDirection.Normalize();

        isDashing = true;
        hasAirDashed = true;
        isPogoing = false;
        isPogoAiming = false;

        dashTimer = dashDuration;
        dashCooldownTimer = dashCooldown;

        if (resetVerticalVelocityOnDash)
            verticalVelocity = 0f;
    }

    private void OnPogoStarted(InputAction.CallbackContext context)
    {
        if (isGrounded)
            return;

        bool canSteerPogo =
            allowPogoSteering &&
            (!pogoSteeringRequiresDash || hasAirDashed);

        if (canSteerPogo)
        {
            isPogoAiming = true;
            isDashing = false;
            isPogoing = false;
            return;
        }

        StartNormalPogo();
    }

    private void OnPogoReleased(InputAction.CallbackContext context)
    {
        if (!isPogoAiming)
            return;

        isPogoAiming = false;
        isPogoing = true;

        pogoDirection = transform.forward.normalized;

        horizontalVelocity = pogoDirection * pogoLaunchSpeed;
        verticalVelocity = 0f;
    }

    private void StartNormalPogo()
    {
        isPogoing = true;
        isDashing = false;
        isPogoAiming = false;

        pogoDirection = Vector3.down;
        verticalVelocity = pogoDownSpeed;
    }

    private void HandlePogoCheck()
    {
        if (!isPogoing)
            return;

        Vector3 checkOrigin = pogoCheckPoint != null
            ? pogoCheckPoint.position
            : transform.position + Vector3.up * 0.5f;

        bool hitPogoObject = Physics.SphereCast(
            checkOrigin,
            pogoCheckRadius,
            pogoDirection,
            out RaycastHit hit,
            pogoCheckDistance,
            pogoLayer
        );

        if (hitPogoObject)
        {
            verticalVelocity = pogoBounceForce;
            isPogoing = false;

            horizontalVelocity = -pogoDirection * pogoLaunchSpeed * 0.35f;

            pogoDirection = Vector3.down;
        }
    }

    private void HandleCrouch()
    {
        float targetHeight = isCrouching ? crouchingHeight : standingHeight;

        controller.height = Mathf.Lerp(
            controller.height,
            targetHeight,
            crouchTransitionSpeed * Time.deltaTime
        );

        controller.center = new Vector3(0f, controller.height / 2f, 0f);
    }

    private Vector3 GetCameraRelativeMoveDirection()
    {
        Vector3 inputDirection = new Vector3(moveInput.x, 0f, moveInput.y);

        if (inputDirection.sqrMagnitude < 0.01f || cameraRig == null)
            return transform.forward;

        inputDirection.Normalize();

        Vector3 camForward = cameraRig.forward;
        Vector3 camRight = cameraRig.right;

        camForward.y = 0f;
        camRight.y = 0f;

        camForward.Normalize();
        camRight.Normalize();

        Vector3 direction = camForward * inputDirection.z + camRight * inputDirection.x;

        return direction.normalized;
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 checkOrigin = pogoCheckPoint != null
            ? pogoCheckPoint.position
            : transform.position + Vector3.down * 0.9f;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(
            checkOrigin + pogoDirection * pogoCheckDistance,
            pogoCheckRadius
        );
    }
}