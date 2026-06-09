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

    [Header("Opcional")]
    [Tooltip("Permite pular a dedicatória apertando Espaço/Enter/clique.")]
    [SerializeField] private bool permitirPular = false;

    // -----------------------------------------------------------------
    // TEMPOS DA SEQUÊNCIA (em segundos) — mude aqui se quiser ajustar.
    // -----------------------------------------------------------------
    private const float FADE_IN_TEXTO   = 1.5f; // quão devagar o texto aparece
    private const float ESPERA_ANTES_FOTO = 3f; // espera entre o texto e a foto
    private const float FADE_IN_FOTO    = 1.5f; // quão devagar a foto aparece
    private const float SEGURA_JUNTOS   = 5f;   // texto + foto juntos na tela
    private const float FADE_OUT_FINAL  = 2f;   // quão devagar tudo escurece

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
        // 1) Título (texto) entra em cima
        yield return Fade(grupoTexto, 0f, 1f, FADE_IN_TEXTO);

        // 2) Espera 3 segundos com o texto na tela
        yield return new WaitForSeconds(ESPERA_ANTES_FOTO);

        // 3) Foto do gato entra embaixo (o título continua visível)
        yield return Fade(grupoFoto, 0f, 1f, FADE_IN_FOTO);

        // 4) Texto + foto ficam juntos na tela por 5 segundos
        yield return new WaitForSeconds(SEGURA_JUNTOS);

        // 5) Os dois somem juntos (ao mesmo tempo)
        StartCoroutine(Fade(grupoTexto, 1f, 0f, FADE_OUT_FINAL));
        yield return Fade(grupoFoto, 1f, 0f, FADE_OUT_FINAL);

        // 6) Volta ao menu
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