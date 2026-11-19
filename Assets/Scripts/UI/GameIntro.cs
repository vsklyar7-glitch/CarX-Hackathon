using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-1000)]
public class GameIntro : MonoBehaviour
{
    [SerializeField] private GameObject player;
    private static bool hasStarted;
    private float previousTimeScale;
    private bool waitingForStart;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetIntro() => hasStarted = false;

    private void Awake()
    {
        if (hasStarted || ApartmentTransition.Current != null || player == null)
        {
            gameObject.SetActive(false);
            return;
        }

        previousTimeScale = Time.timeScale;
        waitingForStart = true;
        Time.timeScale = 0f;
        player.SetActive(false);
    }

    private void Update()
    {
        var keyboard = Keyboard.current;
        if (!waitingForStart || keyboard == null ||
            (!keyboard.enterKey.wasPressedThisFrame && !keyboard.numpadEnterKey.wasPressedThisFrame))
            return;

        hasStarted = true;
        gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        if (!waitingForStart) return;
        waitingForStart = false;
        Time.timeScale = previousTimeScale;
        if (hasStarted && player != null) player.SetActive(true);
    }
}
