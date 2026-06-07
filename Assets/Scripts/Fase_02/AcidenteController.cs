using UnityEngine;

public class AccidentController : MonoBehaviour
{
    public AccidentScene accidentScene;
    public AccidentFlashback accidentFlashback;

    [Header("Forçar visita (só para teste)")]
    public int forceVisit = 0;

    void Start()
    {
        Debug.Log("AccidentController — accidentVisit: " + 
            (GameManager.Instance != null ? GameManager.Instance.accidentVisit.ToString() : "NULL"));

        int visit;

        if (forceVisit != 0)
            visit = forceVisit;
        else
            visit = GameManager.Instance != null ? GameManager.Instance.accidentVisit : 1;

        Debug.Log("Visit selecionada: " + visit);

        if (visit == 1)
        {
            Debug.Log("Ativando AccidentScene");
            accidentScene.gameObject.SetActive(true);
            accidentFlashback.gameObject.SetActive(false);
        }
        else
        {
            Debug.Log("Ativando AccidentFlashback");
            accidentScene.gameObject.SetActive(false);
            accidentFlashback.gameObject.SetActive(true);
        }
    }
}