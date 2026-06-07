using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class SceneTransition : MonoBehaviour, IInteractable
{
    [Header("Configurações")]
    public string sceneName     = "padaria_scene";
    public string promptMessage = "[E] Sair de casa";

    [Header("Referências")]
    public GameObject    confirmPanel;
    public PlayerMovement playerMovement;
    public Image         fadeImage;

    [Header("Sons da Transição")]
    public AudioClip soundPortaCasa;
    public AudioClip soundPassos;
    public AudioClip soundSinoPadaria;

    [Header("Timing total com tela preta")]
    public float tempoTelaPreta = 14f;  // ← Corrigido caracteres cirílicos aqui

    AudioSource audioSource;

    void Start()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 0f;
        audioSource.playOnAwake  = false;

        if (fadeImage != null)
        {
            Color c = fadeImage.color;
            c.a = 0f;
            fadeImage.color = c;
        }
    }

    public void Interact()
    {
        confirmPanel.SetActive(true);

        if (playerMovement != null)
            playerMovement.SetMovementLocked(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;
    }

    public void ConfirmExit()
    {
        confirmPanel.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;
        StartCoroutine(TransitionSequence());
    }

    IEnumerator TransitionSequence()
    {
        // 1. Tela preta IMEDIATA
        if (fadeImage != null)
        {
            Color c = fadeImage.color;
            c.a = 1f;
            fadeImage.color = c;
        }

        // Pequena pausa para a tela preta aparecer
        yield return new WaitForSeconds(0.1f);

        // 2. Toca os 3 sons em sequência
        // Som 1 — porta da casa
        if (soundPortaCasa != null)
        {
            audioSource.PlayOneShot(soundPortaCasa);
            // Espera a duração EXATA do clip
            yield return new WaitForSeconds(soundPortaCasa.length);
        }

        // Som 2 — passos
        if (soundPassos != null)
        {
            audioSource.PlayOneShot(soundPassos);
            yield return new WaitForSeconds(soundPassos.length);
        }

        // Som 3 — sino da padaria
        if (soundSinoPadaria != null)
        {
            audioSource.PlayOneShot(soundSinoPadaria);
            yield return new WaitForSeconds(soundSinoPadaria.length);
        }

        // 3. Pausa final antes de carregar
        // (preenche o tempo restante até os 14 segundos)
        float tempoUsado = 0f;
        if (soundPortaCasa   != null) tempoUsado += soundPortaCasa.length;
        if (soundPassos      != null) tempoUsado += soundPassos.length;
        if (soundSinoPadaria != null) tempoUsado += soundSinoPadaria.length;

        float tempoRestante = tempoTelaPreta - tempoUsado; // ← Corrigido aqui também
        if (tempoRestante > 0f)
            yield return new WaitForSeconds(tempoRestante);

        // 4. Carrega a próxima fase
        SceneManager.LoadScene(sceneName);
    }

    public void CancelExit()
    {
        confirmPanel.SetActive(false);

        if (playerMovement != null)
            playerMovement.SetMovementLocked(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;
    }

    public string GetPromptText() => promptMessage;
}