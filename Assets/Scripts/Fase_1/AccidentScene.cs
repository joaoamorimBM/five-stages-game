using System.Collections;
using UnityEngine;

public class AccidentScene : MonoBehaviour
{
    [Header("Referências")]
    public Transform player;
    public Transform startPosition;
    public Transform carTrigger;
    public BlinkTransition blinkTransition;

    [Header("Diálogos")]
    public DialogueData dialogoNoah1;
    public DialogueData dialogoNoah2;
    public DialogueData dialogoNoah3;
    public DialogueData dialogoEmily;

    [Header("Configurações")]
    public float triggerDistance = 3f;

    private bool eventTriggered = false;
    private int blinkCount = 0;

    void Start()
    {
        StartCoroutine(DialogoEntrada());
    }

    private IEnumerator DialogoEntrada()
    {
        yield return new WaitForSeconds(1f);

        if (DialogueManager.Instance == null)
        {
            Debug.LogError("DialogueManager não encontrado!");
            yield break;
        }

        DialogueManager.Instance.StartDialogue(dialogoNoah1);
    }

    void Update()
    {
        if (eventTriggered) return;

        float dist = Vector3.Distance(player.position, carTrigger.position);

        if (dist <= triggerDistance)
        {
            eventTriggered = true;

            if (blinkCount == 0)
                StartCoroutine(Blink1());
            else if (blinkCount == 1)
                StartCoroutine(Blink2());
            else if (blinkCount == 2)
                StartCoroutine(Blink3());
        }
    }

    private IEnumerator Blink1()
    {
        player.GetComponent<PlayerMovement>().SetMovementLocked(true);

        yield return StartCoroutine(blinkTransition.FadeToBlack());

        player.position = startPosition.position;
        player.GetComponent<PlayerMovement>().ForceRotation(
            startPosition.eulerAngles.y, 0f
        );

        yield return new WaitForSeconds(0.1f);
        yield return StartCoroutine(blinkTransition.FadeFromBlack());

        player.GetComponent<PlayerMovement>().SetMovementLocked(false);

        yield return null;
        DialogueManager.Instance.StartDialogue(dialogoNoah2);
        yield return null;
        yield return new WaitUntil(() => !DialogueManager.Instance.isDialogueActive);

        blinkCount = 1;
        eventTriggered = false;
    }

    private IEnumerator Blink2()
    {
        player.GetComponent<PlayerMovement>().SetMovementLocked(true);

        yield return StartCoroutine(blinkTransition.FadeToBlack());

        player.position = startPosition.position;
        player.GetComponent<PlayerMovement>().ForceRotation(
            startPosition.eulerAngles.y, 0f
        );

        yield return new WaitForSeconds(0.1f);
        yield return StartCoroutine(blinkTransition.FadeFromBlack());

        player.GetComponent<PlayerMovement>().SetMovementLocked(false);

        yield return null;
        DialogueManager.Instance.StartDialogue(dialogoNoah3);
        yield return null;
        yield return new WaitUntil(() => !DialogueManager.Instance.isDialogueActive);

        blinkCount = 2;
        eventTriggered = false;
    }

    private IEnumerator Blink3()
{
    player.GetComponent<PlayerMovement>().SetMovementLocked(true);

    yield return StartCoroutine(blinkTransition.FadeToBlack());

    yield return new WaitForSeconds(0.8f);

    Debug.Log("Iniciando dialogo Emily...");
    yield return StartCoroutine(DialogueManager.Instance.PlayDialogueAuto(dialogoEmily, 3f));
    Debug.Log("Dialogo Emily terminou!");

    if (GameManager.Instance != null)
        GameManager.Instance.accidentVisit = 2;

    yield return new WaitForSeconds(1f);
    Debug.Log("Carregando Scene_Alley...");
    UnityEngine.SceneManagement.SceneManager.LoadScene("Scene_Alley");
}
}