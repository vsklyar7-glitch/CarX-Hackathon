using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public static class VerifyPhotoPuzzle
{
    public static string Geometry()
    {
        float area = 0;
        for (int i = 0; i < PhotoPuzzleLayout.Count; i++)
        {
            Vector2 center = PhotoPuzzleLayout.Center(i);
            Vector2[] outline = PhotoPuzzleLayout.Outline(i);
            for (int j = 0; j < outline.Length; j++)
            {
                Vector2 a = outline[j] - center, b = outline[(j + 1) % outline.Length] - center;
                float triangle = (a.x * b.y - a.y * b.x) * .5f;
                Check(triangle > 0, "Invalid triangle in fragment " + i);
                area += triangle;
            }
        }
        Check(Mathf.Abs(area - 1) < .0001f, "Pieces do not cover the photograph exactly");
        return "Seven irregular fragments: valid triangles; total area = " + area;
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    public static string Open()
    {
        var player = UnityEngine.Object.FindAnyObjectByType<PlayerInteraction>();
        var item = UnityEngine.Object.FindObjectsByType<EvidenceItem>().Single(e => e.evidenceId == "family_photo");
        var journal = EvidenceJournal.GetOrCreate();
        var items = UnityEngine.Object.FindObjectsByType<EvidenceItem>();
        foreach (string id in InvestigationDayFlow.Current.requiredEvidenceIds)
            journal.Add(items.Single(item => item.evidenceId == id));
        Check(!InvestigationDayFlow.Current.CanReturnToStation, "Day2 must wait for restoration");
        Check(item.requiresPhotoAssembly && item.tornPhotoVisual.activeSelf && !item.intactPhotoVisual.activeSelf, "World torn visuals missing");
        player.Inspect(item);
        Check(player.IsSolvingPhoto && player.GetComponent<PlayerController>().ControlsBlocked, "Puzzle must block controls");
        Check(!player.GetComponent<PlayerNotebook>().Open(), "Notebook must stay closed");
        var puzzle = player.inspectionUI.GetComponent<PhotoPuzzleView>();
        Check(puzzle.Surface.GetComponentsInChildren<PhotoPuzzlePiece>().Length == 7, "Expected seven UI fragments");
        Check(item.tornPhotoVisual.transform.childCount == 7, "Expected seven world fragments");
        Check(EventSystem.current != null, "EventSystem is missing");
        var piece = puzzle.Surface.GetComponentsInChildren<PhotoPuzzlePiece>()[0];
        Check(!puzzle.TryPlace(piece), "Wrong location must not snap");
        player.CloseInspection();
        Check(!player.IsSolvingPhoto && !player.GetComponent<PlayerController>().ControlsBlocked && !journal.FamilyPhotoRestored, "Cancel must restore controls without solving");
        player.Inspect(item);
        Debug.Log("PHOTO CHECK: puzzle opened, controls and notebook blocked, incorrect drop rejected, cancellation verified.");
        return "Open checks passed";
    }

    public static async Task<string> Complete()
    {
        var player = UnityEngine.Object.FindAnyObjectByType<PlayerInteraction>();
        var puzzle = player.inspectionUI.GetComponent<PhotoPuzzleView>();
        var journal = EvidenceJournal.GetOrCreate();
        int count = journal.Count, notifications = 0;
        Action onRestore = () => notifications++;
        journal.PhotoRestored += onRestore;
        foreach (var piece in puzzle.Surface.GetComponentsInChildren<PhotoPuzzlePiece>().OrderByDescending(p => p.transform.GetSiblingIndex()).ToArray())
        {
            Vector2 destination = Vector2.Scale(PhotoPuzzlePiece.Center(piece.Index) - Vector2.one * .5f, puzzle.PhotoSize);
            Vector2 start = RectTransformUtility.WorldToScreenPoint(null, piece.rectTransform.position);
            Vector2 end = RectTransformUtility.WorldToScreenPoint(null, puzzle.Surface.TransformPoint(destination));
            var data = new PointerEventData(EventSystem.current) { position = start };
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(data, hits);
            Check(hits.Count > 0 && hits[0].gameObject == piece.gameObject, "Piece is not reachable by mouse: " + piece.Index);
            InputSystem.QueueStateEvent(Mouse.current, new MouseState { position = start });
            await Task.Delay(80);
            InputSystem.QueueStateEvent(Mouse.current, new MouseState { position = start }.WithButton(MouseButton.Left));
            await Task.Delay(100);
            InputSystem.QueueStateEvent(Mouse.current, new MouseState { position = Vector2.Lerp(start, end, .5f) }.WithButton(MouseButton.Left));
            await Task.Delay(100);
            InputSystem.QueueStateEvent(Mouse.current, new MouseState { position = end }.WithButton(MouseButton.Left));
            await Task.Delay(100);
            InputSystem.QueueStateEvent(Mouse.current, new MouseState { position = end });
            await Task.Delay(120);
            Check(piece == null || piece.IsPlaced, "Real input did not place fragment " + piece.Index);
        }
        Check(journal.FamilyPhotoRestored && !player.IsSolvingPhoto, "Puzzle not completed");
        Check(journal.Count == count, "Duplicate evidence was added");
        Check(InvestigationDayFlow.Current.CanReturnToStation, "Day2 remains blocked after solving");
        var item = UnityEngine.Object.FindObjectsByType<EvidenceItem>().Single(e => e.evidenceId == "family_photo");
        Check(!item.tornPhotoVisual.activeSelf && item.intactPhotoVisual.activeSelf, "World photo did not restore");
        player.CloseInspection();
        Check(!player.GetComponent<PlayerController>().ControlsBlocked, "Controls not restored");
        player.Inspect(item);
        Check(!player.IsSolvingPhoto && journal.Count == count, "Repeated inspection restarted puzzle or duplicated evidence");
        journal.RestoreFamilyPhoto();
        Check(notifications == 1, "Restoration event fired twice");
        journal.PhotoRestored -= onRestore;
        player.CloseInspection();
        Debug.Log("PHOTO CHECK: seven real InputSystem mouse drags through EventSystem passed; world photo restored; Day2 unlocked; no duplicate evidence/event; controls restored; repeated inspection passed.");
        return "All completion checks passed";
    }
}
