using UnityEngine;
using UnityEngine.InputSystem;

<<<<<<< HEAD
// Attach only to the player in Scene2. Runs once when the scene starts.
[DefaultExecutionOrder(-125)]
[RequireComponent(typeof(PlayerController), typeof(CharacterController))]
public class StationSeatedIntro : MonoBehaviour
{
    public Transform standingPoint;
    [Min(0.1f)] public float seatedCameraHeight = 1.12f;
    [Range(0f, 60f)] public float seatedLookDownAngle = 28f;
    [Min(0.1f)] public float standUpDuration = 1.2f;

    private PlayerController player;
    private CharacterController capsule;
    private Transform view;
    private Vector3 standingCameraPosition;
    private Quaternion standingCameraRotation;
    private Vector3 seatedPosition;
    private Vector3 seatedViewPosition;
    private Quaternion seatedViewRotation;
    private bool capsuleWasEnabled;
    private bool controlsWereBlocked;
    private bool initialized;
    private bool standingUp;
    private float elapsed;

    private void Start()
    {
        player = GetComponent<PlayerController>();
        capsule = GetComponent<CharacterController>();
        view = player.playerCamera;
        if (view == null || standingPoint == null)
        {
            Debug.LogError("StationSeatedIntro: assign Player Camera and Standing Point.", this);
            enabled = false;
            return;
        }

        standingCameraPosition = view.localPosition;
        standingCameraRotation = view.localRotation;
        seatedPosition = transform.position;
        seatedViewPosition = new Vector3(standingCameraPosition.x, seatedCameraHeight, standingCameraPosition.z);
        seatedViewRotation = Quaternion.Euler(seatedLookDownAngle, 0f, 0f);
        capsuleWasEnabled = capsule.enabled;
        controlsWereBlocked = player.ControlsBlocked;
        // Keep the standing capsule out of the chair until the player gets up.
        capsule.enabled = false;
        player.SetControlsBlocked(true);
        view.SetLocalPositionAndRotation(seatedViewPosition, seatedViewRotation);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        initialized = true;
=======
// Добавляется только игроку в Scene2, общий префаб не меняется.
[DefaultExecutionOrder(-50)]
[RequireComponent(typeof(PlayerController))]
public class StationSeatedIntro : MonoBehaviour
{
    public Vector3 standingPosition;
    public float standingCameraHeight = 1.45f;
    [Min(0.1f)] public float standDuration = 1.2f;

    private PlayerController player;
    private Vector3 seatedPosition;
    private Vector3 seatedCameraPosition;
    private float elapsed;
    private bool rising;
    private bool finished;

    private void Awake()
    {
        player = GetComponent<PlayerController>();
        player.MovementBlocked = true;
        seatedPosition = transform.position;
        seatedCameraPosition = player.playerCamera.localPosition;
>>>>>>> origin/main
    }

    private void Update()
    {
<<<<<<< HEAD
        if (!initialized) return;
        if (!standingUp)
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            if (Cursor.lockState != CursorLockMode.Locked || keyboard == null || Time.timeScale == 0f) return;
            if (!keyboard.wKey.wasPressedThisFrame && !keyboard.aKey.wasPressedThisFrame
                && !keyboard.sKey.wasPressedThisFrame && !keyboard.dKey.wasPressedThisFrame) return;
            standingUp = true;
        }

        elapsed += Time.deltaTime;
        float progress = Mathf.Clamp01(elapsed / standUpDuration);
        float smooth = Mathf.SmoothStep(0f, 1f, progress);
        transform.position = Vector3.Lerp(seatedPosition, standingPoint.position, smooth);
        view.localPosition = Vector3.Lerp(seatedViewPosition, standingCameraPosition, smooth);
        view.localRotation = Quaternion.Slerp(seatedViewRotation, standingCameraRotation, smooth);
        if (progress >= 1f) enabled = false;
=======
        if (finished || player.ControlsBlocked || Cursor.lockState != CursorLockMode.Locked) return;
        Keyboard keys = Keyboard.current;
        if (!rising && keys != null)
            rising = keys.wKey.isPressed || keys.aKey.isPressed || keys.sKey.isPressed
                || keys.dKey.isPressed || keys.leftShiftKey.isPressed || keys.spaceKey.isPressed;
        if (!rising) return;

        elapsed += Time.deltaTime;
        float progress = Mathf.Clamp01(elapsed / standDuration);
        float smooth = Mathf.SmoothStep(0f, 1f, progress);
        transform.position = Vector3.Lerp(seatedPosition, standingPosition, smooth);
        Vector3 cameraPosition = seatedCameraPosition;
        cameraPosition.y = Mathf.Lerp(seatedCameraPosition.y, standingCameraHeight, smooth);
        player.playerCamera.localPosition = cameraPosition;
        if (progress < 1f) return;

        Physics.SyncTransforms();
        player.MovementBlocked = false;
        finished = true;
>>>>>>> origin/main
    }

    private void OnDisable()
    {
<<<<<<< HEAD
        if (!initialized) return;
        view.SetLocalPositionAndRotation(standingCameraPosition, standingCameraRotation);
        capsule.enabled = capsuleWasEnabled;
        Physics.SyncTransforms();
        player.SetControlsBlocked(controlsWereBlocked);
        initialized = false;
=======
        if (player != null) player.MovementBlocked = false;
>>>>>>> origin/main
    }
}
