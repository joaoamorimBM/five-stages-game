using System.Collections;
using UnityEngine;
using TMPro;

public class NPCInteraction : MonoBehaviour
{
    [Header("Referências")]
    public DialogueData    dialogueData;
    public Transform       player;
    public float           interactionDistance = 2.5f;
    public ChairInteraction chairInteraction;

    [Header("Prompt UI")]
    public GameObject promptUI;        // arraste o InteractPrompt aqui
    public TMP_Text   promptText;      // arraste o TMP_Text aqui
    public string     promptMessage = "[E] Conversar";

    [Header("Player")]
    public PlayerMovement playerMovement; // arraste o PlayerMovement aqui

    private bool playerInRange   = false;
    private bool alreadyTalked   = false;

    void Update()
    {
        if (alreadyTalked)
        {
            HidePrompt();
            return;
        }

        float dist = Vector3.Distance(transform.position, player.position);
        playerInRange = dist <= interactionDistance;

        // Mostra ou esconde o prompt
        if (playerInRange)
        {
            ShowPrompt();

            if (Input.GetKeyDown(KeyCode.E))
            {
                if (!DialogueManager.Instance.isDialogueActive)
                {
                    alreadyTalked = true;
                    HidePrompt();

                    // Trava o movimento
                    if (playerMovement != null)
                        playerMovement.SetMovementLocked(true);

                    DialogueManager.Instance.StartDialogue(dialogueData);
                    StartCoroutine(ActivateChairAfterDialogue());
                }
            }
        }
        else
        {
            HidePrompt();
        }
    }

    void ShowPrompt()
    {
        if (promptUI != null)
        {
            promptUI.SetActive(true);
            if (promptText != null)
                promptText.text = promptMessage;
        }
    }

    void HidePrompt()
    {
        if (promptUI != null)
            promptUI.SetActive(false);
    }

    private IEnumerator ActivateChairAfterDialogue()
    {
        // Espera o diálogo terminar
        yield return null;
        while (DialogueManager.Instance != null 
               && DialogueManager.Instance.isDialogueActive)
        {
            yield return null;
        }

        // Libera o movimento
        if (playerMovement != null)
            playerMovement.SetMovementLocked(false);

        // Ativa a cadeira
        if (chairInteraction != null)
            chairInteraction.Activate();
    }
}