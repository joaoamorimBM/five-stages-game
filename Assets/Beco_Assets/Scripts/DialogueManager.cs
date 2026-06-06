using System.Collections;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance;
    public static event Action OnDialogueEnded;

    [Header("Configuração Global")]
    [SerializeField] private CharacterDatabase characterDatabase;

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
    private float originalPortraitRightY;
    private int currentLine = 0;
    private bool isTyping = false; 
    private Coroutine blinkCoroutine;
    private bool _isDialogueActive = false;
    public bool isDialogueActive => _isDialogueActive;

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
        if (portraitRight != null) originalPortraitRightY = portraitRight.rectTransform.anchoredPosition.y;
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
        _isDialogueActive = true;
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

    // Pega a linha atual
    DialogueData.DialogueLine lineInfo = currentDialogueData.dialogueLines[currentLine];
    
    // Busca o perfil completo do personagem no Banco de Dados automaticamente
    CharacterDatabase.CharacterProfile profile = characterDatabase.GetProfile(lineInfo.speaker);
    string speakerName = profile.displayName;
    Sprite speakerPortrait = profile.defaultPortrait;

    // CONFIGURAÇÃO DE CORES PARA FOCO/DESFOCO
    Color corFoco = Color.white; 
    Color corSemFoco = new Color(0.35f, 0.35f, 0.35f, 1f); // Deixa o personagem cinza escuro

    // LÓGICA DE POSIÇÃO, NOME E FOCO DO PERSONAGEM
    if (lineInfo.speaker == CharacterDatabase.CharacterType.Noah)
    {
        // Ativa e destaca o lado esquerdo (Protagonista)
        groupNameLeft.SetActive(true);
        groupNameRight.SetActive(false);
        textNameLeft.text = speakerName;

        // Atualiza o Retrato da Esquerda e dá FOCO
        UpdatePortrait(portraitLeft, speakerPortrait);
        if (portraitLeft != null) portraitLeft.color = corFoco;

        // Tira o foco do Retrato da Direita (se houver alguma imagem lá)
        if (portraitRight != null && portraitRight.sprite != null) 
            portraitRight.color = corSemFoco;
    }
    else
    {
        // Ativa e destaca o lado direito ( Claire / Emily / Atendente )
        groupNameLeft.SetActive(false);
        groupNameRight.SetActive(true);
        textNameRight.text = speakerName;

        // Atualiza o Retrato da Direita e dá FOCO
        UpdatePortrait(portraitRight, speakerPortrait);
        if (portraitRight != null) portraitRight.color = corFoco;

        // Tira o foco do Retrato da Esquerda
        if (portraitLeft != null && portraitLeft.sprite != null) 
            portraitLeft.color = corSemFoco;

        // --- LÓGICA CORRIGIDA DE POSICIONAMENTO ---
        if (portraitRight != null)
        {
            portraitRight.rectTransform.localScale = new Vector3(1f, 1f, 1f);

            // Se for a Claire, nós pegamos a altura original do Inspector e subtraímos um valor
            if (lineInfo.speaker.ToString() == "Claire")
            {
                // Comece testando com um recuo pequeno (ex: -50f ou -80f) em relação ao original
                float novaAltura = originalPortraitRightY - 35f; 
                portraitRight.rectTransform.anchoredPosition = new Vector2(portraitRight.rectTransform.anchoredPosition.x, novaAltura);
            }
            else
            {
                // Para a Emily e outros, devolve EXATAMENTE a altura que estava no Inspector antes do bug
                portraitRight.rectTransform.anchoredPosition = new Vector2(portraitRight.rectTransform.anchoredPosition.x, originalPortraitRightY);
            }
        }
    }
    
    // Digitação do Texto
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
         _isDialogueActive = false;
        StopBlinking();
        if (dialogueBox != null) dialogueBox.SetActive(false);
        if (portraitLeft != null) portraitLeft.gameObject.SetActive(false);
        if (portraitRight != null) portraitRight.gameObject.SetActive(false);

        OnDialogueEnded?.Invoke();
    }
}