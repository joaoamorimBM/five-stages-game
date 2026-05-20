using UnityEngine;

public class DialogueTrigger : MonoBehaviour
{
    [System.Serializable]
    public struct DialogueNode
    {
        public string speakerName;
        [TextArea(3, 5)] public string sentence;
    }

    [Header("Configuração do Diálogo")]
    [SerializeField] private DialogueNode[] dialogueSequence;

    [Header("Configuração do Gatilho")]
    [SerializeField] private bool triggerOnEnter = true; // Se verdadeiro, dispara ao pisar. Se falso, precisa de clique/interação.
    [SerializeField] private bool destroyAfterTrigger = true; // Garante que o diálogo aconteça apenas uma vez

    private void OnTriggerEnter(Collider other)
    {
        // Verifica se quem pisou no gatilho foi o Jogador (Daniel)
        if (triggerOnEnter && other.CompareTag("Player"))
        {
            TriggerDialogue();
        }
    }

    public void TriggerDialogue()
    {
        if (DialogueManager.Instance == null)
        {
            Debug.LogError("DialogueManager não foi encontrado na cena!");
            return;
        }

        // Separa os dados da struct em dois arrays separados que o DialogueManager espera receber
        string[] names = new string[dialogueSequence.Length];
        string[] lines = new string[dialogueSequence.Length];

        for (int i = 0; i < dialogueSequence.Length; i++)
        {
            names[i] = dialogueSequence[i].speakerName;
            lines[i] = dialogueSequence[i].sentence;
        }

        // Envia os dados para a Central de Diálogos e inicia a cena
        DialogueManager.Instance.StartDialogue(names, lines);

        // Se configurado para acontecer só uma vez, destrói este gatilho
        if (destroyAfterTrigger)
        {
            Destroy(gameObject);
        }
    }
}