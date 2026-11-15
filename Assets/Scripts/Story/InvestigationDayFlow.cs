using System;
using UnityEngine;
using UnityEngine.SceneManagement;

// Один компонент в каждой сцене; постоянный прогресс хранится в существующем блокноте.
public class InvestigationDayFlow : MonoBehaviour
{
    public static InvestigationDayFlow Current { get; private set; }
    [Tooltip("0 — участок; 1, 2, 3 — состояние квартиры.")]
    [Range(0, 3)] public int day;
    public string stationScene = "Assets/Scenes/Scene2.unity";
    public string[] apartmentScenes = {
        "Assets/Scenes/Day1.unity", "Assets/Scenes/Day2.unity", "Assets/Scenes/Day3.unity"
    };
    public string[] requiredEvidenceIds;
    public const string ReturnPrompt = "E - вернуться в участок и завершить смену";
    public bool CanReturnToStation { get; private set; }
    private EvidenceJournal journal;
    private bool loading;

    private void OnEnable()
    {
        Current = this;
        journal = EvidenceJournal.GetOrCreate();
        if (day > 0) journal.SetApartmentDay(day);
        journal.EvidenceAdded += OnEvidenceAdded;
        RefreshCompletion();
    }

    private void OnEvidenceAdded(string id) => RefreshCompletion();

    private void RefreshCompletion()
    {
        CanReturnToStation = day > 0 && day < 3 && requiredEvidenceIds != null
            && requiredEvidenceIds.Length > 0
            && Array.TrueForAll(requiredEvidenceIds, id => !string.IsNullOrEmpty(id) && journal.Contains(id));
    }

    public void LeaveStation(PlayerInteraction player)
    {
        if (day != 0 || apartmentScenes == null || apartmentScenes.Length < 3) return;
        Load(apartmentScenes[journal.ApartmentDay - 1], player, false);
    }

    public void ReturnToStation(PlayerInteraction player)
    {
        if (CanReturnToStation) Load(stationScene, player, true);
    }

    private void Load(string path, PlayerInteraction player, bool endShift)
    {
        if (loading || player == null || player.IsInspecting) return;
        var controller = player.GetComponent<PlayerController>();
        if (controller.ControlsBlocked) return;
        if (SceneUtility.GetBuildIndexByScenePath(path) < 0)
        {
            Debug.LogError("Сцена не включена в Build Settings: " + path, this);
            return;
        }
        loading = true;
        int previousDay = journal.ApartmentDay;
        if (endShift) journal.SetApartmentDay(day + 1);
        controller.SetControlsBlocked(true);
        player.inspectionUI.SetPrompt("");
        try { SceneManager.LoadScene(path, LoadSceneMode.Single); }
        catch (Exception error)
        {
            journal.SetApartmentDay(previousDay);
            loading = false;
            controller.SetControlsBlocked(false);
            Debug.LogException(error, this);
        }
    }

    private void OnDisable()
    {
        if (journal != null) journal.EvidenceAdded -= OnEvidenceAdded;
        if (Current == this) Current = null;
    }
}
