using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-150)]
[RequireComponent(typeof(PlayerController))]
public class PlayerNotebook : MonoBehaviour
{
    public NotebookView view;
    public bool IsOpen { get; private set; }
    private PlayerController controller;
    private CursorLockMode savedCursor;
    private bool savedCursorVisible;
    private Canvas inspectionCanvas;
    private bool savedCanvasEnabled;
    private PlayerInteraction interaction;
    private bool savedControlsBlocked;
    private int closedFrame = -1;
    public bool ConsumesModalInput => IsOpen || Time.frameCount == closedFrame;

    private void Start()
    {
        controller = GetComponent<PlayerController>();
        interaction = GetComponent<PlayerInteraction>();
        if (interaction != null && interaction.inspectionUI != null)
            inspectionCanvas = interaction.inspectionUI.GetComponent<Canvas>();
        if (view == null)
        {
            GameObject prefab = Resources.Load<GameObject>("NotebookCanvas");
            if (prefab == null) { Debug.LogError("Не найден Resources/NotebookCanvas.prefab. Выполните Tools/Detective/Build Notebook.", this); enabled = false; return; }
            view = Instantiate(prefab, transform).GetComponent<NotebookView>();
        }
        view.Initialize(Close);
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null || view == null) return;
        if (keyboard.jKey.wasPressedThisFrame)
        {
            if (IsOpen) Close(); else Open();
            return;
        }
        if (!IsOpen) return;
        if (keyboard.escapeKey.wasPressedThisFrame) { Close(); return; }
        if (keyboard.rightArrowKey.wasPressedThisFrame) view.Turn(1);
        else if (keyboard.leftArrowKey.wasPressedThisFrame) view.Turn(-1);
    }

    public bool Open()
    {
        if (interaction != null && (interaction.IsSolvingPhoto || interaction.IsReadingStationMail)) return false;
        if (IsOpen || view == null || (controller.ControlsBlocked && (interaction == null || !interaction.IsInspecting))) return false;
        savedControlsBlocked = controller.ControlsBlocked;
        savedCursor = Cursor.lockState;
        savedCursorVisible = Cursor.visible;
        IsOpen = true;
        if (inspectionCanvas != null) { savedCanvasEnabled = inspectionCanvas.enabled; inspectionCanvas.enabled = false; }
        controller.SetControlsBlocked(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        view.Open(EvidenceJournal.GetOrCreate());
        Debug.Log("Notebook opened; player controls blocked.");
        return true;
    }

    public void Close()
    {
        if (!IsOpen) return;
        IsOpen = false;
        closedFrame = Time.frameCount;
        if (view != null) view.Close();
        if (inspectionCanvas != null) inspectionCanvas.enabled = savedCanvasEnabled;
        if (controller != null) controller.SetControlsBlocked(savedControlsBlocked && interaction != null && interaction.IsInspecting);
        Cursor.lockState = savedCursor;
        Cursor.visible = savedCursorVisible;
        Debug.Log("Notebook closed; player controls restored.");
    }

    private void OnDisable() { if (IsOpen) Close(); }
}
