using System;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class ApartmentTransitionVerification
{
    private const string Station = "Assets/Scenes/Scene2.unity";

    [MenuItem("Tools/Detective/Verify apartment transitions (Play Mode)")]
    public static async void RunAll()
    {
        try
        {
            for (int day = 1; day <= 3; day++) Debug.Log(await Run(day));
        }
        catch (Exception error) { Debug.LogException(error); }
    }

    public static async Task<object> Run(int day)
    {
        Check(Application.isPlaying, "Enter Play Mode first.");
        if (SceneManager.GetActiveScene().path != Station)
        {
            SceneManager.LoadScene(Station);
            await WaitUntil(() => SceneManager.GetActiveScene().path == Station);
        }
        await NextFrame();
        var previousBackgroundBehavior = InputSystem.settings.backgroundBehavior;
        var previousEditorInputBehavior = InputSystem.settings.editorInputBehaviorInPlayMode;
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings.editorInputBehaviorInPlayMode =
            InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        var keyboard = InputSystem.AddDevice<Keyboard>();
        try
        {
            if (UnityEngine.Object.FindAnyObjectByType<GameIntro>() != null)
            {
                QueueKey(keyboard, Key.Enter);
                await WaitUntil(() => UnityEngine.Object.FindAnyObjectByType<GameIntro>() == null);
                ReleaseKeys(keyboard);
                await NextFrame();
            }

            var journal = EvidenceJournal.GetOrCreate();
            journal.SetApartmentDay(day);
            journal.ReadStationMail();
            var item = UnityEngine.Object.FindObjectsByType<EvidenceItem>().Single(e => e.evidenceId == "case_file");
            journal.Add(item);
            int evidenceCount = journal.Count;
            var player = UnityEngine.Object.FindFirstObjectByType<PlayerInteraction>();
            var controller = player.GetComponent<PlayerController>();
            var intro = player.GetComponent<StationSeatedIntro>();
            if (intro != null) intro.enabled = false;
            var exit = player.nearbyExits.First(e => e != null && e.isActiveAndEnabled && e.dayFlow != null);
            var area = exit.GetComponent<BoxCollider>();
            var destination = new GameObject("Transition test position");
            destination.transform.position = area.transform.TransformPoint(area.center) - Vector3.up * .9f;
            controller.Teleport(destination.transform);
            UnityEngine.Object.Destroy(destination);
            Check(player.FindTarget() == exit, "Door is not the interaction target.");
            exit.Interact(player);
            string dayPath = $"Assets/Scenes/Day{day}.unity";
            string sound = day == 1 ? "transitionSound" : "transitionAudioDay" + day;
            var outward = VerifyWaiting(player, "StationToApartment", sound);
            Check(!player.GetComponent<PlayerNotebook>().Open(), "Notebook opens during transition.");
            exit.dayFlow.LeaveStation(player);
            Check(ApartmentTransition.Current == outward, "Duplicate transition.");

            // Один переход переживает звук целиком. Остальные проверяют ранний пропуск.
            var audio = outward.GetComponent<AudioSource>();
            if (day == 1)
            {
                await WaitUntil(() => !audio.isPlaying, audio.clip.length + 4);
                Check(SceneManager.GetActiveScene().path == Station && ApartmentTransition.Current == outward,
                    "Scene changed when audio ended.");
                VerifyWaiting(player, "StationToApartment", sound);
            }
            else Check(audio.isPlaying, "Audio did not start with splash.");

            await Continue(keyboard, Key.Enter, outward, dayPath);
            player = UnityEngine.Object.FindFirstObjectByType<PlayerInteraction>();
            Check(!player.GetComponent<PlayerController>().ControlsBlocked, "Apartment controls remain blocked.");
            Check(journal == EvidenceJournal.Instance && journal.Count == evidenceCount, "Evidence progress changed.");

            if (day < 3)
            {
                var flow = InvestigationDayFlow.Current;
                foreach (string id in flow.requiredEvidenceIds) journal.Add(id, id);
                if (day == 2) journal.RestoreFamilyPhoto();
                Check(flow.CanReturnToStation, "Return is unavailable.");
                flow.ReturnToStation(player);
                var returning = VerifyWaiting(player, "ApartmentToStation", "transitionSound");
                await NextFrame();
                Check(SceneManager.GetActiveScene().path == dayPath && journal.ApartmentDay == day,
                    "Return advanced before confirmation.");
                await Continue(keyboard, Key.NumpadEnter, returning, Station);
                Check(journal.ApartmentDay == day + 1, "Return did not advance the day.");

                var stationIntro = UnityEngine.Object.FindFirstObjectByType<StationSeatedIntro>();
                Check(stationIntro != null && stationIntro.enabled, "Station seated intro did not start.");
                QueueKey(keyboard, Key.W);
                await NextFrame();
                ReleaseKeys(keyboard);
                await WaitUntil(() => !stationIntro.enabled);
                Check(!stationIntro.GetComponent<PlayerController>().ControlsBlocked,
                    "Station controls remain blocked after standing up.");
            }
            return $"PASS Day{day}: splash and prompt immediately visible, Enter gates scene load, progress retained.";
        }
        finally
        {
            InputSystem.RemoveDevice(keyboard);
            InputSystem.settings.editorInputBehaviorInPlayMode = previousEditorInputBehavior;
            InputSystem.settings.backgroundBehavior = previousBackgroundBehavior;
        }
    }

    private static ApartmentTransition VerifyWaiting(PlayerInteraction player, string image, string sound)
    {
        var transition = ApartmentTransition.Current;
        Check(transition != null && player.GetComponent<PlayerController>().ControlsBlocked,
            "Transition did not block player controls.");
        var photo = transition.GetComponentInChildren<RawImage>();
        var prompt = transition.GetComponentInChildren<TMP_Text>();
        Check(photo.texture != null && photo.texture.name == image, "Wrong transition image.");
        Check(prompt.text == "Enter - продолжить" && prompt.color == Color.white &&
            prompt.GetComponentInParent<CanvasGroup>().alpha == 1f, "Prompt is not immediately visible.");
        Check(transition.GetComponent<AudioSource>().clip?.name == sound, "Wrong transition audio.");
        return transition;
    }

    private static async Task Continue(Keyboard keyboard, Key key, ApartmentTransition transition, string path)
    {
        await NextFrame();
        QueueKey(keyboard, key);
        await WaitUntil(() => !transition.GetComponent<AudioSource>().isPlaying &&
            transition.GetComponentInChildren<RawImage>().GetComponentInParent<CanvasGroup>().alpha < 1f);
        ReleaseKeys(keyboard);
        await WaitUntil(() => SceneManager.GetActiveScene().path == path);
        var controller = UnityEngine.Object.FindFirstObjectByType<PlayerController>();
        Check(ApartmentTransition.Current != null && controller.ControlsBlocked, "Destination was unlocked under the overlay.");
        await WaitUntil(() => ApartmentTransition.Current == null);
    }

    private static void QueueKey(Keyboard keyboard, Key key)
    {
        keyboard.MakeCurrent();
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(key));
    }

    private static void ReleaseKeys(Keyboard keyboard) =>
        InputSystem.QueueStateEvent(keyboard, new KeyboardState());

    private static async Task NextFrame()
    {
        int frame = Time.frameCount;
        await WaitUntil(() => Time.frameCount != frame);
    }

    private static async Task WaitUntil(Func<bool> condition, float timeout = 8)
    {
        double until = Time.realtimeSinceStartupAsDouble + timeout;
        while (!condition() && Time.realtimeSinceStartupAsDouble < until) await Task.Delay(16);
        Check(condition(), "Timed out waiting for transition state.");
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
