using UnityEngine;
using UnityEngine.InputSystem;

// Контроллер Дениса: сохранены имя класса, поля и CharacterController.
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    public float walkSpeed = 1.5f;
    public float runSpeed = 3.5f;
    public float gravity = -9.81f;
    [Min(0f)] public float jumpHeight = 0.45f;
    [Min(1f), Tooltip("Масса в кг: влияет на толкание подвижных Rigidbody. Не меняет высоту прыжка.")]
    public float mass = 70f;
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
        if (captured && keyboard != null && keyboard.spaceKey.wasPressedThisFrame
            && controller.isGrounded && gravity < 0f)
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
        verticalVelocity += gravity * Time.deltaTime;
        movement.y = verticalVelocity;
        if (controller.enabled)
        {
            CollisionFlags collisions = controller.Move(movement * Time.deltaTime);
            if ((collisions & CollisionFlags.Above) != 0 && verticalVelocity > 0f)
                verticalVelocity = 0f;
        }

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

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        Rigidbody body = hit.rigidbody;
        if (ControlsBlocked || body == null || body.isKinematic || hit.moveDirection.y < -0.3f) return;
        Vector3 direction = new Vector3(hit.moveDirection.x, 0f, hit.moveDirection.z);
        Vector3 velocity = controller.velocity;
        float speed = new Vector2(velocity.x, velocity.z).magnitude;
        body.AddForceAtPosition(direction.normalized * mass * speed, hit.point, ForceMode.Force);
    }

    public void Teleport(Transform destination)
    {
        if (destination == null) return;
        if (controller == null) controller = GetComponent<CharacterController>();
        bool wasEnabled = controller.enabled;
        controller.enabled = false;
        transform.SetPositionAndRotation(destination.position, destination.rotation);
        verticalVelocity = 0f;
        cameraRotationX = 0f;
        if (playerCamera != null) playerCamera.localRotation = Quaternion.identity;
        controller.enabled = wasEnabled;
        Physics.SyncTransforms();
        blockedThroughFrame = Time.frameCount;
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
