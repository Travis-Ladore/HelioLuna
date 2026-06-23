using UnityEngine;

public class PoweredLamp : MonoBehaviour
{
    [Header("Power Source")]
    [SerializeField] private SolarPanel solarPanel;

    [Header("Power Usage")]
    [SerializeField] private float energyPerSecond = 5f;

    [Header("Lamp")]
    [SerializeField] private Light lampLight;

    private void Update()
    {
        if (solarPanel == null || lampLight == null)
            return;

        float energyNeeded = energyPerSecond * Time.deltaTime;

        if (solarPanel.TryUseEnergy(energyNeeded))
        {
            lampLight.enabled = true;
        }
        else
        {
            lampLight.enabled = false;
        }
    }
}