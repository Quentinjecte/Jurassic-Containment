using System;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Définition d'un plan de la cinématique Raptor Attack.
/// Chaque entrée correspond à une CinemachineCamera (CM3) activée par priorité.
/// </summary>
[Serializable]
public class CinematicShotDefinition
{
    public enum ShotId
    {
        OpeningLandscape = 0,
        PachyFeetWalk = 1,
        RaptorFeetRun = 2,
        PachyDrink = 3,
        PachyAlert = 4,
        EncirclementA = 5,
        EncirclementB = 6,
        EncirclementC = 7,
        FinalAttack = 8
    }

    [Tooltip("Identifiant du plan (ordre narratif).")]
    public ShotId shotId;

    [Tooltip("Nom affiché dans l'Inspector / debug.")]
    public string displayName;

    [Tooltip("Caméra Cinemachine (CM3) à activer pour ce plan.")]
    public CinemachineCamera virtualCamera;

    [Tooltip("Durée du plan en secondes (temps réel, hors slow-motion).")]
    [Min(0.1f)]
    public float duration = 5f;

    [Tooltip("Durée du blend Cinemachine Brain entrant vers ce plan.")]
    [Min(0f)]
    public float blendInDuration = 1.5f;

    [Tooltip("Style de transition entrant.")]
    public CinemachineBlendDefinition.Styles blendStyle = CinemachineBlendDefinition.Styles.EaseInOut;

    [Tooltip("Événement déclenché au début du plan.")]
    public UnityEvent onShotStart;

    [Tooltip("Événement déclenché à la fin du plan.")]
    public UnityEvent onShotEnd;
}
