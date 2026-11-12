using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    public float walkSpeed = 2.2f;
    public float runSpeed = 3.5f;

    public float gravity = -9.81f;

    public Transform playerCamera;
    public float mouseSensitivity = 2f;

    private CharacterController controller;
    private float verticalVelocity;
    private float cameraRotationX = 0f;

    void Start()
    {
        controller = GetComponent<CharacterController>();

        // Блокируем курсор и прячем его
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        Move();
        Look();

        // Esc, чтобы освободить мышь
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        // Левая кнопка мыши, чтобы захватить курсор
        if (Input.GetMouseButtonDown(0))
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    void Move()
    {
        // Получаем нажатия клавиш
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        // Направление движения относительно персонажа
        Vector3 move = transform.right * x + transform.forward * z;
        move = Vector3.ClampMagnitude(move, 1f);

        // Обычная ходьба или бег
        float speed = Input.GetKey(KeyCode.LeftShift) ? runSpeed : walkSpeed;

        // Применяем движение
        controller.Move(move * speed * Time.deltaTime);

        // Гравитация
        if (controller.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
        }

        verticalVelocity += gravity * Time.deltaTime;

        controller.Move(
            Vector3.up * verticalVelocity * Time.deltaTime
        );
    }

    void Look()
    {
        // Если мышь свободна, не вращаем камеру
        if (Cursor.lockState != CursorLockMode.Locked)
            return;

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        // Поворот персонажа влево и вправо
        transform.Rotate(Vector3.up * mouseX);

        // Поворот камеры вверх и вниз
        cameraRotationX -= mouseY;
        cameraRotationX = Mathf.Clamp(cameraRotationX, -80f, 80f);

        playerCamera.localRotation =
            Quaternion.Euler(cameraRotationX, 0f, 0f);
    }
}