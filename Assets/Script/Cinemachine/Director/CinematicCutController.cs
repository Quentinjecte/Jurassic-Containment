using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Gère le slow-motion et le cut brutal final (fade noir instantané).
/// </summary>
public class CinematicCutController : MonoBehaviour
{
    [SerializeField] private CanvasGroup fadeGroup;
    [SerializeField] private float slowMotionScale = 0.15f;
    [SerializeField] private float slowMotionDuration = 0.85f;
    [SerializeField] private bool useUnscaledTimeForSlowMo = true;

    private Coroutine _slowMoRoutine;
    private float _defaultTimeScale = 1f;

    private void Awake()
    {
        if (fadeGroup != null)
            fadeGroup.alpha = 0f;
    }

    public void TriggerSlowMotion()
    {
        if (_slowMoRoutine != null)
            StopCoroutine(_slowMoRoutine);

        _slowMoRoutine = StartCoroutine(SlowMotionRoutine());
    }

    public void HardCutToBlack()
    {
        if (_slowMoRoutine != null)
            StopCoroutine(_slowMoRoutine);

        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;

        if (fadeGroup != null)
            fadeGroup.alpha = 1f;
    }

    public void ResetCut()
    {
        if (_slowMoRoutine != null)
            StopCoroutine(_slowMoRoutine);

        Time.timeScale = _defaultTimeScale;
        Time.fixedDeltaTime = 0.02f * _defaultTimeScale;

        if (fadeGroup != null)
            fadeGroup.alpha = 0f;
    }

    private IEnumerator SlowMotionRoutine()
    {
        _defaultTimeScale = Time.timeScale;
        Time.timeScale = slowMotionScale;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;

        float elapsed = 0f;
        while (elapsed < slowMotionDuration)
        {
            elapsed += useUnscaledTimeForSlowMo ? Time.unscaledDeltaTime : Time.deltaTime;
            yield return null;
        }

        _slowMoRoutine = null;
    }
}
