using System.Collections;
using System.Collections.Generic;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
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

    [Header("Opções de resposta (opcional)")]
    [Tooltip("Onde os botões aparecem. Vazio = criado sozinho acima da caixa de diálogo.")]
    [SerializeField] private RectTransform choicesContainer;
    [Tooltip("Botão com um TMP_Text filho. Vazio = botão simples criado por código.")]
    [SerializeField] private Button choiceButtonPrefab;
    [SerializeField] private float choiceWidth = 600f;
    [SerializeField] private float choiceFontSize = 20f;

    private DialogueData currentDialogueData;
    private DialogueData.DialogueLine currentLineInfo;
    private float originalPortraitRightY;
    private int currentLine = 0;
    private bool isTyping = false;
    private Coroutine blinkCoroutine;
    private bool _isDialogueActive = false;
    public bool isDialogueActive => _isDialogueActive;
    public int currentLineIndex => currentLine;

    // Falas extras inseridas depois de uma escolha (a fala escolhida + resposta)
    private readonly Queue<DialogueData.DialogueLine> insertedLines = new Queue<DialogueData.DialogueLine>();

    // Estado das opções
    private bool awaitingChoice = false;
    public bool isAwaitingChoice => awaitingChoice;
    private float choiceShownTime;
    private int inputBlockedFrame = -1;
    private readonly List<Button> choiceButtons = new List<Button>();
    private CursorLockMode previousLockMode;
    private bool previousCursorVisible;

    // Controla qual personagem está em qual lado
    private CharacterDatabase.CharacterType leftSpeaker;
    private CharacterDatabase.CharacterType rightSpeaker;
    private bool leftSpeakerSet = false;
    private bool rightSpeakerSet = false;

    // Dados pro ReportLogger
    private float dialogueStartTime;
    private int typingSkips;
    private int linesShown;

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
    insertedLines.Clear();
    HideChoices();

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
        if (dialogueBox == null || !dialogueBox.activeSelf) return;

        // Evita que o mesmo clique/tecla que escolheu a opção pule a próxima fala
        if (Time.frameCount == inputBlockedFrame) return;

        if (awaitingChoice)
        {
            HandleChoiceKeys();
            return;
        }

        bool spacePressed = Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
        bool mousePressed = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;

        if (spacePressed || mousePressed)
        {
            if (isTyping)
            {
                StopAllCoroutines();
                dialogueText.text = currentLineInfo.sentence;
                isTyping = false;
                typingSkips++;
                OnLineFullyShown();
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
        dialogueStartTime = Time.realtimeSinceStartup;
        typingSkips = 0;
        linesShown = 0;
        insertedLines.Clear();
        HideChoices();

        // Reseta o mapeamento de lados para cada novo diálogo
        leftSpeakerSet = false;
        rightSpeakerSet = false;

        dialogueBox.SetActive(true);

        if (data == null || data.dialogueLines == null || data.dialogueLines.Length == 0)
        {
            EndDialogue();
            return;
        }

        ShowLine(data.dialogueLines[0]);
    }

    public IEnumerator PlayDialogueAuto(DialogueData data, float delayBetweenLines)
    {
        StartDialogue(data);
        yield return null;

        while (_isDialogueActive)
        {
            // Se a fala tiver opções, espera o jogador escolher
            yield return new WaitUntil(() => !isTyping && !awaitingChoice);
            yield return new WaitForSeconds(delayBetweenLines);
            if (_isDialogueActive && !awaitingChoice)
                NextLine();
        }
    }

    private void ShowLine(DialogueData.DialogueLine lineInfo)
    {
        currentLineInfo = lineInfo;
        linesShown++;

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

            // GARANTIA VISUAL: Busca os perfis individuais para estampar as fotos na tela
            CharacterDatabase.CharacterProfile noahProfile = characterDatabase.GetProfile(CharacterDatabase.CharacterType.Noah);
            CharacterDatabase.CharacterProfile emilyProfile = characterDatabase.GetProfile(CharacterDatabase.CharacterType.Emily);

            if (noahProfile.defaultPortrait != null) UpdatePortrait(portraitLeft, noahProfile.defaultPortrait);
            if (emilyProfile.defaultPortrait != null) UpdatePortrait(portraitRight, emilyProfile.defaultPortrait);

            // Dá FOCO TOTAL (Cor branca) para os dois retratos na tela simultaneamente
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
        OnLineFullyShown();
    }

    // Chamado quando a fala termina de aparecer (digitando ou pulando)
    private void OnLineFullyShown()
    {
        if (currentLineInfo.choices != null && currentLineInfo.choices.Length > 0)
            ShowChoices();
        else
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
        if (currentDialogueData == null || awaitingChoice) return;

        // Primeiro mostra as falas que vieram de uma escolha
        if (insertedLines.Count > 0)
        {
            ShowLine(insertedLines.Dequeue());
            return;
        }

        currentLine++;
        if (currentLine < currentDialogueData.dialogueLines.Length)
            ShowLine(currentDialogueData.dialogueLines[currentLine]);
        else
            EndDialogue();
    }

    private void EndDialogue()
    {
        if (_isDialogueActive && currentDialogueData != null)
            ReportLogger.LogDialogue(currentDialogueData.ReportName, Time.realtimeSinceStartup - dialogueStartTime,
                                     linesShown, typingSkips);

        _isDialogueActive = false;
        HideChoices();
        StopBlinking();
        if (dialogueBox != null) dialogueBox.SetActive(false);
        if (portraitLeft != null) portraitLeft.gameObject.SetActive(false);
        if (portraitRight != null) portraitRight.gameObject.SetActive(false);
        OnDialogueEnded?.Invoke();
    }

    // ---------- Opções de resposta ----------

    private void ShowChoices()
    {
        StopBlinking();

        try
        {
            EnsureChoicesUI();

            DialogueData.DialogueChoice[] choices = currentLineInfo.choices;
            for (int i = 0; i < choices.Length; i++)
            {
                int index = i; // cópia pro lambda
                Button button = CreateChoiceButton((i + 1) + ". " + choices[i].text);
                button.onClick.AddListener(() => PickChoice(index));
                choiceButtons.Add(button);
            }

            choicesContainer.gameObject.SetActive(true);
            choicesContainer.SetAsLastSibling();
        }
        catch (Exception e)
        {
            // Se a UI das opções falhar, o diálogo segue sem elas em vez de travar o jogo
            Debug.LogError("[DialogueManager] Não deu pra mostrar as opções: " + e);
            HideChoices();
            StartBlinking();
            return;
        }

        awaitingChoice = true;
        choiceShownTime = Time.realtimeSinceStartup;

        // Libera o mouse pra clicar nas opções
        previousLockMode = Cursor.lockState;
        previousCursorVisible = Cursor.visible;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void HideChoices()
    {
        foreach (Button b in choiceButtons)
            if (b != null) Destroy(b.gameObject);
        choiceButtons.Clear();

        if (choicesContainer != null) choicesContainer.gameObject.SetActive(false);

        if (awaitingChoice)
        {
            awaitingChoice = false;
            Cursor.lockState = previousLockMode;
            Cursor.visible = previousCursorVisible;
        }
    }

    private void HandleChoiceKeys()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null) return;

        Key[] digits = { Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4 };
        Key[] numpad = { Key.Numpad1, Key.Numpad2, Key.Numpad3, Key.Numpad4 };

        int count = Mathf.Min(choiceButtons.Count, digits.Length);
        for (int i = 0; i < count; i++)
        {
            if (kb[digits[i]].wasPressedThisFrame || kb[numpad[i]].wasPressedThisFrame)
            {
                PickChoice(i);
                return;
            }
        }
    }

    private void PickChoice(int index)
    {
        if (!awaitingChoice || currentLineInfo.choices == null || index >= currentLineInfo.choices.Length) return;

        DialogueData.DialogueLine line = currentLineInfo;
        DialogueData.DialogueChoice choice = line.choices[index];

        ReportLogger.LogChoice(currentDialogueData.ReportName, choice.text, choice.stage,
                               Time.realtimeSinceStartup - choiceShownTime);

        HideChoices();
        inputBlockedFrame = Time.frameCount;

        // A opção escolhida vira uma fala, e a resposta (se tiver) vem logo depois
        insertedLines.Enqueue(new DialogueData.DialogueLine { speaker = line.choiceSpeaker, sentence = choice.text });
        if (!string.IsNullOrEmpty(choice.response))
            insertedLines.Enqueue(new DialogueData.DialogueLine { speaker = line.speaker, sentence = choice.response });

        NextLine();
    }

    private void EnsureChoicesUI()
    {
        if (choicesContainer == null)
        {
            // Lista vertical no meio da tela, filha do Canvas raiz.
            // (O dialogueBox ocupa a tela inteira e tem escala achatada, então não serve de pai.)
            Canvas rootCanvas = dialogueBox.GetComponentInParent<Canvas>(true).rootCanvas;

            GameObject go = new GameObject("Opcoes", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster),
                                           typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            go.layer = dialogueBox.layer;
            go.transform.SetParent(rootCanvas.transform, false);

            // Canvas próprio por cima dos outros (a caixa e os retratos têm canvas próprios)
            Canvas own = go.GetComponent<Canvas>();
            own.overrideSorting = true;
            own.sortingOrder = 1000;

            RectTransform rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, 40f);
            rt.sizeDelta = new Vector2(choiceWidth, 0f);

            VerticalLayoutGroup layout = go.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 8f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            go.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            choicesContainer = rt;
        }

        // Sem isso o clique nos botões não funciona
        Canvas canvas = choicesContainer.GetComponentInParent<Canvas>();
        if (canvas != null && canvas.GetComponent<GraphicRaycaster>() == null)
            canvas.gameObject.AddComponent<GraphicRaycaster>();

        if (EventSystem.current == null)
        {
            GameObject es = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            DontDestroyOnLoad(es);
        }
    }

    private Button CreateChoiceButton(string label)
    {
        if (choiceButtonPrefab != null)
        {
            Button custom = Instantiate(choiceButtonPrefab, choicesContainer);
            TMP_Text customText = custom.GetComponentInChildren<TMP_Text>();
            if (customText != null) customText.text = label;
            return custom;
        }

        GameObject go = new GameObject("Opcao", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        go.transform.SetParent(choicesContainer, false);
        go.GetComponent<LayoutElement>().minHeight = 40f;

        Image image = go.GetComponent<Image>();
        image.color = Color.white;

        Button button = go.GetComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.08f, 0.08f, 0.08f, 0.85f);
        colors.highlightedColor = new Color(0.3f, 0.3f, 0.3f, 0.95f);
        colors.selectedColor = colors.highlightedColor;
        colors.pressedColor = new Color(0.45f, 0.45f, 0.45f, 1f);
        button.colors = colors;

        GameObject textGo = new GameObject("Texto", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGo.transform.SetParent(go.transform, false);
        RectTransform trt = (RectTransform)textGo.transform;
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = new Vector2(16f, 6f);
        trt.offsetMax = new Vector2(-16f, -6f);

        TextMeshProUGUI tmp = textGo.GetComponent<TextMeshProUGUI>();
        if (dialogueText != null) tmp.font = dialogueText.font;
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = 16f;
        tmp.fontSizeMax = choiceFontSize;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.text = label;

        return button;
    }
}
