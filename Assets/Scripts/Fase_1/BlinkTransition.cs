using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class BlinkTransition : MonoBehaviour
{
    public static BlinkTransition Instance;

    [Header("Referências")]
    public Image darknessOverlay;

    void Awake()
    {
        Instance = this;
    }

    public void DoBlink(System.Action onBlackScreen)
    {
        StartCoroutine(BlinkRoutine(onBlackScreen));
    }

    private IEnumerator BlinkRoutine(System.Action onBlackScreen)
    {
        yield return StartCoroutine(Fade(0f, 1f, 0.3f));
        onBlackScreen?.Invoke();
        yield return new WaitForSeconds(0.1f);
        yield return StartCoroutine(Fade(1f, 0f, 0.5f));
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        float elapsed = 0f;
        Color c = darknessOverlay.color;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            c.a = Mathf.Lerp(from, to, elapsed / duration);
            darknessOverlay.color = c;
            yield return null;
        }

        c.a = to;
        darknessOverlay.color = c;
    }
}