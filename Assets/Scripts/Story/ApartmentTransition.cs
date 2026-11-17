using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Временный экран перехода. Сохраняется только до окончания загрузки квартиры.
public class ApartmentTransition : MonoBehaviour
{
    public static ApartmentTransition Current { get; private set; }
    private CanvasGroup blackScreen;
    private AudioSource sound;
    private readonly Dictionary<PlayerController, bool> blockedPlayers = new Dictionary<PlayerController, bool>();
    private CursorLockMode savedCursor;
    private bool savedCursorVisible;
    private bool released;

    public static void Begin(string path, PlayerInteraction player, AudioClip clip,
        float fadeOutDuration, float fadeInDuration, float volume)
    {
        if (Current != null || player == null || player.IsInspecting) return;
        PlayerController controller = player.GetComponent<PlayerController>();
        if (controller == null || controller.ControlsBlocked) return;
        var transition = new GameObject("ApartmentTransition").AddComponent<ApartmentTransition>();
        Current = transition;
        DontDestroyOnLoad(transition.gameObject);
        transition.savedCursor = Cursor.lockState;
        transition.savedCursorVisible = Cursor.visible;
        transition.CreateScreen();
        transition.Block(controller);
        player.inspectionUI.SetPrompt("");
        SceneManager.sceneLoaded += transition.OnSceneLoaded;
        transition.StartCoroutine(transition.Run(path, clip, fadeOutDuration, fadeInDuration, volume));
    }

    private void CreateScreen()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue;
        blackScreen = gameObject.AddComponent<CanvasGroup>();
        blackScreen.alpha = 0f;
        blackScreen.interactable = false;
        blackScreen.blocksRaycasts = false;
        var panel = new GameObject("BlackScreen", typeof(RectTransform), typeof(UnityEngine.UI.Image));
        panel.transform.SetParent(transform, false);
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        UnityEngine.UI.Image image = panel.GetComponent<UnityEngine.UI.Image>();
        image.color = Color.black;
        image.raycastTarget = false;
        sound = gameObject.AddComponent<AudioSource>();
        sound.playOnAwake = false;
        sound.loop = false;
        sound.spatialBlend = 0f;
        sound.ignoreListenerPause = true;
    }

    private IEnumerator Run(string path, AudioClip clip, float fadeOutDuration, float fadeInDuration, float volume)
    {
        try
        {
            yield return Fade(1f, fadeOutDuration);
            sound.clip = clip;
            sound.volume = volume;
            if (clip != null) sound.Play();
            else Debug.LogWarning("Не назначен звук перехода в " + path);

            // Загружаем квартиру во время записи, активируем только после её окончания.
            AsyncOperation load = StartLoading(path);
            if (load != null)
            {
                load.allowSceneActivation = false;
                if (clip != null) yield return new WaitForSecondsRealtime(clip.length);
                load.allowSceneActivation = true;
                yield return load;
                // Start нового игрока выполняется под чёрным экраном с заблокированным управлением.
                yield return null;
            }
            sound.Stop();
            yield return Fade(0f, fadeInDuration);
        }
        finally
        {
            Release();
            Destroy(gameObject);
        }
    }

    private AsyncOperation StartLoading(string path)
    {
        try { return SceneManager.LoadSceneAsync(path, LoadSceneMode.Single); }
        catch (Exception error)
        {
            Debug.LogException(error, this);
            return null;
        }
    }

    private IEnumerator Fade(float target, float duration)
    {
        float from = blackScreen.alpha;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            blackScreen.alpha = Mathf.Lerp(from, target, Mathf.SmoothStep(0f, 1f, elapsed / duration));
            yield return null;
        }
        blackScreen.alpha = target;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // sceneLoaded вызывается до Start: игрок не успеет получить управление на один кадр.
        foreach (PlayerController controller in UnityEngine.Object.FindObjectsByType<PlayerController>())
            if (controller.gameObject.scene == scene) Block(controller);
    }

    private void Block(PlayerController controller)
    {
        if (!blockedPlayers.ContainsKey(controller)) blockedPlayers.Add(controller, controller.ControlsBlocked);
        controller.SetControlsBlocked(true);
    }

    private void Release()
    {
        if (released) return;
        released = true;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        foreach (var entry in blockedPlayers)
            if (entry.Key != null) entry.Key.SetControlsBlocked(entry.Value);
        if (Application.isPlaying)
        {
            Cursor.lockState = savedCursor;
            Cursor.visible = savedCursorVisible;
        }
        if (Current == this) Current = null;
    }

    private void OnDestroy() => Release();
}
