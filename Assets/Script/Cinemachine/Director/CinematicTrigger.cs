using UnityEngine;

/// <summary>
/// Déclenche la cinématique au contact d'un collider (trigger) ou via appel externe.
/// </summary>
[RequireComponent(typeof(Collider))]
public class CinematicTrigger : MonoBehaviour
{
    [SerializeField] private RaptorAttackCinematicDirector director;
    [SerializeField] private bool playOnce = true;
    [SerializeField] private string requiredTag = "Player";

    private bool _hasPlayed;

    private void Reset()
    {
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_hasPlayed && playOnce)
            return;

        if (!string.IsNullOrEmpty(requiredTag) && !other.CompareTag(requiredTag))
            return;

        if (director == null)
            director = FindFirstObjectByType<RaptorAttackCinematicDirector>();

        if (director == null)
            return;

        director.PlayCinematic();
        _hasPlayed = true;
    }
}
