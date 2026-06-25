using System.Collections;
using UnityEngine;


public class LoadingOverlay : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private float fadIn = .5f;
    [SerializeField] private float fadOut = .5f;

    public IEnumerator FadeIn()
    {
        yield return FadeTo(1f, fadIn);
    }
    public IEnumerator FadeOut()
    {
        yield return FadeTo(0f, fadOut);
    }

    private IEnumerator FadeTo(float targatAlpha, float duration)
    {
        float starAlpha = canvasGroup.alpha;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            canvasGroup.alpha = Mathf.Lerp(starAlpha, targatAlpha, t);
            yield return null;
        }
        canvasGroup.alpha = targatAlpha;
    }
}
