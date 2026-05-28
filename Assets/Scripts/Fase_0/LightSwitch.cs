using UnityEngine;

public class LightSwitch : MonoBehaviour, IInteractable
{
    public Light[] lights;
    bool isOn = true;

    void Start() => UpdateLights();

    public void Interact()
    {
        isOn = !isOn;
        UpdateLights();
    }

    void UpdateLights()
    {
        foreach (Light l in lights)
            if (l != null) l.enabled = isOn;
    }

    public string GetPromptText()
    {
        return isOn ? "[E] Apagar luz" : "[E] Acender luz";
    }
}