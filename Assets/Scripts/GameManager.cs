using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public int accidentVisit = 1;
    public bool returningFromFlashback = false;

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