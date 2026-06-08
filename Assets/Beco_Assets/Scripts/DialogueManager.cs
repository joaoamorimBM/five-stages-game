using System.Collections;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

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

    private DialogueData currentDialogueData;
    private float originalPortraitRightY;
    private int currentLine = 0;
    private bool isTyping = false; 
    private Coroutine blinkCoroutine;
    private bool _isDialogueActive = false;
    public bool isDialogueActive => _isDialogueActive;
    public int currentLineIndex => currentLine;

    // Controla qual personagem está em qual lado
    private CharacterDatabase.CharacterType leftSpeaker;
    private CharacterDatabase.CharacterType rightSpeaker;
    private bool leftSpeakerSet = false;
    private bool rightSpeakerSet = false;

    void Awake()
    {
        if (Instance == null) 
        { 
            Instance = this; 
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else 
        { 
            Destroy(gameObject); 
        }
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
{
    leftSpeakerSet = false;
    rightSpeakerSet = false;
    _isDialogueActive = false;

    if (portraitLeft != null)
    {
        portraitLeft.sprite = null;  // limpa o sprite
        portraitLeft.gameObject.SetActive(false);
    }
    if (portraitRight != null)
    {
        portraitRight.sprite = null;  // limpa o sprite
        portraitRight.gameObject.SetActive(false);
    }
    if (dialogueBox != null) dialogueBox.SetActive(false);
}

    private void Start()
    {
        if (dialogueBox != null) dialogueBox.SetActive(false);
        if (nextTextIndicator != null) nextTextIndicator.SetActive(false);
        if (portraitLeft != null) portraitLeft.gameObject.SetActive(false);
        if (portraitRight != null) portraitRight.gameObject.SetActive(false);
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

    public void StartDialogue(DialogueData data)
    {
        if (portraitRight != null) 
            originalPortraitRightY = portraitRight.rectTransform.anchoredPosition.y;

        currentDialogueData = data;
        _isDialogueActive = true;
        currentLine = 0;

        // Reseta o mapeamento de lados para cada novo diálogo
        leftSpeakerSet = false;
        rightSpeakerSet = false;

        dialogueBox.SetActive(true);
        ShowLine();
    }

    public IEnumerator PlayDialogueAuto(DialogueData data, float delayBetweenLines)
    {
        StartDialogue(data);
        yield return null;

        while (_isDialogueActive)
        {
            yield return new WaitUntil(() => !isTyping);
            yield return new WaitForSeconds(delayBetweenLines);
            if (_isDialogueActive)
                NextLine();
        }
    }

    private void ShowLine()
    {
        if (currentDialogueData == null || currentLine >= currentDialogueData.dialogueLines.Length)
        {
            EndDialogue();
            return;
        }

        DialogueData.DialogueLine lineInfo = currentDialogueData.dialogueLines[currentLine];
        CharacterDatabase.CharacterProfile profile = characterDatabase.GetProfile(lineInfo.speaker);
        string speakerName = profile.displayName;
        Sprite speakerPortrait = profile.defaultPortrait;

        Color corFoco = Color.white; 
        Color corSemFoco = new Color(0.35f, 0.35f, 0.35f, 1f); 

        // Caso especial: dois personagens falando juntos
        if (lineInfo.speaker == CharacterDatabase.CharacterType.Ambos)
        {
            groupNameLeft.SetActive(true);
            groupNameRight.SetActive(true);
            textNameLeft.text = "Noah";
            textNameRight.text = "Emily";
            if (portraitLeft != null && portraitLeft.gameObject.activeSelf) 
                portraitLeft.color = corFoco;
            if (portraitRight != null && portraitRight.gameObject.activeSelf) 
                portraitRight.color = corFoco;
            if (portraitRight != null)
                portraitRight.rectTransform.anchoredPosition = new Vector2(
                    portraitRight.rectTransform.anchoredPosition.x, originalPortraitRightY);

            StopBlinking();
            StartCoroutine(TypeSentence(lineInfo.sentence));
            return;
        }

        // Determina o lado do speaker dinamicamente
        bool falaEsquerda = false;

        if (!leftSpeakerSet)
        {
            // Primeiro speaker sempre vai para a esquerda
            leftSpeaker = lineInfo.speaker;
            leftSpeakerSet = true;
            falaEsquerda = true;
        }
        else if (lineInfo.speaker == leftSpeaker)
        {
            falaEsquerda = true;
        }
        else if (!rightSpeakerSet)
        {
            // Segundo speaker novo vai para a direita
            rightSpeaker = lineInfo.speaker;
            rightSpeakerSet = true;
            falaEsquerda = false;
        }
        else if (lineInfo.speaker == rightSpeaker)
        {
            falaEsquerda = false;
        }
        else
        {
            // Terceiro speaker ou mais — substitui o lado direito
            rightSpeaker = lineInfo.speaker;
            falaEsquerda = false;
        }

        if (falaEsquerda)
        {
            groupNameLeft.SetActive(true);
            groupNameRight.SetActive(false);
            textNameLeft.text = speakerName;

            UpdatePortrait(portraitLeft, speakerPortrait);
            if (portraitLeft != null) portraitLeft.color = corFoco;

            if (portraitRight != null && portraitRight.sprite != null)
            {
                portraitRight.gameObject.SetActive(true);
                portraitRight.color = corSemFoco;
            }
        }
        else
        {
            groupNameLeft.SetActive(false);
            groupNameRight.SetActive(true);
            textNameRight.text = speakerName;

            UpdatePortrait(portraitRight, speakerPortrait);
            if (portraitRight != null) portraitRight.color = corFoco;

            if (portraitLeft != null && portraitLeft.sprite != null)
            {
                portraitLeft.gameObject.SetActive(true);
                portraitLeft.color = corSemFoco;
            }

            if (portraitRight != null)
            {
                portraitRight.rectTransform.localScale = new Vector3(1f, 1f, 1f);
                if (lineInfo.speaker.ToString() == "Claire")
                {
                    float novaAltura = originalPortraitRightY - 35f;
                    portraitRight.rectTransform.anchoredPosition = new Vector2(
                        portraitRight.rectTransform.anchoredPosition.x, novaAltura);
                }
                else
                {
                    portraitRight.rectTransform.anchoredPosition = new Vector2(
                        portraitRight.rectTransform.anchoredPosition.x, originalPortraitRightY);
                }
            }
        }

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
                group.alpha = group.alpha == 1f ? 0f : 1f;
            else
                nextTextIndicator.SetActive(!nextTextIndicator.activeSelf);
            yield return new WaitForSeconds(blinkSpeed);
        }
    }

    public void NextLine()
    {
        currentLine++;
        if (currentLine < currentDialogueData.dialogueLines.Length) 
            ShowLine(); 
        else 
            EndDialogue(); 
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