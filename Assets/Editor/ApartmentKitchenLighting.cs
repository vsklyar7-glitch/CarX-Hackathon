using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ApartmentKitchenLighting
{
    [MenuItem("Tools/Detective/Update kitchen lighting")]
    public static void ApplyAll()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Остановите Play Mode.");
        var sourceScene = SceneManager.GetSceneByPath("Assets/Scenes/Day2.unity");
        bool openedSource = !sourceScene.isLoaded;
        if (openedSource) sourceScene = EditorSceneManager.OpenScene("Assets/Scenes/Day2.unity", OpenSceneMode.Additive);
        try
        {
            var source = sourceScene.GetRootGameObjects().Single(g => g.name.StartsWith("americannredotoday"));
            var sourceLamp = source.GetComponentsInChildren<Renderer>(true).Single(r => r.name == "pCylinder204");
            var sourceLight = source.GetComponentsInChildren<Light>(true).Single(l => l.name == "areaLight1");
            var sourceDiffuser = source.transform.Find("Kitchen lamp diffuser");
        for (int day = 1; day <= 3; day++)
        {
            var scene = SceneManager.GetSceneByPath("Assets/Scenes/Day" + day + ".unity");
            bool opened = !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene("Assets/Scenes/Day" + day + ".unity", OpenSceneMode.Additive);
            try
            {
                var flat = scene.GetRootGameObjects().Single(g => g.name.StartsWith("americannredotoday"));
                var lamp = flat.GetComponentsInChildren<Renderer>(true).Single(r => r.name == "pCylinder204");
                var light = flat.GetComponentsInChildren<Light>(true).Single(l => l.name == "areaLight1");
                if (day != 2)
                {
                    CopyTransform(sourceLamp.transform, lamp.transform);
                    lamp.sharedMaterials = sourceLamp.sharedMaterials;
                    EditorUtility.CopySerialized(sourceLight, light);
                    CopyTransform(sourceLight.transform, light.transform);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(lamp);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(light);
                    var diffuser = flat.transform.Find("Kitchen lamp diffuser");
                    if (!sourceDiffuser && diffuser) UnityEngine.Object.DestroyImmediate(diffuser.gameObject);
                    else if (sourceDiffuser)
                    {
                        if (!diffuser)
                        {
                            var go = UnityEngine.Object.Instantiate(sourceDiffuser.gameObject, flat.transform);
                            go.name = sourceDiffuser.name;
                            diffuser = go.transform;
                        }
                        CopyTransform(sourceDiffuser, diffuser);
                        diffuser.GetComponent<Renderer>().sharedMaterials = sourceDiffuser.GetComponent<Renderer>().sharedMaterials;
                    }
                }
                ConfigureRoomSpots(flat);
                var atmosphere = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<ApartmentDayAtmosphere>(true)).Single();
                atmosphere.roomLights = atmosphere.roomLights.Where(l => l && l.enabled && l.intensity > 0)
                    .Concat(new[] { light }).Distinct().ToArray();
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
        }
        }
        finally { if (openedSource) EditorSceneManager.CloseScene(sourceScene, true); }
        AssetDatabase.SaveAssets();
    }

    private static void CopyTransform(Transform source, Transform target)
    {
        target.localPosition = source.localPosition;
        target.localRotation = source.localRotation;
        target.localScale = source.localScale;
        PrefabUtility.RecordPrefabInstancePropertyModifications(target);
    }

    private static void ConfigureRoomSpots(GameObject flat)
    {
        foreach (var light in flat.GetComponentsInChildren<Light>(true).Where(l => l.name.StartsWith("lightbulb")))
        {
            light.type = LightType.Spot;
            light.transform.rotation = Quaternion.Euler(90, 0, 0);
            light.spotAngle = 100;
            light.innerSpotAngle = 70;
            PrefabUtility.RecordPrefabInstancePropertyModifications(light);
            PrefabUtility.RecordPrefabInstancePropertyModifications(light.transform);
        }
    }

    public static void Apply(Scene scene, int day)
    {
        var roots = scene.GetRootGameObjects();
        var flat = roots.Single(g => g.name.StartsWith("americannredotoday"));
        foreach (var light in flat.GetComponentsInChildren<Light>(true).Where(l => l.name.StartsWith("lightbulb")))
        {
            light.enabled = false;
            PrefabUtility.RecordPrefabInstancePropertyModifications(light);
        }
        ConfigureRoomSpots(flat);
        foreach (var light in roots.SelectMany(g => g.GetComponentsInChildren<Light>(true)).Where(l => l.name == "Readable room fill"))
            light.enabled = false;

        var shade = flat.GetComponentsInChildren<Renderer>(true).Single(r => r.name == "pCylinder204");
        var bounds = shade.bounds;
        var kitchen = flat.GetComponentsInChildren<Light>(true).Single(l => l.name == "areaLight1");
        kitchen.type = LightType.Spot;
        kitchen.lightmapBakeType = LightmapBakeType.Realtime;
        kitchen.transform.position = new Vector3(bounds.center.x, bounds.min.y - .045f, bounds.center.z);
        kitchen.transform.rotation = Quaternion.Euler(90, 0, 0);
        kitchen.spotAngle = 110;
        kitchen.innerSpotAngle = 75;
        kitchen.range = 5;
        kitchen.intensity = day == 1 ? 3.5f : day == 2 ? 2.6f : 1.7f;
        kitchen.color = new Color(1, day == 1 ? .80f : day == 2 ? .76f : .70f, day == 1 ? .55f : day == 2 ? .49f : .42f);
        kitchen.shadows = LightShadows.Soft;
        kitchen.shadowBias = .025f;
        kitchen.shadowNormalBias = .12f;
        kitchen.shadowNearPlane = .05f;
        kitchen.enabled = true;
        PrefabUtility.RecordPrefabInstancePropertyModifications(kitchen);
        PrefabUtility.RecordPrefabInstancePropertyModifications(kitchen.transform);

        string materialPath = "Assets/ApartmentDays/Materials/KitchenLampDay" + day + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (!material)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "KitchenLampDay" + day };
            AssetDatabase.CreateAsset(material, materialPath);
        }
        material.SetColor("_BaseColor", kitchen.color);
        material.EnableKeyword("_EMISSION");
        material.SetColor("_EmissionColor", kitchen.color * (day == 1 ? 2.5f : day == 2 ? 1.8f : 1.2f));
        material.SetFloat("_Smoothness", .15f);
        EditorUtility.SetDirty(material);
        var diffuser = flat.transform.Find("Kitchen lamp diffuser");
        if (!diffuser)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = "Kitchen lamp diffuser";
            diffuser = go.transform;
            diffuser.SetParent(flat.transform, false);
            UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
        }
        diffuser.position = new Vector3(bounds.center.x, bounds.min.y - .006f, bounds.center.z);
        diffuser.rotation = Quaternion.identity;
        diffuser.localScale = new Vector3(.34f / flat.transform.lossyScale.x,
            .004f / flat.transform.lossyScale.y, .34f / flat.transform.lossyScale.z);
        diffuser.GetComponent<Renderer>().sharedMaterial = material;
        var atmosphere = roots.SelectMany(g => g.GetComponentsInChildren<ApartmentDayAtmosphere>(true)).Single();
        atmosphere.roomLights = atmosphere.roomLights.Where(l => l && l.enabled && l.intensity > 0)
            .Concat(new[] { kitchen }).Distinct().ToArray();
    }
}
