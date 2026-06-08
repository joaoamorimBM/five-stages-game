using UnityEngine;

public class AlleyReturnManager : MonoBehaviour
{
    public AlleyIntroManager introManager;
    public BecoSequenceManager becoSequenceManager;

    void Start()
    {
        if (GameManager.Instance != null && GameManager.Instance.returningFromFlashback)
        {
            // Reseta a flag
            GameManager.Instance.returningFromFlashback = false;

            // Não mostra a intro da crise
            if (introManager != null)
                introManager.gameObject.SetActive(false);

            // Vai direto para o diálogo final
            if (becoSequenceManager != null)
                becoSequenceManager.RetornarDoFlashback();
        }
        else
        {
            // Primeira vez — mostra a intro da crise
            if (introManager != null)
                introManager.gameObject.SetActive(true);

            // BecoSequenceManager já está ativo esperando o player chegar no gato
        }
    }
}