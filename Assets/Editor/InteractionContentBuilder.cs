using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

// Генератор редактируемых ассетов. Ничего не создаёт при запуске игры.
public static class InteractionContentBuilder
{
    private const string Root = "Assets/InteractionContent";
    private static TMP_FontAsset font;
    private static Material paper, dark, brass, ink, red;
    public static readonly string[] Names = { "CaseFile", "FamilyPhoto", "Receipt", "BasementKey", "DmitryDiary", "IvanLetter", "ChildDrawing", "FatherNote" };
    public static readonly string[] Ids = { "case_file", "family_photo", "electricity_receipt", "basement_key_17", "dmitry_diary", "ivan_letter", "child_drawing", "father_note" };
    private static readonly string[] Titles = { "Папка дела", "Семейная фотография", "Квитанция за электричество", "Ключ №17", "Дневник Дмитрия", "Письмо Ивана", "Детский рисунок", "Записка отца" };
    private static readonly string[] Descriptions = {
        "Дело о пропавшем Фёдоре Павловиче Карамазове. В материалах указан адрес нашей старой квартиры. Почему это дело снова оказалось на моём столе?",
        "Отец и трое сыновей: Дмитрий, Иван и я. За нашими спинами видна дверь подвала. Мы всегда старались не смотреть в её сторону.",
        "Счета за электричество продолжали оплачивать после официального исчезновения отца. Кто-то оставался в квартире. Возможно, он никуда не уезжал.",
        "Старый латунный ключ с биркой «17». На нём знакомые следы ржавчины. Дверь подвала всё ещё заперта: одного ключа недостаточно, чтобы понять случившееся.",
        "«Я хотел остановить его. Я был в ярости, но не смог сделать то, что задумал. После той ночи мы договорились молчать». — Дмитрий.",
        "«Я знал, что он прячет за дверью. Не открывайте подвал. Мы слишком долго делали вид, что ничего не происходит». — Иван.",
        "Детский рисунок: маленький Алексей, отец и открытая дверь. Я узнаю свой почерк. Отец попросил меня открыть её, и я послушался, не понимая последствий.",
        "«Алёша, не вини себя. Ты был ребёнком и выполнил мою просьбу. Я сам решил остаться за дверью, чтобы удержать Глубину. Прости меня». — Отец."
    };

    [MenuItem("Tools/Detective/Generate Interaction Content")]
    public static void Generate()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Сначала остановите Play Mode.");
        if (string.IsNullOrEmpty(SceneManager.GetActiveScene().path))
            throw new InvalidOperationException("Сначала сохраните текущую Untitled-сцену. Генератор не заменяет несохранённые сцены.");
        Directory.CreateDirectory(Root + "/Prefabs");
        Directory.CreateDirectory(Root + "/Materials");
        AssetDatabase.Refresh();
        if (!File.Exists("Assets/TextMesh Pro/Resources/TMP Settings.asset"))
        {
            TMP_PackageResourceImporter.ImportResources(true, false, false);
            EditorApplication.delayCall += Generate;
            return;
        }
        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Root + "/RussianFont.asset");
        if (font == null)
        {
            Font source = AssetDatabase.LoadAssetAtPath<Font>("Assets/TextMesh Pro/Fonts/LiberationSans.ttf");
            if (source == null) throw new InvalidOperationException("Не найден LiberationSans.ttf из TMP Essentials.");
            font = TMP_FontAsset.CreateFontAsset(source);
            if (font == null) throw new InvalidOperationException("Не удалось создать шрифт для интерфейса.");
            font.name = "RussianFont";
            font.atlasPopulationMode = AtlasPopulationMode.Dynamic;
            AssetDatabase.CreateAsset(font, Root + "/RussianFont.asset");
            AssetDatabase.AddObjectToAsset(font.material, font);
            foreach (Texture2D texture in font.atlasTextures) AssetDatabase.AddObjectToAsset(texture, font);
            font.TryAddCharacters("АБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯабвгдеёжзийклмнопрстуфхцчшщъыьэюя0123456789 №—«»•.,:!?()-ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz");
            EditorUtility.SetDirty(font);
        }
        paper = Material("AgedPaper", new Color(0.76f, 0.68f, 0.48f));
        dark = Material("LeatherAndFrame", new Color(0.13f, 0.08f, 0.055f));
        brass = Material("OldBrass", new Color(0.57f, 0.39f, 0.13f));
        ink = Material("Ink", new Color(0.08f, 0.1f, 0.12f));
        red = Material("RedMark", new Color(0.47f, 0.07f, 0.04f));

        // Изолированная сцена: не сохраняем и не меняем текущую сцену команды.
        Scene previous = SceneManager.GetActiveScene();
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        SceneManager.SetActiveScene(scene);
        try
        {
            for (int i = 0; i < Names.Length; i++)
            {
                string path = Root + "/Prefabs/" + Names[i] + ".prefab";
                if (File.Exists(path)) continue;
                GameObject item = MakeItem(i);
                PrefabUtility.SaveAsPrefabAsset(item, path);
                UnityEngine.Object.DestroyImmediate(item);
            }
            string playerPath = Root + "/Prefabs/InvestigationPlayer.prefab";
            if (!File.Exists(playerPath))
            {
                GameObject player = MakePlayer();
                PrefabUtility.SaveAsPrefabAsset(player, playerPath);
                UnityEngine.Object.DestroyImmediate(player);
            }
            if (!File.Exists(Root + "/InteractionDemo.unity"))
            {
                MakeDemo();
                EditorSceneManager.SaveScene(scene, Root + "/InteractionDemo.unity");
            }
            AssetDatabase.SaveAssets();
            Debug.Log("Interaction content ready: 8 evidence prefabs, InvestigationPlayer and InteractionDemo.");
            Directory.CreateDirectory("Documentation");
            File.WriteAllText("Documentation/interaction-generation.txt", "Generated successfully in Unity " + Application.unityVersion + "\n");
        }
        finally
        {
            SceneManager.SetActiveScene(previous);
            EditorSceneManager.CloseScene(scene, true);
        }
    }

    private static Material Material(string name, Color color)
    {
        string path = Root + "/Materials/" + name + ".mat";
        Material result = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (result != null) return result;
        result = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        result.color = color;
        AssetDatabase.CreateAsset(result, path);
        return result;
    }

    private static GameObject Part(Transform parent, string name, Vector3 position, Vector3 size, Material material, PrimitiveType type = PrimitiveType.Cube)
    {
        GameObject part = GameObject.CreatePrimitive(type);
        part.name = name;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = position;
        part.transform.localScale = size;
        if (material != null) part.GetComponent<Renderer>().sharedMaterial = material;
        // Один общий коллайдер улики; декоративные детали не перекрывают луч.
        UnityEngine.Object.DestroyImmediate(part.GetComponent<Collider>());
        return part;
    }

    private static GameObject MakeItem(int index)
    {
        GameObject root = new GameObject(Names[index]);
        EvidenceItem evidence = root.AddComponent<EvidenceItem>();
        evidence.evidenceId = Ids[index];
        evidence.title = Titles[index];
        evidence.description = Descriptions[index];
        BoxCollider collider = root.GetComponent<BoxCollider>();
        collider.center = new Vector3(0, 0.025f, 0);
        collider.size = new Vector3(0.34f, 0.06f, 0.43f);
        Transform t = root.transform;
        if (index == 3)
        {
            collider.size = new Vector3(0.21f, 0.06f, 0.38f);
            // Кольцо из сегментов, стержень и зубцы ключа.
            for (int i = 0; i < 16; i++)
            {
                float angle = i * Mathf.PI * 2 / 16;
                GameObject segment = Part(t, "KeyRing", new Vector3(Mathf.Cos(angle) * 0.06f, 0.02f, 0.1f + Mathf.Sin(angle) * 0.06f), new Vector3(0.03f, 0.018f, 0.018f), brass);
                segment.transform.localRotation = Quaternion.Euler(0, -angle * Mathf.Rad2Deg - 90, 0);
            }
            Part(t, "Shaft", new Vector3(0, 0.02f, -0.04f), new Vector3(0.018f, 0.018f, 0.2f), brass);
            for (int i = 0; i < 3; i++) Part(t, "Tooth", new Vector3(0.022f, 0.02f, -0.08f - i * 0.025f), new Vector3(0.055f, 0.018f, 0.012f), brass);
            Part(t, "Tag17", new Vector3(-0.08f, 0.02f, 0.03f), new Vector3(0.07f, 0.018f, 0.09f), paper);
        }
        else
        {
            bool book = index == 0 || index == 4;
            Part(t, "Cover", new Vector3(0, 0.016f, 0), new Vector3(0.32f, book ? 0.035f : 0.015f, 0.41f), book || index == 1 ? dark : paper);
            Part(t, "Page", new Vector3(0, book ? 0.037f : 0.026f, 0), new Vector3(0.28f, 0.003f, 0.37f), paper);
            if (index == 1)
            {
                for (int i = 0; i < 4; i++)
                {
                    float x = -0.09f + i * 0.06f;
                    Part(t, "PortraitHead", new Vector3(x, 0.032f, 0.05f), new Vector3(0.038f, 0.004f, 0.042f), ink, PrimitiveType.Sphere);
                    Part(t, "PortraitBody", new Vector3(x, 0.032f, -0.025f), new Vector3(0.045f, 0.004f, 0.095f), ink);
                }
            }
            else if (index == 6)
            {
                // Детский рисунок: дверь и две фигуры, а не ещё одна страница текста.
                Part(t, "DrawnDoor", new Vector3(0.07f, 0.041f, 0.01f), new Vector3(0.09f, 0.002f, 0.21f), ink);
                for (int i = 0; i < 2; i++)
                {
                    float x = -0.09f + i * 0.06f;
                    Part(t, "DrawnHead", new Vector3(x, 0.042f, 0.06f), new Vector3(0.035f, 0.002f, 0.035f), red, PrimitiveType.Sphere);
                    Part(t, "DrawnBody", new Vector3(x, 0.042f, 0), new Vector3(0.008f, 0.002f, 0.09f), red);
                    Part(t, "DrawnArms", new Vector3(x, 0.042f, 0.01f), new Vector3(0.045f, 0.002f, 0.008f), red);
                }
            }
            else
            {
                for (int i = 0; i < 7; i++) Part(t, "Writing", new Vector3(0, 0.041f, 0.11f - i * 0.034f), new Vector3(i % 3 == 0 ? 0.16f : 0.23f, 0.002f, 0.004f), ink);
                Part(t, "Mark", new Vector3(0.08f, 0.043f, -0.13f), new Vector3(0.045f, 0.002f, 0.035f), red);
            }
        }
        return root;
    }

    private static GameObject MakePlayer()
    {
        GameObject root = new GameObject("InvestigationPlayer");
        CharacterController character = root.AddComponent<CharacterController>();
        character.height = 1.8f; character.radius = 0.3f; character.center = new Vector3(0, 0.9f, 0);
        GameObject cameraObject = new GameObject("PlayerCamera", typeof(Camera), typeof(AudioListener));
        cameraObject.tag = "MainCamera";
        cameraObject.transform.SetParent(root.transform, false);
        cameraObject.transform.localPosition = new Vector3(0, 1.65f, 0);
        PlayerController controller = root.AddComponent<PlayerController>();
        controller.playerCamera = cameraObject.transform;
        PlayerInteraction interaction = root.AddComponent<PlayerInteraction>();
        interaction.playerCamera = cameraObject.GetComponent<Camera>();
        interaction.inspectionUI = MakeUI(root.transform);
        return root;
    }

    private static RectTransform Rect(string name, Transform parent, Vector2 min, Vector2 max, Vector2 inset)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform rect = (RectTransform)go.transform;
        rect.SetParent(parent, false); rect.anchorMin = min; rect.anchorMax = max;
        rect.offsetMin = inset; rect.offsetMax = -inset;
        return rect;
    }

    private static TMP_Text Text(string name, Transform parent, string value, Vector2 min, Vector2 max, float size)
    {
        RectTransform rect = Rect(name, parent, min, max, new Vector2(14, 8));
        TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.font = font; text.text = value; text.fontSize = size;
        text.color = new Color(0.92f, 0.87f, 0.74f); text.raycastTarget = false;
        text.overflowMode = TextOverflowModes.Truncate;
        return text;
    }

    private static InspectionUI MakeUI(Transform parent)
    {
        GameObject canvasObject = new GameObject("InspectionCanvas", typeof(RectTransform), typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
        canvasObject.transform.SetParent(parent, false);
        canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        UnityEngine.UI.CanvasScaler scaler = canvasObject.GetComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720); scaler.matchWidthOrHeight = 0.5f;
        InspectionUI ui = canvasObject.AddComponent<InspectionUI>();
        TMP_Text crosshair = Text("Crosshair", canvasObject.transform, "+", new Vector2(0.47f, 0.47f), new Vector2(0.53f, 0.53f), 22);
        crosshair.alignment = TextAlignmentOptions.Center;
        ui.prompt = Text("InteractionPrompt", canvasObject.transform, "", new Vector2(0.15f, 0.13f), new Vector2(0.85f, 0.23f), 23);
        ui.prompt.alignment = TextAlignmentOptions.Center;
        Text("Help", canvasObject.transform, "WASD — ходьба   •   Мышь — обзор   •   E — осмотр   •   Esc — курсор", new Vector2(0.02f, 0.01f), new Vector2(0.98f, 0.09f), 17);
        RectTransform panel = Rect("EvidencePanel", canvasObject.transform, new Vector2(0.06f, 0.12f), new Vector2(0.94f, 0.92f), Vector2.zero);
        UnityEngine.UI.Image background = panel.gameObject.AddComponent<UnityEngine.UI.Image>();
        background.color = new Color(0.035f, 0.045f, 0.05f, 0.98f);
        ui.panel = panel.gameObject;
        ui.title = Text("Title", panel, "", new Vector2(0.02f, 0.8f), new Vector2(0.68f, 0.98f), 32);
        ui.description = Text("Description", panel, "", new Vector2(0.02f, 0.29f), new Vector2(0.66f, 0.8f), 25);
        ui.status = Text("Status", panel, "", new Vector2(0.02f, 0.16f), new Vector2(0.66f, 0.28f), 19);
        ui.journal = Text("Notebook", panel, "", new Vector2(0.69f, 0.18f), new Vector2(0.98f, 0.95f), 20);
        RectTransform imageRect = Rect("EvidenceImage", panel, new Vector2(0.48f, 0.29f), new Vector2(0.65f, 0.58f), Vector2.zero);
        ui.image = imageRect.gameObject.AddComponent<UnityEngine.UI.Image>();
        ui.image.preserveAspect = true; ui.image.raycastTarget = false;
        imageRect.gameObject.SetActive(false);
        RectTransform button = Rect("CloseButton", panel, new Vector2(0.03f, 0.03f), new Vector2(0.4f, 0.15f), Vector2.zero);
        button.gameObject.AddComponent<UnityEngine.UI.Image>().color = new Color(0.2f, 0.25f, 0.24f);
        ui.closeButton = button.gameObject.AddComponent<UnityEngine.UI.Button>();
        TMP_Text label = Text("Label", button, "Закрыть  [E / Esc]", Vector2.zero, Vector2.one, 22);
        label.alignment = TextAlignmentOptions.Center;
        panel.gameObject.SetActive(false);
        return ui;
    }

    private static void MakeDemo()
    {
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = "DemoFloor"; floor.transform.position = new Vector3(0, -0.15f, 2);
        floor.transform.localScale = new Vector3(10, 0.3f, 10); floor.GetComponent<Renderer>().sharedMaterial = dark;
        GameObject lightObject = new GameObject("DemoLight", typeof(Light));
        lightObject.GetComponent<Light>().type = LightType.Directional;
        lightObject.GetComponent<Light>().intensity = 1.5f;
        lightObject.transform.rotation = Quaternion.Euler(55, -25, 0);
        RenderSettings.ambientLight = new Color(0.4f, 0.4f, 0.4f);
        GameObject player = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Prefabs/InvestigationPlayer.prefab"));
        player.transform.position = new Vector3(0, 0.05f, -1.3f);
        for (int i = 0; i < Names.Length; i++)
        {
            Vector3 position = new Vector3((i % 4 - 1.5f) * 1.35f, 0.95f, 1.1f + (i / 4) * 2.2f);
            GameObject table = GameObject.CreatePrimitive(PrimitiveType.Cube);
            table.name = "Display_" + Names[i]; table.transform.position = position - Vector3.up * 0.5f;
            table.transform.localScale = new Vector3(1.1f, 0.95f, 0.8f);
            table.GetComponent<Renderer>().sharedMaterial = dark;
            GameObject item = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Prefabs/" + Names[i] + ".prefab"));
            item.transform.position = position;
        }
        new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
    }

    [MenuItem("Tools/Detective/Attach Interaction To Selected Player")]
    private static void AttachToSelectedPlayer()
    {
        PlayerController player = Selection.activeGameObject != null ? Selection.activeGameObject.GetComponent<PlayerController>() : null;
        if (player == null) { Debug.LogError("Выделите объект с PlayerController Дениса."); return; }
        if (player.GetComponent<PlayerInteraction>() != null) { Debug.Log("Взаимодействие уже подключено."); return; }
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Prefabs/InvestigationPlayer.prefab");
        if (prefab == null) { Debug.LogError("Сначала Generate Interaction Content."); return; }
        Undo.RecordObject(player.gameObject, "Attach interaction");
        InspectionUI template = prefab.GetComponentInChildren<InspectionUI>(true);
        GameObject uiObject = UnityEngine.Object.Instantiate(template.gameObject, player.transform);
        Undo.RegisterCreatedObjectUndo(uiObject, "Create inspection UI");
        PlayerInteraction interaction = Undo.AddComponent<PlayerInteraction>(player.gameObject);
        interaction.playerCamera = player.GetComponentInChildren<Camera>();
        interaction.inspectionUI = uiObject.GetComponent<InspectionUI>();
        if (UnityEngine.Object.FindAnyObjectByType<EventSystem>() == null)
            Undo.RegisterCreatedObjectUndo(new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule)), "Create event system");
        EditorSceneManager.MarkSceneDirty(player.gameObject.scene);
    }
}
