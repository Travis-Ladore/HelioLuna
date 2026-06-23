using UnityEngine;

public class DayNightCycle : MonoBehaviour
{
    [Header("Time")]
    [Range(0f, 24f)]
    public float timeOfDay = 12f;

    [Tooltip("How many real seconds one full in-game day takes.")]
    public float dayLengthInSeconds = 300f;

    [Header("Celestial Bodies")]
    public Light sun;
    public Light moon;

    [Header("Rotation")]
    [Tooltip("Tilts the sun/moon path like Earth's axis. Try 15-35.")]
    public float axialTilt = 23.5f;

    [Tooltip("Rotates the sunrise/sunset direction around the world.")]
    public float compassOffset = 0f;

    [Header("Light Settings")]
    public Gradient sunColor;
    public AnimationCurve sunIntensity;

    public Gradient moonColor;
    public AnimationCurve moonIntensity;

    [Header("Environment")]
    public Gradient ambientColor;
    public Gradient fogColor;

    [Header("Skybox")]
    public Material blendedSkyboxMaterial;
    public AnimationCurve nightBlendCurve;
    public float skyboxExposure = 1f;

    private float skyboxUpdateTimer;

    private void Update()
    {
        AdvanceTime();
        RotateSunAndMoon();
        UpdateLighting();
    }

    private void AdvanceTime()
    {
        timeOfDay += Time.deltaTime * (24f / dayLengthInSeconds);

        if (timeOfDay >= 24f)
            timeOfDay -= 24f;
    }

    private void RotateSunAndMoon()
    {
        float timePercent = timeOfDay / 24f;

        // -90 means sunrise starts near horizon
        float sunAngle = timePercent * 360f - 90f;
        float moonAngle = sunAngle + 180f;

        Quaternion baseRotation = Quaternion.Euler(axialTilt, compassOffset, 0f);

        sun.transform.rotation = baseRotation * Quaternion.Euler(sunAngle, 0f, 0f);
        moon.transform.rotation = baseRotation * Quaternion.Euler(moonAngle, 0f, 0f);
    }

    private void UpdateLighting()
    {
        float timePercent = timeOfDay / 24f;

        if (sun != null)
        {
            sun.color = sunColor.Evaluate(timePercent);
            sun.intensity = sunIntensity.Evaluate(timePercent);
        }

        if (moon != null)
        {
            moon.color = moonColor.Evaluate(timePercent);
            moon.intensity = moonIntensity.Evaluate(timePercent);
        }

        RenderSettings.ambientLight = ambientColor.Evaluate(timePercent);
        RenderSettings.fogColor = fogColor.Evaluate(timePercent);

        if (blendedSkyboxMaterial != null)
        {
            float skyBlend = nightBlendCurve.Evaluate(timePercent);

            blendedSkyboxMaterial.SetFloat("_Blend", skyBlend);
            blendedSkyboxMaterial.SetFloat("_Exposure", skyboxExposure);

            RenderSettings.skybox = blendedSkyboxMaterial;

            skyboxUpdateTimer += Time.deltaTime;

            if (skyboxUpdateTimer >= 2f)
            {
                DynamicGI.UpdateEnvironment();
                skyboxUpdateTimer = 0f;
            }
        }
    }
}