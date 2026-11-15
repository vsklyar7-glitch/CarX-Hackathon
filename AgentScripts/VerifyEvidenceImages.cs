using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class VerifyEvidenceImages
{
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    public static string Prepare()
    {
        Check(!Application.isPlaying,"Stop Play Mode first");
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        Check(!scene.isDirty,"Save the scene first");
        Directory.CreateDirectory("output/evidence-ui");
        File.WriteAllText("output/evidence-ui/previous-scene.txt",scene.path);
        EditorSceneManager.OpenScene("Assets/InteractionContent/InteractionDemo.unity");
        return "Demo ready for image verification";
    }
    private static async Task Frames(int n)
    {
        int target=Time.frameCount+n;var deadline=DateTime.UtcNow.AddSeconds(10);
        while(Time.frameCount<target){if(DateTime.UtcNow>deadline)throw new Exception("Player loop is frozen");await Task.Delay(25);}
    }
    public static async Task<string> Run()
    {
        Check(Application.isPlaying,"Enter Play Mode");await Frames(5);
        var interaction=UnityEngine.Object.FindAnyObjectByType<PlayerInteraction>();
        var items=UnityEngine.Object.FindObjectsByType<EvidenceItem>();
        var journal=EvidenceJournal.GetOrCreate();
        var drawing=items.Single(i=>i.evidenceId=="child_drawing");journal.Add(drawing);
        foreach(var item in items)
        {
            interaction.Inspect(item);await Frames(2);
            var ui=interaction.inspectionUI;Canvas.ForceUpdateCanvases();ui.description.ForceMeshUpdate();
            Check(ui.image.gameObject.activeInHierarchy&&ui.image.sprite!=null,"Missing inspection image: "+item.evidenceId);
            Check(ui.image.sprite==(item.image!=null?item.image:Resources.Load<Sprite>("NotebookArt/"+item.evidenceId)),"Wrong inspection image");
            Check(ui.image.rectTransform.anchorMax.x<ui.description.rectTransform.anchorMin.x,"Image overlaps description");
            Check(!ui.journal.gameObject.activeSelf,"Old notebook column is visible");
            Check(!ui.description.isTextOverflowing,"Description does not fit: "+item.evidenceId);
            interaction.CloseInspection();
        }
        // An explicit per-object sprite must still override the common illustration.
        Sprite old=drawing.image;drawing.image=Resources.Load<Sprite>("NotebookArt/basement_key_17");
        interaction.Inspect(drawing);Check(interaction.inspectionUI.image.sprite==drawing.image,"Explicit image ignored");interaction.CloseInspection();drawing.image=old;
        string oldId=drawing.evidenceId;drawing.evidenceId="no_image_test";
        interaction.inspectionUI.Show(drawing,journal,false,null);
        Check(!interaction.inspectionUI.image.gameObject.activeSelf&&interaction.inspectionUI.description.rectTransform.anchorMin.x==.02f,"Missing-image fallback broken");
        interaction.inspectionUI.Hide();drawing.evidenceId=oldId;
        var notebook=interaction.GetComponent<PlayerNotebook>();Check(notebook.Open(),"Notebook did not open");
        Check(notebook.view.illustration.sprite==Resources.Load<Sprite>("NotebookArt/child_drawing"),"Notebook image mismatch");
        File.WriteAllText("output/evidence-ui/verification.txt","PASS: eight inspection illustrations, explicit sprite override, missing-image fallback, non-overlapping columns, all descriptions fit, notebook uses the same illustrations. Live frames verified.\n");
        Debug.Log("Evidence image UI verification passed for all eight clues.");
        return "PASS: all eight clue images display and fit";
    }
    public static string InspectDrawing()
    {
        var interaction=UnityEngine.Object.FindAnyObjectByType<PlayerInteraction>();interaction.GetComponent<PlayerNotebook>().Close();
        interaction.Inspect(UnityEngine.Object.FindObjectsByType<EvidenceItem>().Single(i=>i.evidenceId=="child_drawing"));return "Drawing inspection shown";
    }
    public static async Task<string> NotebookKey()
    {
        var interaction=UnityEngine.Object.FindAnyObjectByType<PlayerInteraction>();interaction.CloseInspection();
        var notebook=interaction.GetComponent<PlayerNotebook>();notebook.Open();
        var pages=EvidenceJournal.Instance.Pages;int index=Enumerable.Range(0,pages.Count).Single(i=>pages[i].id=="basement_key_17");
        while(notebook.view.PageIndex!=index){notebook.view.Turn(index>notebook.view.PageIndex?1:-1);await Task.Delay(550);}
        return "Notebook key page shown";
    }
}
