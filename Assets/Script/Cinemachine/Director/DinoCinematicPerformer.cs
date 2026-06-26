using System.Collections;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

/// <summary>
/// Joue des clips d'animation directement (PlayableGraph) et déplace le dinosaure le long de waypoints.
/// Indépendant de l'état de l'Animator Controller — idéal pour les cinématiques.
/// </summary>
[RequireComponent(typeof(Animator))]
public class DinoCinematicPerformer : MonoBehaviour
{
    [Header("Déplacement")]
    [SerializeField] private float moveSpeed = 1.8f;
    [SerializeField] private float rotationSpeed = 4f;
    [SerializeField] private bool faceMovementDirection = true;

    private Animator _animator;
    private PlayableGraph _graph;
    private AnimationClipPlayable _clipPlayable;
    private Coroutine _moveRoutine;

    public bool IsMoving => _moveRoutine != null;
    public Animator Animator => _animator;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
    }

    private void OnDisable()
    {
        StopClip();
        StopMovement();
    }

    private void OnDestroy()
    {
        StopClip();
    }

    public void PlayClip(AnimationClip clip, bool loop = true, float speed = 1f, bool useRootMotion = false)
    {
        if (clip == null)
        {
            Debug.LogWarning($"{name}: clip d'animation manquant.", this);
            return;
        }

        StopClip();
        _animator.applyRootMotion = useRootMotion;

        _graph = PlayableGraph.Create($"{name}_Cinematic");
        _graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);

        var output = AnimationPlayableOutput.Create(_graph, "Animation", _animator);
        _clipPlayable = AnimationClipPlayable.Create(_graph, clip);
        _clipPlayable.SetSpeed(speed);

        if (loop)
            _clipPlayable.SetDuration(double.PositiveInfinity);
        else
            _clipPlayable.SetDuration(clip.length);

        output.SetSourcePlayable(_clipPlayable);
        _graph.Play();
    }

    public void StopClip()
    {
        if (!_graph.IsValid())
            return;

        _graph.Stop();
        _graph.Destroy();
    }

    public void MoveTo(Transform target, float? speedOverride = null)
    {
        if (target == null)
            return;

        MoveAlong(new[] { target.position }, speedOverride);
    }

    public void MoveAlong(Vector3[] waypoints, float? speedOverride = null)
    {
        StopMovement();
        _moveRoutine = StartCoroutine(MoveRoutine(waypoints, speedOverride ?? moveSpeed));
    }

    public void MoveAlong(Transform[] waypoints, float? speedOverride = null)
    {
        if (waypoints == null || waypoints.Length == 0)
            return;

        var points = new Vector3[waypoints.Length];
        for (int i = 0; i < waypoints.Length; i++)
            points[i] = waypoints[i] != null ? waypoints[i].position : transform.position;

        MoveAlong(points, speedOverride);
    }

    public void StopMovement()
    {
        if (_moveRoutine != null)
        {
            StopCoroutine(_moveRoutine);
            _moveRoutine = null;
        }
    }

    public void SnapTo(Transform target)
    {
        StopMovement();
        if (target == null)
            return;

        transform.SetPositionAndRotation(target.position, target.rotation);
    }

    public void SetActive(bool active)
    {
        gameObject.SetActive(active);
    }

    private IEnumerator MoveRoutine(Vector3[] waypoints, float speed)
    {
        foreach (Vector3 waypoint in waypoints)
        {
            while (Vector3.Distance(transform.position, waypoint) > 0.05f)
            {
                Vector3 direction = (waypoint - transform.position).normalized;
                transform.position += direction * (speed * Time.deltaTime);

                if (faceMovementDirection && direction.sqrMagnitude > 0.001f)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
                }

                yield return null;
            }

            transform.position = waypoint;
        }

        _moveRoutine = null;
    }
}
