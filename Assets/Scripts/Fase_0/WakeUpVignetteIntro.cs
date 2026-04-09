using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class WakeUpStagesIntro : MonoBehaviour
{
    [Header("UI")]
    public Button wakeButton;
    public string nextSceneName = "Scene_Room";

    [Header("Overlays (Canvas_Effects)")]
    public Image darknessOverlay;
    public Image vignetteOverlay;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip alarmBeep;

    [Header("Start")]
    public float initialBlackSeconds = 2f;
    public bool showButtonAfterDelay = true;

    [Header("Progression (Acordando aos poucos)")]
    public int clicksPerStage = 3;
    public int totalStages = 4;

    [Range(0f, 1f)] public float startAlpha = 1f; // 1 = totalmente preto
    [Range(0f, 1f)] public float finalAlpha = 0f; // 0 = totalmente aberto

    [Header("Blink Lento (mesma velocidade pra abrir e fechar)")]
    public float blinkTime = 0.30f;
    public float closedHold = 0.05f;

    [Header("Final Behavior")]
    public bool disableVignetteOnFinal = true;

    [Tooltip("Tempo para a vinheta sumir suavemente no final.")]
    public float vignetteFadeOutTime = 0.40f;

    public bool loadNextSceneOnFinal = true;
    public float finalPauseBeforeLoad = 0.25f;

    private int clicks = 0;
    private int stage = 0;           // 0..totalStages
    private bool isAnimating = false;

    void Start()
    {
        SetDarknessAlpha(startAlpha); // começa fechado

        wakeButton.gameObject.SetActive(!showButtonAfterDelay);
        wakeButton.interactable = true;
        wakeButton.onClick.AddListener(OnWakeClicked);

        StartCoroutine(BeginIntro());
    }

    IEnumerator BeginIntro()
    {
        if (showButtonAfterDelay && initialBlackSeconds > 0f)
        {
            yield return new WaitForSeconds(initialBlackSeconds);
            wakeButton.gameObject.SetActive(true);
        }
    }

    void OnWakeClicked()
    {
        if (isAnimating) return;

        clicks++;

        if (alarmBeep && audioSource)
            audioSource.PlayOneShot(alarmBeep);

        if (clicksPerStage <= 0) return;

        // só faz animação a cada N cliques
        if (clicks % clicksPerStage != 0) return;

        stage++;
        if (stage > totalStages) stage = totalStages;

        StartCoroutine(AdvanceStageRoutine());
    }

    IEnumerator AdvanceStageRoutine()
    {
        isAnimating = true;

        // Se ainda não é o último estágio:
        // Faz: FECHA -> ABRE até o target -> FECHA de novo
        if (stage < totalStages)
        {
            float targetAlpha = GetAlphaForStage(stage);

            yield return AnimateDarknessAlpha(darknessOverlay.color.a, 1f, blinkTime);

            if (closedHold > 0f) yield return new WaitForSeconds(closedHold);

            yield return AnimateDarknessAlpha(1f, targetAlpha, blinkTime);

            yield return AnimateDarknessAlpha(targetAlpha, 1f, blinkTime);

            isAnimating = false;
            yield break;
        }

        // ========= ÚLTIMO ESTÁGIO (ELEGANTE) =========
        // 1) Fecha/garante preto total
        yield return AnimateDarknessAlpha(darknessOverlay.color.a, 1f, blinkTime);
        if (closedHold > 0f) yield return new WaitForSeconds(closedHold);

        // 2) Enquanto ainda está preto, faz a vinheta sumir suavemente
        if (disableVignetteOnFinal && vignetteOverlay != null)
        {
            float fromA = vignetteOverlay.color.a;
            yield return StartCoroutine(FadeImageAlpha(vignetteOverlay, fromA, 0f, vignetteFadeOutTime));
            vignetteOverlay.gameObject.SetActive(false); // opcional, só pra “limpar”
        }

        // 3) Agora abre total com visão já normal
        yield return AnimateDarknessAlpha(1f, finalAlpha, blinkTime);
        // ============================================

        wakeButton.interactable = false;

        if (loadNextSceneOnFinal)
        {
            if (finalPauseBeforeLoad > 0f)
                yield return new WaitForSeconds(finalPauseBeforeLoad);

            SceneManager.LoadScene(nextSceneName);
        }

        isAnimating = false;
    }

    float GetAlphaForStage(int s)
    {
        float t = Mathf.Clamp01((float)s / Mathf.Max(1, totalStages));
        return Mathf.Lerp(startAlpha, finalAlpha, t);
    }

    IEnumerator AnimateDarknessAlpha(float from, float to, float duration)
    {
        if (duration <= 0f)
        {
            SetDarknessAlpha(to);
            yield break;
        }

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            SetDarknessAlpha(Mathf.Lerp(from, to, k));
            yield return null;
        }
        SetDarknessAlpha(to);
    }

    // Faz fade no alpha de qualquer Image (usamos na vinheta)
    IEnumerator FadeImageAlpha(Image img, float from, float to, float duration)
    {
        if (img == null) yield break;

        if (duration <= 0f)
        {
            Color c0 = img.color;
            c0.a = to;
            img.color = c0;
            yield break;
        }

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);

            Color c = img.color;
            c.a = Mathf.Lerp(from, to, k);
            img.color = c;

            yield return null;
        }

        Color c1 = img.color;
        c1.a = to;
        img.color = c1;
    }

    void SetDarknessAlpha(float a)
    {
        if (darknessOverlay == null) return;
        Color c = darknessOverlay.color;
        c.a = a;
        darknessOverlay.color = c;
    }
}