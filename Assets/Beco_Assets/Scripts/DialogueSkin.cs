using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Visual "memória íntima" da caixa de diálogo, montado por código em cima do prefab:
// painel carvão quente com cantos arredondados e borda fina creme, retrato de busto
// num quadro à esquerda (de quem está falando) e o nome em versalete acima da fala.
// Os retratos grandes e as etiquetas de nome antigas ficam escondidos.
// Pra voltar ao visual antigo: desmarque "Usar Visual Novo" no DialogueManager.
public class DialogueSkin : MonoBehaviour
{
    // Medidas no Canvas de referência (800 de largura)
    private const float BoxWidth = 720f;
    private const float BoxHeight = 170f;
    private const float Pad = 16f;
    private const float Gap = 22f;
    private const float NameTop = 16f;
    private const float TextTop = 44f;
    private const float TextRight = 30f;
    private const float TextBottom = 20f;

    private static readonly Color PanelFill = new Color(0.10f, 0.085f, 0.075f, 0.90f);
    private static readonly Color PanelLine = new Color(0.96f, 0.92f, 0.84f, 0.28f);
    private static readonly Color FrameFill = new Color(0.17f, 0.15f, 0.13f, 1f);
    private static readonly Color FrameLine = new Color(0.96f, 0.92f, 0.84f, 0.40f);
    private static readonly Color TextColor = new Color(0.95f, 0.93f, 0.88f, 1f);
    private static readonly Color NameColor = new Color(0.86f, 0.78f, 0.64f, 1f);

    private RectTransform frame;
    private GameObject frameBorder;
    private Image portrait;
    private TextMeshProUGUI nameText;
    private bool built;

    public void Build(TextMeshProUGUI dialogueText, Image[] oldPortraits, GameObject[] oldNameGroups)
    {
        if (built || dialogueText == null) return;
        RectTransform box = dialogueText.transform.parent as RectTransform;
        if (box == null) return;
        built = true;

        // Some com o visual antigo
        foreach (Image p in oldPortraits) if (p != null) p.gameObject.SetActive(false);
        foreach (GameObject g in oldNameGroups) if (g != null) g.SetActive(false);

        // Painel
        box.sizeDelta = new Vector2(BoxWidth, BoxHeight);
        box.anchoredPosition = new Vector2(box.anchoredPosition.x, BoxHeight / 2f + 12f);
        Image panel = box.GetComponent<Image>();
        if (panel != null)
        {
            panel.sprite = UiShapes.Rounded(18, 2, PanelFill, PanelLine);
            panel.type = Image.Type.Sliced;
            panel.color = Color.white;
        }

        // Quadro do retrato (máscara arredondada) + moldura por cima
        float side = BoxHeight - 2f * Pad;
        frame = NewRect("Quadro", box);
        frame.anchorMin = frame.anchorMax = new Vector2(0f, 1f);
        frame.pivot = new Vector2(0f, 1f);
        frame.anchoredPosition = new Vector2(Pad, -Pad);
        frame.sizeDelta = new Vector2(side, side);
        Image frameBg = frame.gameObject.AddComponent<Image>();
        frameBg.sprite = UiShapes.Rounded(12, 0, FrameFill, FrameFill);
        frameBg.type = Image.Type.Sliced;
        frameBg.raycastTarget = false;
        frame.gameObject.AddComponent<Mask>().showMaskGraphic = true;

        RectTransform portraitRt = NewRect("Retrato", frame);
        Stretch(portraitRt);
        portrait = portraitRt.gameObject.AddComponent<Image>();
        portrait.preserveAspect = true;
        portrait.raycastTarget = false;

        RectTransform border = NewRect("Moldura", box);
        border.anchorMin = border.anchorMax = frame.anchorMin;
        border.pivot = frame.pivot;
        border.anchoredPosition = frame.anchoredPosition;
        border.sizeDelta = frame.sizeDelta;
        Image borderImg = border.gameObject.AddComponent<Image>();
        borderImg.sprite = UiShapes.Rounded(12, 2, new Color(0, 0, 0, 0), FrameLine);
        borderImg.type = Image.Type.Sliced;
        borderImg.raycastTarget = false;
        frameBorder = border.gameObject;

        float textLeft = Pad + side + Gap;

        // Nome em versalete acima da fala
        RectTransform nameRt = NewRect("Nome", box);
        nameRt.anchorMin = new Vector2(0f, 1f);
        nameRt.anchorMax = new Vector2(1f, 1f);
        nameRt.pivot = new Vector2(0f, 1f);
        nameRt.offsetMin = new Vector2(textLeft, -NameTop - 22f);
        nameRt.offsetMax = new Vector2(-TextRight, -NameTop);
        nameText = nameRt.gameObject.AddComponent<TextMeshProUGUI>();
        nameText.font = GameFonts.BodyMedium != null ? GameFonts.BodyMedium : dialogueText.font;
        nameText.fontSize = 15;
        nameText.fontStyle = FontStyles.UpperCase;
        nameText.characterSpacing = 14;
        nameText.color = NameColor;
        nameText.alignment = TextAlignmentOptions.BottomLeft;
        nameText.textWrappingMode = TextWrappingModes.NoWrap;
        nameText.raycastTarget = false;

        // Fala ao lado do retrato
        RectTransform textRt = dialogueText.rectTransform;
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = new Vector2(textLeft, TextBottom);
        textRt.offsetMax = new Vector2(-TextRight, -TextTop);
        if (GameFonts.Body != null) dialogueText.font = GameFonts.Body;
        dialogueText.color = TextColor;
        dialogueText.lineSpacing = 4;
        dialogueText.textWrappingMode = TextWrappingModes.Normal;
        dialogueText.margin = Vector4.zero;
        dialogueText.transform.SetAsLastSibling();
    }

    // Quem está falando: nome + retrato de busto. Sem retrato, o quadro some.
    public void Show(string speakerName, Sprite portraitSprite)
    {
        if (!built) return;
        nameText.text = speakerName;

        Sprite bust = PortraitCrop.Bust(portraitSprite);
        portrait.sprite = bust;
        bool hasPortrait = bust != null;
        frame.gameObject.SetActive(hasPortrait);
        frameBorder.SetActive(hasPortrait);
    }

    private static RectTransform NewRect(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.layer = parent.gameObject.layer;
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }
}
