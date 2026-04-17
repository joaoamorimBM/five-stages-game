using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class DialogueManager : MonoBehaviour
{
    [Header("Referências UI")]
    [SerializeField] private GameObject dialogueBox;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI dialogueText;

    [Header("Diálogo")]
    [SerializeField] private string[] characterNames;
    [SerializeField] private string[] dialogueLines;

    private int currentLine = 0;

    private void Start()
    {
        if (dialogueBox != null)
            dialogueBox.SetActive(true);

        if (characterNames == null || dialogueLines == null)
        {
            Debug.LogError("Os arrays de diálogo não foram configurados.");
            return;
        }

        if (characterNames.Length != dialogueLines.Length)
        {
            Debug.LogError("characterNames e dialogueLines precisam ter o mesmo tamanho.");
            return;
        }

        ShowLine();
    }

    private void Update()
    {
        bool spacePressed = Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
        bool mousePressed = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;

        if (spacePressed || mousePressed)
        {
            NextLine();
        }
    }

    private void ShowLine()
    {
        if (currentLine < dialogueLines.Length)
        {
            nameText.text = characterNames[currentLine];
            dialogueText.text = dialogueLines[currentLine];
        }
    }

    public void NextLine()
    {
        currentLine++;

        if (currentLine < dialogueLines.Length)
        {
            ShowLine();
        }
        else
        {
            EndDialogue();
        }
    }

    private void EndDialogue()
    {
        if (nameText != null) nameText.text = "";
        if (dialogueText != null) dialogueText.text = "";

        if (dialogueBox != null)
            dialogueBox.SetActive(false);
    }
}