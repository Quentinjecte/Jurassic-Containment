using UnityEngine;

/// <summary>
/// Oscillation procédurale (style caméra portée) pour renforcer la tension.
/// À placer sur le Transform d'une CinemachineCamera ou sur un enfant "ShakePivot".
/// </summary>
public class CinematicCameraShake : MonoBehaviour
{
    [SerializeField] private bool activeOnStart;
    [SerializeField] private float positionAmplitude = 0.04f;
    [SerializeField] private float rotationAmplitude = 0.6f;
    [SerializeField] private float frequency = 12f;
    [SerializeField] private float dampingSpeed = 3f;

    private Vector3 _restLocalPosition;
    private Quaternion _restLocalRotation;
    private float _intensity;
    private float _seed;

    public bool IsActive => _intensity > 0.001f;

    private void Awake()
    {
        _restLocalPosition = transform.localPosition;
        _restLocalRotation = transform.localRotation;
        _seed = Random.Range(0f, 100f);
        _intensity = activeOnStart ? 1f : 0f;
    }

    private void OnEnable()
    {
        _restLocalPosition = transform.localPosition;
        _restLocalRotation = transform.localRotation;
    }

    private void LateUpdate()
    {
        _intensity = Mathf.MoveTowards(_intensity, activeOnStart ? 1f : 0f, dampingSpeed * Time.deltaTime);

        if (_intensity <= 0.001f)
        {
            transform.localPosition = _restLocalPosition;
            transform.localRotation = _restLocalRotation;
            return;
        }

        float t = Time.time * frequency + _seed;
        Vector3 posOffset = new Vector3(
            (Mathf.PerlinNoise(t, 0f) - 0.5f) * 2f,
            (Mathf.PerlinNoise(0f, t) - 0.5f) * 2f,
            (Mathf.PerlinNoise(t, t) - 0.5f) * 2f
        ) * positionAmplitude * _intensity;

        Vector3 rotOffset = new Vector3(
            (Mathf.PerlinNoise(t + 1f, 0f) - 0.5f) * 2f,
            (Mathf.PerlinNoise(0f, t + 2f) - 0.5f) * 2f,
            (Mathf.PerlinNoise(t + 3f, t + 4f) - 0.5f) * 2f
        ) * rotationAmplitude * _intensity;

        transform.localPosition = _restLocalPosition + posOffset;
        transform.localRotation = _restLocalRotation * Quaternion.Euler(rotOffset);
    }

    public void SetActive(bool active)
    {
        Debug.Log($"CinematicCameraShake.SetActive({active}) called on {gameObject.name}");
        activeOnStart = active;
    }

    public void SetIntensity(float intensity)
    {
        _intensity = Mathf.Clamp01(intensity);
        activeOnStart = _intensity > 0.001f;
    }

    public void Pulse(float peakIntensity, float decaySpeed = 6f)
    {
        _intensity = peakIntensity;
        dampingSpeed = decaySpeed;
        activeOnStart = false;
    }
}
