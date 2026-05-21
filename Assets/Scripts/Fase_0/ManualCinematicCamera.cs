using UnityEngine;
using UnityEngine.InputSystem;

public class ManualCinematicCamera : MonoBehaviour
{
    [Header("Movimento")]
    public float moveSpeed = 2f;
    public float slowSpeed = 0.45f;
    public float moveSmoothness = 8f;

    [Header("Mouse Look")]
    public float mouseSensitivity = 0.01f;
    public float mouseSmoothness = 10f;
    public float mouseDeadZone = 0.03f;
    public float maxMouseDelta = 25f;

    [Header("Cursor / Tela")]
    public bool lockCursorOnStart = true;
    public bool forceFullscreenOnStart = true;
    public bool unlockCursorWithEscape = true;

    [Header("FOV")]
    public Camera cam;
    public float normalFOV = 45f;
    public float zoomFOV = 30f;
    public float minFOV = 25f;
    public float maxFOV = 70f;
    public float fovSpeed = 5f;
    public float scrollFOVSpeed = 4f;

    private float yaw;
    private float pitch;

    private Vector2 smoothedMouseDelta;
    private Vector3 currentVelocity;

    private void Start()
    {
        if (cam == null)
            cam = GetComponent<Camera>();

        Vector3 rotation = transform.eulerAngles;
        yaw = rotation.y;
        pitch = rotation.x;

        if (cam != null)
            cam.fieldOfView = normalFOV;

        if (forceFullscreenOnStart)
        {
            Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
            Screen.fullScreen = true;
        }

        if (lockCursorOnStart)
            SetCursorLocked(true);
    }

    private void Update()
    {
        if (Keyboard.current == null || Mouse.current == null)
            return;

        HandleCursor();
        LookAround();
        MoveCamera();
        HandleFOV();
    }

    private void HandleCursor()
    {
        if (unlockCursorWithEscape && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            SetCursorLocked(false);
        }

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            SetCursorLocked(true);
        }

        if (Keyboard.current.f11Key.wasPressedThisFrame)
        {
            Screen.fullScreen = !Screen.fullScreen;
        }
    }

    private void SetCursorLocked(bool locked)
    {
        if (locked)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    private void LookAround()
    {
        Vector2 rawMouseDelta = Mouse.current.delta.ReadValue();

        if (rawMouseDelta.magnitude < mouseDeadZone)
            rawMouseDelta = Vector2.zero;

        rawMouseDelta = Vector2.ClampMagnitude(rawMouseDelta, maxMouseDelta);

        smoothedMouseDelta = Vector2.Lerp(
            smoothedMouseDelta,
            rawMouseDelta,
            1f - Mathf.Exp(-mouseSmoothness * Time.deltaTime)
        );

        yaw += smoothedMouseDelta.x * mouseSensitivity;
        pitch -= smoothedMouseDelta.y * mouseSensitivity;

        pitch = Mathf.Clamp(pitch, -80f, 80f);

        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    private void MoveCamera()
    {
        float currentSpeed = moveSpeed;

        if (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed)
            currentSpeed = slowSpeed;

        Vector3 direction = Vector3.zero;

        if (Keyboard.current.wKey.isPressed)
            direction += transform.forward;

        if (Keyboard.current.sKey.isPressed)
            direction -= transform.forward;

        if (Keyboard.current.dKey.isPressed)
            direction += transform.right;

        if (Keyboard.current.aKey.isPressed)
            direction -= transform.right;

        if (Keyboard.current.spaceKey.isPressed)
            direction += Vector3.up;

        if (Keyboard.current.leftCtrlKey.isPressed || Keyboard.current.rightCtrlKey.isPressed)
            direction -= Vector3.up;

        Vector3 targetVelocity = direction.normalized * currentSpeed;

        currentVelocity = Vector3.Lerp(
            currentVelocity,
            targetVelocity,
            1f - Mathf.Exp(-moveSmoothness * Time.deltaTime)
        );

        transform.position += currentVelocity * Time.deltaTime;
    }

    private void HandleFOV()
    {
        if (cam == null)
            return;

        float scroll = Mouse.current.scroll.ReadValue().y;

        if (Mathf.Abs(scroll) > 0.01f)
        {
            normalFOV -= scroll * scrollFOVSpeed * Time.deltaTime;
            normalFOV = Mathf.Clamp(normalFOV, minFOV, maxFOV);
        }

        float targetFOV = normalFOV;

        if (Mouse.current.rightButton.isPressed)
            targetFOV = zoomFOV;

        cam.fieldOfView = Mathf.Lerp(
            cam.fieldOfView,
            targetFOV,
            Time.deltaTime * fovSpeed
        );
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus && lockCursorOnStart)
            SetCursorLocked(true);
    }
}