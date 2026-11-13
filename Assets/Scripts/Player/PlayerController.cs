using UnityEngine;
using UnityEngine.InputSystem;

// Контроллер Дениса: сохранены имя класса, поля и CharacterController.
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    public float walkSpeed = 1.5f;
    public float runSpeed = 3.5f;
    public float gravity = -9.81f;
    public Transform playerCamera;
    public float mouseSensitivity = 2f;

    private CharacterController controller;
    private float verticalVelocity;
    private float cameraRotationX;
    private int blockedThroughFrame = -1;
    public bool ControlsBlocked { get; private set; }

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (playerCamera == null)
        {
            Camera childCamera = GetComponentInChildren<Camera>();
            if (childCamera != null) playerCamera = childCamera.transform;
        }
    }

    private void Start()
    {
        if (!ControlsBlocked) SetCursor(true);
    }

    private void Update()
    {
        if (ControlsBlocked || Time.frameCount <= blockedThroughFrame) return;
        Keyboard keyboard = Keyboard.current;
        Mouse mouse = Mouse.current;
        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame) SetCursor(false);
        else if (mouse != null && mouse.leftButton.wasPressedThisFrame) SetCursor(true);

        bool captured = Cursor.lockState == CursorLockMode.Locked;
        Vector2 input = Vector2.zero;
        if (captured && keyboard != null)
        {
            input.x = (keyboard.dKey.isPressed ? 1 : 0) - (keyboard.aKey.isPressed ? 1 : 0);
            input.y = (keyboard.wKey.isPressed ? 1 : 0) - (keyboard.sKey.isPressed ? 1 : 0);
        }
        input = Vector2.ClampMagnitude(input, 1f);
        float speed = keyboard != null && keyboard.leftShiftKey.isPressed ? runSpeed : walkSpeed;
        Vector3 movement = (transform.right * input.x + transform.forward * input.y) * speed;
        if (controller.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
        verticalVelocity += gravity * Time.deltaTime;
        movement.y = verticalVelocity;
        if (controller.enabled) controller.Move(movement * Time.deltaTime);

        if (!captured || mouse == null || playerCamera == null) return;
        // Mouse.delta уже содержит смещение за кадр, умножать на deltaTime не нужно.
        Vector2 look = mouse.delta.ReadValue() * (mouseSensitivity * 0.08f);
        transform.Rotate(Vector3.up * look.x);
        cameraRotationX = Mathf.Clamp(cameraRotationX - look.y, -80f, 80f);
        playerCamera.localRotation = Quaternion.Euler(cameraRotationX, 0f, 0f);
    }

    public void SetControlsBlocked(bool blocked)
    {
        ControlsBlocked = blocked;
        if (!blocked) blockedThroughFrame = Time.frameCount;
    }

    private void OnDisable()
    {
        SetCursor(false);
    }

    private static void SetCursor(bool captured)
    {
        Cursor.lockState = captured ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !captured;
    }
}
