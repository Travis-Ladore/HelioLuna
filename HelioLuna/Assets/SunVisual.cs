using UnityEngine;

public class SunVisual : MonoBehaviour
{
    [SerializeField] private Light sunLight;
    [SerializeField] private Transform playerCamera;
    [SerializeField] private float distance = 500f;

    [SerializeField] private Renderer rend;
    [SerializeField] private AnimationCurve visibilityCurve;

    private void LateUpdate()
    {
        if (sunLight == null || playerCamera == null)
            return;

        // Direction the sun is shining FROM
        Vector3 sunDirection = -sunLight.transform.forward;

        // Position it far away in that direction
        transform.position = playerCamera.position + sunDirection * distance;

        // Always face camera
        transform.LookAt(playerCamera);

        float intensity = sunLight.intensity; // or moonLight

        Color color = rend.material.color;
        color.a = visibilityCurve.Evaluate(intensity);
        rend.material.color = color;
    }
}