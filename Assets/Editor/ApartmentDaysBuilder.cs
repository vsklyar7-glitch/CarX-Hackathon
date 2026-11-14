using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ApartmentDaysBuilder
{
    [MenuItem("Tools/Detective/Build apartment days")]
    public static string Build()
    {
        for(int day=1;day<=3;day++) if(File.Exists("Assets/Scenes/Day"+day+".unity"))
            throw new InvalidOperationException("Day-сцены уже существуют; автоматическая перезапись запрещена.");
        Directory.CreateDirectory("Assets/ApartmentDays/Materials");
        Directory.CreateDirectory("output/apartment-days");
        var previous=SceneManager.GetActiveScene();
        var source=EditorSceneManager.OpenScene("Assets/Scenes/Scene2.unity",OpenSceneMode.Additive);
        var demo=EditorSceneManager.OpenScene("Assets/InteractionContent/InteractionDemo.unity",OpenSceneMode.Additive);
        var apartment=source.GetRootGameObjects().First(g=>g.name.StartsWith("americannredotoday"));
        var sourceItems=demo.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<EvidenceItem>(true)).ToDictionary(e=>e.evidenceId);
        var report=new System.Collections.Generic.List<string>();
        try
        {
            for(int day=1;day<=3;day++)
            {
                var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
                SceneManager.SetActiveScene(scene);
                try
                {
                    var flat=UnityEngine.Object.Instantiate(apartment);
                    flat.name="americannredotoday";
                    flat.transform.position=Vector3.zero;
                    var meshes=flat.GetComponentsInChildren<MeshRenderer>(true);
                    var table=meshes.Single(r=>r.name=="table").bounds;
                    var counter=meshes.Single(r=>r.name=="pCube273").bounds;
                    var side=meshes.Single(r=>r.name=="toptablesurface").bounds;
                    var floor=meshes.Single(r=>r.name=="pPlane9").bounds;
                    var story=new GameObject("Day"+day+" — Story dressing");
                    var paper=Mat("AgedPaper",new Color(.63f,.56f,.4f));
                    var wood=Mat("DoorWood",new Color(.16f,.10f,.065f));
                    var black=Mat("DepthBlack",Color.black,true);
                    var red=Mat("RedMarks",new Color(.32f,.025f,.018f));
                    // Actual instances from InteractionDemo, including their existing EvidenceItem data.
                    var photo=Clue(sourceItems,"family_photo",story.transform,new Vector3(table.center.x-.30f,table.max.y+.008f,table.center.z),0);
                    Clue(sourceItems,"electricity_receipt",story.transform,new Vector3(table.center.x+.30f,table.max.y+.008f,table.center.z),-12);
                    Clue(sourceItems,"basement_key_17",story.transform,new Vector3(counter.center.x,counter.max.y+.008f,counter.center.z-.5f),15);
                    if(day>=2)
                    {
                        Clue(sourceItems,"dmitry_diary",story.transform,new Vector3(side.center.x,side.max.y+.008f,side.center.z),8);
                        Clue(sourceItems,"ivan_letter",story.transform,new Vector3(counter.center.x,counter.max.y+.008f,counter.center.z+.45f),-8);
                        var chair=meshes.Single(r=>r.name=="highchair");
                        chair.transform.Rotate(0,0,72,Space.World);
                        chair.transform.position+=Vector3.up*(floor.max.y+.025f-chair.bounds.min.y);
                        for(int i=0;i<5;i++) Box(story.transform,"Scattered paper "+i,new Vector3(table.center.x-.6f+i*.24f,floor.max.y+.008f,table.center.z+.8f+i*.13f),new Vector3(.18f,.004f,.24f),paper,Quaternion.Euler(0,i*37,0));
                        photo.GetComponent<EvidenceItem>().description="На прежней семейной фотографии лицо Алексея зачёркнуто. Дмитрий, Иван и отец остались нетронутыми. Кто-то хочет, чтобы я вспомнил ту дверь.";
                        Box(photo.transform,"Crossed-out Alexei A",new Vector3(.09f,.047f,.05f),new Vector3(.065f,.004f,.009f),red,Quaternion.Euler(0,42,0),true);
                        Box(photo.transform,"Crossed-out Alexei B",new Vector3(.09f,.049f,.05f),new Vector3(.065f,.004f,.009f),red,Quaternion.Euler(0,-42,0),true);
                    }
                    if(day==3)
                    {
                        Clue(sourceItems,"child_drawing",story.transform,new Vector3(table.center.x-.25f,floor.max.y+.012f,table.center.z+.85f),-15);
                        Clue(sourceItems,"father_note",story.transform,new Vector3(table.center.x,table.max.y+.010f,table.center.z+.4f),12);
                    }
                    // A staged basement threshold: the dark backing suggests depth without adding another level.
                    var doorPosition=new Vector3(1.2f,floor.max.y,-4.42f);
                    var door=new GameObject("Basement threshold 17");door.transform.SetParent(story.transform,false);door.transform.position=doorPosition;
                    Box(door.transform,"Darkness beyond door",new Vector3(0,1.04f,-.04f),new Vector3(1.03f,2.1f,.025f),black,Quaternion.identity,true);
                    Box(door.transform,"Left jamb",new Vector3(-.56f,1.09f,.02f),new Vector3(.10f,2.18f,.12f),wood,Quaternion.identity,true);
                    Box(door.transform,"Right jamb",new Vector3(.56f,1.09f,.02f),new Vector3(.10f,2.18f,.12f),wood,Quaternion.identity,true);
                    Box(door.transform,"Lintel",new Vector3(0,2.14f,.02f),new Vector3(1.22f,.10f,.12f),wood,Quaternion.identity,true);
                    var hinge=new GameObject("Door hinge");hinge.transform.SetParent(door.transform,false);hinge.transform.localPosition=new Vector3(-.50f,0,.07f);
                    Box(hinge.transform,"Door leaf",new Vector3(.5f,1.04f,0),new Vector3(1,2.08f,.055f),wood,Quaternion.identity,true);
                    Box(hinge.transform,"Door handle",new Vector3(.88f,1.02f,.07f),new Vector3(.12f,.035f,.045f),paper,Quaternion.identity,true);
                    if(day==3)hinge.transform.localRotation=Quaternion.Euler(0,-68,0);
                    Text(door.transform,"Door number","17",new Vector3(0,1.75f,.12f),.13f,Color.white);
                    if(day>=2)Text(door.transform,"Wall inscription",day==2?"ТЫ ОТКРЫЛ\nДВЕРЬ":"НЕ ВИНИ\nСЕБЯ",new Vector3(-1.30f,1.45f,.10f),.16f,new Color(.45f,.045f,.03f));
                    var clock=new GameObject("Stopped clock — 01:17");clock.transform.SetParent(door.transform,false);clock.transform.localPosition=new Vector3(1.1f,2.15f,.12f);
                    Box(clock.transform,"Clock face",Vector3.zero,new Vector3(.34f,.34f,.035f),paper,Quaternion.identity,true);
                    var hour=Box(clock.transform,"Hour hand",new Vector3(.02f,.045f,.025f),new Vector3(.014f,.11f,.008f),black,Quaternion.Euler(0,0,-30),true);
                    var minute=Box(clock.transform,"Minute hand",new Vector3(.06f,-.015f,.026f),new Vector3(.014f,.15f,.008f),black,Quaternion.Euler(0,0,-102),true);
                    var atmosphere=story.AddComponent<ApartmentDayAtmosphere>();atmosphere.day=day;atmosphere.clockHourHand=hour.transform;atmosphere.clockMinuteHand=minute.transform;
                    var existing=flat.GetComponentsInChildren<Light>(true);
                    foreach(var light in existing){light.color=day==1?new Color(1,.83f,.62f):day==2?new Color(.65f,.78f,1):new Color(.62f,.68f,.78f);light.intensity*=day==1?.65f:day==2?.28f:.12f;}
                    var fill=new GameObject("Readable room fill",typeof(Light));fill.transform.position=new Vector3(table.center.x,2.2f,table.center.z);var fillLight=fill.GetComponent<Light>();fillLight.type=LightType.Point;fillLight.range=7;fillLight.intensity=day==1?1.1f:.55f;fillLight.color=day==1?new Color(1,.84f,.65f):new Color(.65f,.75f,1);
                    var depthLight=new GameObject("Light from basement",typeof(Light));depthLight.transform.SetParent(door.transform,false);depthLight.transform.localPosition=new Vector3(0,.4f,.25f);var dl=depthLight.GetComponent<Light>();dl.type=LightType.Point;dl.range=2;dl.color=new Color(1,.19f,.06f);dl.intensity=day==3?1.2f:0;
                    atmosphere.roomLights=existing.Concat(new[]{fillLight,dl}).Where(l=>l.intensity>0).ToArray();
                    UseExistingBasementDoor(flat,door.transform,day);
                    RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=day==1?new Color(.18f,.17f,.15f):new Color(.09f,.11f,.15f);
                    var player=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/InteractionContent/Prefabs/InvestigationPlayer.prefab"),scene);
                    var spawn=flat.transform.Find("PlayerSpawn");
                    var cc=player.GetComponent<CharacterController>();cc.enabled=false;player.transform.position=spawn?spawn.position:new Vector3(floor.center.x,.05f,floor.center.z);player.transform.rotation=Quaternion.LookRotation(new Vector3(table.center.x,player.transform.position.y,table.center.z)-player.transform.position);cc.enabled=true;
                    Physics.SyncTransforms();
                    var clues=story.GetComponentsInChildren<EvidenceItem>();
                    if(clues.Select(e=>e.evidenceId).Distinct().Count()!=clues.Length||clues.Length!=(day==1?3:day==2?5:7))throw new Exception("Wrong evidence set");
                    if(player.GetComponent<PlayerInteraction>().inspectionUI==null)throw new Exception("Player UI missing");
                    var path="Assets/Scenes/Day"+day+".unity";
                    if(!EditorSceneManager.SaveScene(scene,path))throw new Exception("Could not save "+path);
                    report.Add(path+": "+clues.Length+" unique clues; source=InteractionDemo; player, colliders, atmosphere, door pose checked");
                }
                finally{EditorSceneManager.CloseScene(scene,true);}
            }
            var settings=EditorBuildSettings.scenes.ToList();
            for(int day=1;day<=3;day++){string path="Assets/Scenes/Day"+day+".unity";if(!settings.Any(s=>s.path==path))settings.Add(new EditorBuildSettingsScene(path,true));}
            EditorBuildSettings.scenes=settings.ToArray();
            File.WriteAllLines("output/apartment-days/generation.txt",report);
            return string.Join("\n",report);
        }
        finally{SceneManager.SetActiveScene(previous);EditorSceneManager.CloseScene(demo,true);EditorSceneManager.CloseScene(source,true);}
    }
    // Переносит оформление на штатную дверь, сохраняя её материал и ручку.
    public static void UseExistingBasementDoor(GameObject flat,Transform threshold,int day)
    {
        if(threshold.name=="Basement door 17") return;
        var leaf=flat.GetComponentsInChildren<MeshRenderer>(true).Single(r=>r.name=="pCube217");
        var handle=flat.GetComponentsInChildren<MeshRenderer>(true).Single(r=>r.name=="pCube343");
        var bounds=leaf.bounds;
        foreach(string name in new[]{"Left jamb","Right jamb","Lintel","Door hinge"})
        {
            var part=threshold.Find(name);
            if(part) UnityEngine.Object.DestroyImmediate(part.gameObject);
        }
        threshold.name="Basement door 17";
        threshold.position=new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
        threshold.rotation=Quaternion.identity;
        var hinge=new GameObject("Door hinge").transform;
        hinge.SetParent(threshold,false);
        hinge.localPosition=new Vector3(-bounds.size.x*.5f,0,0);
        leaf.transform.SetParent(hinge,true);
        handle.transform.SetParent(leaf.transform,true);
        var darkness=threshold.Find("Darkness beyond door");
        darkness.localPosition=new Vector3(0,bounds.size.y*.5f,-.065f);
        darkness.localScale=new Vector3(bounds.size.x+.015f,bounds.size.y,.025f);
        var number=threshold.Find("Door number");
        number.SetParent(hinge,false);
        number.localPosition=new Vector3(bounds.size.x*.5f,bounds.size.y*.83f,.075f);
        if(day==3) hinge.localRotation=Quaternion.Euler(0,-68,0);
    }

    private static GameObject Clue(System.Collections.Generic.Dictionary<string,EvidenceItem> sources,string id,Transform parent,Vector3 position,float yaw)
    {
        var go=UnityEngine.Object.Instantiate(sources[id].gameObject);go.name=sources[id].gameObject.name;go.transform.SetParent(parent,true);go.transform.position=position;go.transform.rotation=Quaternion.Euler(0,yaw,0);go.transform.localScale=Vector3.one;return go;
    }
    private static Material Mat(string name,Color color,bool unlit=false)
    {
        string path="Assets/ApartmentDays/Materials/"+name+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);if(material)return material;material=new Material(Shader.Find(unlit?"Universal Render Pipeline/Unlit":"Universal Render Pipeline/Lit"));material.SetColor("_BaseColor",color);AssetDatabase.CreateAsset(material,path);return material;
    }
    private static GameObject Box(Transform parent,string name,Vector3 position,Vector3 scale,Material material,Quaternion rotation,bool local=false)
    {
        var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent,false);if(local)go.transform.localPosition=position;else go.transform.position=position;go.transform.localRotation=rotation;go.transform.localScale=scale;go.GetComponent<Renderer>().sharedMaterial=material;return go;
    }
    private static void Text(Transform parent,string name,string value,Vector3 local,float size,Color color)
    {
        var go=new GameObject(name,typeof(TextMeshPro));go.transform.SetParent(parent,false);go.transform.localPosition=local;go.transform.localRotation=Quaternion.Euler(0,180,0);var text=go.GetComponent<TextMeshPro>();text.font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/InteractionContent/RussianFont.asset");text.text=value;text.fontSize=size*10;text.color=color;text.alignment=TextAlignmentOptions.Center;text.rectTransform.sizeDelta=new Vector2(1.2f,.5f);
    }
}
