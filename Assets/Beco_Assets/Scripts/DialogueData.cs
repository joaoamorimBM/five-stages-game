using UnityEngine;

[CreateAssetMenu(fileName = "NovoDialogo", menuName = "Five Stages/Sistema de Dialogo/Node de Conversa")]
public class DialogueData : ScriptableObject
{
    [System.Serializable]
    public struct DialogueLine
    {
        // Agora o designer escolhe quem fala por uma lista pré-definida!
        public CharacterDatabase.CharacterType speaker;
        
        [TextArea(3, 5)] public string sentence;
    }

    [Header("Sequência de Falas do Capítulo")]
    public DialogueLine[] dialogueLines;
}