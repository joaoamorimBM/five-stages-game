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

    [Header("Diálogos")]
    public DialogueData dialogoFamilia;
    public DialogueData dialogoGraceFinal;

    [Header("Configurações")]
    public float blinkDuration = 1.5f;
    public int indexZumbido = 3;

    private Vignette vignette;
    private ChromaticAberration chromaticAberration;
    private bool sequenceStarted = false;
    private bool zumbidoStarted = false;
    private bool anxietyBuildStarted = false;

    void Start()
    {
        if (globalVolume != null && globalVolume.profile.TryGet(out vignette))
            vignette.intensity.value = 0f;

        if (globalVolume != null && globalVolume.profile.TryGet(out chromaticAberration))
            chromaticAberration.intensity.value = 0f;
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
        // Fade para preto
        yield return StartCoroutine(blinkTransition.FadeToBlack());

        // Sino toca na tela preta
        if (sino != null)
            sino.Play();

        yield return new WaitForSeconds(blinkDuration);

        // Família aparece ainda na tela preta
        if (familyGroup != null)
            familyGroup.SetActive(true);

        // Abre a tela
        yield return StartCoroutine(blinkTransition.FadeFromBlack());

        // Pausa antes do diálogo
        yield return new WaitForSeconds(1.5f);

        // Inicia diálogo da família
        yield return null;
        DialogueManager.Instance.StartDialogue(dialogoFamilia);
        yield return null;

        // Monitora índice para disparar efeitos
        StartCoroutine(MonitorDialogue());

        // Espera o diálogo terminar
        yield return new WaitUntil(() => !DialogueManager.Instance.isDialogueActive);

        // Para ansiedade e respiração
        if (respiracao != null) respiracao.Stop();
        if (ansiedade != null) ansiedade.Stop();

        // Tela vai para o preto
        yield return StartCoroutine(blinkTransition.FadeToBlack());

        // Todos os sons tocam na tela preta
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

        // Para corrida após sininho
        if (corrida != null) corrida.Stop();

        // Diálogo da Grace na tela preta
        yield return new WaitForSeconds(0.3f);
        yield return null;
        DialogueManager.Instance.StartDialogue(dialogoGraceFinal);
        yield return null;
        yield return new WaitUntil(() => !DialogueManager.Instance.isDialogueActive);

        // Transição para o acidente
        yield return new WaitForSeconds(2f);
        UnityEngine.SceneManagement.SceneManager.LoadScene("Acidente_Scene");
    }

    private IEnumerator MonitorDialogue()
    {
        while (DialogueManager.Instance.isDialogueActive)
        {
            int index = DialogueManager.Instance.currentLineIndex;

            // Element 3 — primeira fala da criança: inicia zumbido E respiração juntos
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
}