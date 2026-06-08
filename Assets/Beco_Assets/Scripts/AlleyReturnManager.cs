using UnityEngine;

public class AlleyReturnManager : MonoBehaviour
{
    public AlleyIntroManager introManager;
    public BecoSequenceManager becoSequenceManager;

    void Start()
    {
        if (GameManager.Instance != null && GameManager.Instance.returningFromFlashback)
        {
            GameManager.Instance.returningFromFlashback = false;

            if (introManager != null)
                introManager.gameObject.SetActive(false);

            if (becoSequenceManager != null)
                becoSequenceManager.RetornarDoFlashback();
        }
        else
        {
            if (introManager != null)
                introManager.gameObject.SetActive(true);
        }
    }
}