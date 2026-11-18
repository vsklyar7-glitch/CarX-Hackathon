using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class InvestigationDaySetup
{
    private static readonly string[] EvidenceIds = {
        "family_photo", "electricity_receipt", "basement_key_17", "dmitry_diary",
        "ivan_letter", "child_drawing", "father_note"
    };

    [MenuItem("Tools/Detective/Configure investigation days")]
    public static void Apply()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Остановите Play Mode.");
        var paths = new[] { "Assets/Scenes/Scene2.unity", "Assets/Scenes/Day1.unity",
            "Assets/Scenes/Day2.unity", "Assets/Scenes/Day3.unity" };
        var report = new List<string>();
        for (int day = 0; day <= 3; day++)
        {
            var scene = SceneManager.GetSceneByPath(paths[day]);
            bool opened = !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(paths[day], OpenSceneMode.Additive);
            try
            {
                var flow = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<InvestigationDayFlow>(true)).SingleOrDefault();
                if (!flow)
                {
                    var go = new GameObject("Investigation day flow");
                    SceneManager.MoveGameObjectToScene(go, scene);
                    flow = go.AddComponent<InvestigationDayFlow>();
                }
                flow.day = day;
                flow.requiredEvidenceIds = day == 0 ? Array.Empty<string>() : EvidenceIds.Take(day == 1 ? 3 : day == 2 ? 5 : 7).ToArray();
                var player = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<PlayerController>(true)).Single();
                if (day == 0) ConfigureStation(scene, flow);
                else ConfigureApartment(scene, day, player);
                var clues = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<EvidenceItem>(true)).ToArray();
                if (clues.Select(e => e.evidenceId).Distinct().Count() != clues.Length)
                    throw new InvalidOperationException("Повторяются ID улик в " + scene.path);
                foreach (var id in flow.requiredEvidenceIds)
                    if (!clues.Any(e => e.evidenceId == id)) throw new InvalidOperationException("Нет улики " + id);
                report.Add(scene.path + ": day=" + day + "; clues=" + string.Join(",", clues.Select(e => e.evidenceId)));
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
        }
        var settings = EditorBuildSettings.scenes.ToList();
        foreach (var path in paths)
        {
            var entry = settings.FirstOrDefault(s => s.path == path);
            if (entry == null) settings.Add(new EditorBuildSettingsScene(path, true));
            else entry.enabled = true;
        }
        var station = settings.Single(s => s.path == paths[0]);
        settings.Remove(station);
        settings.Insert(0, station);
        EditorBuildSettings.scenes = settings.ToArray();
        System.IO.Directory.CreateDirectory("output/investigation-days");
        System.IO.File.WriteAllLines("output/investigation-days/setup.txt", report);
    }

    private static void ConfigureStation(Scene scene, InvestigationDayFlow flow)
    {
        var root = scene.GetRootGameObjects().Single(g => g.name == "imagecheck");
        foreach (var exit in root.GetComponentsInChildren<StationExit>(true))
        {
            exit.dayFlow = flow;
            exit.apartmentSpawn = null;
        }
        var item = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<EvidenceItem>(true))
            .SingleOrDefault(e => e.evidenceId == "case_file");
        if (!item)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/InteractionContent/Prefabs/CaseFile.prefab"), scene);
            item = go.GetComponent<EvidenceItem>();
            go.name = "CaseFile";
        }
        var desk = root.GetComponentsInChildren<Renderer>().Single(r => r.name == "polySurface6163").bounds;
        Place(item, new Vector3(desk.center.x + .38f, desk.max.y + .008f, desk.center.z + .22f), 0);
    }

    private static void ConfigureApartment(Scene scene, int day, PlayerController player)
    {
        var flat = scene.GetRootGameObjects().Single(g => g.name.StartsWith("americannredotoday"));
        var meshes = flat.GetComponentsInChildren<Renderer>(true);
        float floor = meshes.Single(r => r.name == "pPlane9").bounds.max.y;
        // Позиции улик задаются в сценах и сохраняются при повторной настройке дней.
        if (day == 1)
        {
            var key = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<EvidenceItem>(true))
                .Single(item => item.evidenceId == "basement_key_17");
            key.transform.localScale = Vector3.one * 2.5f;
            var keyCollider = key.GetComponent<BoxCollider>();
            keyCollider.size = new Vector3(.084f, .024f, .152f);
            keyCollider.center = new Vector3(0, .008f, 0);
        }
        // All days begin just inside the apartment's existing front door.
        var frontDoor = meshes.Single(r => r.name == "pCube219").bounds;
        var spawn = flat.transform.Find("PlayerSpawn");
        spawn.position = new Vector3(frontDoor.center.x - 1.15f, floor + .035f, frontDoor.center.z);
        spawn.rotation = Quaternion.Euler(0, 270, 0);
        var controller = player.GetComponent<CharacterController>();
        bool enabled = controller.enabled;
        controller.enabled = false;
        player.transform.SetPositionAndRotation(spawn.position, spawn.rotation);
        controller.enabled = enabled;
        player.playerCamera.localRotation = Quaternion.identity;
        PrefabUtility.RecordPrefabInstancePropertyModifications(player.transform);
        PrefabUtility.RecordPrefabInstancePropertyModifications(player.playerCamera);
        foreach (var camera in flat.GetComponentsInChildren<Camera>(true))
        {
            camera.enabled = false;
            PrefabUtility.RecordPrefabInstancePropertyModifications(camera);
        }
        foreach (var listener in flat.GetComponentsInChildren<AudioListener>(true))
        {
            listener.enabled = false;
            PrefabUtility.RecordPrefabInstancePropertyModifications(listener);
        }
    }

    private static void Place(EvidenceItem item, Vector3 position, float yaw)
    {
        item.transform.SetPositionAndRotation(position, Quaternion.Euler(0, yaw, 0));
        PrefabUtility.RecordPrefabInstancePropertyModifications(item.transform);
    }
}
