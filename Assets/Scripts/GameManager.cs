using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public int accidentVisit = 1;
    public bool returningFromFlashback = false;

    public bool playFinalCutscene = false;

    // Posições salvas antes do flashback
    public Vector3 noahPositionBeforeFlashback;
    public Quaternion noahRotationBeforeFlashback;
    public Vector3 emilyPositionBeforeFlashback;
    public Quaternion emilyRotationBeforeFlashback;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            accidentVisit = 1;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void CreateInstance()
    {
        if (Instance == null)
        {
            GameObject go = new GameObject("GameManager");
            go.AddComponent<GameManager>();
        }
    }
}