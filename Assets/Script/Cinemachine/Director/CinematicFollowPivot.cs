using UnityEngine;

/// <summary>
/// Pivot caméra qui suit une cible avec offset (plans bas sur les pieds).
/// </summary>
public class CinematicFollowPivot : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 worldOffset = new Vector3(-1.2f, 0.35f, -2f);
    [SerializeField] private Transform lookAtTarget;
    [SerializeField] private float positionDamping = 5f;
    [SerializeField] private float rotationDamping = 6f;
    [SerializeField] private bool lockYToTarget;

    public Transform Target
    {
        get => target;
        set => target = value;
    }

    public Transform LookAtTarget
    {
        get => lookAtTarget;
        set => lookAtTarget = value;
    }

    private void LateUpdate()
    {
        if (target == null)
            return;

        Vector3 desiredPosition = target.position + worldOffset;
        if (lockYToTarget)
            desiredPosition.y = target.position.y + worldOffset.y;

        transform.position = Vector3.Lerp(transform.position, desiredPosition, positionDamping * Time.deltaTime);

        Transform lookTarget = lookAtTarget != null ? lookAtTarget : target;
        Quaternion desiredRotation = Quaternion.LookRotation(lookTarget.position - transform.position, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, rotationDamping * Time.deltaTime);
    }
}
