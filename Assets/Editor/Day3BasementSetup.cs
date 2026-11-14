using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Day3BasementSetup
{
    [MenuItem("Tools/Detective/Connect basement to Day3")]
    public static void Apply()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Остановите Play Mode.");
        var destination = SceneManager.GetSceneByPath("Assets/Scenes/Day3.unity");
        if (!destination.isLoaded)
            destination = EditorSceneManager.OpenScene("Assets/Scenes/Day3.unity", OpenSceneMode.Additive);
        if (destination.GetRootGameObjects().Any(g => g.name == "Basement"))
            throw new InvalidOperationException("В Day3 уже есть Basement; повторное добавление отменено.");
        var source = SceneManager.GetSceneByPath("Assets/Scenes/Scene2.unity");
        bool openedSource = !source.isLoaded;
        if (openedSource)
            source = EditorSceneManager.OpenScene("Assets/Scenes/Scene2.unity", OpenSceneMode.Additive);
        try
        {
            var original = source.GetRootGameObjects().Single(g => g.name == "Basement");
            var apartment = destination.GetRootGameObjects().Single(g => g.name.StartsWith("americannredotoday"));
            var entrance = destination.GetRootGameObjects()
                .SelectMany(g => g.GetComponentsInChildren<Transform>(true))
                .Single(t => t.name == "Basement door 17");
            var copy = UnityEngine.Object.Instantiate(original);
            copy.name = "Basement";
            SceneManager.MoveGameObjectToScene(copy, destination);
            // Keep the room width; raise the low imported doorway to fit the player.
            copy.transform.SetPositionAndRotation(Vector3.zero, Quaternion.Euler(0, 180, 0));
            copy.transform.localScale = new Vector3(.25f, .375f, .25f);
            var body = copy.GetComponentsInChildren<MeshFilter>().Single(f => f.name == "body");
            var originalMesh = body.sharedMesh;
            int doorIndex = Array.FindIndex(body.GetComponent<Renderer>().sharedMaterials,
                material => material.name == "door");
            if (doorIndex < 0) throw new InvalidOperationException("Не найдена геометрия входной двери.");
            var points = originalMesh.GetTriangles(doorIndex).Distinct()
                .Select(i => body.transform.TransformPoint(originalMesh.vertices[i])).ToArray();
            var opening = new Bounds(points[0], Vector3.zero);
            foreach (var point in points) opening.Encapsulate(point);
            float floor = apartment.GetComponentsInChildren<Renderer>().Single(r => r.name == "pPlane9").bounds.max.y;
            var target = new Vector3(entrance.position.x, floor + .015f, entrance.position.z - .08f);
            copy.transform.position += target - new Vector3(opening.center.x, opening.min.y, opening.center.z);

            // The existing open door 17 is the shared entrance, not a second closed leaf.
            var mesh = UnityEngine.Object.Instantiate(originalMesh);
            mesh.name = "Day3 basement walls with open entrance";
            mesh.SetTriangles(Array.Empty<int>(), doorIndex);
            mesh.RecalculateBounds();
            const string meshPath = "Assets/ApartmentDays/Day3BasementBody.asset";
            AssetDatabase.CreateAsset(mesh, meshPath);
            body.sharedMesh = mesh;
            var darkness = entrance.Find("Darkness beyond door");
            if (darkness) UnityEngine.Object.DestroyImmediate(darkness.gameObject);
            entrance.Find("Door hinge").localRotation = Quaternion.Euler(0, -95, 0);

            // One detailed collider per mesh: shelves, pipes, furniture and steps keep their openings.
            foreach (var filter in copy.GetComponentsInChildren<MeshFilter>(true))
            {
                if (!filter.sharedMesh || filter.sharedMesh.vertexCount < 3) continue;
                var collider = filter.GetComponent<MeshCollider>();
                if (!collider) collider = filter.gameObject.AddComponent<MeshCollider>();
                collider.sharedMesh = null;
                collider.convex = false;
                collider.isTrigger = false;
                collider.sharedMesh = filter.sharedMesh;
                collider.enabled = true;
                foreach (var extra in filter.GetComponents<Collider>().Where(c => c != collider).ToArray())
                    UnityEngine.Object.DestroyImmediate(extra);
            }
            foreach (var camera in copy.GetComponentsInChildren<Camera>(true)) camera.enabled = false;
            foreach (var listener in copy.GetComponentsInChildren<AudioListener>(true)) listener.enabled = false;
            AddSkinnedColliders(copy);
            EditorSceneManager.MarkSceneDirty(destination);
            EditorSceneManager.SaveScene(destination);
            AssetDatabase.SaveAssets();
            SceneManager.SetActiveScene(destination);
            Selection.activeGameObject = copy;
        }
        finally
        {
            if (openedSource) EditorSceneManager.CloseScene(source, true);
        }
    }

    // These two meshes are posed corpses, so their scene pose is a static collision mesh.
    public static void AddSkinnedColliders(GameObject basement)
    {
        foreach (var renderer in basement.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            if (!renderer.sharedMesh || renderer.GetComponent<MeshCollider>()) continue;
            var mesh = new Mesh { name = "Day3 collision " + renderer.name };
            renderer.BakeMesh(mesh);
            AssetDatabase.CreateAsset(mesh, "Assets/ApartmentDays/Day3Basement_" + renderer.name + "_Collision.asset");
            var collider = renderer.gameObject.AddComponent<MeshCollider>();
            collider.sharedMesh = mesh;
            collider.convex = false;
            collider.isTrigger = false;
        }
    }
}
