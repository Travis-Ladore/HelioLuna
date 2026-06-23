using UnityEngine;

public class SolarPanel : MonoBehaviour
{
    [Header("Sun Reference")]
    [SerializeField] private Light sun;

    [Header("Energy")]
    [SerializeField] private float maxEnergy = 100f;
    [SerializeField] private float currentEnergy = 0f;
    [SerializeField] private float generationRate = 10f;

    [Header("Panel Direction")]
    [Tooltip("Usually the panel's local up direction should face the sun.")]
    [SerializeField] private Transform panelSurface;

    public float CurrentEnergy => currentEnergy;
    public float MaxEnergy => maxEnergy;

    private void Update()
    {
        GenerateEnergy();
    }

    private void GenerateEnergy()
    {
        if (sun == null || panelSurface == null)
            return;

        // If the sun is off, generate nothing
        if (sun.intensity <= 0.01f)
            return;

        Vector3 directionToSun = -sun.transform.forward;

        float facingAmount = Vector3.Dot(panelSurface.up, directionToSun);

        // Only generate if panel is facing toward the sun
        facingAmount = Mathf.Clamp01(facingAmount);

        float generatedEnergy = generationRate * facingAmount * sun.intensity * Time.deltaTime;

        currentEnergy += generatedEnergy;
        currentEnergy = Mathf.Clamp(currentEnergy, 0f, maxEnergy);
    }

    public bool TryUseEnergy(float amount)
    {
        if (currentEnergy < amount)
            return false;

        currentEnergy -= amount;
        return true;
    }

    public float GetEnergyPercent()
    {
        return currentEnergy / maxEnergy;
    }
}