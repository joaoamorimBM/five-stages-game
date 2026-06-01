using UnityEngine;

public class ThoughtInteractable : MonoBehaviour, IInteractable
{
    [Header("Diálogo")]
    [SerializeField] private DialogueData dialogueAsset;

    [Header("Configurações")]
    [SerializeField] private string promptMessage = "[E] Observar";
    [SerializeField] private bool oneTimeOnly = true; // só abre uma vez?

    [Header("Referências — arraste no Inspector")]
    [SerializeField] private PlayerMovement playerMovement;

    private bool alreadyUsed = false;

    public void Interact()
    {
        // Se for uso único e já foi usado, ignora
        if (oneTimeOnly && alreadyUsed) return;

        if (dialogueAsset == null)
        {
            Debug.LogWarning("ThoughtInteractable em " + gameObject.name + " sem DialogueData!");
            return;
        }

        if (DialogueManager.Instance == null)
        {
            Debug.LogError("DialogueManager não encontrado!");
            return;
        }

        // Trava o movimento do player
        if (playerMovement != null)
            playerMovement.SetMovementLocked(true);

        // Inicia o diálogo
        DialogueManager.Instance.StartDialogue(dialogueAsset);

        // Marca como usado
        if (oneTimeOnly) alreadyUsed = true;

        // Registra callback para quando o diálogo terminar
        StartCoroutine(WaitForDialogueEnd());
    }

    private System.Collections.IEnumerator WaitForDialogueEnd()
    {
        // Espera o diálogo começar
        yield return null;

        // Espera o diálogo terminar
        while (DialogueManager.Instance != null 
               && DialogueManager.Instance.isDialogueActive)
        {
            yield return null;
        }

        // Libera o movimento quando o diálogo fechar
        if (playerMovement != null)
            playerMovement.SetMovementLocked(false);
    }

    public string GetPromptText()
    {
        if (oneTimeOnly && alreadyUsed) return "";
        return promptMessage;
    }
}