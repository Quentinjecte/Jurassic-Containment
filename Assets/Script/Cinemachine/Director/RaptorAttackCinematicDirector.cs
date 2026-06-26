using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Orchestrateur principal de la cinématique « Raptors vs Pachycéphalosaure ».
/// Active les CinemachineCamera (CM3) dans l'ordre avec blends fluides,
/// pilote les animations et déclenche slow-motion + cut final.
/// </summary>
public class RaptorAttackCinematicDirector : MonoBehaviour
{
    [Header("Références")]
    [SerializeField] private CinemachineBrain cinemachineBrain;
    [SerializeField] private CinematicCutController cutController;

    [Header("Acteurs")]
    [SerializeField] private DinoCinematicPerformer pachyPerformer;
    [SerializeField] private DinoCinematicPerformer[] raptorPerformers;
    [SerializeField] private DinoCinematicPerformer leapRaptor;

    [Header("Waypoints Pachy")]
    [SerializeField] private Transform pachyWalkStart;
    [SerializeField] private Transform pachyRiverPoint;

    [Header("Waypoints Raptors")]
    [SerializeField] private Transform[] raptorRunWaypoints;

    [Header("Cibles caméra (pieds / tête)")]
    [SerializeField] private Transform pachyFeetTarget;
    [SerializeField] private Transform pachyHeadTarget;
    [SerializeField] private Transform raptorFeetTarget;
    [SerializeField] private Transform leapTargetPoint;

    [Header("Composants caméra spéciaux")]
    [SerializeField] private CinematicDollyDriver openingDolly;
    [SerializeField] private CinematicCameraShake openingShake;
    [SerializeField] private CinematicCameraShake pachyFeetShake;
    [SerializeField] private CinematicCameraShake raptorFeetShake;
    [SerializeField] private CinematicCameraHeightDriver alertHeightDriver;
    [SerializeField] private CinematicOrbitalDriver[] encircleOrbitals;

    [Header("Clips d'animation")]
    [SerializeField] private AnimationClip pachyWalkClip;
    [SerializeField] private AnimationClip pachyDrinkClip;
    [SerializeField] private AnimationClip pachyAlertClip;
    [SerializeField] private AnimationClip raptorRunClip;
    [SerializeField] private AnimationClip raptorLeapClip;

    [Header("Plans")]
    [SerializeField] private List<CinematicShotDefinition> shots = new();

    [Header("Lecture")]
    [SerializeField] private bool playOnStart;
    [SerializeField] private float delayBeforeStart = 0.5f;
    [SerializeField] private int activeCameraPriority = 100;

    [Header("Événements globaux")]
    public UnityEvent onCinematicStart;
    public UnityEvent onCinematicComplete;
    public UnityEvent onImpactCut;

    private Coroutine _playRoutine;
    private CinemachineCamera _currentCamera;

    public bool IsPlaying { get; private set; }

    private void Start()
    {
        if (playOnStart)
            PlayCinematic();
    }

    private void OnDisable()
    {
        StopCinematic();
    }

    [ContextMenu("Play Cinematic")]
    public void PlayCinematic()
    {
        if (IsPlaying)
            return;

        if (shots == null || shots.Count == 0)
        {
            Debug.LogError("RaptorAttackCinematicDirector: aucun plan configuré.", this);
            return;
        }

        _playRoutine = StartCoroutine(PlayRoutine());
    }

    public void StopCinematic()
    {
        if (_playRoutine != null)
        {
            StopCoroutine(_playRoutine);
            _playRoutine = null;
        }

        IsPlaying = false;
        DeactivateAllCameras();
    }

    private IEnumerator PlayRoutine()
    {
        IsPlaying = true;
        onCinematicStart?.Invoke();

        PrepareScene();
        yield return new WaitForSeconds(delayBeforeStart);

        shots.Sort((a, b) => a.shotId.CompareTo(b.shotId));

        foreach (CinematicShotDefinition shot in shots)
        {
            if (shot.virtualCamera == null)
            {
                Debug.LogWarning($"Plan ignoré (caméra manquante): {shot.displayName}", this);
                continue;
            }

            ApplyBrainBlend(shot);
            ActivateCamera(shot.virtualCamera);
            HandleShotStart(shot);
            shot.onShotStart?.Invoke();

            yield return new WaitForSeconds(shot.duration);

            shot.onShotEnd?.Invoke();
            HandleShotEnd(shot);
        }

        IsPlaying = false;
        onCinematicComplete?.Invoke();
        _playRoutine = null;
    }

    private void PrepareScene()
    {
        cutController?.ResetCut();
        openingDolly?.ResetToStart();

        if (pachyPerformer != null && pachyWalkStart != null)
            pachyPerformer.SnapTo(pachyWalkStart);

        foreach (DinoCinematicPerformer raptor in raptorPerformers)
        {
            if (raptor != null)
                raptor.gameObject.SetActive(false);
        }

        if (leapRaptor != null)
            leapRaptor.gameObject.SetActive(false);

        SetShake(openingShake, false);
        SetShake(pachyFeetShake, false);
        SetShake(raptorFeetShake, false);
        alertHeightDriver?.ResetHeight();

        foreach (CinematicOrbitalDriver orbital in encircleOrbitals)
            orbital?.Stop();

        DeactivateAllCameras();
    }

    private void HandleShotStart(CinematicShotDefinition shot)
    {
        switch (shot.shotId)
        {
            case CinematicShotDefinition.ShotId.OpeningLandscape:
                openingDolly?.StartDolly();
                SetShake(openingShake, true, 0.25f);
                break;

            case CinematicShotDefinition.ShotId.PachyFeetWalk:
                openingDolly?.StopDolly();
                pachyPerformer?.PlayClip(pachyWalkClip, true);
                pachyPerformer?.MoveTo(pachyRiverPoint);
                SetShake(pachyFeetShake, true, 0.35f);
                break;

            case CinematicShotDefinition.ShotId.RaptorFeetRun:
                pachyPerformer?.StopMovement();
                ActivateRaptors(true);
                foreach (DinoCinematicPerformer raptor in raptorPerformers)
                {
                    raptor?.PlayClip(raptorRunClip, true, 1.35f);
                    raptor?.MoveAlong(raptorRunWaypoints, 5.5f);
                }
                SetShake(raptorFeetShake, true, 1f);
                break;

            case CinematicShotDefinition.ShotId.PachyDrink:
                SetShake(raptorFeetShake, false);
                pachyPerformer?.StopMovement();
                pachyPerformer?.PlayClip(pachyDrinkClip, true, 0.85f);
                SetShake(pachyFeetShake, true, 0.2f);
                break;

            case CinematicShotDefinition.ShotId.PachyAlert:
                pachyPerformer?.PlayClip(pachyAlertClip, false);
                alertHeightDriver?.Play();
                SetShake(pachyFeetShake, true, 0.55f);
                break;

            case CinematicShotDefinition.ShotId.EncirclementA:
            case CinematicShotDefinition.ShotId.EncirclementB:
            case CinematicShotDefinition.ShotId.EncirclementC:
                PlayEncirclementShot(shot.shotId);
                break;

            case CinematicShotDefinition.ShotId.FinalAttack:
                StartFinalAttack();
                break;
        }
    }

    private void HandleShotEnd(CinematicShotDefinition shot)
    {
        switch (shot.shotId)
        {
            case CinematicShotDefinition.ShotId.OpeningLandscape:
                openingDolly?.StopDolly();
                SetShake(openingShake, false);
                break;

            case CinematicShotDefinition.ShotId.RaptorFeetRun:
                foreach (DinoCinematicPerformer raptor in raptorPerformers)
                    raptor?.StopMovement();
                break;

            case CinematicShotDefinition.ShotId.EncirclementA:
            case CinematicShotDefinition.ShotId.EncirclementB:
            case CinematicShotDefinition.ShotId.EncirclementC:
                StopEncirclementShot(shot.shotId);
                break;
        }
    }

    private void PlayEncirclementShot(CinematicShotDefinition.ShotId shotId)
    {
        int index = shotId - CinematicShotDefinition.ShotId.EncirclementA;
        if (encircleOrbitals == null || index < 0 || index >= encircleOrbitals.Length)
            return;

        CinematicOrbitalDriver orbital = encircleOrbitals[index];
        orbital?.Play(index * 120f);

        foreach (DinoCinematicPerformer raptor in raptorPerformers)
            raptor?.PlayClip(raptorRunClip, true, 1.1f);
    }

    private void StopEncirclementShot(CinematicShotDefinition.ShotId shotId)
    {
        int index = shotId - CinematicShotDefinition.ShotId.EncirclementA;
        if (encircleOrbitals == null || index < 0 || index >= encircleOrbitals.Length)
            return;

        encircleOrbitals[index]?.Stop();
    }

    private void StartFinalAttack()
    {
        foreach (DinoCinematicPerformer raptor in raptorPerformers)
            raptor?.StopMovement();

        if (leapRaptor != null)
        {
            leapRaptor.gameObject.SetActive(true);

            if (leapTargetPoint != null)
                leapRaptor.SnapTo(leapTargetPoint);

            leapRaptor.PlayClip(raptorLeapClip, false, 1f, useRootMotion: true);
        }

        cutController?.TriggerSlowMotion();
        StartCoroutine(ImpactCutRoutine());
    }

    private IEnumerator ImpactCutRoutine()
    {
        float impactDelay = raptorLeapClip != null ? raptorLeapClip.length * 0.55f : 0.6f;
        yield return new WaitForSecondsRealtime(impactDelay);

        cutController?.HardCutToBlack();
        onImpactCut?.Invoke();
    }

    private void ActivateRaptors(bool active)
    {
        foreach (DinoCinematicPerformer raptor in raptorPerformers)
        {
            if (raptor != null)
                raptor.gameObject.SetActive(active);
        }
    }

    private void ApplyBrainBlend(CinematicShotDefinition shot)
    {
        if (cinemachineBrain == null)
            return;

        cinemachineBrain.DefaultBlend = new CinemachineBlendDefinition(shot.blendStyle, shot.blendInDuration);
    }

    private void ActivateCamera(CinemachineCamera camera)
    {
        if (_currentCamera != null && _currentCamera != camera)
        {
            _currentCamera.Priority = new PrioritySettings { Enabled = false, Value = 0 };
        }

        camera.Priority = new PrioritySettings { Enabled = true, Value = activeCameraPriority };
        _currentCamera = camera;
    }

    private void DeactivateAllCameras()
    {
        foreach (CinematicShotDefinition shot in shots)
        {
            if (shot.virtualCamera == null)
                continue;

            shot.virtualCamera.Priority = new PrioritySettings { Enabled = false, Value = 0 };
        }

        _currentCamera = null;
    }

    private static void SetShake(CinematicCameraShake shake, bool active, float intensity = 1f)
    {
        if (shake == null)
            return;

        shake.SetActive(active);
        if (active)
            shake.SetIntensity(intensity);
    }
}
