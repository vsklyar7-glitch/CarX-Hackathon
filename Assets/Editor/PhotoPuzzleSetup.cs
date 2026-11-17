using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class PhotoPuzzleSetup
{
    public const string InitialDescription = "Отец и трое сыновей: Дмитрий, Иван и я. Обычный семейный снимок. Теперь трудно поверить, что когда-то мы могли просто собраться вместе.";
    public const string RestoredDescription = "Кто-то разорвал наш семейный снимок. Я собрал фрагменты: отец, Дмитрий, Иван и я снова вместе. Кто и зачем пытался уничтожить эту фотографию?";

    [MenuItem("Tools/Detective/Configure torn photograph")]
    public static void Apply()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Остановите Play Mode.");
        string prefabPath = "Assets/InteractionContent/Prefabs/FamilyPhoto.prefab";
        var prefab = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            SetDescription(prefab.GetComponent<EvidenceItem>(), false);
            PrefabUtility.SaveAsPrefabAsset(prefab, prefabPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(prefab); }
        foreach (string path in new[] { "Assets/InteractionContent/InteractionDemo.unity", "Assets/Scenes/Day1.unity", "Assets/Scenes/Day2.unity", "Assets/Scenes/Day3.unity" })
        {
            var scene = SceneManager.GetSceneByPath(path);
            bool opened = !scene.isLoaded;
            if (!opened && scene.isDirty) throw new InvalidOperationException("Сначала сохраните сцену: " + path);
            if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try
            {
                foreach (var item in scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<EvidenceItem>(true)).Where(e => e.evidenceId == "family_photo"))
                {
                    bool torn = path.EndsWith("Day2.unity");
                    SetDescription(item, path.EndsWith("Day3.unity") || torn);
                    item.requiresPhotoAssembly = torn;
                    if (torn && (item.tornPhotoVisual == null || item.tornPhotoVisual.transform.childCount != PhotoPuzzleLayout.Count)) BuildWorldPieces(item);
                    // Старые кресты больше не относятся к новому сюжету.
                    foreach (var t in item.GetComponentsInChildren<Transform>(true).Where(t => t.name.StartsWith("Crossed-out Alexei")).ToArray())
                        UnityEngine.Object.DestroyImmediate(t.gameObject);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(item);
                }
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
        }
        AssetDatabase.SaveAssets();
        Directory.CreateDirectory("output/photo-puzzle");
        File.WriteAllText("output/photo-puzzle/setup.txt", "Configured prefab, demo and Day1/Day2/Day3. Day2 requires 7 photo fragments.\n");
        Debug.Log("Torn photograph configured.");
    }

    private static void SetDescription(EvidenceItem item, bool restored)
    {
        item.description = restored ? RestoredDescription : InitialDescription;
        item.notebookDescription = InitialDescription;
        EditorUtility.SetDirty(item);
    }

    private static void BuildWorldPieces(EvidenceItem item)
    {
        var source = item.intactPhotoVisual != null ? item.intactPhotoVisual : item.gameObject;
        var surface = source.GetComponentsInChildren<MeshRenderer>(true).First(r => r.sharedMaterial != null && r.sharedMaterial.name == "FamilyPhotograph");
        Bounds bounds = surface.bounds;
        var material = surface.sharedMaterial;
        var intact = item.intactPhotoVisual;
        if (intact == null)
        {
            intact = new GameObject("Intact photograph");
            intact.transform.SetParent(item.transform, false);
            foreach (Transform child in item.transform.Cast<Transform>().Where(t => t != intact.transform).ToArray()) child.SetParent(intact.transform, true);
            item.intactPhotoVisual = intact;
        }
        var torn = item.tornPhotoVisual;
        if (torn == null)
        {
            torn = new GameObject("Torn photograph fragments");
            torn.transform.SetParent(item.transform, false);
            item.tornPhotoVisual = torn;
        }
        else foreach (Transform child in torn.transform.Cast<Transform>().ToArray()) UnityEngine.Object.DestroyImmediate(child.gameObject);
        Directory.CreateDirectory("Assets/EvidenceRealism/PhotoFragments");
        for (int i = 0; i < PhotoPuzzleLayout.Count; i++)
        {
            Vector2 center = PhotoPuzzlePiece.Center(i);
            Vector2[] outline = PhotoPuzzlePiece.Outline(i);
            var vertices = new Vector3[outline.Length + 1];
            var uv = new Vector2[vertices.Length];
            var triangles = new int[outline.Length * 3];
            uv[0] = center;
            for (int j = 0; j < outline.Length; j++)
            {
                Vector2 delta = outline[j] - center;
                vertices[j + 1] = new Vector3(delta.x * bounds.size.x, 0, delta.y * bounds.size.z);
                uv[j + 1] = outline[j];
                triangles[j * 3] = 0;
                triangles[j * 3 + 1] = (j + 1) % outline.Length + 1;
                triangles[j * 3 + 2] = j + 1;
            }
            string meshPath = "Assets/EvidenceRealism/PhotoFragments/Fragment" + i + ".asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if (mesh == null) { mesh = new Mesh(); AssetDatabase.CreateAsset(mesh, meshPath); }
            mesh.Clear(); mesh.vertices = vertices; mesh.uv = uv; mesh.triangles = triangles; mesh.RecalculateNormals(); mesh.RecalculateBounds();
            EditorUtility.SetDirty(mesh);
            var piece = new GameObject("Torn fragment " + (i + 1), typeof(MeshFilter), typeof(MeshRenderer));
            piece.transform.SetParent(torn.transform, true);
            piece.transform.position = new Vector3(bounds.center.x + (center.x - .5f) * bounds.size.x * 1.22f, bounds.max.y + .002f + i * .0002f, bounds.center.z + (center.y - .5f) * bounds.size.z * 1.18f);
            piece.transform.rotation = Quaternion.Euler(0, i % 2 == 0 ? -7 : 6, 0);
            piece.GetComponent<MeshFilter>().sharedMesh = mesh;
            piece.GetComponent<MeshRenderer>().sharedMaterial = material;
        }
        intact.SetActive(false);
        // Луч попадает по всей области разложенных фрагментов.
        var collider = item.GetComponent<BoxCollider>();
        collider.size = new Vector3(Mathf.Max(collider.size.x, bounds.size.x * 1.5f), collider.size.y, Mathf.Max(collider.size.z, bounds.size.z * 1.5f));
    }
}
