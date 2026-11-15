using System;
using System.IO;
using System.Threading.Tasks;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class VerifyNotebookTransfer
{
    public static async Task<string> Run()
    {
        var before = EvidenceJournal.Instance;
        if (before == null || before.Count != 2) throw new Exception("Run VerifyNotebook first");
        string description = before.Pages[0].description;
        var load = EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/InteractionContent/InteractionDemo.unity", new LoadSceneParameters(LoadSceneMode.Single));
        var deadline = DateTime.UtcNow.AddSeconds(20);
        while (!load.isDone)
        {
            if (DateTime.UtcNow > deadline) throw new Exception("Scene load timeout");
            await Task.Delay(25);
        }
        int frame = Time.frameCount + 5;
        while (Time.frameCount < frame) await Task.Delay(25);
        if (EvidenceJournal.Instance != before || before.Count != 2 || before.Pages[0].description != description)
            throw new Exception("Evidence lost on scene change");
        var player = UnityEngine.Object.FindAnyObjectByType<PlayerNotebook>();
        if (player == null || !player.Open() || player.view.heading.text != before.Pages[0].title)
            throw new Exception("New player cannot read previous evidence");
        player.Close();
        File.AppendAllText("output/notebook/verification.txt", "\nPASS: scene reload preserves journal, copied descriptions and pages; new player opens previous evidence.\n");
        Debug.Log("Notebook scene-transfer verification passed.");
        return "PASS: journal and pages survive scene change";
    }
}
