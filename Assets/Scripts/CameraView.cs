using UnityEngine;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif

public class CameraView : MonoBehaviour
{
    [Header("Target")]
    public Transform target; // pivot to orbit around. If left null, world origin (0,0,0) is used.

    [Header("Distance")]
    public float distance = 20f;
    public float minDistance = 2f;
    public float maxDistance = 50f;
    public float zoomSpeed = 2f;

    [Header("Rotation")]
    public float rotationSpeed = 540f; // degrees per second per mouse unit
    public bool requireMouseButton = true; // if true, hold rightMouseButtonToRotate to rotate
    public int mouseButtonToUse = 1; // 1 = right mouse button, 0 = left, 2 = middle
    public bool invertY = false;

    [Header("Pitch Limits")]
    [Tooltip("Clamp pitch (vertical) to avoid flipping the camera. Values are in degrees.")]
    public float minPitch = -80f;
    public float maxPitch = 80f;

    [Header("Smoothing")]
    public bool enableSmoothing = true;
    [Tooltip("Smooth time for rotation (seconds). Lower = snappier.")]
    public float rotationSmoothTime = 0.25f;
    [Tooltip("Smooth time for positional movement (seconds). Lower = snappier.")]
    public float positionSmoothTime = 0.25f;
    [Tooltip("Smooth time for zoom (seconds). Lower = snappier.")]
    public float zoomSmoothTime = 0.125f;

    // internal (target angles / smoothed values)
    float _yaw;   // target horizontal angle (unclamped / wraps 360)
    float _pitch; // target vertical angle (clamped between minPitch and maxPitch)

    float _smoothedYaw;
    float _smoothedPitch;
    float _smoothedDistance;

    // velocities for SmoothDamp
    float _yawVelocity;
    float _pitchVelocity;
    float _distanceVelocity;
    Vector3 _positionVelocity;

    void Start()
    {
        Vector3 angles = transform.eulerAngles;
        _yaw = angles.y;
        // EulerAngles.x -> normalize into -180..180 range for proper clamping
        float rawPitch = angles.x;
        if (rawPitch > 180f) rawPitch -= 360f;
        _pitch = Mathf.Clamp(rawPitch, minPitch, maxPitch);

        // initialize smoothed values to the starting state
        _smoothedYaw = _yaw;
        _smoothedPitch = _pitch;
        _smoothedDistance = distance;
        _positionVelocity = Vector3.zero;
        _yawVelocity = _pitchVelocity = _distanceVelocity = 0f;
    }

    void LateUpdate()
    {
        // Two supported input paths:
        // - New Input System (when ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER is defined)
        // - Legacy Input Manager (fallback)
        bool rotating = false;
        float mouseX = 0f;
        float mouseY = 0f;
        float scroll = 0f;

    #if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        var mouse = Mouse.current;
        if (mouse != null)
        {
            // Determine if rotation allowed based on configured mouse button
            if (!requireMouseButton)
            {
                rotating = true;
            }
            else
            {
                switch (mouseButtonToUse)
                {
                    case 0: rotating = mouse.leftButton.isPressed; break;
                    case 1: rotating = mouse.rightButton.isPressed; break;
                    case 2: rotating = mouse.middleButton.isPressed; break;
                    default: rotating = mouse.rightButton.isPressed; break;
                }
            }

            // Mouse delta is in pixels/frame. Scale to behave similarly to legacy Input.GetAxis.
            Vector2 delta = mouse.delta.ReadValue();
            const float newInputScale = 0.1f;
            mouseX = delta.x * newInputScale;
            mouseY = delta.y * newInputScale;

            // Scroll value
            scroll = mouse.scroll.ReadValue().y;
        }
    #else
        // Legacy input manager path (unchanged behavior)
        rotating = !requireMouseButton || Input.GetMouseButton(mouseButtonToUse);
        mouseX = Input.GetAxis("Mouse X");
        mouseY = Input.GetAxis("Mouse Y");
        scroll = Input.GetAxis("Mouse ScrollWheel");
    #endif

        if (rotating)
        {
            // Keep same sign convention as before: legacy used -mouseY to invert vertical drag
            _yaw += mouseX * rotationSpeed * Time.deltaTime;
            _pitch += (invertY ? mouseY : -mouseY) * rotationSpeed * Time.deltaTime;

            // Keep yaw free to complete full 360 (wrap to avoid large values)
            if (_yaw > 360f || _yaw < -360f)
            {
                _yaw = Mathf.Repeat(_yaw, 360f);
            }

            // Clamp pitch so the camera never flips over the top/bottom
            _pitch = Mathf.Clamp(_pitch, minPitch, maxPitch);
        }

        // Zoom (mouse wheel)
        if (Mathf.Abs(scroll) > 0.0001f)
        {
            distance = Mathf.Clamp(distance - scroll * zoomSpeed, minDistance, maxDistance);
        }

        // Smooth (or snap) yaw/pitch/distance
        if (enableSmoothing)
        {
            _smoothedYaw = Mathf.SmoothDampAngle(_smoothedYaw, _yaw, ref _yawVelocity, rotationSmoothTime);
            _smoothedPitch = Mathf.SmoothDampAngle(_smoothedPitch, _pitch, ref _pitchVelocity, rotationSmoothTime);
            _smoothedDistance = Mathf.SmoothDamp(_smoothedDistance, distance, ref _distanceVelocity, zoomSmoothTime);
        }
        else
        {
            _smoothedYaw = _yaw;
            _smoothedPitch = _pitch;
            _smoothedDistance = distance;
            _yawVelocity = _pitchVelocity = _distanceVelocity = 0f;
        }

        // Compute rotation and target position
        Quaternion rotation = Quaternion.Euler(_smoothedPitch, _smoothedYaw, 0f);

        Vector3 pivot = (target != null) ? target.position : Vector3.zero;
        Vector3 desiredPosition = pivot + rotation * new Vector3(0f, 0f, -_smoothedDistance);

        if (enableSmoothing)
        {
            transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref _positionVelocity, positionSmoothTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, rotation, Mathf.Clamp01(Time.deltaTime / Mathf.Max(0.0001f, rotationSmoothTime)));
        }
        else
        {
            transform.position = desiredPosition;
            transform.rotation = rotation;
            _positionVelocity = Vector3.zero;
        }
    }
}
