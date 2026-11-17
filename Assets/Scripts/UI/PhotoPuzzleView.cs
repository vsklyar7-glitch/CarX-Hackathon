using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

public class PhotoPuzzleView : MonoBehaviour
{
    [Min(1)] public float snapDistance = 45;
    public bool IsOpen => root != null && root.activeSelf;
    public Vector2 PhotoSize { get; private set; }
    public RectTransform Surface { get; private set; }
    private GameObject root;
    private TMP_Text progress;
    private Action onComplete;
    private int placed;

    public void Show(EvidenceItem item, InspectionUI inspection, Action complete, Action cancel)
    {
        Hide();
        Sprite photo = item.image != null ? item.image : Resources.Load<Sprite>("NotebookArt/family_photo");
        if (photo == null) { Debug.LogError("Не найдена семейная фотография.", this); cancel(); return; }
        onComplete = complete;
        placed = 0;
        PhotoSize = new Vector2(560, 560 * photo.rect.height / photo.rect.width);
        root = new GameObject("TornPhotoPuzzle", typeof(RectTransform), typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;
        var scaler = root.GetComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600, 1000);
        scaler.matchWidthOrHeight = 1;
        // В Day-сценах EventSystem может отсутствовать: без него ни drag, ни Button не работают.
        if (UnityEngine.Object.FindAnyObjectByType<EventSystem>() == null)
        {
            var events = new GameObject("PhotoPuzzleEventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            events.transform.SetParent(root.transform, false);
            events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }
        var background = Rect("Background", root.transform, Vector2.zero, Vector2.zero);
        background.anchorMin = Vector2.zero;
        background.anchorMax = Vector2.one;
        background.gameObject.AddComponent<UnityEngine.UI.Image>().color = new Color(.035f, .03f, .025f, .98f);
        Text("Title", "Порванная семейная фотография", root.transform, new Vector2(0, 445), new Vector2(1450, 65), inspection.title.font, 38);
        progress = Text("Instructions", "Перетаскивайте фрагменты мышью в рамку · Собрано 0 / 7", root.transform, new Vector2(0, 390), new Vector2(1450, 50), inspection.title.font, 25);
        var surface = Rect("AssemblySurface", root.transform, new Vector2(0, -20), new Vector2(1450, 800));
        Surface = surface;
        var board = Rect("PhotoOutline", surface, Vector2.zero, PhotoSize + Vector2.one * 8);
        board.gameObject.AddComponent<UnityEngine.UI.Image>().color = new Color(.22f, .19f, .15f);
        var pieces = Rect("Pieces", surface, Vector2.zero, surface.sizeDelta);
        Surface = pieces;
        Vector2[] slots = { new Vector2(-530, 240), new Vector2(-500, -20), new Vector2(-510, -255),
            new Vector2(510, 225), new Vector2(535, -35), new Vector2(500, -260), new Vector2(-70, -300) };
        var random = new System.Random();
        int[] order = { 0, 1, 2, 3, 4, 5, 6 };
        for (int i = order.Length - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            int temporary = order[i]; order[i] = order[j]; order[j] = temporary;
        }
        for (int i = 0; i < PhotoPuzzleLayout.Count; i++)
        {
            int index = order[i];
            Vector2 jitter = new Vector2((float)random.NextDouble() * 36 - 18, (float)random.NextDouble() * 32 - 16);
            var rect = Rect("Fragment " + (index + 1), pieces, slots[i] + jitter, PhotoPuzzleLayout.Size(index, PhotoSize));
            rect.localRotation = Quaternion.Euler(0, 0, (float)random.NextDouble() * 26 - 13);
            rect.gameObject.AddComponent<PhotoPuzzlePiece>().Initialize(index, photo, this);
        }
        var close = Rect("ClosePuzzle", root.transform, new Vector2(0, -450), new Vector2(500, 60));
        close.gameObject.AddComponent<UnityEngine.UI.Image>().color = new Color(.23f, .20f, .16f);
        close.gameObject.AddComponent<UnityEngine.UI.Button>().onClick.AddListener(() => cancel());
        Text("Label", "Отложить · Esc / E", close, Vector2.zero, close.sizeDelta, inspection.title.font, 26);
    }

    public bool TryPlace(PhotoPuzzlePiece piece)
    {
        if (!IsOpen || piece.IsPlaced) return false;
        Vector2 target = Vector2.Scale(PhotoPuzzlePiece.Center(piece.Index) - Vector2.one * .5f, PhotoSize);
        if (Vector2.Distance(piece.rectTransform.anchoredPosition, target) > snapDistance) return false;
        piece.Place(target);
        placed++;
        progress.text = "Перетаскивайте фрагменты мышью в рамку · Собрано " + placed + " / 7";
        if (placed == PhotoPuzzleLayout.Count)
        {
            Action complete = onComplete;
            Hide();
            complete?.Invoke();
        }
        return true;
    }

    public void Hide()
    {
        onComplete = null;
        if (root != null) { root.SetActive(false); Destroy(root); }
        root = null;
    }

    private void OnDisable() => Hide();

    private static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * .5f;
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        return rect;
    }

    private static TMP_Text Text(string name, string value, Transform parent, Vector2 position, Vector2 size, TMP_FontAsset font, float fontSize)
    {
        var text = Rect(name, parent, position, size).gameObject.AddComponent<TextMeshProUGUI>();
        text.font = font;
        text.fontSize = fontSize;
        text.text = value;
        text.alignment = TextAlignmentOptions.Center;
        text.color = new Color(.92f, .86f, .73f);
        text.raycastTarget = false;
        return text;
    }
}
