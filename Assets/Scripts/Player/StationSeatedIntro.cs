using UnityEngine;
using UnityEngine.InputSystem;

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
        controlsWereBlocked = player.ControlsBlockedByGameplay;
        // Keep the standing capsule out of the chair until the player gets up.
        capsule.enabled = false;
        player.SetControlsBlocked(true);
        view.SetLocalPositionAndRotation(seatedViewPosition, seatedViewRotation);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        initialized = true;
    }

    private void Update()
    {
        if (!initialized || ApartmentTransition.Current != null) return;
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
    }

    private void OnDisable()
    {
        if (!initialized) return;
        view.SetLocalPositionAndRotation(standingCameraPosition, standingCameraRotation);
        capsule.enabled = capsuleWasEnabled;
        Physics.SyncTransforms();
        player.SetControlsBlocked(controlsWereBlocked);
        initialized = false;
    }
}
