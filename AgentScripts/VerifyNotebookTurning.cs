using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using UnityEditor;
using UnityEngine;

public static class VerifyNotebookTurning
{
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    private static async Task Until(Func<bool> condition)
    {
        var deadline=DateTime.UtcNow.AddSeconds(8);
        while(!condition()){if(DateTime.UtcNow>deadline)throw new Exception("Animation did not advance");await Task.Delay(15);}
    }
    private static bool Text(Transform page,string text)=>page.GetComponentsInChildren<TMP_Text>(true).Any(t=>t.text==text);
    private static bool Picture(Transform page,Sprite sprite)=>page.GetComponentsInChildren<UnityEngine.UI.Image>(true).Any(i=>i.sprite==sprite);
    private static Transform Snapshot(Transform parent,string name)=>parent.Cast<Transform>().Last(t=>t.name==name);
    public static async Task<string> Run()
    {
        Check(Application.isPlaying,"Enter Play Mode");int frame=Time.frameCount+5;await Until(()=>Time.frameCount>=frame);
        var player=UnityEngine.Object.FindAnyObjectByType<PlayerNotebook>();var journal=EvidenceJournal.GetOrCreate();
        var items=UnityEngine.Object.FindObjectsByType<EvidenceItem>();
        var photo=items.Single(i=>i.evidenceId=="family_photo");var receipt=items.Single(i=>i.evidenceId=="electricity_receipt");
        Check(journal.Count==0,"Start with a fresh Play session");journal.Add(photo);journal.Add(receipt);Check(player.Open(),"Cannot open notebook");
        var view=player.view;view.turnDuration=.8f;float scale=Time.timeScale;
        try
        {
            Time.timeScale=0;
            view.Turn(1);view.Turn(-1);
            Check(view.IsTurning&&view.PageIndex==1,"Rapid input changed the target page");
            Transform front=Snapshot(view.turningLeaf,"TurningFront"),back=Snapshot(view.turningLeaf,"TurningBack");
            var stationary=Snapshot(view.turningLeaf.parent,"OutgoingStationary");
            Check(Text(front,photo.title)&&Picture(stationary,Resources.Load<Sprite>("NotebookArt/family_photo")),"Outgoing page content missing");
            Check(Picture(back,Resources.Load<Sprite>("NotebookArt/electricity_receipt")),"Incoming image absent on reverse face");
            Check(view.heading.text==receipt.title,"Incoming spread not prepared underneath");
            Check(back.localEulerAngles.y>179&&back.localEulerAngles.y<181,"Reverse face would mirror its content");
            await Until(()=>!front.gameObject.activeSelf);
            Check(back.gameObject.activeSelf&&stationary.gameObject.activeSelf,"Reverse face or underlying old page disappeared");
            await Until(()=>!view.IsTurning);
            Check(view.body.text==receipt.description&&view.illustration.sprite!=null,"Final spread is incomplete");
            view.Turn(-1);
            front=Snapshot(view.turningLeaf,"TurningFront");back=Snapshot(view.turningLeaf,"TurningBack");
            Check(Picture(front,Resources.Load<Sprite>("NotebookArt/electricity_receipt"))&&Text(back,photo.title),"Backward turn loses image/text");
            Check(view.turningLeaf.pivot.x==1,"Backward hinge is not at the spine");
            await Until(()=>!view.IsTurning);Check(view.PageIndex==0,"Wrong page after backward turn");
            view.Turn(1);player.Close();await Task.Delay(70);
            Check(!view.IsTurning&&!view.turningLeaf.gameObject.activeSelf,"Closing mid-turn leaves an animation");
            Check(view.turningLeaf.parent.Find("OutgoingStationary")==null&&view.turningLeaf.childCount==0,"Temporary snapshots leaked");
            Check(player.Open()&&view.heading.text==receipt.title,"Reopening after cancellation gives an incomplete page");
            view.turnDuration=.38f;
            Directory.CreateDirectory("output/notebook-turning");
            string report="PASS: live frames; outgoing text/image retained; incoming spread underneath; populated reverse face without mirrored orientation; forward/backward hinges; timeScale=0; rapid-input guard; cancellation cleanup; stable reopening.";
            File.WriteAllText("output/notebook-turning/verification.txt",report);Debug.Log(report);return report;
        }
        finally{Time.timeScale=scale;}
    }

    // Pause only this agent's test session for reproducible intermediate captures.
    public static async Task<string> PoseFront()
    {
        EditorApplication.isPaused=false;
        var view=UnityEngine.Object.FindAnyObjectByType<PlayerNotebook>().view;
        view.turnDuration=2;
        view.Turn(-1);
        await Until(()=>Mathf.Abs(Mathf.DeltaAngle(0,view.turningLeaf.localEulerAngles.y))>35);
        EditorApplication.isPaused=true;return "Paused at front face with image";
    }
    public static async Task<string> PoseBack()
    {
        EditorApplication.isPaused=false;
        var view=UnityEngine.Object.FindAnyObjectByType<PlayerNotebook>().view;
        await Until(()=>!view.IsTurning);
        view.turnDuration=2;
        view.Turn(view.PageIndex==0?1:-1);
        await Until(()=>Mathf.Abs(Mathf.DeltaAngle(0,view.turningLeaf.localEulerAngles.y))>125);
        EditorApplication.isPaused=true;return "Paused at reverse face with text";
    }
}
