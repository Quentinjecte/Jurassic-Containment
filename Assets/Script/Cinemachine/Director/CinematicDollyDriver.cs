using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// Contrôle la vitesse d'un CinemachineSplineDolly (CM3) pour les plans travelling.
/// </summary>
[RequireComponent(typeof(CinemachineSplineDolly))]
public class CinematicDollyDriver : MonoBehaviour
{
    [SerializeField] private float travelSpeed = 0.08f;
    [SerializeField] private bool autoStart;

    private CinemachineSplineDolly _dolly;
    private SplineAutoDolly.FixedSpeed _fixedSpeed;

    public float TravelSpeed
    {
        get => travelSpeed;
        set => travelSpeed = value;
    }

    public bool IsMoving => _fixedSpeed != null && _fixedSpeed.Speed > 0.001f;

    private void Awake()
    {
        _dolly = GetComponent<CinemachineSplineDolly>();
        CacheFixedSpeed();

        if (autoStart)
            StartDolly();
        else
            StopDolly();
    }

    public void StartDolly(float? speedOverride = null)
    {
        CacheFixedSpeed();
        if (_fixedSpeed == null)
            return;

        _dolly.AutomaticDolly.Enabled = true;
        _fixedSpeed.Speed = speedOverride ?? travelSpeed;
    }

    public void StopDolly()
    {
        CacheFixedSpeed();
        if (_fixedSpeed != null)
            _fixedSpeed.Speed = 0f;
    }

    public void ResetToStart()
    {
        StopDolly();
        _dolly.CameraPosition = 0f;
    }

    private void CacheFixedSpeed()
    {
        if (_dolly == null)
            _dolly = GetComponent<CinemachineSplineDolly>();

        if (_dolly.AutomaticDolly.Method is SplineAutoDolly.FixedSpeed fixedSpeed)
            _fixedSpeed = fixedSpeed;
    }
}
