using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class VerifyEvidenceRealism
{
    private static void Check(bool value,string error){if(!value)throw new Exception(error);}
    public static string Prepare()
    {
        Check(!Application.isPlaying,"Stop Play Mode");
        int count=0,triangles=0;var ids=new System.Collections.Generic.HashSet<string>();
        foreach(string path in Directory.GetFiles("Assets/InteractionContent/Prefabs","*.prefab"))
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);var item=prefab.GetComponent<EvidenceItem>();if(item==null)continue;
            Check(ids.Add(item.evidenceId),"Duplicate ID");count++;
            Check(prefab.transform.Find("RealisticVisual")!=null,"Missing upgraded model");
            Check(prefab.GetComponentsInChildren<Renderer>().All(r=>r.sharedMaterial!=null&&r.sharedMaterial.name!="Default-Material"),"Default/missing material");
            Check(prefab.GetComponentsInChildren<Renderer>().Length<=10,"Too many draw-call parts");
            Check(prefab.GetComponentsInChildren<Collider>().Length==1,"Decorative collider added");
            var collider=prefab.GetComponent<BoxCollider>();
            Check(collider.center==new Vector3(0,.025f,0),"Collider center changed");
            Check(collider.size==(item.evidenceId=="basement_key_17"?new Vector3(.21f,.06f,.38f):new Vector3(.34f,.06f,.43f)),"Collider size changed");
            foreach(var filter in prefab.GetComponentsInChildren<MeshFilter>()) if(filter.sharedMesh!=null)triangles+=filter.sharedMesh.triangles.Length/3;
        }
        Check(count==8,"Expected eight prefabs");
        var previous=SceneManager.GetActiveScene();int sceneCount=0;
        for(int day=1;day<=3;day++)
        {
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/Day"+day+".unity",OpenSceneMode.Additive);
            try
            {
                var items=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<EvidenceItem>(true)).ToArray();
                Check(items.Length==day*2+1,"Scene clue count changed");
                Check(items.Select(i=>i.evidenceId).Distinct().Count()==items.Length,"Scene duplicate ID");
                Check(items.All(i=>i.transform.Find("RealisticVisual")!=null),"Old scene visuals remain");
                if(day>1)Check(items.Single(i=>i.evidenceId=="family_photo").transform.Cast<Transform>().Count(t=>t.name.StartsWith("Crossed-out Alexei"))==2,"Story cross-out lost");
                sceneCount+=items.Length;
            }
            finally{SceneManager.SetActiveScene(previous);EditorSceneManager.CloseScene(scene,true);}
        }
        Check(!previous.isDirty,"Active scene has unsaved changes");
        File.WriteAllText("output/evidence-realism/previous-scene.txt",previous.path);
        EditorSceneManager.OpenScene("Assets/Scenes/Day1.unity");
        string report="PASS EDIT: eight unique IDs, collider dimensions unchanged, no decorative colliders/default materials, <=10 renderers per clue, "+sceneCount+" apartment clues and story cross-outs preserved. Total prefab triangles including text: "+triangles;
        File.WriteAllText("output/evidence-realism/verification.txt",report+"\n");return report;
    }
    private static async Task Frames(int count)
    {
        int target=Time.frameCount+count;DateTime deadline=DateTime.UtcNow.AddSeconds(10);
        while(Time.frameCount<target){if(DateTime.UtcNow>deadline)throw new Exception("Player loop frozen");await Task.Delay(25);}
    }
    public static async Task<string> Play()
    {
        Check(Application.isPlaying,"Enter Play Mode first");await Frames(5);
        var interaction=UnityEngine.Object.FindAnyObjectByType<PlayerInteraction>();var control=interaction.GetComponent<PlayerController>();
        var items=UnityEngine.Object.FindObjectsByType<EvidenceItem>();
        foreach(var item in items)
        {
            control.SetControlsBlocked(true);
            interaction.playerCamera.transform.position=item.transform.position+new Vector3(0,.5f,-.12f);
            interaction.playerCamera.transform.LookAt(item.transform.position+Vector3.up*.02f);Physics.SyncTransforms();
            Check(ReferenceEquals(interaction.FindTarget(),item),"Clue not reachable: "+item.evidenceId);
            control.SetControlsBlocked(false);interaction.Inspect(item);await Frames(2);
            Check(interaction.inspectionUI.panel.activeSelf&&control.ControlsBlocked,"Inspection failure");
            interaction.CloseInspection();int count=EvidenceJournal.Instance.Count;
            interaction.Inspect(item);Check(EvidenceJournal.Instance.Count==count,"Duplicate clue");interaction.CloseInspection();
        }
        var photo=items.Single(i=>i.evidenceId=="family_photo");control.SetControlsBlocked(true);
        interaction.playerCamera.transform.position=photo.transform.position+new Vector3(.10f,.42f,-.36f);
        interaction.playerCamera.transform.LookAt(photo.transform.position+Vector3.up*.02f);
        await Frames(3);
        Debug.Log("Realistic evidence: Day1 raycasts, inspection and journal verification passed.");
        File.AppendAllText("output/evidence-realism/verification.txt","PASS PLAY: live frames, all three Day1 clues reachable, inspection locks controls, journal deduplicates.\n");
        return "PASS: new models work in the actual apartment";
    }
}
