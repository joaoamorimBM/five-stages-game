using System.Collections;
using UnityEngine;

public class AccidentFlashback : MonoBehaviour
{
    [Header("Referências")]
    public Transform player;
    public Transform triggerEntrada;    // longe — Noah fala ao entrar
    public Transform triggerFlashback;  // perto do carro — trava e roda cena
    public BlinkTransition blinkTransition;
    public ParticleSystem rain;

    [Header("Família")]
    public GameObject familyGroup;

    [Header("Sons")]
    public AudioSource heartbeatSource;
    public AudioSource anxietySource;

    [Header("Diálogos")]
    public DialogueData dialogoNoahEntrada;
    public DialogueData dialogoFlashback;

    [Header("Configurações")]
    public float triggerDistance = 4f;
    public string nextSceneName = "Scene_Beco";

    private bool entradaTriggered = false;
    private bool flashbackTriggered = false;
    private PlayerMovement playerMovement;

    void Start()
    {
        playerMovement = player.GetComponent<PlayerMovement>();
        if (familyGroup != null)
            familyGroup.SetActive(false);
    }

    void Update()
    {
        // Trigger de entrada — Noah fala longe
        if (!entradaTriggered)
        {
            float dist = Vector3.Distance(player.position, triggerEntrada.position);
            if (dist <= triggerDistance)
            {
                entradaTriggered = true;
                StartCoroutine(DialogoEntrada());
            }
        }

        // Trigger do flashback — perto do carro
        if (!flashbackTriggered)
        {
            float dist = Vector3.Distance(player.position, triggerFlashback.position);
            if (dist <= triggerDistance)
            {
                flashbackTriggered = true;
                StartCoroutine(FlashbackSequence());
            }
        }
    }

    private IEnumerator DialogoEntrada()
    {
        yield return null;
        DialogueManager.Instance.StartDialogue(dialogoNoahEntrada);
    }

    private IEnumerator FlashbackSequence()
    {
        // Espera o diálogo de entrada terminar se ainda estiver rodando
        yield return new WaitUntil(() => !DialogueManager.Instance.isDialogueActive);

        if (familyGroup != null)
            familyGroup.SetActive(true);

        playerMovement.SetMovementLocked(true);

        if (anxietySource != null) anxietySource.Play();
        if (heartbeatSource != null) heartbeatSource.Play();

        yield return null;
        DialogueManager.Instance.StartDialogue(dialogoFlashback);
        yield return null;
        yield return new WaitUntil(() => !DialogueManager.Instance.isDialogueActive);

        StartCoroutine(IntensificarChuva());
        yield return StartCoroutine(blinkTransition.FadeToBlack());

        yield return new WaitForSeconds(1f);
        UnityEngine.SceneManagement.SceneManager.LoadScene(nextSceneName);
    }

    private IEnumerator IntensificarChuva()
    {
        if (rain == null) yield break;

        var emission = rain.emission;
        float tempo = 0f;
        float duracao = 4f;
        float rateInicial = emission.rateOverTime.constant;
        float rateFinal = rateInicial * 4f;

        while (tempo < duracao)
        {
            tempo += Time.deltaTime;
            emission.rateOverTime = Mathf.Lerp(rateInicial, rateFinal, tempo / duracao);
            yield return null;
        }
    }
}