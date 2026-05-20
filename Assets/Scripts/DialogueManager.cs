using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance;

    [Header("Referências UI")]
    [SerializeField] private GameObject dialogueBox; 
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private Image portraitLeft;   
    [SerializeField] private Image portraitRight;  
    [SerializeField] private GameObject nextTextIndicator; // Arraste seu "Indicador_Avancar" aqui

    [Header("Configurações")]
    [SerializeField] private float typingSpeed = 0.04f; 
    [SerializeField] private float blinkSpeed = 0.5f; // Velocidade do pisca-pisca

    private string[] currentCharacterNames;
    private string[] currentDialogueLines;
    private int currentLine = 0;
    private bool isTyping = false; 
    private Coroutine blinkCoroutine; // Guarda a animação de piscar

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (dialogueBox != null)
            dialogueBox.SetActive(false);

        if (nextTextIndicator != null)
            nextTextIndicator.SetActive(false);
    }

    private void Update()
    {
        if (!dialogueBox.activeSelf) return;

        bool spacePressed = Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
        bool mousePressed = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;

        if (spacePressed || mousePressed)
        {
            if (isTyping)
            {
                // Botão de Skip: Mostra a frase inteira imediatamente
                StopAllCoroutines();
                dialogueText.text = currentDialogueLines[currentLine];
                isTyping = false;
                
                // Ativa o indicador piscante, já que o texto terminou
                StartBlinking();
            }
            else
            {
                NextLine();
            }
        }
    }

    public void StartDialogue(string[] names, string[] lines)
    {
        currentCharacterNames = names;
        currentDialogueLines = lines;
        currentLine = 0;

        dialogueBox.SetActive(true);
        ShowLine();
    }

    private void ShowLine()
    {
        if (currentLine < currentDialogueLines.Length)
        {
            nameText.text = currentCharacterNames[currentLine];
            
            // Garante que o indicador suma enquanto uma nova frase está sendo digitada
            StopBlinking();
            
            StartCoroutine(TypeSentence(currentDialogueLines[currentLine]));
        }
    }

    private IEnumerator TypeSentence(string sentence)
    {
        isTyping = true;
        dialogueText.text = ""; 

        foreach (char letter in sentence.ToCharArray())
        {
            dialogueText.text += letter;
            yield return new WaitForSeconds(typingSpeed);
        }

        isTyping = false;
        
        // Texto terminou de aparecer naturalmente: começa a piscar o indicador
        StartBlinking();
    }

    // Liga o indicador e inicia o loop do pisca-pisca
    private void StartBlinking()
    {
        if (nextTextIndicator != null)
        {
            nextTextIndicator.SetActive(true);
            blinkCoroutine = StartCoroutine(BlinkIndicator());
        }
    }

    // Desliga o indicador e para a animação para não dar conflito
    private void StopBlinking()
    {
        if (blinkCoroutine != null)
        {
            StopCoroutine(blinkCoroutine);
            blinkCoroutine = null;
        }

        if (nextTextIndicator != null)
        {
            nextTextIndicator.SetActive(false);
        }
    }

    // Loop que liga e desliga o objeto baseado no blinkSpeed
    private IEnumerator BlinkIndicator()
    {
        while (true)
        {
            nextTextIndicator.SetActive(!nextTextIndicator.activeSelf);
            yield return new WaitForSeconds(blinkSpeed);
        }
    }

    public void NextLine()
    {
        currentLine++;

        if (currentLine < currentDialogueLines.Length)
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
        StopBlinking();
        if (dialogueBox != null)
            dialogueBox.SetActive(false);
    }
}