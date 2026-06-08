using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class BakeryAnxiety : MonoBehaviour
{
    [Header("Referências")]
    public Transform player;
    public BlinkTransition blinkTransition;
    public Volume globalVolume;
    public GameObject familyGroup;

    [Header("Sons")]
    public AudioSource sino;
    public AudioSource respiracao;
    public AudioSource ansiedade;
    public AudioSource corrida;
    public AudioSource cadeiraArrastando;
    public AudioSource chuvaAmbiente;
    public AudioSource trilha;

    [Header("Diálogos")]
    public DialogueData dialogoFamilia;
    public DialogueData dialogoGraceFinal;

    [Header("Configurações")]
    public float blinkDuration = 1.5f;
    public int indexZumbido = 3;
    public float volumeTrilhaNormal = 1f;
    public float volumeTrilhaDialogo = 0.3f;

    private Vignette vignette;
    private ChromaticAberration chromaticAberration;
    private bool sequenceStarted = false;
    private bool zumbidoStarted = false;
    private bool anxietyBuildStarted = false;
    private bool sequenceEnding = false; // flag para bloquear o OnDialogueEnded no final

    void Start()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.accidentVisit = 1;

        if (globalVolume != null && globalVolume.profile.TryGet(out vignette))
            vignette.intensity.value = 0f;

        if (globalVolume != null && globalVolume.profile.TryGet(out chromaticAberration))
            chromaticAberration.intensity.value = 0f;

        if (chuvaAmbiente != null)
        {
            chuvaAmbiente.loop = true;
            chuvaAmbiente.volume = 0.3f;
            chuvaAmbiente.Play();
        }

        DialogueManager.OnDialogueEnded += OnDialogueEnded;
    }

    void OnDestroy()
    {
        DialogueManager.OnDialogueEnded -= OnDialogueEnded;
    }

    private void OnDialogueEnded()
    {
        // Não volta a trilha se a sequência final já começou
        if (sequenceEnding) return;

        if (trilha != null)
            StartCoroutine(FadeTrilha(trilha.volume, volumeTrilhaNormal, 1f));
    }

    public void StartSequence()
    {
        if (!sequenceStarted)
        {
            sequenceStarted = true;
            StartCoroutine(BakerySequence());
        }
    }

    private IEnumerator BakerySequence()
    {
        yield return StartCoroutine(FadeTrilha(volumeTrilhaNormal, volumeTrilhaDialogo, 0.5f));

        yield return StartCoroutine(blinkTransition.FadeToBlack());

        if (sino != null)
            sino.Play();

        yield return new WaitForSeconds(blinkDuration);

        if (familyGroup != null)
            familyGroup.SetActive(true);

        yield return StartCoroutine(blinkTransition.FadeFromBlack());

        yield return new WaitForSeconds(1.5f);

        yield return null;
        DialogueManager.Instance.StartDialogue(dialogoFamilia);
        yield return null;

        StartCoroutine(MonitorDialogue());

        yield return new WaitUntil(() => !DialogueManager.Instance.isDialogueActive);

        if (respiracao != null) respiracao.Stop();
        if (ansiedade != null) ansiedade.Stop();

        // Marca que a sequência de saída começou — bloqueia o OnDialogueEnded
        sequenceEnding = true;

        if (trilha != null) StartCoroutine(FadeTrilha(trilha.volume, 0f, 1f));
        if (chuvaAmbiente != null) StartCoroutine(FadeAudio(chuvaAmbiente, chuvaAmbiente.volume, 0f, 1f));

        yield return StartCoroutine(blinkTransition.FadeToBlack());

        if (cadeiraArrastando != null) cadeiraArrastando.Play();
        yield return new WaitForSeconds(0.8f);

        if (corrida != null)
        {
            corrida.loop = false;
            corrida.Play();
        }
        yield return new WaitForSeconds(1.5f);

        if (sino != null) sino.Play();
        yield return new WaitForSeconds(1f);

        if (corrida != null) corrida.Stop();

        yield return new WaitForSeconds(0.3f);
        yield return StartCoroutine(DialogueManager.Instance.PlayDialogueAuto(dialogoGraceFinal, 3f));

        if (GameManager.Instance != null)
            GameManager.Instance.accidentVisit = 1;

        yield return new WaitForSeconds(2f);
        UnityEngine.SceneManagement.SceneManager.LoadScene("Acidente_Scene");
    }

    private IEnumerator MonitorDialogue()
    {
        while (DialogueManager.Instance.isDialogueActive)
        {
            int index = DialogueManager.Instance.currentLineIndex;

            if (index >= indexZumbido && !zumbidoStarted)
            {
                zumbidoStarted = true;

                if (ansiedade != null)
                {
                    ansiedade.volume = 0.15f;
                    ansiedade.loop = true;
                    ansiedade.Play();
                }

                if (respiracao != null)
                {
                    respiracao.volume = 0.2f;
                    respiracao.loop = true;
                    respiracao.Play();
                }

                if (!anxietyBuildStarted)
                {
                    anxietyBuildStarted = true;
                    StartCoroutine(AnxietyBuild());
                }
            }

            yield return null;
        }
    }

    private IEnumerator AnxietyBuild()
    {
        float tempo = 0f;
        float duracao = 15f;

        while (DialogueManager.Instance.isDialogueActive)
        {
            tempo += Time.deltaTime;
            float t = Mathf.Clamp01(tempo / duracao);

            if (vignette != null)
                vignette.intensity.value = Mathf.Lerp(0f, 0.7f, t);

            if (chromaticAberration != null)
                chromaticAberration.intensity.value = Mathf.Lerp(0f, 1f, t);

            if (respiracao != null)
                respiracao.volume = Mathf.Lerp(0.2f, 0.9f, t);
            if (ansiedade != null)
                ansiedade.volume = Mathf.Lerp(0.15f, 1f, t);

            yield return null;
        }
    }

    private IEnumerator FadeTrilha(float from, float to, float duration)
    {
        if (trilha == null) yield break;
        yield return StartCoroutine(FadeAudio(trilha, from, to, duration));
    }

    private IEnumerator FadeAudio(AudioSource source, float from, float to, float duration)
    {
        if (source == null) yield break;

        float elapsed = 0f;
        source.volume = from;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            source.volume = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }

        source.volume = to;
    }
}