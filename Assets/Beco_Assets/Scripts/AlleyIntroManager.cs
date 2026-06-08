using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class AlleyIntroManager : MonoBehaviour
{
    [Header("Referências")]
    public PlayerMovement playerMovement;
    public BlinkTransition blinkTransition;
    public Volume globalVolume;

    [Header("Sons da Crise")]
    public AudioSource respiracao;
    public AudioSource heartbeat;

    [Header("Configurações")]
    public float recoveryDuration = 4f;

    private Vignette vignette;
    private ChromaticAberration chromaticAberration;

    void Start()
    {
        if (playerMovement != null)
            playerMovement.SetMovementLocked(true);

        if (globalVolume != null)
        {
            globalVolume.profile.TryGet(out vignette);
            globalVolume.profile.TryGet(out chromaticAberration);

            if (vignette != null) vignette.intensity.value = 0.7f;
            if (chromaticAberration != null) chromaticAberration.intensity.value = 1f;
        }

        if (respiracao != null)
        {
            respiracao.volume = 0.9f;
            respiracao.loop = true;
            respiracao.Play();
        }
        if (heartbeat != null)
        {
            heartbeat.volume = 1f;
            heartbeat.loop = true;
            heartbeat.Play();
        }

        StartCoroutine(RecoverFromCrisis());
    }

    private IEnumerator RecoverFromCrisis()
    {
        float elapsed = 0f;

        while (elapsed < recoveryDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / recoveryDuration;

            if (vignette != null)
                vignette.intensity.value = Mathf.Lerp(0.7f, 0f, t);
            if (chromaticAberration != null)
                chromaticAberration.intensity.value = Mathf.Lerp(1f, 0f, t);

            if (respiracao != null)
                respiracao.volume = Mathf.Lerp(0.9f, 0f, t);
            if (heartbeat != null)
                heartbeat.volume = Mathf.Lerp(1f, 0f, t);

            yield return null;
        }

        if (respiracao != null) respiracao.Stop();
        if (heartbeat != null) heartbeat.Stop();

        if (vignette != null) vignette.intensity.value = 0f;
        if (chromaticAberration != null) chromaticAberration.intensity.value = 0f;

        // Libera o player para andar
        if (playerMovement != null)
            playerMovement.SetMovementLocked(false);
    }
}