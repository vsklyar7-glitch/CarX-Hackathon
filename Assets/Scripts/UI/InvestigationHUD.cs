using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

// Подсказки управления и текущая задача на общем Canvas игрока.
public class InvestigationHUD : MonoBehaviour
{
    public TMP_Text controlsText;
    public TMP_Text toggleText;
    public TMP_Text objectiveText;
    public Key toggleKey = Key.H;
    public bool showHintsOnStart = true;

    // Выбор сохраняется только в рамках текущего прохождения, между сценами.
    private static bool? hintsVisible;
    private PlayerInteraction interaction;
    private PlayerNotebook notebook;
    private StationSeatedIntro seatedIntro;
    private InvestigationDayFlow flow;
    private EvidenceJournal journal;
    private bool wasSeated;
    private int apartmentDay;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetHints() => hintsVisible = null;

    private void Start()
    {
        interaction = GetComponentInParent<PlayerInteraction>();
        if (interaction != null)
        {
            notebook = interaction.GetComponent<PlayerNotebook>();
            seatedIntro = interaction.GetComponent<StationSeatedIntro>();
        }
        flow = InvestigationDayFlow.Current;
        journal = EvidenceJournal.GetOrCreate();
        journal.EvidenceAdded += OnEvidenceAdded;
        journal.PhotoRestored += RefreshObjective;
        if (!hintsVisible.HasValue) hintsVisible = showHintsOnStart;
        wasSeated = IsSeated;
        apartmentDay = journal.ApartmentDay;
        RefreshControls();
        RefreshObjective();
    }

    private bool IsSeated => seatedIntro != null && seatedIntro.isActiveAndEnabled;

    private void LateUpdate()
    {
        bool modal = (interaction != null && interaction.IsInspecting)
            || (notebook != null && notebook.IsOpen) || ApartmentTransition.Current != null;
        var keyboard = Keyboard.current;
        if (!modal && keyboard != null && keyboard[toggleKey].wasPressedThisFrame)
        {
            hintsVisible = !hintsVisible.GetValueOrDefault(showHintsOnStart);
            RefreshControls();
        }
        if (wasSeated != IsSeated)
        {
            wasSeated = IsSeated;
            RefreshControls();
            RefreshObjective();
        }
        if (journal != null && apartmentDay != journal.ApartmentDay)
        {
            apartmentDay = journal.ApartmentDay;
            RefreshObjective();
        }
        if (controlsText != null) controlsText.enabled = !modal && hintsVisible.GetValueOrDefault(showHintsOnStart);
        if (notebook != null && notebook.view != null && notebook.view.shortcutHint != null)
            notebook.view.shortcutHint.enabled = !modal && !IsSeated && hintsVisible.GetValueOrDefault(showHintsOnStart);
        if (toggleText != null) toggleText.enabled = !modal;
        if (objectiveText != null) objectiveText.enabled = !modal;
    }

    private void RefreshControls()
    {
        if (controlsText != null)
            controlsText.text = IsSeated ? "WASD — встать"
                : "WASD — ходьба   •   Мышь — обзор   •   Shift — бег   •   Пробел — прыжок\nE — осмотр   •   J — блокнот   •   Esc — курсор";
        if (toggleText != null)
            toggleText.text = toggleKey + (hintsVisible.GetValueOrDefault(showHintsOnStart)
                ? " — скрыть подсказки" : " — показать подсказки");
    }

    private void OnEvidenceAdded(string id) => RefreshObjective();

    private void RefreshObjective()
    {
        if (objectiveText == null) return;
        string objective;
        if (flow == null) objective = "Осмотрите улики вокруг вас.";
        else if (flow.day == 0)
        {
            if (IsSeated) objective = "Встаньте из-за стола.";
            else if (journal.ApartmentDay == 1 && !journal.Contains("case_file"))
                objective = "Осмотрите папку дела на столе.";
            else objective = "Покиньте участок и отправляйтесь в квартиру №17.";
        }
        else
        {
            int missing = 0;
            if (flow.requiredEvidenceIds != null)
                foreach (string id in flow.requiredEvidenceIds)
                    if (!string.IsNullOrEmpty(id) && !journal.Contains(id)) missing++;
            if (missing > 0) objective = "Исследуйте квартиру. Найдите оставшиеся улики: " + missing + ".";
            else if (flow.day == 2 && !journal.FamilyPhotoRestored)
                objective = "Соберите семейную фотографию.";
            else if (flow.CanReturnToStation) objective = "Вернитесь в участок и завершите смену: E.";
            else objective = "Найдите дверь в подвал.";
        }
        objectiveText.text = "ТЕКУЩАЯ ЗАДАЧА\n" + objective;
    }

    private void OnDestroy()
    {
        if (journal == null) return;
        journal.EvidenceAdded -= OnEvidenceAdded;
        journal.PhotoRestored -= RefreshObjective;
    }
}
