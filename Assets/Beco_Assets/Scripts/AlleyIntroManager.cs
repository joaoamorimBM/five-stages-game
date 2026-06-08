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
    public float angerBuildDuration = 6f; // duração do crescimento da raiva

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

        if (playerMovement != null)
            playerMovement.SetMovementLocked(false);
    }

    // Chamado pelo BecoSequenceManager quando o gato foge
    public void StartAngerBuild()
    {
        StartCoroutine(AngerBuildRoutine());
    }

    // Para tudo ao chutar a lixeira
    public void StopAngerEffects()
    {
        StopCoroutine(AngerBuildRoutine());
        if (respiracao != null) respiracao.Stop();
        if (heartbeat != null) heartbeat.Stop();
        if (vignette != null) vignette.intensity.value = 0f;
        if (chromaticAberration != null) chromaticAberration.intensity.value = 0f;
    }

    private IEnumerator AngerBuildRoutine()
    {
        // Garante que começa zerado
        if (vignette != null) vignette.intensity.value = 0f;
        if (chromaticAberration != null) chromaticAberration.intensity.value = 0f;

        // Inicia sons baixos
        if (respiracao != null)
        {
            respiracao.volume = 0.1f;
            respiracao.loop = true;
            respiracao.Play();
        }
        if (heartbeat != null)
        {
            heartbeat.volume = 0.1f;
            heartbeat.loop = true;
            heartbeat.Play();
        }

        float elapsed = 0f;

        while (elapsed < angerBuildDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / angerBuildDuration;

            // Vignette fica vermelha e cresce
            if (vignette != null)
                vignette.intensity.value = Mathf.Lerp(0f, 0.7f, t);
            if (chromaticAberration != null)
                chromaticAberration.intensity.value = Mathf.Lerp(0f, 1f, t);

            // Sons crescem
            if (respiracao != null)
                respiracao.volume = Mathf.Lerp(0.1f, 0.9f, t);
            if (heartbeat != null)
                heartbeat.volume = Mathf.Lerp(0.1f, 1f, t);

            yield return null;
        }
    }
}