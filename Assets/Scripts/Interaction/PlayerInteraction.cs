using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-100)]
[RequireComponent(typeof(PlayerController))]
public class PlayerInteraction : MonoBehaviour
{
    public Camera playerCamera;
    public InspectionUI inspectionUI;
    [Min(0.1f)] public float interactionDistance = 2.2f;
    // Маска включает стены и мебель: первый непрозрачный коллайдер перекрывает улику.
    public LayerMask raycastMask = Physics.DefaultRaycastLayers;
    private PlayerController controller;
    private IInteractable target;
    private bool modalOpen;
    private bool previousBlocked;
    private CursorLockMode previousCursor;
    private bool previousCursorVisible;
    private int closedFrame = -1;

    private void Awake()
    {
        controller = GetComponent<PlayerController>();
        if (playerCamera == null) playerCamera = GetComponentInChildren<Camera>();
        if (inspectionUI == null) inspectionUI = GetComponentInChildren<InspectionUI>(true);
        if (playerCamera == null || inspectionUI == null)
        {
            Debug.LogError("PlayerInteraction: назначьте Player Camera и Inspection UI.", this);
            enabled = false;
            return;
        }
        inspectionUI.SetPrompt("");
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (modalOpen)
        {
            if (keyboard != null && (keyboard.escapeKey.wasPressedThisFrame || keyboard.eKey.wasPressedThisFrame)) CloseInspection();
            return;
        }
        if (closedFrame == Time.frameCount) return;
        target = null;
        if (!controller.ControlsBlocked && Cursor.lockState == CursorLockMode.Locked)
            target = FindTarget();
        inspectionUI.SetPrompt(target != null ? target.Prompt : "");
        if (target != null && keyboard != null && keyboard.eKey.wasPressedThisFrame) target.Interact(this);
    }

    // Отдельный метод позволяет проверять дистанцию и препятствия без имитации клавиш.
    public IInteractable FindTarget()
    {
        if (playerCamera == null) return null;
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f));
        // Игнорируем только коллайдеры своего игрока; стены остаются препятствиями.
        RaycastHit[] hits = Physics.RaycastAll(ray, interactionDistance, raycastMask, QueryTriggerInteraction.Ignore);
        float nearest = float.PositiveInfinity;
        Collider first = null;
        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.transform.IsChildOf(transform) || hit.distance >= nearest) continue;
            nearest = hit.distance;
            first = hit.collider;
        }
        return first != null ? first.GetComponentInParent<IInteractable>() : null;
    }

    public void Inspect(EvidenceItem item)
    {
        if (modalOpen || item == null || controller.ControlsBlocked || inspectionUI == null) return;
        if (string.IsNullOrWhiteSpace(item.evidenceId))
        {
            Debug.LogError("У улики не заполнен Evidence Id.", item);
            return;
        }
        previousBlocked = controller.ControlsBlocked;
        previousCursor = Cursor.lockState;
        previousCursorVisible = Cursor.visible;
        modalOpen = true;
        controller.SetControlsBlocked(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        EvidenceJournal journal = EvidenceJournal.GetOrCreate();
        bool added = journal.Add(item.evidenceId, item.title);
        inspectionUI.Show(item, journal, added, CloseInspection);
    }

    public void CloseInspection()
    {
        if (!modalOpen) return;
        modalOpen = false;
        closedFrame = Time.frameCount;
        if (inspectionUI != null) inspectionUI.Hide();
        controller.SetControlsBlocked(previousBlocked);
        Cursor.lockState = previousCursor;
        Cursor.visible = previousCursorVisible;
    }

    private void OnDisable()
    {
        if (modalOpen) CloseInspection();
        if (inspectionUI != null) inspectionUI.SetPrompt("");
    }
}
