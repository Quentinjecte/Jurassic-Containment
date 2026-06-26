using UnityEngine;

/// <summary>
/// Fait monter progressivement l'offset vertical d'une caméra (plan Alerte).
/// Pilote le Transform local d'un pivot caméra ou un offset externe.
/// </summary>
public class CinematicCameraHeightDriver : MonoBehaviour
{
    [SerializeField] private Transform pivot;
    [SerializeField] private float startHeight = 0.35f;
    [SerializeField] private float endHeight = 1.4f;
    [SerializeField] private float duration = 1.2f;
    [SerializeField] private AnimationCurve curve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private float _elapsed;
    private bool _playing;
    private Vector3 _baseLocalPosition;

    private void Awake()
    {
        if (pivot == null)
            pivot = transform;

        _baseLocalPosition = pivot.localPosition;
        ApplyHeight(startHeight);
    }

    private void Update()
    {
        if (!_playing)
            return;

        _elapsed += Time.deltaTime;
        float t = duration <= 0f ? 1f : Mathf.Clamp01(_elapsed / duration);
        ApplyHeight(Mathf.Lerp(startHeight, endHeight, curve.Evaluate(t)));

        if (t >= 1f)
            _playing = false;
    }

    public void Play()
    {
        _elapsed = 0f;
        _playing = true;
        ApplyHeight(startHeight);
    }

    public void ResetHeight()
    {
        _playing = false;
        _elapsed = 0f;
        ApplyHeight(startHeight);
    }

    private void ApplyHeight(float height)
    {
        pivot.localPosition = new Vector3(_baseLocalPosition.x, height, _baseLocalPosition.z);
    }
}
