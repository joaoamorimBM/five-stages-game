using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance;

    [Header("Referências de UI - Painel")]
    [SerializeField] private GameObject dialogueBox; 
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private GameObject nextTextIndicator;

    [Header("Referências de UI - Retratos (Portraits)")]
    [SerializeField] private Image portraitLeft;   
    [SerializeField] private Image portraitRight;  

    [Header("Referências de UI - Caixas de Nome")]
    [SerializeField] private GameObject groupNameLeft;
    [SerializeField] private TextMeshProUGUI textNameLeft;
    [SerializeField] private GameObject groupNameRight;
    [SerializeField] private TextMeshProUGUI textNameRight;

    [Header("Configurações de Texto")]
    [SerializeField] private float typingSpeed = 0.03f; 
    [SerializeField] private float blinkSpeed = 0.5f;

    // Armazenamento dos dados ativos do diálogo
    private DialogueData currentDialogueData;
    private int currentLine = 0;
    private bool isTyping = false; 
    private Coroutine blinkCoroutine;

    void Awake()
    {
        // Sistema de instância única (Singleton) para o DontDestroyOnLoad funcionar entre as cenas
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
        // Garante que a UI comece limpa e escondida
        if (dialogueBox != null) dialogueBox.SetActive(false);
        if (nextTextIndicator != null) nextTextIndicator.SetActive(false);
        
        // Esconde os retratos inicialmente
        if (portraitLeft != null) portraitLeft.gameObject.SetActive(false);
        if (portraitRight != null) portraitRight.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (!dialogueBox.activeSelf) return;

        // Suporte para Barra de Espaço (New Input System) e Clique Esquerdo do Mouse
        bool spacePressed = Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
        bool mousePressed = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;

        if (spacePressed || mousePressed)
        {
            if (isTyping)
            {
                // Se o jogador clicar enquanto o texto está digitando, pula direto para o final da frase
                StopAllCoroutines();
                dialogueText.text = currentDialogueData.dialogueLines[currentLine].sentence;
                isTyping = false;
                StartBlinking();
            }
            else 
            { 
                NextLine(); 
            }
        }
    }

    // O gatilho agora chama esta função passando o ScriptableObject diretamente
    public void StartDialogue(DialogueData data)
    {
        currentDialogueData = data;
        currentLine = 0;
        dialogueBox.SetActive(true);
        ShowLine();
    }

    private void ShowLine()
    {
        if (currentDialogueData == null || currentLine >= currentDialogueData.dialogueLines.Length)
        {
            EndDialogue();
            return;
        }

        // Resgata os dados da linha atual do ScriptableObject
        DialogueData.DialogueLine lineInfo = currentDialogueData.dialogueLines[currentLine];
        string speaker = lineInfo.speakerName;

        // 1. LÓGICA DE POSIÇÃO DO NOME E DO RETRATO
        if (speaker.ToLower() == "noah")
        {
            // Ativa o lado esquerdo (Protagonista)
            groupNameLeft.SetActive(true);
            groupNameRight.SetActive(false);
            textNameLeft.text = speaker;

            // Atualiza o Retrato da Esquerda
            UpdatePortrait(portraitLeft, lineInfo.characterPortrait);
        }
        else
        {
            // Ativa o lado direito (Emily / Outros personagens)
            groupNameLeft.SetActive(false);
            groupNameRight.SetActive(true);
            textNameRight.text = speaker;

            // Atualiza o Retrato da Direita
            UpdatePortrait(portraitRight, lineInfo.characterPortrait);
        }
        
        // 2. DIGITAÇÃO DO TEXTO
        StopBlinking();
        StartCoroutine(TypeSentence(lineInfo.sentence));
    }

    private void UpdatePortrait(Image portraitImage, Sprite characterSprite)
    {
        if (portraitImage == null) return;

        if (characterSprite != null)
        {
            portraitImage.gameObject.SetActive(true);
            portraitImage.sprite = characterSprite;
        }
        else
        {
            // Se não colocarmos nenhuma foto no arquivo, o retrato daquele lado some
            portraitImage.gameObject.SetActive(false);
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
        StartBlinking();
    }

    // Gerenciamento do Indicador de Avanço Piscante
    private void StartBlinking()
    {
        if (nextTextIndicator != null)
        {
            nextTextIndicator.SetActive(true);
            blinkCoroutine = StartCoroutine(BlinkIndicator());
        }
    }

    private void StopBlinking()
    {
        if (blinkCoroutine != null) StopCoroutine(blinkCoroutine);
        if (nextTextIndicator != null) nextTextIndicator.SetActive(false);
    }

    private IEnumerator BlinkIndicator()
    {
        while (true)
        {
            CanvasGroup group = nextTextIndicator.GetComponent<CanvasGroup>();
            if (group != null)
            {
                group.alpha = group.alpha == 1f ? 0f : 1f;
            }
            else
            {
                nextTextIndicator.SetActive(!nextTextIndicator.activeSelf);
            }
            yield return new WaitForSeconds(blinkSpeed);
        }
    }

    public void NextLine()
    {
        currentLine++;
        if (currentLine < currentDialogueData.dialogueLines.Length) 
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
        if (dialogueBox != null) dialogueBox.SetActive(false);
        if (portraitLeft != null) portraitLeft.gameObject.SetActive(false);
        if (portraitRight != null) portraitRight.gameObject.SetActive(false);
    }
}