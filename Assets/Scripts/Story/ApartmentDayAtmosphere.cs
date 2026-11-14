using UnityEngine;

// Небольшие фоновые эффекты; улики и управление остаются в существующей системе.
public class ApartmentDayAtmosphere : MonoBehaviour
{
    [Range(1, 3)] public int day = 1;
    public Light[] roomLights;
    public Transform clockMinuteHand;
    public Transform clockHourHand;
    private float[] intensities;

    private void Awake()
    {
        intensities = new float[roomLights.Length];
        for (int i = 0; i < roomLights.Length; i++)
            if (roomLights[i]) intensities[i] = roomLights[i].intensity;
    }

    private void Update()
    {
        float cycle = Mathf.Repeat(Time.time, day == 1 ? 12f : 8f);
        float factor = day == 3 ? 0.85f + Mathf.Sin(Time.time * 0.7f) * 0.15f
            : cycle < 0.25f ? 0.35f : 1f;
        for (int i = 0; i < roomLights.Length; i++)
            if (roomLights[i]) roomLights[i].intensity = intensities[i] * factor;
        if (day == 2)
        {
            if (clockMinuteHand) clockMinuteHand.Rotate(0, 0, 12f * Time.deltaTime, Space.Self);
            if (clockHourHand) clockHourHand.Rotate(0, 0, Time.deltaTime, Space.Self);
        }
    }
}
