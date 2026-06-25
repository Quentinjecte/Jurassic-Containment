using Assets.Script.Player;
using GlobalEnum;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// IA du Pachycephalosaurus — Jurassic Containment
/// Classe C (★☆☆) — Herbivore territorial
/// States : PASSIF → ALERTE → AGRESSIF → BLESSE_ENRAGE
///
/// Prérequis :
///   - NavMeshAgent sur le GameObject
///   - Animator avec les paramètres définis plus bas
///   - Une référence au script de santé du dino (DinoHealth)
///   - Layer "Player" sur les joueurs
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
public class PachycephalosaurusAI : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  ÉTATS
    // ─────────────────────────────────────────────
    [Header("État")]
    public DinoState _dinoState;
    public MovementState _movementState;
    // ─────────────────────────────────────────────
    //  STATS & TUNING (modifiables dans l'Inspector)
    // ─────────────────────────────────────────────
    [Header("Détection")]
    [Tooltip("Rayon de détection visuelle en état Passif")]
    public float detectionRadius   = 18f;
    [Tooltip("Angle de vision (demi-angle, degré)")]
    public float detectionAngle    = 70f;
    [Tooltip("Rayon d'ouïe (pas besoin de ligne de vue)")]
    public float hearingRadius     = 10f;

    [Header("Déplacements")]
    public float speedPassif       = 2.5f;
    public float speedAlerte       = 3.5f;
    public float speedAgressif     = 6f;
    public float speedEnrage       = 9f;

    [Header("Combat")]
    [Tooltip("Distance à partir de laquelle le Pachy déclenche une charge")]
    public float chargeStartDist   = 14f;
    [Tooltip("Distance à laquelle la charge inflige des dégâts")]
    public float chargeImpactDist  = 1.8f;
    public float chargeDamage      = 30f;
    public float chargeCooldown    = 5f;
    [Tooltip("Dégâts de morsure/coup de tête au corps-à-corps")]
    public float meleeDamage       = 15f;
    public float meleeRange        = 2f;
    public float meleeCooldown     = 2f;

    [Header("Territoire")]
    [Tooltip("Rayon de patrouille autour du point de spawn")]
    public float patrolRadius      = 25f;
    [Tooltip("Distance max avant de retourner au territoire")]
    public float territoryMaxDist  = 50f;

    [Header("Santé & Rage")]
    [Tooltip("% de PV en dessous duquel le Pachy entre en mode Enragé")]
    [Range(0f, 1f)]
    public float enrageThreshold   = 0.3f;
    [Tooltip("% de PV pour tenter la fuite (avant la rage)")]
    [Range(0f, 1f)]
    public float fleeThreshold     = 0.5f;

    [Header("Fuite")]
    [Tooltip("Distance de fuite avant de s'arrêter")]
    public float fleeDistance      = 20f;
    public float fleeCooldown      = 8f;

    // ─────────────────────────────────────────────
    //  RÉFÉRENCES INTERNES
    // ─────────────────────────────────────────────
    NavMeshAgent    _agent;
    Animator        _anim;
    DinoHealth      _health;          // Script de santé à créer/adapter
    Transform       _target;          // Joueur le plus proche détecté

    Vector3         _spawnPoint;
    Vector3         _patrolTarget;
    float           _chargeTimer;
    float           _meleeTimer;
    float           _fleeTimer;
    bool            _isCharging;
    bool            _hasFled;

    // Paramètres Animator (à créer dans l'Animator Controller)
    static readonly int AnimSpeed      = Animator.StringToHash("Vélocity");
    static readonly int AnimIsCharging = Animator.StringToHash("IsCharging");
    static readonly int AnimIsHurt     = Animator.StringToHash("IsHurt");
    static readonly int AnimDead       = Animator.StringToHash("IsDead");
    static readonly int Moving         = Animator.StringToHash("Moving");
    private static readonly int Sprinting = Animator.StringToHash("Sprinting");


    // ─────────────────────────────────────────────
    //  INIT
    // ─────────────────────────────────────────────
    void Awake()
    {
        _agent  = GetComponent<NavMeshAgent>();
        _anim   = GetComponent<Animator>();
        _health = GetComponent<DinoHealth>();
        _spawnPoint   = transform.position;
        _patrolTarget = GetPatrolPoint();
    }

    void Start()
    {
        SetState(DinoState.Passif);
    }

    // ─────────────────────────────────────────────
    //  BOUCLE PRINCIPALE
    // ─────────────────────────────────────────────
    void Update()
    {
        if (_health != null && _health.IsDead) return;

        UpdateTimers();
        CheckHealthThresholds();

        switch (_dinoState)
        {
            case DinoState.Passif:        UpdatePassif();       break;
            case DinoState.Alerte:        UpdateAlerte();       break;
            case DinoState.Agressif:      UpdateAgressif();     break;
            case DinoState.BlesseEnrage:  UpdateBlesseEnrage(); break;
        }

        // Sync Animator
        UpdateAnimatorParams();
    }
    public bool IsMoving() => !Mathf.Approximately(_agent.velocity.magnitude, 0f);

    private void UpdateAnimatorParams()
    {
        _anim.SetFloat(AnimSpeed, _agent.velocity.magnitude);
        //_anim.SetBool(InAir, !isGrounded);
        _anim.SetBool(Moving, IsMoving());

        float sprintWeight = _anim.GetFloat(Sprinting);
        //float t = Mathf.Lerp(0, 1,);
        sprintWeight = Mathf.Lerp(sprintWeight, _movementState == MovementState.Sprinting ? 1f : 0f, Time.deltaTime);
        _anim.SetFloat(AnimSpeed, sprintWeight);

        /*        _inputController.SetValue(FPSANames.MoveInput,
                    new Vector4(AnimatorVelocity.x, AnimatorVelocity.y));*/
    }

    void UpdateTimers()
    {
        if (_chargeTimer > 0) _chargeTimer -= Time.deltaTime;
        if (_meleeTimer  > 0) _meleeTimer  -= Time.deltaTime;
        if (_fleeTimer   > 0) _fleeTimer   -= Time.deltaTime;
    }

    // ─────────────────────────────────────────────
    //  VÉRIFICATION SANTÉ → CHANGEMENT D'ÉTAT
    // ─────────────────────────────────────────────
    void CheckHealthThresholds()
    {
        if (_health == null) return;
        float ratio = _health.CurrentHP / _health.MaxHP;

        if (ratio <= enrageThreshold && _dinoState != DinoState.BlesseEnrage)
        {
            SetState(DinoState.BlesseEnrage);
            return;
        }

        // Fuite uniquement si Classe C et pas encore en rage
        if (ratio <= fleeThreshold
            && _dinoState == DinoState.Agressif
            && !_hasFled
            && _fleeTimer <= 0)
        {
            StartCoroutine(Flee());
        }
    }

    // ─────────────────────────────────────────────
    //  ÉTAT 1 — PASSIF
    //  Le Pachy chasse sa propre nourriture, patrouille.
    // ─────────────────────────────────────────────
    void UpdatePassif()
    {
        _agent.speed = speedPassif;

        // Patrouille sur le territoire
        if (!_agent.pathPending && _agent.remainingDistance < 1f)
        {
            _patrolTarget = GetPatrolPoint();
            _agent.SetDestination(_patrolTarget);
        }

        // Détection
        if (DetectPlayer())
            SetState(DinoState.Alerte);
    }

    // ─────────────────────────────────────────────
    //  ÉTAT 2 — ALERTE
    //  A détecté une présence, observe, se tourne vers la cible.
    // ─────────────────────────────────────────────
    void UpdateAlerte()
    {
        _agent.speed = speedAlerte;
        _agent.ResetPath();

        if (_target == null)
        {
            SetState(DinoState.Passif);
            return;
        }

        // Regarde la cible
        LookAt(_target.position);

        float dist = Vector3.Distance(transform.position, _target.position);

        // Si la cible s'approche trop → attaque
        if (dist < chargeStartDist)
        {
            SetState(DinoState.Agressif);
            return;
        }

        // Si la cible s'éloigne et disparaît de la vue → retour Passif
        if (!DetectPlayer())
        {
            StartCoroutine(LostTargetDelay());
        }
    }

    // ─────────────────────────────────────────────
    //  ÉTAT 3 — AGRESSIF
    //  Attaque : alterne charge crânienne et mêlée.
    // ─────────────────────────────────────────────
    void UpdateAgressif()
    {
        if (_target == null) { SetState(DinoState.Passif); return; }

        _agent.speed = speedAgressif;
        float dist = Vector3.Distance(transform.position, _target.position);

        // Retour territoire si trop loin
        if (dist > territoryMaxDist)
        {
            _agent.SetDestination(_spawnPoint);
            SetState(DinoState.Passif);
            return;
        }

        // Charge crânienne (signature du Pachycephalosaurus)
        if (!_isCharging && dist <= chargeStartDist && _chargeTimer <= 0)
        {
            StartCoroutine(HeadCharge());
            return;
        }

        // Mêlée au corps à corps
        if (dist <= meleeRange && _meleeTimer <= 0)
        {
            MeleeAttack();
            return;
        }

        // Poursuit la cible
        _agent.SetDestination(_target.position);
    }

    // ─────────────────────────────────────────────
    //  ÉTAT 4 — BLESSÉ-ENRAGÉ
    //  Mode berserk : plus rapide, charge en continu, ignore la fuite.
    // ─────────────────────────────────────────────
    void UpdateBlesseEnrage()
    {
        if (_target == null)
        {
            // Si plus de cible visible, cherche les joueurs autour
            _target = FindNearestPlayer();
            if (_target == null) { SetState(DinoState.Passif); return; }
        }

        _agent.speed = speedEnrage;
        float dist = Vector3.Distance(transform.position, _target.position);

        _anim.SetBool(AnimIsHurt, true);

        // Charge en continu, cooldown réduit de moitié
        if (!_isCharging && _chargeTimer <= 0)
        {
            StartCoroutine(HeadCharge(reducedCooldown: true));
            return;
        }

        if (dist <= meleeRange && _meleeTimer <= 0)
        {
            MeleeAttack();
            return;
        }

        _agent.SetDestination(_target.position);
    }

    // ─────────────────────────────────────────────
    //  ACTIONS : CHARGE CRÂNIENNE
    // ─────────────────────────────────────────────
    IEnumerator HeadCharge(bool reducedCooldown = false)
    {
        if (_target == null) yield break;
        _isCharging = true;
        _anim.SetBool(AnimIsCharging, true);

        // Télégraphe : bref arrêt avant la charge (0.6s)
        _agent.ResetPath();
        LookAt(_target.position);
        yield return new WaitForSeconds(0.6f);

        // Charge
        Vector3 chargeDir = (_target.position - transform.position).normalized;
        float   elapsed   = 0f;
        float   duration  = 0.8f;

        while (elapsed < duration)
        {
            _agent.Move(chargeDir * speedEnrage * Time.deltaTime);
            elapsed += Time.deltaTime;

            // Impact
            if (Vector3.Distance(transform.position, _target.position) < chargeImpactDist)
            {
                ApplyDamageToTarget(chargeDamage);
                break;
            }
            yield return null;
        }

        _isCharging = false;
        _anim.SetBool(AnimIsCharging, false);
        _chargeTimer = reducedCooldown ? chargeCooldown * 0.5f : chargeCooldown;
    }

    // ─────────────────────────────────────────────
    //  ACTIONS : MÊLÉE
    // ─────────────────────────────────────────────
    void MeleeAttack()
    {
        LookAt(_target.position);
        ApplyDamageToTarget(meleeDamage);
        _meleeTimer = meleeCooldown;
    }

    // ─────────────────────────────────────────────
    //  ACTIONS : FUITE (Classe C — instinct de survie)
    // ─────────────────────────────────────────────
    IEnumerator Flee()
    {
        _hasFled = true;
        _fleeTimer = fleeCooldown;

        Vector3 fleeDir = (transform.position - _target.position).normalized;
        Vector3 fleeDest = transform.position + fleeDir * fleeDistance;

        if (NavMesh.SamplePosition(fleeDest, out NavMeshHit hit, 5f, NavMesh.AllAreas))
        {
            _agent.speed = speedEnrage;
            _agent.SetDestination(hit.position);
        }

        yield return new WaitForSeconds(3f);
        _hasFled = false;

        // Reprend l'attaque si le joueur est encore là
        if (_target != null)
            SetState(DinoState.Agressif);
    }

    // ─────────────────────────────────────────────
    //  DÉTECTION
    // ─────────────────────────────────────────────
    bool DetectPlayer()
    {
        // Vision en cône
        Collider[] cols = Physics.OverlapSphere(transform.position, detectionRadius, LayerMask.GetMask("Player"));
        foreach (var col in cols)
        {
            Vector3 dir = (col.transform.position - transform.position).normalized;
            float angle = Vector3.Angle(transform.forward, dir);

            if (angle < detectionAngle)
            {
                // Ligne de vue
                if (!Physics.Raycast(transform.position + Vector3.up, dir,
                    Vector3.Distance(transform.position, col.transform.position),
                    LayerMask.GetMask("Default")))
                {
                    _target = col.transform;
                    return true;
                }
            }
        }

        // Ouïe (pas de ligne de vue requise)
        Collider[] heard = Physics.OverlapSphere(transform.position, hearingRadius, LayerMask.GetMask("Player"));
        if (heard.Length > 0)
        {
            _target = heard[0].transform;
            return true;
        }

        return false;
    }

    Transform FindNearestPlayer()
    {
        Collider[] cols = Physics.OverlapSphere(transform.position, detectionRadius * 2f, LayerMask.GetMask("Player"));
        Transform nearest = null;
        float minDist = float.MaxValue;
        foreach (var c in cols)
        {
            float d = Vector3.Distance(transform.position, c.transform.position);
            if (d < minDist) { minDist = d; nearest = c.transform; }
        }
        return nearest;
    }

    // ─────────────────────────────────────────────
    //  UTILITAIRES
    // ─────────────────────────────────────────────
    void SetState(DinoState newState)
    {
        _dinoState = newState;
        // Hook utile pour le debug : Debug.Log($"[Pachy] → {newState}");
    }

    Vector3 GetPatrolPoint()
    {
        Vector3 rand = _spawnPoint + Random.insideUnitSphere * patrolRadius;
        rand.y = _spawnPoint.y;
        if (NavMesh.SamplePosition(rand, out NavMeshHit hit, 5f, NavMesh.AllAreas))
            return hit.position;
        return _spawnPoint;
    }

    void LookAt(Vector3 pos)
    {
        Vector3 dir = (pos - transform.position).normalized;
        dir.y = 0;
        if (dir != Vector3.zero)
            transform.rotation = Quaternion.Slerp(
                transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 5f);
    }

    void ApplyDamageToTarget(float dmg)
    {
        if (_target == null) return;
        var hp = _target.GetComponent<PlayerCondition>(); // À adapter à votre script joueur
        hp?.Damage(dmg);
    }

    IEnumerator LostTargetDelay()
    {
        yield return new WaitForSeconds(4f);
        if (!DetectPlayer())
        {
            _target = null;
            SetState(DinoState.Passif);
        }
    }

    // ─────────────────────────────────────────────
    //  ÉVÉNEMENT MORT (appelé par DinoHealth)
    // ─────────────────────────────────────────────
    public void OnDeath()
    {
        _agent.ResetPath();
        _agent.enabled = false;
        _anim.SetTrigger(AnimDead);
        enabled = false;
    }

    // Gizmos de debug dans l'éditeur
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, hearingRadius);
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(_spawnPoint == Vector3.zero ? transform.position : _spawnPoint, patrolRadius);
    }
}
