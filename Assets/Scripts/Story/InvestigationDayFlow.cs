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
    [Header("Переход между участком и квартирой")]
    public AudioClip transitionSoundDay1;
    public AudioClip transitionSoundDay2;
    public AudioClip transitionSoundDay3;
    [Min(0f)] public float fadeOutDuration = 1.5f;
    [Min(0f)] public float fadeInDuration = 1.5f;
    [Range(0f, 1f)] public float transitionVolume = 1f;
    public const string ReturnPrompt = "E - вернуться в участок и завершить смену";
    public bool CanReturnToStation { get; private set; }
    private EvidenceJournal journal;

    private void OnEnable()
    {
        Current = this;
        journal = EvidenceJournal.GetOrCreate();
        if (day > 0) journal.SetApartmentDay(day);
        journal.EvidenceAdded += OnEvidenceAdded;
        journal.PhotoRestored += RefreshCompletion;
        RefreshCompletion();
    }

    private void OnEvidenceAdded(string id) => RefreshCompletion();

    private void RefreshCompletion()
    {
        CanReturnToStation = day > 0 && day < 3 && requiredEvidenceIds != null
            && (day != 2 || journal.FamilyPhotoRestored)
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
        if (player == null || player.IsInspecting) return;
        var controller = player.GetComponent<PlayerController>();
        if (controller.ControlsBlocked) return;
        if (SceneUtility.GetBuildIndexByScenePath(path) < 0)
        {
            Debug.LogError("Сцена не включена в Build Settings: " + path, this);
            return;
        }
        AudioClip clip = journal.ApartmentDay == 1 ? transitionSoundDay1
            : journal.ApartmentDay == 2 ? transitionSoundDay2 : transitionSoundDay3;
        Action onArrived = null;
        if (endShift)
        {
            var currentJournal = journal;
            int nextDay = day + 1;
            onArrived = () => currentJournal.SetApartmentDay(nextDay);
        }
        ApartmentTransition.Begin(path, player, endShift, clip, fadeOutDuration, fadeInDuration,
            transitionVolume, onArrived);
    }

    private void OnDisable()
    {
        if (journal != null) journal.EvidenceAdded -= OnEvidenceAdded;
        if (journal != null) journal.PhotoRestored -= RefreshCompletion;
        if (Current == this) Current = null;
    }
}
