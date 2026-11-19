using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class NotebookBuilder
{
    private static TMP_FontAsset font;
    private static readonly Color Ink = new Color(.17f, .13f, .09f);

    [MenuItem("Tools/Detective/Build Notebook")]
    public static string Build()
    {
        Directory.CreateDirectory("Assets/Resources/NotebookArt");
        AssetDatabase.Refresh();
        font = HandwritingFont();
        GenerateIllustrations();
        foreach (string path in Directory.GetFiles("Assets/Resources/NotebookArt", "*.png"))
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.maxTextureSize = 1024;
            importer.SaveAndReimport();
        }
        GameObject root = new GameObject("NotebookCanvas", typeof(RectTransform), typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
        try
        {
            root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            root.GetComponent<Canvas>().sortingOrder = 100;
            var scaler = root.GetComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight = .5f;
            NotebookView view = root.AddComponent<NotebookView>();
            RectTransform shade = Box(root.transform, "NotebookPanel", Vector2.zero, Vector2.one, new Color(.025f,.02f,.018f,.88f), true);
            view.panel = shade.gameObject;
            RectTransform cover = Box(shade, "LeatherCover", new Vector2(.055f,.115f), new Vector2(.945f,.9f), new Color(.16f,.095f,.055f));
            Box(cover, "CoverInset", new Vector2(.008f,.018f), new Vector2(.992f,.982f), new Color(.29f,.19f,.11f));
            view.leftPaper = Box(cover, "LeftPaper", new Vector2(.022f,.03f), new Vector2(.498f,.97f), new Color(.83f,.76f,.60f));
            view.rightPaper = Box(cover, "RightPaper", new Vector2(.502f,.03f), new Vector2(.978f,.97f), new Color(.91f,.85f,.70f));
            Box(cover, "SpineShadow", new Vector2(.486f,.03f), new Vector2(.514f,.97f), new Color(.24f,.15f,.07f,.35f));
            var leftHeader = Text(cover, "LeftHeader", "Личный блокнот  /  А. Карамазов", new Vector2(.055f,.85f), new Vector2(.46f,.93f), 20, Ink);
            RectTransform mat = Box(cover, "PhotographMount", new Vector2(.055f,.19f), new Vector2(.46f,.82f), new Color(.94f,.89f,.77f));
            RectTransform picture = Rect(mat, "EvidenceIllustration", new Vector2(.04f,.055f), new Vector2(.96f,.945f));
            view.illustration = picture.gameObject.AddComponent<UnityEngine.UI.Image>();
            view.illustration.preserveAspect = true;
            view.illustration.raycastTarget = false;
            var caption = Text(cover, "Caption", "Материалы расследования", new Vector2(.06f,.075f), new Vector2(.46f,.16f), 20, Ink);
            view.evidenceCount = Text(cover, "EvidenceCount", "", new Vector2(.55f,.84f), new Vector2(.93f,.92f), 20, new Color(.4f,.29f,.16f));
            view.heading = Text(cover, "EvidenceTitle", "", new Vector2(.55f,.64f), new Vector2(.935f,.83f), 36, Ink);
            var titleRule = Box(cover, "TitleRule", new Vector2(.56f,.63f), new Vector2(.93f,.633f), new Color(.45f,.3f,.15f,.6f));
            view.body = Text(cover, "EvidenceSummary", "", new Vector2(.55f,.26f), new Vector2(.935f,.61f), 28, Ink);
            view.body.enableAutoSizing = true; view.body.fontSizeMin = 21; view.body.fontSizeMax = 28;
            view.pageNumber = Text(cover, "PageNumber", "", new Vector2(.67f,.09f), new Vector2(.86f,.19f), 20, Ink);
            view.pageNumber.alignment = TextAlignmentOptions.Center;
            view.leftPageContent = new[] { leftHeader.rectTransform, mat, caption.rectTransform };
            view.rightPageContent = new[] { view.evidenceCount.rectTransform, view.heading.rectTransform,
                titleRule, view.body.rectTransform, view.pageNumber.rectTransform };
            view.previous = Button(cover, "PreviousPage", "←", new Vector2(.55f,.075f), new Vector2(.65f,.20f));
            view.next = Button(cover, "NextPage", "→", new Vector2(.87f,.075f), new Vector2(.95f,.20f));
            view.close = Button(shade, "CloseNotebook", "ЗАКРЫТЬ  [ J / ESC ]", new Vector2(.72f,.025f), new Vector2(.945f,.09f));
            var controlsHint = Text(shade, "ControlsHint", "← / →  —  ПЕРЕЛИСТЫВАТЬ", new Vector2(.055f,.025f), new Vector2(.5f,.09f), 17, new Color(.86f,.8f,.66f));
            controlsHint.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/InteractionContent/RussianFont.asset");
            view.turningLeaf = Box(cover, "TurningLeaf", new Vector2(.5f,.03f), new Vector2(.978f,.97f), new Color(.86f,.79f,.63f));
            view.turningLeaf.pivot = new Vector2(0,.5f);
            view.turningLeaf.anchoredPosition = Vector2.zero;
            view.turningLeaf.gameObject.SetActive(false);
            shade.gameObject.SetActive(false);
            PrefabUtility.SaveAsPrefabAsset(root, "Assets/Resources/NotebookCanvas.prefab");
            AssetDatabase.SaveAssets();
            return "NotebookCanvas.prefab and 8 evidence illustrations ready";
        }
        finally { Object.DestroyImmediate(root); }
    }

    private static TMP_FontAsset HandwritingFont()
    {
        const string path = "Assets/Fonts/MarckScript/NotebookHandwriting.asset";
        var asset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
        if (asset != null) return asset;
        var source = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/MarckScript/MarckScript-Regular.ttf");
        if (source == null) throw new System.Exception("Marck Script source font is missing.");
        asset = TMP_FontAsset.CreateFontAsset(source, 90, 9, UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
        asset.name = "NotebookHandwriting";
        AssetDatabase.CreateAsset(asset, path);
        const string alphabet = "АБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯабвгдеёжзийклмнопрстуфхцчшщъыьэюя";
        if (!asset.TryAddCharacters(alphabet)) throw new System.Exception("Font does not contain complete Russian alphabet.");
        asset.TryAddCharacters("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789 .,!?;:—–-()[]/«»\"'№+←→•");
        asset.fallbackFontAssetTable = new System.Collections.Generic.List<TMP_FontAsset> { AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/InteractionContent/RussianFont.asset") };
        asset.atlasPopulationMode = AtlasPopulationMode.Static;
        AssetDatabase.AddObjectToAsset(asset.material, asset);
        foreach (var texture in asset.atlasTextures) AssetDatabase.AddObjectToAsset(texture, asset);
        EditorUtility.SetDirty(asset);
        return asset;
    }

    private static RectTransform Rect(Transform parent, string name, Vector2 min, Vector2 max)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false); rect.anchorMin = min; rect.anchorMax = max;
        rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
        return rect;
    }
    private static RectTransform Box(Transform parent, string name, Vector2 min, Vector2 max, Color color, bool raycast = false)
    {
        RectTransform rect = Rect(parent, name, min, max);
        var image = rect.gameObject.AddComponent<UnityEngine.UI.Image>();
        image.color = color; image.raycastTarget = raycast;
        return rect;
    }
    private static TMP_Text Text(Transform parent, string name, string value, Vector2 min, Vector2 max, float size, Color color)
    {
        var text = Rect(parent, name, min, max).gameObject.AddComponent<TextMeshProUGUI>();
        text.font = font; text.text = value; text.fontSize = size; text.color = color;
        text.raycastTarget = false;
        return text;
    }
    private static UnityEngine.UI.Button Button(Transform parent, string name, string label, Vector2 min, Vector2 max)
    {
        var rect = Box(parent, name, min, max, new Color(.35f,.23f,.13f), true);
        var button = rect.gameObject.AddComponent<UnityEngine.UI.Button>();
        var text = Text(rect, "Label", label, new Vector2(.03f,.05f), new Vector2(.97f,.95f), label.Length < 3 ? 34 : 17, new Color(.95f,.88f,.73f));
        text.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/InteractionContent/RussianFont.asset");
        text.alignment = TextAlignmentOptions.Center;
        return button;
    }

    private static void GenerateIllustrations()
    {
        var scene = EditorSceneManager.NewPreviewScene();
        try
        {
            var cameraObject = new GameObject("NotebookArtCamera", typeof(Camera));
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cameraObject, scene);
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.scene = scene; camera.orthographic = true; camera.orthographicSize = .27f;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.83f,.76f,.60f);
            camera.transform.position = new Vector3(0,1,-.25f); camera.transform.LookAt(Vector3.zero);
            var lamp = new GameObject("NotebookArtLight", typeof(Light));
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(lamp, scene);
            lamp.GetComponent<Light>().type = LightType.Directional; lamp.GetComponent<Light>().intensity = 2.5f;
            lamp.transform.rotation = Quaternion.Euler(65,-20,0);
            foreach (string path in Directory.GetFiles("Assets/InteractionContent/Prefabs", "*.prefab"))
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                EvidenceItem evidence = prefab.GetComponent<EvidenceItem>();
                if (evidence == null) continue;
                string output = "Assets/Resources/NotebookArt/" + evidence.evidenceId + ".png";
                if (File.Exists(output)) continue;
                GameObject instance = Object.Instantiate(prefab);
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(instance, scene);
                var rt = new RenderTexture(512,512,24);
                var texture = new Texture2D(512,512,TextureFormat.RGB24,false);
                RenderTexture old = RenderTexture.active;
                try
                {
                    camera.targetTexture = rt; camera.Render(); RenderTexture.active = rt;
                    texture.ReadPixels(new Rect(0,0,512,512),0,0); texture.Apply();
                    File.WriteAllBytes(output, texture.EncodeToPNG());
                }
                finally { RenderTexture.active = old; camera.targetTexture = null; Object.DestroyImmediate(rt); Object.DestroyImmediate(texture); Object.DestroyImmediate(instance); }
            }
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
        AssetDatabase.Refresh();
    }
}
