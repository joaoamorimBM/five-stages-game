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
    [SerializeField] private float choiceFontSize = 14f;

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
            FitChoicesInsideBox();
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
        RestoreBoxLayout();

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

        // Vai pro relatório com TODAS as opções mostradas: o que ele não escolheu também importa
        string speakerName = characterDatabase != null ? characterDatabase.GetProfile(line.speaker).displayName : line.speaker.ToString();
        ReportLogger.LogChoice(currentDialogueData.ReportName, speakerName, line.sentence, line.choices, index,
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
            // As opções ficam DENTRO da caixa de diálogo, embaixo da fala, alinhadas com o texto.
            // A caixa é o pai do texto da fala; sem ela, cai no meio do Canvas raiz.
            RectTransform box = dialogueText != null ? dialogueText.transform.parent as RectTransform : null;
            Transform parent = box != null ? box : dialogueBox.GetComponentInParent<Canvas>(true).rootCanvas.transform;

            GameObject go = new GameObject("Opcoes", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster),
                                           typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            go.layer = dialogueBox.layer;
            go.transform.SetParent(parent, false);

            // Canvas próprio por cima dos outros (a caixa e os retratos têm canvas próprios)
            Canvas own = go.GetComponent<Canvas>();
            own.overrideSorting = true;
            own.sortingOrder = 1000;

            RectTransform rt = (RectTransform)go.transform;
            if (box != null)
            {
                // Faixa no pé da caixa; o texto da opção começa na mesma margem da fala
                float side = Mathf.Max(0f, dialogueText.rectTransform.offsetMin.x - ChoiceRowPadding);
                rt.anchorMin = new Vector2(0f, 0f);
                rt.anchorMax = new Vector2(1f, 0f);
                rt.pivot = new Vector2(0.5f, 0f);
                rt.anchoredPosition = new Vector2(0f, ChoicesBottomPadding);
                rt.sizeDelta = new Vector2(-2f * side, 0f);
            }
            else
            {
                rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(choiceWidth, 0f);
            }

            VerticalLayoutGroup layout = go.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 2f;
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

        // Linha discreta dentro da caixa: cresce com o texto (quebra de linha em vez de estourar)
        GameObject go = new GameObject("Opcao", typeof(RectTransform), typeof(Image), typeof(Button),
                                       typeof(VerticalLayoutGroup), typeof(LayoutElement));
        go.transform.SetParent(choicesContainer, false);
        go.GetComponent<LayoutElement>().minHeight = 22f;

        VerticalLayoutGroup inner = go.GetComponent<VerticalLayoutGroup>();
        int pad = Mathf.RoundToInt(ChoiceRowPadding);
        inner.padding = new RectOffset(pad, pad, 3, 3);
        inner.childAlignment = TextAnchor.MiddleLeft;
        inner.childControlWidth = true;
        inner.childControlHeight = true;
        inner.childForceExpandWidth = true;
        inner.childForceExpandHeight = false;

        Image image = go.GetComponent<Image>();
        image.color = Color.white;

        Button button = go.GetComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        // Fundo transparente: aparece só uma faixa clara ao passar o mouse
        colors.normalColor = new Color(1f, 1f, 1f, 0f);
        colors.highlightedColor = new Color(1f, 1f, 1f, 0.12f);
        colors.selectedColor = colors.highlightedColor;
        colors.pressedColor = new Color(1f, 1f, 1f, 0.2f);
        button.colors = colors;

        GameObject textGo = new GameObject("Texto", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGo.transform.SetParent(go.transform, false);

        TextMeshProUGUI tmp = textGo.GetComponent<TextMeshProUGUI>();
        if (dialogueText != null) tmp.font = dialogueText.font;
        tmp.enableAutoSizing = false;
        tmp.fontSize = choiceFontSize;
        tmp.overflowMode = TextOverflowModes.Overflow; // a altura vem do layout, então nada é cortado
        tmp.textWrappingMode = TextWrappingModes.Normal;
        tmp.raycastTarget = false;
        tmp.color = new Color(0.95f, 0.93f, 0.88f, 1f);
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.text = label;

        return button;
    }

    // ---------- Caixa abrindo espaço pras opções ----------

    private const float ChoicesBottomPadding = 14f; // do pé da caixa até a última opção
    private const float ChoicesGap = 8f;            // entre a fala e a primeira opção
    private const float ChoiceRowPadding = 8f;      // respiro dentro de cada linha de opção

    private bool boxLayoutChanged;
    private Vector2 boxSizeBefore;
    private Vector2 boxPosBefore;
    private Vector2 textOffsetMinBefore;

    // Sobe o pé do texto da fala pra caber as opções embaixo dele. A caixa só cresce
    // (pra cima) se a fala + as opções não couberem no tamanho normal.
    private void FitChoicesInsideBox()
    {
        RectTransform box = choicesContainer.parent as RectTransform;
        if (box == null || dialogueText == null || dialogueText.transform.parent != box) return;

        RectTransform textRt = dialogueText.rectTransform;
        if (!boxLayoutChanged)
        {
            boxSizeBefore = box.sizeDelta;
            boxPosBefore = box.anchoredPosition;
            textOffsetMinBefore = textRt.offsetMin;
            boxLayoutChanged = true;
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(choicesContainer);
        float choicesHeight = choicesContainer.rect.height;

        float topInset = -textRt.offsetMax.y;
        float promptHeight = dialogueText.GetPreferredValues(dialogueText.text, textRt.rect.width, 0f).y;
        float needed = topInset + promptHeight + ChoicesGap + choicesHeight + ChoicesBottomPadding;
        float extra = Mathf.Max(0f, needed - boxSizeBefore.y);

        // Cresce pra cima: o pé da caixa fica onde estava
        box.sizeDelta = new Vector2(boxSizeBefore.x, boxSizeBefore.y + extra);
        box.anchoredPosition = boxPosBefore + new Vector2(0f, extra * box.pivot.y);
        textRt.offsetMin = new Vector2(textOffsetMinBefore.x, ChoicesBottomPadding + choicesHeight + ChoicesGap);
    }

    private void RestoreBoxLayout()
    {
        if (!boxLayoutChanged) return;
        boxLayoutChanged = false;

        RectTransform box = dialogueText != null ? dialogueText.transform.parent as RectTransform : null;
        if (box == null) return;
        box.sizeDelta = boxSizeBefore;
        box.anchoredPosition = boxPosBefore;
        dialogueText.rectTransform.offsetMin = textOffsetMinBefore;
    }
}
