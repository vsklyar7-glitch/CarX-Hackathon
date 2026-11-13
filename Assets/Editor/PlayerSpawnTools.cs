using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Только редактор: быстро переносит игрока к точке проверки нужной локации.
public static class PlayerSpawnTools
{
    [MenuItem("Tools/Detective/Player spawn/Police station")]
    public static void PoliceStation() => MoveTo("imagecheck");

    [MenuItem("Tools/Detective/Player spawn/Apartment")]
    public static void Apartment() => MoveTo("americannredotoday1");

    [MenuItem("Tools/Detective/Player spawn/Basement")]
    public static void Basement() => MoveTo("Basement");

    private static void MoveTo(string location)
    {
        var scene = SceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/Scene2.unity") throw new InvalidOperationException("Сначала откройте Scene2.");
        var player = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
        var root = GameObject.Find(location);
        var spawn = root ? root.transform.Find("PlayerSpawn") : null;
        if (!player || !spawn) throw new InvalidOperationException("Не найден игрок или PlayerSpawn в " + location);

        Undo.RecordObject(player.transform, "Move player to " + location);
        var capsule = player.GetComponent<CharacterController>();
        bool wasEnabled = capsule.enabled;
        capsule.enabled = false;
        player.transform.SetPositionAndRotation(spawn.position, spawn.rotation);
        capsule.enabled = wasEnabled;
        Physics.SyncTransforms();
        if (!Application.isPlaying)
        {
            PrefabUtility.RecordPrefabInstancePropertyModifications(player.transform);
            EditorSceneManager.MarkSceneDirty(scene);
        }
        Selection.activeGameObject = player.gameObject;
    }
}
