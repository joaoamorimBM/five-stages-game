using System.Collections;
using UnityEngine;

public class AccidentFlashback : MonoBehaviour
{
    [Header("Referências")]
    public Transform player;
    public Transform triggerEntrada;
    public Transform triggerFlashback;
    public BlinkTransition blinkTransition;

    [Header("Família")]
    public GameObject familyGroup;

    [Header("Sons")]
    public AudioSource heartbeatSource;
    public AudioSource anxietySource;
    public AudioSource rainSource;

    [Header("Diálogos")]
    public DialogueData dialogoNoahEntrada;
    public DialogueData dialogoFlashback;

    [Header("Configurações")]
    public float triggerDistanceEntrada = 4f;
    public float triggerDistanceFlashback = 5f;
    public string nextSceneName = "Scene_Beco";

    private bool entradaTriggered = false;
    private bool flashbackTriggered = false;
    private PlayerMovement playerMovement;

    void OnEnable()
    {
        playerMovement = player.GetComponent<PlayerMovement>();

        // Família aparece junto com a cena
        if (familyGroup != null)
            familyGroup.SetActive(true);
    }

    void Update()
    {
        if (!entradaTriggered)
        {
            float dist = Vector3.Distance(player.position, triggerEntrada.position);
            if (dist <= triggerDistanceEntrada)
            {
                entradaTriggered = true;
                StartCoroutine(DialogoEntrada());
            }
        }

        if (!flashbackTriggered)
        {
            float dist = Vector3.Distance(player.position, triggerFlashback.position);
            if (dist <= triggerDistanceFlashback)
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
        yield return new WaitUntil(() => !DialogueManager.Instance.isDialogueActive);

        playerMovement.SetMovementLocked(true);

        if (anxietySource != null) anxietySource.Play();
        if (heartbeatSource != null) heartbeatSource.Play();

        yield return null;
        DialogueManager.Instance.StartDialogue(dialogoFlashback);
        yield return null;
        yield return new WaitUntil(() => !DialogueManager.Instance.isDialogueActive);

        StartCoroutine(IntensificarSomChuva());
        yield return StartCoroutine(blinkTransition.FadeToBlack());

        yield return new WaitForSeconds(1f);
        UnityEngine.SceneManagement.SceneManager.LoadScene(nextSceneName);
    }

    private IEnumerator IntensificarSomChuva()
    {
        if (rainSource == null) yield break;

        float tempo = 0f;
        float duracao = 4f;
        float volumeInicial = rainSource.volume;
        float volumeFinal = 1f;

        while (tempo < duracao)
        {
            tempo += Time.deltaTime;
            rainSource.volume = Mathf.Lerp(volumeInicial, volumeFinal, tempo / duracao);
            yield return null;
        }
    }
}