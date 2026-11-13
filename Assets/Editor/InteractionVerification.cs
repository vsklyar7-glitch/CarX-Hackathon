using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

// Проверяет реальные префабы и физику в Play Mode. Не запускается без команды.
[InitializeOnLoad]
public static class InteractionVerification
{
    private const string Key = "Detective.InteractionVerification";
    private static PlayerInteraction player;
    private static PlayerController controller;
    private static EvidenceItem item;
    private static Vector3 position;
    private static Quaternion rotation;
    private static int phaseFrame;
    private static int phase;
    private static int events;
    private static Keyboard testKeyboard;
    private static Mouse testMouse;

    static InteractionVerification() { EditorApplication.update += Tick; }

    [MenuItem("Tools/Detective/Verify Interaction Demo")]
    public static void Run()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode first.");
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        EditorSceneManager.OpenScene("Assets/InteractionContent/InteractionDemo.unity");
        SessionState.SetBool(Key, true);
        phase = 0;
        events = 0;
        EditorApplication.isPlaying = true;
    }

    public static void GenerateAndRun()
    {
        if (Application.isBatchMode && string.IsNullOrEmpty(UnityEngine.SceneManagement.SceneManager.GetActiveScene().path))
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), "Assets/ValidationHost.unity");
        InteractionContentBuilder.Generate();
        Run();
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    private static void Tick()
    {
        if (!SessionState.GetBool(Key, false) || !EditorApplication.isPlaying || Time.frameCount < 5) return;
        try
        {
            if (phase == 0)
            {
                player = UnityEngine.Object.FindAnyObjectByType<PlayerInteraction>();
                controller = player.GetComponent<PlayerController>();
                Require(player.enabled && player.inspectionUI != null, "Player references missing");
                EvidenceItem[] items = UnityEngine.Object.FindObjectsByType<EvidenceItem>();
                Require(items.Length == 8, "Expected 8 scene evidence items");
                var ids = new System.Collections.Generic.HashSet<string>();
                foreach (EvidenceItem evidence in items) Require(ids.Add(evidence.evidenceId), "Duplicate evidence ID");
                item = items[0];
                // Убираем выбранную улику со стола и помещаем точно перед камерой.
                item.transform.position = player.playerCamera.transform.position + player.playerCamera.transform.forward;
                Physics.SyncTransforms();
                Require(ReferenceEquals(player.FindTarget(), item), "Cannot target evidence");
                GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wall.transform.position = player.playerCamera.transform.position + player.playerCamera.transform.forward * 0.5f;
                wall.transform.localScale = Vector3.one * 0.15f;
                Physics.SyncTransforms();
                Require(player.FindTarget() == null, "Interaction passes through wall");
                UnityEngine.Object.DestroyImmediate(wall);
                item.transform.position = player.playerCamera.transform.position + player.playerCamera.transform.forward * 4;
                Physics.SyncTransforms();
                Require(player.FindTarget() == null, "Interaction exceeds distance");
                item.transform.position = player.playerCamera.transform.position + player.playerCamera.transform.forward;
                Physics.SyncTransforms();
                EvidenceJournal.GetOrCreate().EvidenceAdded += _ => events++;
                player.Inspect(item);
                Require(controller.ControlsBlocked && player.inspectionUI.panel.activeSelf, "Inspection does not lock controller");
                position = controller.transform.position;
                rotation = player.playerCamera.transform.rotation;
                testKeyboard = InputSystem.AddDevice<Keyboard>();
                testMouse = InputSystem.AddDevice<Mouse>();
                InputSystem.QueueStateEvent(testKeyboard, new KeyboardState(UnityEngine.InputSystem.Key.W));
                InputSystem.QueueDeltaStateEvent(testMouse.delta, new Vector2(100, 80));
                phaseFrame = Time.frameCount;
                phase = 1;
            }
            else if (phase == 1 && Time.frameCount > phaseFrame + 4)
            {
                Require(controller.transform.position == position && player.playerCamera.transform.rotation == rotation, "Player moves during inspection");
                InputSystem.QueueStateEvent(testKeyboard, new KeyboardState());
                player.inspectionUI.closeButton.onClick.Invoke();
                Require(!controller.ControlsBlocked && !player.inspectionUI.panel.activeSelf, "Close button does not restore controls");
                player.Inspect(item);
                Require(EvidenceJournal.Instance.Count == 1 && events == 1, "Repeated inspection duplicates evidence/event");
                player.CloseInspection();
                controller.SetControlsBlocked(true);
                player.Inspect(item);
                Require(!player.inspectionUI.panel.activeSelf && controller.ControlsBlocked, "External control lock ignored");
                controller.SetControlsBlocked(false);
                Directory.CreateDirectory("Documentation");
                File.WriteAllText("Documentation/interaction-verification.txt", "PASS — Unity " + Application.unityVersion + " Play Mode\n8 unique evidence items; camera targeting; wall occlusion; distance limit; movement/camera lock across frames; close button; duplicate prevention; single evidence event; external control lock.\n");
                Debug.Log("INTERACTION_VERIFICATION_PASS");
                Finish(0);
            }
        }
        catch (Exception error)
        {
            Directory.CreateDirectory("Documentation");
            File.WriteAllText("Documentation/interaction-verification.txt", "FAIL\n" + error);
            Debug.LogException(error);
            Finish(1);
        }
    }

    private static void Finish(int exitCode)
    {
        if (testKeyboard != null) InputSystem.RemoveDevice(testKeyboard);
        if (testMouse != null) InputSystem.RemoveDevice(testMouse);
        SessionState.SetBool(Key, false);
        EditorApplication.isPlaying = false;
        if (Application.isBatchMode) EditorApplication.delayCall += () => EditorApplication.Exit(exitCode);
    }
}
