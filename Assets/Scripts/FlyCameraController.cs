using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class FlyCameraControllerInputSystem : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 8f;
    public float sprintMultiplier = 2.5f;
    public float verticalSpeed = 6f;

    [Header("Look")]
    [Tooltip("Grad pro Maus-Pixel (Daumenwert: 0.08 - 0.25)")]
    public float lookSensitivity = 0.12f;
    public bool invertY = false;
    public bool requireRightMouseToLook = true;
    public bool lockCursorOnStart = true;
    public float pitchMin = -85f;
    public float pitchMax = 85f;

    float _yaw;
    float _pitch;

    void Start()
    {
        Vector3 e = transform.eulerAngles;
        _yaw = e.y;
        _pitch = e.x;

        if (lockCursorOnStart)
            LockCursor(true);
    }

    void Update()
    {
        // Sicherheit: falls keine Devices vorhanden sind (z.B. Mobile/Build ohne Maus/Tastatur)
        if (Keyboard.current == null || Mouse.current == null)
            return;

        HandleLook();
        HandleMove();

        // Esc = Cursor freigeben (praktisch im Editor)
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
            LockCursor(false);
    }

    void HandleLook()
    {
        if (requireRightMouseToLook && !Mouse.current.rightButton.isPressed)
            return;

        Vector2 delta = Mouse.current.delta.ReadValue(); // Pixel pro Frame

        float mx = delta.x * lookSensitivity;
        float my = delta.y * lookSensitivity * (invertY ? 1f : -1f);

        _yaw += mx;
        _pitch += my;
        _pitch = Mathf.Clamp(_pitch, pitchMin, pitchMax);

        transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);

        // Optional: beim Looken Cursor immer locken, damit er nicht “rausläuft”
        if (lockCursorOnStart && Cursor.lockState != CursorLockMode.Locked)
            LockCursor(true);
    }

    void HandleMove()
    {
        float speed = moveSpeed * (Keyboard.current.leftShiftKey.isPressed ? sprintMultiplier : 1f);

        float x = 0f;
        float z = 0f; 

        if (Keyboard.current.aKey.isPressed) x -= 1f;
        if (Keyboard.current.dKey.isPressed) x += 1f;
        if (Keyboard.current.wKey.isPressed) z += 1f;
        if (Keyboard.current.sKey.isPressed) z -= 1f;

        float y = 0f; // hoch/runter
        if (Keyboard.current.eKey.isPressed) y += 1f;
        if (Keyboard.current.qKey.isPressed) y -= 1f;

        Vector3 planar = (transform.right * x + transform.forward * z) * speed;
        Vector3 vertical = Vector3.up * (y * verticalSpeed);

        transform.position += (planar + vertical) * Time.deltaTime;
    }

    void LockCursor(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }
}