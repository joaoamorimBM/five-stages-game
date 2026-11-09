using System;
using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Tela de código, por cima do menu. Aparece toda vez que o Scene_MainMenu carrega,
// pra um computador de clínica não herdar o código do paciente anterior.
// Tudo é criado por código: não precisa mexer na cena.
public class CodeScreen : MonoBehaviour
{
    private const string ConsentText =
        "Este jogo foi pensado para apoiar a conversa com o seu psicólogo. Durante a partida, ele registra as respostas " +
        "que você escolhe nos diálogos, os objetos que você observa e o tempo de cada momento.\n\n" +
        "Nenhum nome ou dado pessoal é registrado: só o código abaixo. O relatório vai apenas para o psicólogo que gerou o código, " +
        "e ele pode apagá-lo quando quiser.";

    private static readonly Color Background = new Color(0.07f, 0.06f, 0.055f, 0.97f);
    private static readonly Color Cream = new Color(0.96f, 0.94f, 0.89f, 1f);
    private static readonly Color CreamSoft = new Color(0.96f, 0.94f, 0.89f, 0.78f);
    private static readonly Color Ink = new Color(0.12f, 0.10f, 0.09f, 1f);
    private static readonly Color Ok = new Color(0.62f, 0.78f, 0.55f, 1f);
    private static readonly Color Error = new Color(0.95f, 0.58f, 0.48f, 1f);

    private TMP_InputField input;
    private TextMeshProUGUI status;
    private TMP_FontAsset displayFont; // a mesma fonte do título do menu (Bebas Neue)
    private Button startButton;
    private Button skipButton;
    private bool checking;

    [Serializable]
    private class CheckResponse
    {
        public bool existe;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Init()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != ReportLogger.MenuSceneName) return;
        if (FindAnyObjectByType<CodeScreen>() != null) return;

        GameObject root = new GameObject("TelaDeCodigo", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        root.SetActive(false); // monta tudo antes de ligar

        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5000;

        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        root.AddComponent<CodeScreen>().Build();
        root.SetActive(true);
    }

    private void Build()
    {
        if (EventSystem.current == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Usa a fonte do título do menu pra tela ter a cara do jogo
        foreach (TMP_Text t in FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (t.name == "Titulo" && t.font != null)
            {
                displayFont = t.font;
                break;
            }
        }

        // Fundo escuro na tela inteira (também bloqueia cliques no menu)
        Image bg = NewChild<Image>("Fundo", transform);
        Stretch(bg.rectTransform);
        bg.color = Background;

        // Coluna central
        RectTransform column = NewChild<RectTransform>("Coluna", transform);
        column.anchorMin = column.anchorMax = new Vector2(0.5f, 0.5f);
        column.pivot = new Vector2(0.5f, 0.5f);
        column.sizeDelta = new Vector2(820, 0);
        VerticalLayoutGroup layout = column.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 22;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        column.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        TextMeshProUGUI title = Text("Titulo", column, "ANTES DE COMEÇAR", 84, Cream, FontStyles.Normal);
        if (displayFont != null) title.font = displayFont;
        else title.text = "Antes de começar";

        Text("Consentimento", column, ConsentText, 26, CreamSoft, FontStyles.Normal).lineSpacing = 8;
        Spacer(column, 6);
        Text("Rotulo", column, "Código do paciente (o psicólogo te passa)", 24, Cream, FontStyles.Bold);

        input = CodeField(column);
        status = Text("Status", column, " ", 24, CreamSoft, FontStyles.Normal);

        RectTransform buttons = NewChild<RectTransform>("Botoes", column);
        HorizontalLayoutGroup row = buttons.gameObject.AddComponent<HorizontalLayoutGroup>();
        row.spacing = 20;
        row.childControlWidth = true;
        row.childControlHeight = true;
        row.childForceExpandWidth = false;
        row.childForceExpandHeight = false;

        startButton = MakeButton(buttons, "COMEÇAR", true, OnStart, displayFont);
        skipButton = MakeButton(buttons, "JOGAR SEM CÓDIGO", false, OnSkip, displayFont);

        Text("Nota", column, "Sem código, o jogo funciona normalmente e nada é registrado.", 22, CreamSoft, FontStyles.Italic);
    }

    // ---------- Ações ----------

    private void OnStart()
    {
        if (checking) return;

        string code = PatientCode.Normalize(input.text);
        if (code == null)
        {
            SetStatus("Código inválido. Confira as letras e os números com o seu psicólogo.", Error);
            return;
        }

        input.text = code;
        if (!GameConfig.Online)
        {
            Accept(code);
            return;
        }
        StartCoroutine(CheckOnline(code));
    }

    private void OnSkip()
    {
        if (checking) return;
        ReportLogger.DisarmSession();
        Destroy(gameObject);
    }

    private IEnumerator CheckOnline(string code)
    {
        checking = true;
        SetInteractable(false);
        SetStatus("Conferindo o código…", CreamSoft);

        string body = "{\"codigo\":\"" + code + "\"}";
        using (UnityWebRequest req = new UnityWebRequest(GameConfig.FunctionsUrl + "/check-code", "POST"))
        {
            req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.timeout = 6;

            yield return req.SendWebRequest();

            checking = false;
            SetInteractable(true);

            if (req.result == UnityWebRequest.Result.Success && req.responseCode == 200)
            {
                CheckResponse resp = null;
                try { resp = JsonUtility.FromJson<CheckResponse>(req.downloadHandler.text); }
                catch { /* resposta estranha: trata como offline */ }

                if (resp != null && !resp.existe)
                {
                    SetStatus("Esse código não existe. Confira com o seu psicólogo.", Error);
                    yield break;
                }
                SetStatus("Código reconhecido.", Ok);
            }
            // Sem internet: o dígito verificador já pegou erro de digitação, e o envio fica pra depois
            Accept(code);
        }
    }

    private void Accept(string code)
    {
        ReportLogger.ArmSession(code);
        Destroy(gameObject);
    }

    private void SetStatus(string text, Color color)
    {
        status.text = text;
        status.color = color;
    }

    private void SetInteractable(bool value)
    {
        startButton.interactable = value;
        skipButton.interactable = value;
        input.interactable = value;
    }

    // ---------- Construção da UI ----------

    private static T NewChild<T>(string name, Transform parent) where T : Component
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        go.transform.SetParent(parent, false);
        return typeof(T) == typeof(RectTransform) ? go.GetComponent<T>() : go.AddComponent<T>();
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    private static TextMeshProUGUI Text(string name, Transform parent, string text, float size, Color color, FontStyles style)
    {
        TextMeshProUGUI tmp = NewChild<TextMeshProUGUI>(name, parent);
        if (GameFonts.Body != null) tmp.font = GameFonts.Body; // mesma fonte da caixa de diálogo
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.fontStyle = style;
        tmp.textWrappingMode = TextWrappingModes.Normal; // criado por código não herda a quebra de linha
        tmp.raycastTarget = false;
        return tmp;
    }

    private static void Spacer(Transform parent, float height)
    {
        LayoutElement le = NewChild<LayoutElement>("Espaco", parent);
        le.minHeight = height;
    }

    private TMP_InputField CodeField(Transform parent)
    {
        Image box = NewChild<Image>("Campo", parent);
        box.color = new Color(1f, 1f, 1f, 0.08f);
        LayoutElement le = box.gameObject.AddComponent<LayoutElement>();
        le.minHeight = le.preferredHeight = 88;

        RectTransform area = NewChild<RectTransform>("Area", box.transform);
        Stretch(area);
        area.offsetMin = new Vector2(24, 8);
        area.offsetMax = new Vector2(-24, -8);
        area.gameObject.AddComponent<RectMask2D>();

        TextMeshProUGUI placeholder = NewChild<TextMeshProUGUI>("Dica", area);
        Stretch(placeholder.rectTransform);
        placeholder.text = "XXXX-XXXX";
        placeholder.fontSize = 48;
        placeholder.color = new Color(1f, 1f, 1f, 0.25f);
        placeholder.alignment = TextAlignmentOptions.Center;
        placeholder.characterSpacing = 12;

        TextMeshProUGUI text = NewChild<TextMeshProUGUI>("Texto", area);
        Stretch(text.rectTransform);
        text.fontSize = 48;
        text.color = Cream;
        text.alignment = TextAlignmentOptions.Center;
        text.characterSpacing = 12;

        TMP_InputField field = box.gameObject.AddComponent<TMP_InputField>();
        field.targetGraphic = box;
        field.textViewport = area;
        field.textComponent = text;
        field.placeholder = placeholder;
        field.characterLimit = 9;
        field.contentType = TMP_InputField.ContentType.Standard;
        field.lineType = TMP_InputField.LineType.SingleLine;
        // Só letras, números e hífen; tudo em maiúscula
        field.onValidateInput = (s, i, c) => char.IsLetterOrDigit(c) || c == '-' ? char.ToUpperInvariant(c) : '\0';
        field.onSubmit.AddListener(_ => OnStart());
        return field;
    }

    private static Button MakeButton(Transform parent, string label, bool primary, UnityEngine.Events.UnityAction onClick, TMP_FontAsset font)
    {
        Image img = NewChild<Image>(label, parent);
        img.color = Color.white;

        Button button = img.gameObject.AddComponent<Button>();
        button.targetGraphic = img;
        ColorBlock colors = button.colors;
        colors.normalColor = primary ? Cream : new Color(1f, 1f, 1f, 0.06f);
        colors.highlightedColor = primary ? Color.white : new Color(1f, 1f, 1f, 0.16f);
        colors.selectedColor = colors.highlightedColor;
        colors.pressedColor = primary ? new Color(0.85f, 0.82f, 0.76f, 1f) : new Color(1f, 1f, 1f, 0.24f);
        colors.disabledColor = new Color(1f, 1f, 1f, 0.3f);
        button.colors = colors;
        button.onClick.AddListener(onClick);

        HorizontalLayoutGroup pad = img.gameObject.AddComponent<HorizontalLayoutGroup>();
        pad.padding = new RectOffset(36, 36, 18, 18);
        pad.childControlWidth = true;
        pad.childControlHeight = true;

        TextMeshProUGUI tmp = Text("Texto", img.transform, label, 28, primary ? Ink : Cream, FontStyles.Bold);
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        if (font != null)
        {
            tmp.font = font;
            tmp.fontStyle = FontStyles.Normal;
            tmp.fontSize = 40;
            tmp.characterSpacing = 4;
        }
        return button;
    }
}
