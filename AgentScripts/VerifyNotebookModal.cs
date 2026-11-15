using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public static class VerifyNotebookModal
{
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    private static async Task Frames(int n)
    {
        int target=Time.frameCount+n;DateTime deadline=DateTime.UtcNow.AddSeconds(10);
        while(Time.frameCount<target){if(DateTime.UtcNow>deadline)throw new Exception("Player loop frozen");await Task.Delay(25);}
    }
    private static async Task Press(Keyboard device,Key key)
    {
        InputSystem.QueueStateEvent(device,new KeyboardState(key));await Frames(2);
        InputSystem.QueueStateEvent(device,new KeyboardState());await Frames(2);
    }
    public static async Task<string> Run()
    {
        Check(Application.isPlaying,"Start Play Mode");await Frames(5);
        var player=UnityEngine.Object.FindAnyObjectByType<PlayerInteraction>();var notebook=player.GetComponent<PlayerNotebook>();var control=player.GetComponent<PlayerController>();
        var items=UnityEngine.Object.FindObjectsByType<EvidenceItem>();var photo=items.Single(i=>i.evidenceId=="family_photo");
        var keyboard=InputSystem.AddDevice<Keyboard>();
        try
        {
            CursorLockMode cursor=Cursor.lockState;bool visible=Cursor.visible;
            player.Inspect(photo);Check(player.IsInspecting&&control.ControlsBlocked,"Inspection did not open");
            Check(!player.inspectionUI.journal.gameObject.activeSelf,"Old notebook column is visible");
            await Press(keyboard,Key.J);
            Check(notebook.IsOpen&&player.IsInspecting&&control.ControlsBlocked,"J cannot open nested notebook");
            Check(!player.inspectionUI.GetComponent<Canvas>().enabled,"Underlying inspection is visible");
            Check(notebook.view.body.font.name=="NotebookHandwriting","Wrong notebook font");
            foreach(char c in "АБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯабвгдеёжзийклмнопрстуфхцчшщъыьэюя")
                Check(notebook.view.body.font.HasCharacter(c),"Missing Russian glyph: "+c);
            await Press(keyboard,Key.E);
            Check(notebook.IsOpen&&player.IsInspecting,"E closes underlying inspection");
            await Press(keyboard,Key.Escape);
            Check(!notebook.IsOpen&&player.IsInspecting&&control.ControlsBlocked,"Esc closes both windows or unlocks player");
            Check(player.inspectionUI.GetComponent<Canvas>().enabled,"Inspection canvas not restored");
            await Press(keyboard,Key.J);await Press(keyboard,Key.J);
            Check(!notebook.IsOpen&&player.IsInspecting&&control.ControlsBlocked,"J round trip broke inspection");
            await Press(keyboard,Key.J);notebook.view.close.onClick.Invoke();
            Check(!notebook.IsOpen&&player.IsInspecting&&control.ControlsBlocked,"Close button broke inspection");
            player.inspectionUI.closeButton.onClick.Invoke();
            Check(!player.IsInspecting&&!control.ControlsBlocked,"Inspection close leaves player blocked");
            Check(Cursor.lockState==cursor&&Cursor.visible==visible,"Original cursor not restored");
            control.SetControlsBlocked(true);Check(!notebook.Open(),"External control lock ignored");control.SetControlsBlocked(false);
            foreach(var item in items)EvidenceJournal.GetOrCreate().Add(item);
            notebook.Open();
            for(int i=0;i<EvidenceJournal.Instance.Count;i++)
            {
                if(i>0){notebook.view.Turn(1);await Task.Delay(500);}
                Canvas.ForceUpdateCanvases();notebook.view.body.ForceMeshUpdate();notebook.view.heading.ForceMeshUpdate();
                Check(!notebook.view.body.isTextOverflowing&&!notebook.view.heading.isTextOverflowing,"Handwriting does not fit page "+i);
            }
            notebook.Close();Check(!control.ControlsBlocked,"Standalone notebook leaves player blocked");
            player.Inspect(photo);notebook.Open();player.CloseInspection();
            Check(!notebook.IsOpen&&!player.IsInspecting&&!control.ControlsBlocked,"Closing underlying inspection breaks stack");
            player.Inspect(photo);notebook.Open();
            while(notebook.view.PageIndex>0){notebook.view.Turn(-1);await Task.Delay(500);}
            Directory.CreateDirectory("output/notebook-handwriting");
            string report="PASS: complete Russian alphabet; all 8 handwritten pages fit; no journal column; J opens notebook from inspection; Esc/J/button return only to inspection; E ignored underneath; movement lock/cursor restored correctly; standalone/external locks and closing entire stack verified.";
            File.WriteAllText("output/notebook-handwriting/verification.txt",report);Debug.Log(report);return report;
        }
        finally{InputSystem.RemoveDevice(keyboard);}
    }
}
