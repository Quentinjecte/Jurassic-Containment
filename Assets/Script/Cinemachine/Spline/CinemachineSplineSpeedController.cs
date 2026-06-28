using System;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Splines;
using Assets.Script.Utils;

public class CinemachineSplineSpeedController : MonoBehaviour
{
    public enum SpeedMode
    {
        ConstantSpeed,
        PerKnotSpeed,
        CurveControlled
    }

    [Header("References")]
    [SerializeField] private CinemachineSplineDolly splineDolly;
    [SerializeField] private SplineContainer splineContainer;

    [Header("Speed Settings")]
    public SpeedMode speedMode = SpeedMode.ConstantSpeed;

    [Tooltip("Vitesse constante si SpeedMode = ConstantSpeed")]
    public float constantSpeed = 1f;

    [Tooltip("Courbe d'accélération si SpeedMode = CurveControlled")]
    public AnimationCurve speedCurve = AnimationCurve.Linear(0, 1, 1, 1);

    [Tooltip("Interpolation")]
    public bool interpolate = false;
    public float starSpeed = 1f;
    public float endSpeed = 1f;

    [Tooltip("Vitesse par Knot si SpeedMode = PerKnotSpeed")]
    public List<KnotSpeed> knotSpeeds = new List<KnotSpeed>();

    private KnotLinkCollection knotLinks;
/*
    private void Awake()
    {
        if (splineDolly == null)
            splineDolly = GetComponent<CinemachineSplineDolly>();

        splineContainer = splineDolly.Spline;
        RefreshKnotList();
    }*/

    /*private void OnValidate()
    {
        if (splineDolly == null)
            return;

        splineContainer = splineDolly.Spline;
        RefreshKnotList();
    }*/

    /// <summary>
    /// Synchronise la liste knotSpeeds avec les knots du spline.
    /// </summary>
    public void RefreshKnotList()
    {
        var splineDollyComponent = ComponentExtensions.GetComponentSafe<CinemachineSplineDolly>(this, "CinemachineSplineSpeedController: splineDolly is null. Please assign a CinemachineSplineDolly component.");
        if (splineDollyComponent != null)
        {
            Debug.LogWarning($"Warning the component of type {splineDollyComponent} is auto-assigned. Be careful!");
            splineDolly = splineDollyComponent;
        }
        else
            return;

        var splineContainerComponent = splineDolly.Spline;
        if (splineContainerComponent != null)
        {
            Debug.LogWarning($"Warning the component of type {splineContainerComponent} is auto-assigned. Be careful!");
            splineContainer = splineContainerComponent;
        }
        else
            return;

        //knotLinks = splineContainer.Splines.;
        // Resize propre
        if (knotSpeeds.Count != splineContainer.Spline.Count)
        {
            knotSpeeds.Clear();
            for (int i = 0; i < splineContainer.Spline.Count; i++)
                knotSpeeds.Add(new KnotSpeed(i, 1f));
        }
    }

    private void Update()
    {
        if (splineDolly == null || splineContainer == null)
            return;

        float t = splineDolly.CameraPosition; // Dolly position (0 → 1)
        float speed = GetSpeedAt(t);

        if (splineDolly.AutomaticDolly.Method is SplineAutoDolly.FixedSpeed fixedSpeed)
            if (interpolate)
                fixedSpeed.Speed = Mathf.Lerp(fixedSpeed.Speed, speed, Time.deltaTime);
            else 
                fixedSpeed.Speed = speed;
    }

    /// <summary>
    /// Retourne la vitesse selon le mode choisi.
    /// </summary>
    private float GetSpeedAt(float t)
    {
        switch (speedMode)
        {
            case SpeedMode.ConstantSpeed:
                return constantSpeed;

            case SpeedMode.CurveControlled:
                return speedCurve.Evaluate(t);

            case SpeedMode.PerKnotSpeed:
                return GetPerKnotSpeed(t);

            default:
                return 1f;
        }
    }

    /// <summary>
    /// Retourne la vitesse du knot correspondant à t.
    /// </summary>
    private float GetPerKnotSpeed(float t)
    {
        int knotIndex = (int)splineDolly.SplineSettings.Position;

        if (knotIndex < 0 || knotIndex >= knotSpeeds.Count)
            return constantSpeed;

        return knotSpeeds[knotIndex].Speed;
    }
}

[Serializable]
public class KnotSpeed
{
    public int KnotIndex;
    public float Speed;

    public KnotSpeed(int index, float speed)
    {
        KnotIndex = index;
        Speed = speed;
    }
}
