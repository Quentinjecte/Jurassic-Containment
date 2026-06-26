using UnityEngine;

/// <summary>
/// Fait orbiter un pivot caméra autour d'une cible (plan Encerclement).
/// </summary>
public class CinematicOrbitalDriver : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float radius = 6f;
    [SerializeField] private float height = 0.6f;
    [SerializeField] private float degreesPerSecond = 35f;
    [SerializeField] private float startAngle;
    [SerializeField] private bool lookAtTarget = true;

    private bool _active;
    private float _currentAngle;

    public Transform Target
    {
        get => target;
        set => target = value;
    }

    private void Start()
    {
        _currentAngle = startAngle;
        ApplyOrbit();
    }

    private void Update()
    {
        if (!_active || target == null)
            return;

        _currentAngle += degreesPerSecond * Time.deltaTime;
        ApplyOrbit();
    }

    public void Play(float? angleOverride = null)
    {
        if (angleOverride.HasValue)
            _currentAngle = angleOverride.Value;

        _active = true;
        ApplyOrbit();
    }

    public void Stop()
    {
        _active = false;
    }

    public void SnapToAngle(float angleDegrees)
    {
        _currentAngle = angleDegrees;
        ApplyOrbit();
    }

    private void ApplyOrbit()
    {
        if (target == null)
            return;

        float rad = _currentAngle * Mathf.Deg2Rad;
        Vector3 offset = new Vector3(Mathf.Sin(rad) * radius, height, Mathf.Cos(rad) * radius);
        transform.position = target.position + offset;

        if (lookAtTarget)
            transform.LookAt(target.position + Vector3.up * height * 0.5f);
    }
}
