using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class SetupApartmentTransition
{
    public static object Run()
    {
        if (EditorApplication.isPlaying) throw new Exception("Stop Play Mode before setup.");
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/Scene2.unity" || scene.isDirty)
            throw new Exception("Open saved Scene2 before setup.");
        InvestigationDayFlow flow = UnityEngine.Object.FindObjectsByType<InvestigationDayFlow>()
            .Single(f => f.gameObject.scene == scene && f.day == 0);
        AudioClip day1 = Clip("transitionSound");
        AudioClip day2 = Clip("transitionAudioDay2");
        AudioClip day3 = Clip("transitionAudioDay3");
        Undo.RecordObject(flow, "Configure apartment transitions");
        flow.transitionSoundDay1 = day1;
        flow.transitionSoundDay2 = day2;
        flow.transitionSoundDay3 = day3;
        flow.fadeOutDuration = flow.fadeInDuration = 1.5f;
        flow.transitionVolume = 1f;
        EditorUtility.SetDirty(flow);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        return new { scene = scene.path, day1 = day1.name, day2 = day2.name, day3 = day3.name };
    }

    private static AudioClip Clip(string name)
    {
        AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Cutscene/" + name + ".mp3");
        if (clip == null) throw new Exception("Missing transition audio: " + name);
        return clip;
    }
}
