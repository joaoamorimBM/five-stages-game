using UnityEngine;

[CreateAssetMenu(fileName = "NovoDialogo", menuName = "Five Stages/Sistema de Dialogo/Node de Conversa")]
public class DialogueData : ScriptableObject
{
    [System.Serializable]
    public struct DialogueLine
    {
        public string speakerName; // "Noah", "Emily", "Atendente", etc.
        [TextArea(3, 5)] public string sentence;
        public Sprite characterPortrait; // Opcional: Se quiser mudar a expressão do personagem na fala
    }

    [Header("Sequência de Falas do Capítulo")]
    public DialogueLine[] dialogueLines;
}