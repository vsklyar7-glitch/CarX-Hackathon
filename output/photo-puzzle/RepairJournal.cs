using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

// Исправляет только тестовые заглушки текущего Play Mode, не добавляя улик и не меняя этап.
public static class RepairJournal
{
    public static string Run()
    {
        var journal = EvidenceJournal.Instance;
        if (journal == null) return "No running journal; no repair needed";
        int count = journal.Count, day = journal.ApartmentDay;
        bool restored = journal.FamilyPhotoRestored;
        var titles = (Dictionary<string, string>)typeof(EvidenceJournal).GetField("entries", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(journal);
        var report = new List<string>();
        foreach (var entry in journal.Pages)
        {
            if (entry.title != entry.id && !string.IsNullOrWhiteSpace(entry.description)) continue;
            int index = Array.IndexOf(InteractionContentBuilder.Ids, entry.id);
            if (index < 0) throw new Exception("Unknown evidence: " + entry.id);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/InteractionContent/Prefabs/" + InteractionContentBuilder.Names[index] + ".prefab");
            var item = prefab.GetComponent<EvidenceItem>();
            entry.title = item.title;
            entry.description = string.IsNullOrWhiteSpace(item.notebookDescription) ? item.description : item.notebookDescription;
            if (entry.image == null) entry.image = item.image;
            titles[entry.id] = entry.title;
            report.Add(entry.id + " -> " + entry.title);
        }
        foreach (var view in UnityEngine.Object.FindObjectsByType<NotebookView>())
            if (view.panel.activeSelf && !view.IsTurning)
                typeof(NotebookView).GetMethod("RefreshPage", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(view, null);
        if (journal.Count != count || journal.ApartmentDay != day || journal.FamilyPhotoRestored != restored)
            throw new Exception("Progress changed unexpectedly");
        return "Repaired " + report.Count + " entries; count=" + count + ", day=" + day + "; progress preserved.\n" + string.Join("\n", report);
    }
}
