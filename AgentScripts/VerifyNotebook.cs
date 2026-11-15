using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public static class VerifyNotebook
{
    private static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    private static async Task Frames(int count)
    {
        int goal = Time.frameCount + count;
        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (Time.frameCount < goal)
        {
            if (DateTime.UtcNow > deadline) throw new Exception("Play Mode is not advancing");
            await Task.Delay(25);
        }
    }
    private static async Task Press(Keyboard keyboard, Key key)
    {
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(key));
        await Frames(2);
        InputSystem.QueueStateEvent(keyboard, new KeyboardState());
        await Frames(2);
    }
    public static async Task<string> Run()
    {
        Check(Application.isPlaying, "Enter Play Mode first");
        await Frames(5);
        var player = UnityEngine.Object.FindAnyObjectByType<PlayerNotebook>();
        Check(player != null && player.view != null, "Notebook was not connected to player");
        var control = player.GetComponent<PlayerController>();
        var journal = EvidenceJournal.GetOrCreate();
        Check(journal.Count == 0, "Use a fresh Play session");
        var keyboard = InputSystem.AddDevice<Keyboard>();
        var mouse = InputSystem.AddDevice<Mouse>();
        float timeScale = Time.timeScale;
        try
        {
            await Press(keyboard, Key.J);
            Check(player.IsOpen && control.ControlsBlocked, "J does not open/block controls");
            Check(player.view.body.text.Contains("Пока"), "Empty state missing");
            Check(!player.view.previous.interactable && !player.view.next.interactable, "Empty-state arrows enabled");
            Vector3 position = control.transform.position;
            Quaternion rotation = control.playerCamera.rotation;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W));
            InputSystem.QueueDeltaStateEvent(mouse.delta, new Vector2(120,80));
            await Frames(5);
            Check(control.transform.position == position && control.playerCamera.rotation == rotation, "Controls not locked");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            await Press(keyboard, Key.Escape);
            Check(!player.IsOpen && !control.ControlsBlocked, "Esc does not close/unlock");
            var items = UnityEngine.Object.FindObjectsByType<EvidenceItem>();
            var photo = Array.Find(items, i => i.evidenceId == "family_photo");
            var receipt = Array.Find(items, i => i.evidenceId == "electricity_receipt");
            int events = 0;
            journal.EvidenceAdded += _ => events++;
            Check(journal.Add(photo) && journal.Add(receipt) && !journal.Add(photo), "Evidence deduplication failed");
            Check(journal.Count == 2 && events == 2, "Duplicate pages or events");
            Check(journal.Pages[0].description == photo.description, "Evidence description was not copied");
            foreach (var item in items)
                Check(Resources.Load<Sprite>("NotebookArt/" + item.evidenceId) != null, "Missing illustration: " + item.evidenceId);
            await Press(keyboard, Key.J);
            Check(player.view.heading.text == photo.title && player.view.illustration.sprite != null, "Photo page missing");
            player.view.Turn(-1);
            Check(!player.view.IsTurning && player.view.PageIndex == 0, "Lower page boundary broken");
            Time.timeScale = 0;
            player.view.next.onClick.Invoke();
            player.view.Turn(1);
            Check(player.view.IsTurning, "Page-turn animation did not start");
            await Task.Delay(600);
            Check(!player.view.IsTurning && player.view.PageIndex == 1, "Unscaled animation or rapid-input guard failed");
            Check(!player.view.next.interactable && player.view.previous.interactable, "Upper page boundary broken");
            Time.timeScale = timeScale;
            await Press(keyboard, Key.LeftArrow);
            await Task.Delay(500);
            Check(player.view.PageIndex == 0 && !player.view.IsTurning, "Keyboard arrow did not turn page");
            player.view.close.onClick.Invoke();
            Check(!player.IsOpen && !control.ControlsBlocked, "Close button failed");
            player.GetComponent<PlayerInteraction>().Inspect(photo);
            await Press(keyboard, Key.J);
            Check(player.IsOpen && control.ControlsBlocked, "Notebook cannot open over inspection");
            await Press(keyboard, Key.Escape);
            Check(!player.IsOpen && player.GetComponent<PlayerInteraction>().IsInspecting && control.ControlsBlocked, "Esc did not return to inspection");
            player.GetComponent<PlayerInteraction>().CloseInspection();
            await Press(keyboard, Key.J);
            Check(player.IsOpen, "Cannot reopen notebook");
            Directory.CreateDirectory("output/notebook");
            string report = "PASS: live frames; J/Esc/buttons; empty journal; blocked WASD/mouse; 8 illustrations; snapshots; no duplicate pages/events; page boundaries; buttons/keyboard arrows; animated turn with timeScale=0; rapid-input guard; inspection exclusion. Unity " + Application.unityVersion;
            File.WriteAllText("output/notebook/verification.txt", report);
            Debug.Log(report);
            return report;
        }
        finally
        {
            Time.timeScale = timeScale;
            InputSystem.RemoveDevice(keyboard);
            InputSystem.RemoveDevice(mouse);
        }
    }
}
