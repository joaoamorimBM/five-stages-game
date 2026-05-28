using UnityEngine;

public class DialogueTrigger : MonoBehaviour
{
    [Header("Arquivo de Diálogo (ScriptableObject)")]
    [SerializeField] private DialogueData dialogueAsset;

    [Header("Configurações do Gatilho")]
    [SerializeField] private bool triggerOnEnter = true;
    [SerializeField] private bool destroyAfterTrigger = true;

    private void OnTriggerEnter(Collider other)
    {
        if (triggerOnEnter && other.CompareTag("Player"))
        {
            TriggerDialogue();
        }
    }

    public void TriggerDialogue()
    {
        if (dialogueAsset == null)
        {
            Debug.LogWarning($"Gatilho em {gameObject.name} está sem um arquivo de diálogo atribuído!");
            return;
        }

        if (DialogueManager.Instance == null)
        {
            Debug.LogError("DialogueManager não foi encontrado na cena!");
            return;
        }

        // Envia o ScriptableObject direto para o Manager atualizado
        DialogueManager.Instance.StartDialogue(dialogueAsset);

        if (destroyAfterTrigger)
        {
            Destroy(gameObject);
        }
    }
}