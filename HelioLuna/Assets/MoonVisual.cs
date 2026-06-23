using UnityEngine;

public class MoonVisual : MonoBehaviour
{
    [SerializeField] private Light moonLight;
    [SerializeField] private Transform playerCamera;
    [SerializeField] private float distance = 500f;
    [SerializeField] private Renderer rend;
    [SerializeField] private AnimationCurve visibilityCurve;

    private void LateUpdate()
    {
        if (moonLight == null || playerCamera == null)
            return;

        Vector3 moonDirection = -moonLight.transform.forward;

        transform.position = playerCamera.position + moonDirection * distance;
        transform.LookAt(playerCamera);

        float intensity = moonLight.intensity; // or moonLight

        Color color = rend.material.color;
        color.a = visibilityCurve.Evaluate(intensity);
        rend.material.color = color;
    }
}