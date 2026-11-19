using System;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Четыре листа дела поверх обычного окна осмотра.
public class CaseFileView : MonoBehaviour
{
    private static readonly Color Ink = new Color(0.19f, 0.16f, 0.13f);
    private static readonly Color Paper = new Color(0.83f, 0.79f, 0.68f);
    private GameObject root;
    private TMP_Text heading;
    private TMP_Text body;
    private TMP_Text counter;
    private UnityEngine.UI.Image photograph;
    private UnityEngine.UI.Button previous;
    private UnityEngine.UI.Button next;
    private string[] pages;
    private int page;
    private Action close;

    public void Show(TMP_FontAsset font, Action closeAction)
    {
        close = closeAction;
        TextAsset source = Resources.Load<TextAsset>("InvestigationFile");
        if (source == null) { Debug.LogError("Не найден Resources/InvestigationFile.txt", this); return; }
        pages = Regex.Split(source.text.Trim(), @"(?=0[1-4]\. )");
        if (pages.Length > 0 && string.IsNullOrWhiteSpace(pages[0]))
            pages = Array.FindAll(pages, part => !string.IsNullOrWhiteSpace(part));
        if (pages.Length != 4) { Debug.LogError("В InvestigationFile.txt должно быть четыре страницы.", this); return; }
        if (FindAnyObjectByType<EventSystem>() == null)
        {
            var events = new GameObject("CaseFileEventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }
        if (root == null) Build(font);
        page = 0;
        root.SetActive(true);
        root.transform.SetAsLastSibling();
        Refresh();
    }

    private void Build(TMP_FontAsset font)
    {
        RectTransform canvas = GetComponent<RectTransform>();
        root = Box("Case file", canvas, new Color(0.045f, 0.055f, 0.06f, 0.97f),
            new Vector2(0, 0), new Vector2(1, 1)).gameObject;
        Box("Folder shadow", root.transform, new Color(0.15f, 0.12f, 0.09f),
            new Vector2(.09f, .07f), new Vector2(.91f, .91f));
        Box("Folder", root.transform, new Color(.38f, .31f, .22f),
            new Vector2(.08f, .08f), new Vector2(.90f, .92f));
        Box("Folder tab", root.transform, new Color(.48f, .39f, .27f),
            new Vector2(.79f, .87f), new Vector2(.93f, .93f));
        Text("CASE FILE  /  1999", root.transform, font, 15, Color.white,
            new Vector2(.80f, .88f), new Vector2(.92f, .92f), TextAlignmentOptions.Center);
        Box("Paper edge", root.transform, new Color(.65f, .60f, .49f),
            new Vector2(.15f, .12f), new Vector2(.85f, .86f));
        Box("Report sheet", root.transform, Paper,
            new Vector2(.14f, .13f), new Vector2(.84f, .87f));
        Box("Red rule", root.transform, new Color(.47f, .22f, .18f),
            new Vector2(.18f, .755f), new Vector2(.80f, .760f));
        Text("POLICE DEPARTMENT  •  INVESTIGATION DIVISION", root.transform, font, 20, Ink,
            new Vector2(.18f, .79f), new Vector2(.80f, .84f), TextAlignmentOptions.Center);
        heading = Text("Heading", root.transform, font, 25, Ink,
            new Vector2(.19f, .69f), new Vector2(.80f, .75f), TextAlignmentOptions.Left);
        body = Text("Report", root.transform, font, 19, Ink,
            new Vector2(.19f, .23f), new Vector2(.80f, .69f), TextAlignmentOptions.TopLeft);
        body.enableAutoSizing = true;
        body.fontSizeMin = 14;
        body.fontSizeMax = 19;
        photograph = Box("Family photograph", root.transform, Color.white,
            new Vector2(.41f, .36f), new Vector2(.71f, .67f)).GetComponent<UnityEngine.UI.Image>();
        photograph.sprite = Resources.Load<Sprite>("NotebookArt/family_photo");
        photograph.preserveAspect = true;
        photograph.raycastTarget = false;
        counter = Text("Page number", root.transform, font, 16, Ink,
            new Vector2(.40f, .16f), new Vector2(.60f, .21f), TextAlignmentOptions.Center);
        previous = Button("Previous page", "◀  НАЗАД", root.transform, font,
            new Vector2(.18f, .16f), new Vector2(.34f, .22f), () => Turn(-1));
        next = Button("Next page", "ДАЛЕЕ  ▶", root.transform, font,
            new Vector2(.66f, .16f), new Vector2(.82f, .22f), () => Turn(1));
        Button("Close case", "ЗАКРЫТЬ  ·  E / ESC", root.transform, font,
            new Vector2(.39f, .035f), new Vector2(.61f, .09f), () => close?.Invoke());
    }

    private void Update()
    {
        if (root == null || !root.activeSelf || Keyboard.current == null) return;
        if (Keyboard.current.leftArrowKey.wasPressedThisFrame) Turn(-1);
        if (Keyboard.current.rightArrowKey.wasPressedThisFrame) Turn(1);
    }

    private void Turn(int direction)
    {
        page = Mathf.Clamp(page + direction, 0, 3);
        Refresh();
    }

    private void Refresh()
    {
        string content = pages[page].Trim();
        int line = content.IndexOf('\n');
        heading.text = line < 0 ? content : content.Substring(0, line).Trim();
        body.text = line < 0 ? "" : content.Substring(line + 1).Trim();
        bool showPhoto = page == 2;
        photograph.gameObject.SetActive(showPhoto && photograph.sprite != null);
        body.rectTransform.anchorMin = new Vector2(showPhoto ? .19f : .19f, showPhoto ? .24f : .23f);
        body.rectTransform.anchorMax = new Vector2(showPhoto ? .39f : .80f, .69f);
        counter.text = "ЛИСТ " + (page + 1) + " / 4";
        previous.interactable = page > 0;
        next.interactable = page < 3;
    }

    public void Hide() { if (root != null) root.SetActive(false); }

    private static RectTransform Box(string name, Transform parent, Color color, Vector2 min, Vector2 max)
    {
        var rect = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        var image = rect.GetComponent<UnityEngine.UI.Image>();
        image.color = color;
        image.raycastTarget = false;
        return rect;
    }

    private static TMP_Text Text(string name, Transform parent, TMP_FontAsset font, int size, Color color,
        Vector2 min, Vector2 max, TextAlignmentOptions alignment)
    {
        RectTransform rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
        label.font = font;
        label.fontSize = size;
        label.color = color;
        label.alignment = alignment;
        label.raycastTarget = false;
        return label;
    }

    private static UnityEngine.UI.Button Button(string name, string caption, Transform parent, TMP_FontAsset font,
        Vector2 min, Vector2 max, Action action)
    {
        var rect = Box(name, parent, new Color(.23f, .20f, .16f), min, max);
        rect.GetComponent<UnityEngine.UI.Image>().raycastTarget = true;
        var button = rect.gameObject.AddComponent<UnityEngine.UI.Button>();
        button.onClick.AddListener(() => action());
        Text("Label", rect, font, 16, Color.white, Vector2.zero, Vector2.one, TextAlignmentOptions.Center).text = caption;
        return button;
    }
}
