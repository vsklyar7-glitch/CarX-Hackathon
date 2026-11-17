using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public static class VerifyStation
{
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    public static string Configure()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        Check(scene.path == "Assets/Scenes/Scene2.unity", "Wrong scene");
        var intros = UnityEngine.Object.FindObjectsByType<StationSeatedIntro>();
        Check(intros.Length == 1, "Expected one seated intro");
        Check(intros[0].standingPoint != null, "Standing Point missing");
        foreach (var go in scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).Select(t => t.gameObject))
            Check(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go) == 0, "Missing script: " + go.name);
        var camera = intros[0].GetComponent<PlayerController>().playerCamera;
        // Камера хранит смещение относительно игрока, а не мировую позицию.
        camera.localPosition = new Vector3(0, 1.45f, 0);
        camera.localRotation = Quaternion.identity;
        PrefabUtility.RecordPrefabInstancePropertyModifications(camera);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        return "Scene imported; no missing scripts; one intro; camera offset corrected";
    }

    public static async Task<string> Play()
    {
        var intro = UnityEngine.Object.FindAnyObjectByType<StationSeatedIntro>();
        var player = intro.GetComponent<PlayerController>();
        var capsule = intro.GetComponent<CharacterController>();
        Check(player.ControlsBlocked && !capsule.enabled, "Seated start not initialized");
        Check(Mathf.Abs(player.playerCamera.localPosition.y - intro.seatedCameraHeight) < .01f, "Wrong seated camera height");
        Vector3 destination = intro.standingPoint.position;
        var keyboard = Keyboard.current;
        Check(keyboard != null, "Keyboard missing");
        Cursor.lockState = CursorLockMode.Locked;
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W));
        await Task.Delay(150);
        InputSystem.QueueStateEvent(keyboard, new KeyboardState());
        await Task.Delay(1800);
        Check(!intro.enabled, "Intro did not finish on W");
        Check(capsule.enabled && !player.ControlsBlocked, "Controls/collider not restored");
        Check(Vector3.Distance(intro.transform.position, destination) < .15f, "Wrong standing position");
        Check(Vector3.Distance(player.playerCamera.localPosition, new Vector3(0, 1.45f, 0)) < .01f, "Camera did not restore");
        Debug.Log("STATION CHECK: seated start, W key, standing position, collider and controls restored; camera offset correct.");
        return "Station Play Mode checks passed";
    }
}
