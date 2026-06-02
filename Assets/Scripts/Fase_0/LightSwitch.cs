using UnityEngine;

public class LightSwitch : MonoBehaviour, IInteractable
{
    [Header("Luzes")]
    public Light[] lights;
    public bool startsOn = false;  // ← começa apagada

    [Header("Som")]
    public AudioClip soundSwitch;

    AudioSource audioSource;
    bool isOn;

    void Start()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 1f;
        audioSource.playOnAwake  = false;

        isOn = startsOn;
        UpdateLights();
    }

    public void Interact()
    {
        isOn = !isOn;
        UpdateLights();

        if (audioSource != null && soundSwitch != null)
            audioSource.PlayOneShot(soundSwitch);
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