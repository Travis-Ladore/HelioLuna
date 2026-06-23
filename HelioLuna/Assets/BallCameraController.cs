using UnityEngine;
using UnityEngine.InputSystem;

public class BallCameraController : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private InputActionReference lookAction;

    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("Camera")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float distance = 6f;
    [SerializeField] private float height = 2f;

    [Header("Rotation")]
    [SerializeField] private float mouseSensitivity = 0.15f;
    [SerializeField] private float controllerSensitivity = 120f;
    [SerializeField] private float minPitch = -20f;
    [SerializeField] private float maxPitch = 65f;

    [Header("Smoothing")]
    [SerializeField] private float cameraSmoothSpeed = 12f;

    private float yaw;
    private float pitch = 25f;

    private void Awake()
    {
        if (target == null)
            target = transform;

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
    }

    private void OnEnable()
    {
        lookAction.action.Enable();
    }

    private void OnDisable()
    {
        lookAction.action.Disable();
    }

    private void LateUpdate()
    {
        HandleCameraInput();
        MoveCamera();
    }

    private void HandleCameraInput()
    {
        Vector2 lookInput = lookAction.action.ReadValue<Vector2>();

        bool usingMouse = Mouse.current != null && Mouse.current.delta.ReadValue().sqrMagnitude > 0.01f;

        float sensitivity = usingMouse ? mouseSensitivity : controllerSensitivity * Time.deltaTime;

        yaw += lookInput.x * sensitivity;
        pitch -= lookInput.y * sensitivity;

        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
    }

    private void MoveCamera()
    {
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);

        Vector3 targetPosition = target.position + Vector3.up * height;
        Vector3 desiredCameraPosition = targetPosition - rotation * Vector3.forward * distance;

        cameraTransform.position = Vector3.Lerp(
            cameraTransform.position,
            desiredCameraPosition,
            cameraSmoothSpeed * Time.deltaTime
        );

        cameraTransform.LookAt(targetPosition);
    }
}