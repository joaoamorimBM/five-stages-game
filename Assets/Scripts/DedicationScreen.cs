using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using TMPro;

public class DedicationScreen : MonoBehaviour
{
    [Header("Elementos da Tela")]
    [Tooltip("CanvasGroup do texto da dedicatória.")]
    [SerializeField] private CanvasGroup grupoTexto;
    [SerializeField] private TextMeshProUGUI textoDedicatoria;
    [Tooltip("CanvasGroup da foto (imagem do gato).")]
    [SerializeField] private CanvasGroup grupoFoto;

    [Header("Mensagem")]
    [TextArea(2, 4)]
    [SerializeField] private string mensagem =
        "Esse jogo é dedicado a todos aqueles que já perderam alguém e viveram o luto à sua própria forma.";

    [Header("Cena de Destino")]
    [Tooltip("Nome EXATO da cena do menu inicial (precisa estar nas Build Settings).")]
    [SerializeField] private string menuSceneName = "Scene_MainMenu";

    [Header("Tempos (segundos)")]
    [SerializeField] private float fadeInTexto = 2f;
    [SerializeField] private float seguraTexto = 3f;
    [SerializeField] private float fadeOutTexto = 1.5f;
    [SerializeField] private float intervalo = 0.5f;
    [SerializeField] private float fadeInFoto = 2f;
    [SerializeField] private float seguraFoto = 4f;
    [SerializeField] private float fadeOutFinal = 2f;
    // Total padrão ≈ 15s

    [Header("Opcional")]
    [Tooltip("Permite pular a dedicatória apertando Espaço/Enter/clique.")]
    [SerializeField] private bool permitirPular = false;

    private bool _jaSaiu = false;

    private void Start()
    {
        // Começa tudo invisível (tela preta).
        if (grupoTexto != null) grupoTexto.alpha = 0f;
        if (grupoFoto != null) grupoFoto.alpha = 0f;

        if (textoDedicatoria != null && !string.IsNullOrEmpty(mensagem))
            textoDedicatoria.text = mensagem;

        StartCoroutine(Sequencia());
    }

    private void Update()
    {
        if (!permitirPular || _jaSaiu) return;

        bool pulou =
            (Keyboard.current != null &&
                (Keyboard.current.spaceKey.wasPressedThisFrame ||
                 Keyboard.current.enterKey.wasPressedThisFrame)) ||
            (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame);

        if (pulou)
            IrParaMenu();
    }

    private IEnumerator Sequencia()
    {
        // 1) Texto da dedicatória entra
        yield return Fade(grupoTexto, 0f, 1f, fadeInTexto);
        yield return new WaitForSeconds(seguraTexto);

        // 2) Texto sai
        yield return Fade(grupoTexto, 1f, 0f, fadeOutTexto);
        yield return new WaitForSeconds(intervalo);

        // 3) Foto do gato entra
        yield return Fade(grupoFoto, 0f, 1f, fadeInFoto);
        yield return new WaitForSeconds(seguraFoto);

        // 4) Tudo escurece
        yield return Fade(grupoFoto, 1f, 0f, fadeOutFinal);

        // 5) Volta ao menu
        IrParaMenu();
    }

    private IEnumerator Fade(CanvasGroup grupo, float de, float para, float duracao)
    {
        if (grupo == null) yield break;

        float t = 0f;
        grupo.alpha = de;
        while (t < duracao)
        {
            t += Time.deltaTime;
            grupo.alpha = Mathf.Lerp(de, para, t / duracao);
            yield return null;
        }
        grupo.alpha = para;
    }

    private void IrParaMenu()
    {
        if (_jaSaiu) return;
        _jaSaiu = true;

        if (string.IsNullOrEmpty(menuSceneName))
        {
            Debug.LogError("[DedicationScreen] 'menuSceneName' está vazio! " +
                           "Preencha com o nome da cena do menu no Inspector.");
            return;
        }

        SceneManager.LoadScene(menuSceneName);
    }
}