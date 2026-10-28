using UnityEngine;

[CreateAssetMenu(fileName = "NovoDialogo", menuName = "Five Stages/Sistema de Dialogo/Node de Conversa")]
public class DialogueData : ScriptableObject
{
    // Categoria de cada opção no relatório (inspirada nos estágios de Kübler-Ross).
    // Serve só pra mostrar tendências de resposta ao psicólogo, não é diagnóstico.
    // ATENÇÃO: o Unity salva o número do enum. Só adicione valores no FINAL.
    public enum GriefStage
    {
        Negacao,
        Raiva,
        Barganha,
        Depressao,
        Aceitacao,
    }

    [System.Serializable]
    public struct DialogueChoice
    {
        [TextArea(2, 3)] public string text;      // o que o personagem fala se o jogador escolher essa opção
        public GriefStage stage;
        [TextArea(2, 3)] public string response;  // opcional: resposta de quem falou a linha
    }

    [System.Serializable]
    public struct DialogueLine
    {
        // Agora o designer escolhe quem fala por uma lista pré-definida!
        public CharacterDatabase.CharacterType speaker;

        [TextArea(3, 5)] public string sentence;

        [Header("Opções (opcional)")]
        [Tooltip("Se tiver opções, elas aparecem depois desta fala. A escolhida é dita por 'choiceSpeaker' e a história continua igual.")]
        public DialogueChoice[] choices;
        public CharacterDatabase.CharacterType choiceSpeaker;
    }

    [Header("Relatório")]
    [Tooltip("Nome amigável que aparece no relatório (ex.: 'Carta da Claire'). Vazio = nome do arquivo.")]
    public string reportLabel;

    [Header("Sequência de Falas do Capítulo")]
    public DialogueLine[] dialogueLines;

    public string ReportName => string.IsNullOrEmpty(reportLabel) ? name.Replace('_', ' ') : reportLabel;
}
