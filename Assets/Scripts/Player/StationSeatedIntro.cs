using UnityEngine;
using UnityEngine.InputSystem;

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
    }

    private void Update()
    {
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
    }

    private void OnDisable()
    {
        if (player != null) player.MovementBlocked = false;
    }
}
