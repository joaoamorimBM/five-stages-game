using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [Header("Painéis")]
    public GameObject painelOpcoes;

    [Header("Opções")]
    public Slider sliderVolume;
    public Slider sliderSensibilidade;

    private void Start()
    {
        Debug.Log("MainMenu iniciado!");

        if (painelOpcoes != null)
            painelOpcoes.SetActive(false);

        if (sliderVolume != null)
            sliderVolume.value = PlayerPrefs.GetFloat("Volume", 1f);

        if (sliderSensibilidade != null)
            sliderSensibilidade.value = PlayerPrefs.GetFloat("Sensibilidade", 2f);

        AplicarVolume();
        AplicarSensibilidade();
    }

    public void Jogar()
    {
        Debug.Log("Jogar() chamado!");

        if (GameManager.Instance != null)
        {
            GameManager.Instance.accidentVisit = 1;
            GameManager.Instance.returningFromFlashback = false;
            GameManager.Instance.playFinalCutscene = false;
            Debug.Log("GameManager resetado!");
        }
        else
        {
            Debug.LogWarning("GameManager nao encontrado!");
        }

        Debug.Log("Carregando Scene_Intro...");
        SceneManager.LoadScene("Scene_Intro");
    }

    public void AbrirOpcoes()
{
    if (painelOpcoes != null)
    {
        painelOpcoes.SetActive(true);
        // Garante que o botão só funciona quando o painel está aberto
    }
}

public void FecharOpcoes()
{
    if (painelOpcoes != null)
        painelOpcoes.SetActive(false);

    PlayerPrefs.Save();
}

    public void OnVolumeChanged()
    {
        PlayerPrefs.SetFloat("Volume", sliderVolume.value);
        AplicarVolume();
    }

    public void OnSensibilidadeChanged()
    {
        PlayerPrefs.SetFloat("Sensibilidade", sliderSensibilidade.value);
        AplicarSensibilidade();
    }

    private void AplicarVolume()
    {
        AudioListener.volume = sliderVolume != null ? sliderVolume.value : 1f;
    }

    private void AplicarSensibilidade()
    {
        if (sliderSensibilidade == null) return;
        PlayerPrefs.SetFloat("Sensibilidade", sliderSensibilidade.value);
    }

    public void Sair()
{
    Debug.Log("Sair() chamado de: " + System.Environment.StackTrace);
    Application.Quit();
}
}