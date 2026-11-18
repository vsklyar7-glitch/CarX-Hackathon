using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ApartmentDoorNumbers
{
    private const string Folder = "Assets/ApartmentDays/DoorNumber";

    [MenuItem("Tools/Detective/Update apartment door numbers")]
    public static void ApplyAll()
    {
        for (int day = 1; day <= 3; day++)
        {
            var scene = SceneManager.GetSceneByPath("Assets/Scenes/Day" + day + ".unity");
            bool opened = !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene("Assets/Scenes/Day" + day + ".unity", OpenSceneMode.Additive);
            try
            {
                var roots = scene.GetRootGameObjects();
                var door = roots.SelectMany(g => g.GetComponentsInChildren<Transform>(true)).Single(t => t.name == "Basement door 17");
                Attach(door);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
        }
        AssetDatabase.SaveAssets();
    }

    public static void Attach(Transform door)
    {
        if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/ApartmentDays", "DoorNumber");
        var hinge = door.Find("Door hinge");
        var leaf = hinge.GetComponentsInChildren<MeshFilter>(true).Single(f => f.name == "pCube217");
        var vertices = leaf.sharedMesh.vertices;
        var bounds = new Bounds(hinge.InverseTransformPoint(leaf.transform.TransformPoint(vertices[0])), Vector3.zero);
        foreach (var vertex in vertices) bounds.Encapsulate(hinge.InverseTransformPoint(leaf.transform.TransformPoint(vertex)));
        var old = hinge.Find("Door number");
        if (old) UnityEngine.Object.DestroyImmediate(old.gameObject);
        var number = new GameObject("Door number").transform;
        number.SetParent(hinge, false);
        number.localPosition = new Vector3(bounds.center.x, bounds.min.y + bounds.size.y * .60f, bounds.max.z + .014f);
        number.localRotation = Quaternion.Euler(0, 180, 0);
        var material = AssetDatabase.LoadAssetAtPath<Material>(Folder + "/AgedBrass.mat");
        if (!material)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "AgedBrass" };
            material.SetColor("_BaseColor", new Color(.62f, .44f, .19f));
            material.SetFloat("_Metallic", .72f);
            material.SetFloat("_Smoothness", .32f);
            AssetDatabase.CreateAsset(material, Folder + "/AgedBrass.mat");
        }
        material.SetColor("_BaseColor", new Color(.78f, .60f, .30f));
        material.SetFloat("_Metallic", .35f);
        EditorUtility.SetDirty(material);
        AddDigit(number, "1", new Vector2[] { new Vector2(.008f,0),new Vector2(.082f,0),new Vector2(.082f,.014f),
            new Vector2(.055f,.014f),new Vector2(.055f,.16f),new Vector2(.037f,.16f),new Vector2(.008f,.133f),
            new Vector2(.019f,.120f),new Vector2(.035f,.135f),new Vector2(.035f,.014f),new Vector2(.008f,.014f) }, -.106f, material);
        AddDigit(number, "7", new Vector2[] { new Vector2(.005f,.16f),new Vector2(.105f,.16f),new Vector2(.105f,.14f),
            new Vector2(.046f,0),new Vector2(.023f,0),new Vector2(.081f,.139f),new Vector2(.005f,.139f) }, .001f, material);
    }

    private static void AddDigit(Transform parent, string digit, Vector2[] polygon, float x, Material material)
    {
        string path = Folder + "/Digit" + digit + ".asset";
        var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (!mesh) { mesh = Extrude(polygon); mesh.name = "Bevelled brass digit " + digit; AssetDatabase.CreateAsset(mesh, path); }
        var go = new GameObject("Brass digit " + digit, typeof(MeshFilter), typeof(MeshRenderer));
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(x, -.08f, 0);
        go.GetComponent<MeshFilter>().sharedMesh = mesh;
        go.GetComponent<MeshRenderer>().sharedMaterial = material;
    }

    private static float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;

    // Solid polygon extrusion with a small bevel on both faces. No font or runtime generator required.
    private static Mesh Extrude(Vector2[] polygon)
    {
        float area = 0;
        for (int i = 0; i < polygon.Length; i++) area += Cross(polygon[i], polygon[(i + 1) % polygon.Length]);
        if (area < 0) Array.Reverse(polygon);
        int count = polygon.Length;
        var inset = new Vector2[count];
        for (int i = 0; i < count; i++)
        {
            var previous = polygon[(i + count - 1) % count];
            var current = polygon[i]; var next = polygon[(i + 1) % count];
            var a = (current - previous).normalized; var b = (next - current).normalized;
            var startA = current + new Vector2(-a.y, a.x) * .0012f;
            var startB = current + new Vector2(-b.y, b.x) * .0012f;
            float denominator = Cross(a, b);
            inset[i] = Mathf.Abs(denominator) < .00001f ? startA : startA + a * (Cross(startB - startA, b) / denominator);
        }
        var vertices = new List<Vector3>(); var triangles = new List<int>();
        Action<Vector3, Vector3, Vector3> triangle = (a,b,c) => {
            int index = vertices.Count; vertices.Add(a); vertices.Add(b); vertices.Add(c);
            triangles.Add(index); triangles.Add(index + 1); triangles.Add(index + 2);
        };
        var indices = Enumerable.Range(0, count).ToList();
        int safety = count * count;
        while (indices.Count > 2 && safety-- > 0)
        {
            bool clipped = false;
            for (int i = 0; i < indices.Count; i++)
            {
                int a = indices[(i + indices.Count - 1) % indices.Count], b = indices[i], c = indices[(i + 1) % indices.Count];
                if (Cross(inset[b] - inset[a], inset[c] - inset[b]) <= .00000001f) continue;
                bool inside = indices.Any(p => p != a && p != b && p != c
                    && Cross(inset[b] - inset[a], inset[p] - inset[a]) >= 0
                    && Cross(inset[c] - inset[b], inset[p] - inset[b]) >= 0
                    && Cross(inset[a] - inset[c], inset[p] - inset[c]) >= 0);
                if (inside) continue;
                triangle(new Vector3(inset[a].x,inset[a].y,.012f),new Vector3(inset[b].x,inset[b].y,.012f),new Vector3(inset[c].x,inset[c].y,.012f));
                triangle(new Vector3(inset[c].x,inset[c].y,0),new Vector3(inset[b].x,inset[b].y,0),new Vector3(inset[a].x,inset[a].y,0));
                indices.RemoveAt(i); clipped = true; break;
            }
            if (!clipped) throw new InvalidOperationException("Could not triangulate digit.");
        }
        var loops = new[] { inset, polygon, polygon, inset };
        float[] depths = { 0, .0012f, .0108f, .012f };
        for (int layer = 0; layer < 3; layer++)
            for (int i = 0; i < count; i++)
            {
                int j = (i + 1) % count;
                var a = new Vector3(loops[layer][i].x,loops[layer][i].y,depths[layer]);
                var b = new Vector3(loops[layer][j].x,loops[layer][j].y,depths[layer]);
                var c = new Vector3(loops[layer+1][j].x,loops[layer+1][j].y,depths[layer+1]);
                var d = new Vector3(loops[layer+1][i].x,loops[layer+1][i].y,depths[layer+1]);
                triangle(a,b,c); triangle(a,c,d);
            }
        var mesh = new Mesh(); mesh.SetVertices(vertices); mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
    }
}
