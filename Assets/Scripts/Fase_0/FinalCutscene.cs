using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class FinalCutscene : MonoBehaviour
{
    [Header("Objetos da Cena")]
    public GameObject fase0Objects;
    public GameObject camaFinal;

    [Header("Câmera")]
    public Camera cutsceneCamera;
    public Transform cameraStartPosition;
    public Transform cameraEndPosition;
    public float cameraDuration = 8f;

    [Header("Luz da TV")]
    public Light tvLight;
    public float tvFlickerSpeed = 2f;

    [Header("Áudio")]
    public AudioSource cutsceneAudio;
    public float audioFadeInDuration = 3f;

    [Header("Fade")]
    public UnityEngine.UI.Image fadeImage;

    [Header("Próxima Cena (Dedicatória)")]
    [Tooltip("Nome EXATO da cena que entra depois da cutscene. Precisa estar nas Build Settings.")]
    public string proximaCena = "Dedicatoria_Scene";

    [Header("Teste")]
    public bool forcePlayInEditor = false;

    void Start()
{
    bool shouldPlay = forcePlayInEditor ||
        (GameManager.Instance != null && GameManager.Instance.playFinalCutscene);

    if (shouldPlay)
    {
        StartCoroutine(PlayFinalCutscene());
    }
    else
    {
        // Fase normal — garante tela transparente e objetos corretos
        SetFadeAlpha(0f);

        if (fase0Objects != null)
            fase0Objects.SetActive(true);

        if (camaFinal != null)
            camaFinal.SetActive(false);

        if (cutsceneCamera != null)
            cutsceneCamera.gameObject.SetActive(false);
    }
}

    private IEnumerator PlayFinalCutscene()
    {
        SetFadeAlpha(1f);

        if (fase0Objects != null)
            fase0Objects.SetActive(false);

        if (camaFinal != null)
            camaFinal.SetActive(true);

        Camera playerCamera = Camera.main;
        if (playerCamera != null)
            playerCamera.gameObject.SetActive(false);

        if (cutsceneCamera != null)
        {
            cutsceneCamera.gameObject.SetActive(true);
            cutsceneCamera.transform.position = cameraStartPosition.position;
            cutsceneCamera.transform.rotation = cameraStartPosition.rotation;
        }

        if (cutsceneAudio != null)
        {
            cutsceneAudio.volume = 0f;
            cutsceneAudio.Play();
        }

        yield return StartCoroutine(FadeScreen(1f, 0f, 1.5f));

        StartCoroutine(TVFlicker());
        StartCoroutine(FadeAudio(0f, 0.3f, audioFadeInDuration));

        yield return StartCoroutine(MoveCamera());

        yield return new WaitForSeconds(2f);

        yield return StartCoroutine(FadeScreen(0f, 1f, 2f));

        Debug.Log("Cutscene final concluída!");

        // Já não precisamos mais tocar a cutscene de novo se voltar a essa cena.
        if (GameManager.Instance != null)
            GameManager.Instance.playFinalCutscene = false;

        // --- VAI PARA A CENA DA DEDICATÓRIA ---
        if (!string.IsNullOrEmpty(proximaCena))
            SceneManager.LoadScene(proximaCena);
        else
            Debug.LogError("[FinalCutscene] 'proximaCena' está vazio! " +
                           "Preencha com o nome da cena da dedicatória no Inspector.");
    }

    private IEnumerator MoveCamera()
    {
        if (cutsceneCamera == null || cameraEndPosition == null) yield break;

        float elapsed = 0f;
        Vector3 startPos = cameraStartPosition.position;
        Quaternion startRot = cameraStartPosition.rotation;

        while (elapsed < cameraDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / cameraDuration);

            cutsceneCamera.transform.position = Vector3.Lerp(startPos, cameraEndPosition.position, t);
            cutsceneCamera.transform.rotation = Quaternion.Slerp(startRot, cameraEndPosition.rotation, t);

            yield return null;
        }

        cutsceneCamera.transform.position = cameraEndPosition.position;
        cutsceneCamera.transform.rotation = cameraEndPosition.rotation;
    }

    private IEnumerator TVFlicker()
    {
        if (tvLight == null) yield break;

        while (true)
        {
            float intensity = 1.5f + Mathf.Sin(Time.time * tvFlickerSpeed) * 0.5f;
            tvLight.intensity = intensity;
            yield return null;
        }
    }

    private IEnumerator FadeAudio(float from, float to, float duration)
    {
        if (cutsceneAudio == null) yield break;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            cutsceneAudio.volume = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        cutsceneAudio.volume = to;
    }

    private IEnumerator FadeScreen(float from, float to, float duration)
    {
        if (fadeImage == null) yield break;

        float elapsed = 0f;
        Color c = fadeImage.color;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            c.a = Mathf.Lerp(from, to, elapsed / duration);
            fadeImage.color = c;
            yield return null;
        }

        c.a = to;
        fadeImage.color = c;
    }

    private void SetFadeAlpha(float alpha)
    {
        if (fadeImage == null) return;
        Color c = fadeImage.color;
        c.a = alpha;
        fadeImage.color = c;
    }
}