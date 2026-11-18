using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Экран сохраняется между сценами до завершения перехода.
public class ApartmentTransition : MonoBehaviour
{
    public static ApartmentTransition Current { get; private set; }
    [SerializeField] private CanvasGroup screen;
    [SerializeField] private CanvasGroup artwork;
    [SerializeField] private RawImage photo;
    [SerializeField] private AspectRatioFitter photoAspect;
    [SerializeField] private Texture2D stationToApartment;
    [SerializeField] private Texture2D apartmentToStation;
    [SerializeField] private AudioSource sound;
    [SerializeField] private AudioClip returnSound;
    private CursorLockMode savedCursor;
    private bool savedCursorVisible;
    private bool released;

    public static void Begin(string path, PlayerInteraction player, bool returning, AudioClip clip,
        float fadeOutDuration, float fadeInDuration, float volume, Action onArrived = null)
    {
        if (Current != null || player == null || player.IsInspecting) return;
        PlayerController controller = player.GetComponent<PlayerController>();
        if (controller == null || controller.ControlsBlocked) return;
        var prefab = Resources.Load<ApartmentTransition>("ApartmentTransition");
        if (prefab == null)
        {
            Debug.LogError("Не найден префаб ApartmentTransition.");
            return;
        }
        var transition = Instantiate(prefab);
        Current = transition;
        DontDestroyOnLoad(transition.gameObject);
        transition.savedCursor = Cursor.lockState;
        transition.savedCursorVisible = Cursor.visible;
        transition.Show(returning);
        player.inspectionUI.SetPrompt("");
        transition.StartCoroutine(transition.Run(path, returning ? transition.returnSound : clip,
            fadeOutDuration, fadeInDuration, volume, onArrived));
    }

    private void Show(bool returning)
    {
        Texture2D texture = returning ? apartmentToStation : stationToApartment;
        photo.texture = texture;
        if (texture != null) photoAspect.aspectRatio = (float)texture.width / texture.height;
        screen.alpha = artwork.alpha = 1f;
    }

    private IEnumerator Run(string path, AudioClip clip, float fadeOutDuration, float fadeInDuration,
        float volume, Action onArrived)
    {
        try
        {
            sound.clip = clip;
            sound.volume = volume;
            if (clip != null) sound.Play();

            // Даём заставке появиться и ожидаем новое нажатие Enter.
            do { yield return null; }
            while (Keyboard.current == null ||
                (!Keyboard.current.enterKey.wasPressedThisFrame && !Keyboard.current.numpadEnterKey.wasPressedThisFrame));
            sound.Stop();
            yield return Fade(artwork, fadeOutDuration);

            AsyncOperation load = StartLoading(path);
            if (load != null)
            {
                yield return load;
                onArrived?.Invoke();
                yield return null;
            }
            yield return Fade(screen, fadeInDuration);
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

    private static IEnumerator Fade(CanvasGroup group, float duration)
    {
        float from = group.alpha;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            group.alpha = Mathf.Lerp(from, 0f, Mathf.SmoothStep(0f, 1f, elapsed / duration));
            yield return null;
        }
        group.alpha = 0f;
    }

    private void Release()
    {
        if (released) return;
        released = true;
        if (Application.isPlaying)
        {
            Cursor.lockState = savedCursor;
            Cursor.visible = savedCursorVisible;
        }
        if (Current == this) Current = null;
    }

    private void OnDestroy() => Release();
}
