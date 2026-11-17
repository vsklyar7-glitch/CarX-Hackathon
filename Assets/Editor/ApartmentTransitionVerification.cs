using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;

public static class ApartmentTransitionVerification
{
    [MenuItem("Tools/Detective/Verify apartment transitions (Play Mode)")]
    public static async void RunAll()
    {
        try
        {
            for (int day = 1; day <= 3; day++) await Run(day);
            File.WriteAllText("output/apartment-transition/result.txt", "PASS: all three apartment transitions.");
            Debug.Log("PASS: all three apartment transitions.");
        }
        catch (Exception error)
        {
            Directory.CreateDirectory("output/apartment-transition");
            File.WriteAllText("output/apartment-transition/result.txt", "FAIL: " + error);
            Debug.LogException(error);
        }
    }

    public static async Task<object> Run(int day)
    {
        const string station = "Assets/Scenes/Scene2.unity";
        if (!Application.isPlaying) throw new Exception("Enter Play Mode first.");
        if (SceneManager.GetActiveScene().path != station)
        {
            SceneManager.LoadScene(station);
            await Task.Delay(500);
        }
        int frame = Time.frameCount;
        await NextFrame();
        Check(Time.frameCount > frame, "Player loop is not advancing");
        var journal = EvidenceJournal.GetOrCreate();
        journal.SetApartmentDay(day);
        int evidenceCount = journal.Count;
        var player = UnityEngine.Object.FindObjectsByType<PlayerInteraction>()
            .Single(p => p.gameObject.scene.path == station);
        var controller = player.GetComponent<PlayerController>();
        var intro = player.GetComponent<StationSeatedIntro>();
        if (intro != null) intro.enabled = false;
        var exit = player.nearbyExits.First(e => e != null && e.isActiveAndEnabled && e.dayFlow != null);
        var area = exit.GetComponent<BoxCollider>();
        var destination = new GameObject("Transition test position");
        destination.transform.position = area.transform.TransformPoint(area.center) - Vector3.up * 0.9f;
        controller.Teleport(destination.transform);
        UnityEngine.Object.Destroy(destination);
        Check(exit.IsNearby(player), "Player is outside the exit zone");
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        await NextFrame();
        double started = Time.realtimeSinceStartupAsDouble;
        bool fadingOut = false, heardSound = false, fadingIn = false, destinationBlocked = false;
        float lastAlpha = 0f;
        string expectedClip = day == 1 ? "transitionSound" : "transitionAudioDay" + day;
        string destinationPath = "Assets/Scenes/Day" + day + ".unity";
        string folder = "output/apartment-transition";
        Directory.CreateDirectory(folder);
        Check(player.FindTarget() == exit, "Door is not the interaction target");
        // Тот же обработчик двери, который вызывает PlayerInteraction по E.
        // Ввод с виртуального устройства в нефокусированном Editor нестабилен.
        exit.Interact(player);
        Check(ApartmentTransition.Current != null, "Door action did not start transition");
        Check(controller.ControlsBlocked, "Source controls are not blocked");
        Check(!player.GetComponent<PlayerNotebook>().Open(), "Notebook opens during transition");
        exit.dayFlow.LeaveStation(player);
        Check(UnityEngine.Object.FindObjectsByType<ApartmentTransition>().Length == 1, "Duplicate transition");
        while (ApartmentTransition.Current != null && Time.realtimeSinceStartupAsDouble - started < 40)
        {
            var transition = ApartmentTransition.Current;
            float alpha = transition.GetComponent<CanvasGroup>().alpha;
            var audio = transition.GetComponent<AudioSource>();
            bool inApartment = SceneManager.GetActiveScene().path == destinationPath;
            if (!inApartment)
            {
                Check(alpha + 0.001f >= lastAlpha, "Fade-out reversed");
                if (alpha > 0.1f && alpha < 0.9f && !fadingOut)
                {
                    fadingOut = true;
                    ScreenCapture.CaptureScreenshot(folder + "/day" + day + "-fade-out.png");
                }
                if (audio.isPlaying)
                {
                    Check(alpha == 1f, "Sound started before full black");
                    Check(audio.clip != null && audio.clip.name == expectedClip, "Wrong day audio");
                    if (!heardSound) ScreenCapture.CaptureScreenshot(folder + "/day" + day + "-black.png");
                    heardSound = true;
                }
            }
            else
            {
                var arrived = UnityEngine.Object.FindObjectsByType<PlayerController>()
                    .Single(p => p.gameObject.scene.path == destinationPath);
                Check(arrived.ControlsBlocked, "Destination controls released before fade-in completed");
                Check(!arrived.GetComponent<PlayerNotebook>().Open(), "Destination notebook opens during fade-in");
                destinationBlocked = true;
                if (alpha > 0.1f && alpha < 0.9f && !fadingIn)
                {
                    fadingIn = true;
                    ScreenCapture.CaptureScreenshot(folder + "/day" + day + "-fade-in.png");
                }
            }
            lastAlpha = alpha;
            await NextFrame();
        }
        await NextFrame();
        Check(ApartmentTransition.Current == null, "Transition did not finish");
        Check(SceneManager.GetActiveScene().path == destinationPath, "Wrong destination scene");
        Check(fadingOut && heardSound && fadingIn && destinationBlocked, "A transition phase was skipped");
        var finalPlayer = UnityEngine.Object.FindObjectsByType<PlayerController>()
            .Single(p => p.gameObject.scene.path == destinationPath);
        Check(!finalPlayer.ControlsBlocked, "Destination controls remain blocked");
        Check(Cursor.lockState == CursorLockMode.Locked && !Cursor.visible, "Cursor not restored");
        Check(journal == EvidenceJournal.Instance && journal.Count == evidenceCount, "Evidence progress changed");
        Check(finalPlayer.GetComponent<PlayerNotebook>().Open(), "Notebook unavailable after transition");
        finalPlayer.GetComponent<PlayerNotebook>().Close();
        ScreenCapture.CaptureScreenshot(folder + "/day" + day + "-arrived.png");
        double seconds = Time.realtimeSinceStartupAsDouble - started;
        Check(seconds > 13.5, "Sound was cut short");
        string report = "PASS Day" + day + ": door interaction handler, fade-out, " + expectedClip
            + ", scene load, locked destination fade-in, control/cursor restore, notebook, no duplicate transition, progress retained; seconds=" + seconds;
        File.WriteAllText(folder + "/day" + day + ".txt", report);
        return new { day, seconds, fadingOut, heardSound, fadingIn, destinationBlocked, evidenceCount };

    }

    private static async Task NextFrame()
    {
        int frame = Time.frameCount;
        double until = Time.realtimeSinceStartupAsDouble + 5;
        do { await Task.Delay(16); }
        while (Time.frameCount == frame && Time.realtimeSinceStartupAsDouble < until);
        Check(Time.frameCount != frame, "Player loop stopped");
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}
