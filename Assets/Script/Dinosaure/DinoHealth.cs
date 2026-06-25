using System;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// DinoHealth — Jurassic Containment
/// Gère : PV, jauge de Rage, système de tranquillisant (fenêtre de capture)
///
/// Événements exposés (UnityEvent) :
///   OnDeath         → appelé à la mort du dino
///   OnEnrage        → appelé quand le dino passe en mode Enragé
///   OnCaptureReady  → appelé quand le dino entre dans la fenêtre de capture
///   OnCaptureExit   → appelé quand la fenêtre de capture est dépassée
///   OnOverdose      → appelé si trop de tranquillisant (spécimen mort = échec capture)
///   OnKnockedOut    → appelé quand le dino s'endort (capture réussie)
/// </summary>
public class DinoHealth : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  STATS (modifiables dans l'Inspector)
    // ─────────────────────────────────────────────
    [Header("Points de Vie")]
    public float MaxHP = 300f;
    [Tooltip("Régénération passive de PV/seconde (0 = désactivé)")]
    public float HpRegenRate = 0f;

    [Header("Jauge de Rage")]
    [Tooltip("Rage max (0→100)")]
    public float MaxRage = 100f;
    [Tooltip("Rage gagnée par point de dégât reçu")]
    public float RagePerDamage = 0.15f;
    [Tooltip("Rage perdue par seconde au repos")]
    public float RageDecayRate = 2f;
    [Tooltip("Seuil de rage à partir duquel le dino entre en Berserk")]
    [Range(0f, 100f)]
    public float BerserkRageThreshold = 75f;

    [Header("Fenêtre de Capture (tranquillisant)")]
    [Tooltip("% de PV minimum pour que le tranquillisant ait un effet")]
    [Range(0f, 1f)]
    public float CaptureWindowMin = 0.25f;
    [Tooltip("% de PV maximum pour entrer dans la fenêtre de capture")]
    [Range(0f, 1f)]
    public float CaptureWindowMax = 0.60f;
    [Tooltip("Dose de tranquillisant nécessaire pour endormir le dino (dans la fenêtre)")]
    public float KnockoutDoseRequired = 100f;
    [Tooltip("Dose max avant overdose (mort du spécimen)")]
    public float OverdoseDoseLimit = 180f;
    [Tooltip("Le tranquillisant s'élimine naturellement (dose/seconde)")]
    public float TranqElimRate = 3f;

    // ─────────────────────────────────────────────
    //  ÉTAT PUBLIC (lecture seule depuis l'extérieur)
    // ─────────────────────────────────────────────
    public float CurrentHP      { get; private set; }
    public float CurrentRage    { get; private set; }
    public float CurrentTranq   { get; private set; }
    public bool  IsDead         { get; private set; }
    public bool  IsKnockedOut   { get; private set; }
    public bool  IsBerserk      { get; private set; }
    public bool  IsCaptureReady { get; private set; }

    /// Ratio 0→1, utile pour les barres d'UI
    public float HPRatio        => CurrentHP   / MaxHP;
    public float RageRatio      => CurrentRage / MaxRage;
    public float TranqRatio     => CurrentTranq / KnockoutDoseRequired;

    // ─────────────────────────────────────────────
    //  ÉVÉNEMENTS
    // ─────────────────────────────────────────────
    [Header("Événements")]
    public UnityEvent OnDeath;
    public UnityEvent OnEnrage;
    public UnityEvent OnCaptureReady;
    public UnityEvent OnCaptureExit;
    public UnityEvent OnOverdose;
    public UnityEvent OnKnockedOut;

    // ─────────────────────────────────────────────
    //  RÉFÉRENCES
    // ─────────────────────────────────────────────
    PachycephalosaurusAI _ai;   // Référence à l'IA (remplacer par une interface IDinoAI plus tard)

    // ─────────────────────────────────────────────
    //  INIT
    // ─────────────────────────────────────────────
    void Awake()
    {
        CurrentHP   = MaxHP;
        CurrentRage = 0f;
        CurrentTranq= 0f;
        _ai = GetComponent<PachycephalosaurusAI>();
    }

    // ─────────────────────────────────────────────
    //  BOUCLE
    // ─────────────────────────────────────────────
    void Update()
    {
        if (IsDead || IsKnockedOut) return;

        //HandleRegen();
        HandleRageDecay();
        HandleTranqElim();
        CheckCaptureWindow();
        CheckBerserk();
    }

    // ─────────────────────────────────────────────
    //  DÉGÂTS (armes, morsures, chutes…)
    // ─────────────────────────────────────────────
    /// <summary>Inflige des dégâts physiques au dinosaure.</summary>
    public void TakeDamage(float dmg)
    {
        if (IsDead || IsKnockedOut || dmg <= 0f) return;

        CurrentHP = Mathf.Max(0f, CurrentHP - dmg);

        // Chaque dégât alimente la Rage
        AddRage(dmg * RagePerDamage);

        if (CurrentHP <= 0f)
            Die();
    }

    // ─────────────────────────────────────────────
    //  TRANQUILLISANT
    // ─────────────────────────────────────────────
    /// <summary>
    /// Administre une dose de tranquillisant.
    /// Effets selon la fenêtre de capture du GDD :
    ///   Trop tôt (HP > CaptureWindowMax) → aucun effet
    ///   Fenêtre idéale                   → dose s'accumule, KO possible
    ///   Trop tard (HP < CaptureWindowMin) → dose nécessaire x2 (rage avancée)
    ///   Overdose                          → mort du spécimen
    /// </summary>
    public void ApplyTranquilizer(float dose)
    {
        if (IsDead || IsKnockedOut || dose <= 0f) return;

        // Trop tôt : aucun effet
        if (HPRatio > CaptureWindowMax)
        {
            Debug.Log("[DinoHealth] Tranquillisant : trop tôt, aucun effet.");
            return;
        }

        // Trop tard (rage avancée) : dose doublée nécessaire
        float effectiveDose = (HPRatio < CaptureWindowMin) ? dose * 0.5f : dose;

        CurrentTranq += effectiveDose;

        // Overdose → mort du spécimen
        if (CurrentTranq >= OverdoseDoseLimit)
        {
            Debug.Log("[DinoHealth] OVERDOSE — spécimen perdu !");
            OnOverdose?.Invoke();
            Die(overdose: true);
            return;
        }

        // Knockout
        if (CurrentTranq >= KnockoutDoseRequired && IsCaptureReady)
        {
            KnockOut();
        }
    }

    // ─────────────────────────────────────────────
    //  RAGE
    // ─────────────────────────────────────────────
    public void AddRage(float amount)
    {
        if (IsDead || IsKnockedOut) return;
        CurrentRage = Mathf.Min(MaxRage, CurrentRage + amount);
    }

    void HandleRageDecay()
    {
        if (CurrentRage > 0f)
            CurrentRage = Mathf.Max(0f, CurrentRage - RageDecayRate * Time.deltaTime);
    }

    void CheckBerserk()
    {
        if (!IsBerserk && CurrentRage >= BerserkRageThreshold)
        {
            IsBerserk = true;
            OnEnrage?.Invoke();
            Debug.Log("[DinoHealth] ENRAGE — mode Berserk activé !");
        }
        // Reset berserk si la rage redescend (optionnel selon équilibrage)
        else if (IsBerserk && CurrentRage < BerserkRageThreshold * 0.5f)
        {
            IsBerserk = false;
        }
    }

    // ─────────────────────────────────────────────
    //  FENÊTRE DE CAPTURE
    // ─────────────────────────────────────────────
    void CheckCaptureWindow()
    {
        bool inWindow = HPRatio <= CaptureWindowMax && HPRatio > CaptureWindowMin;

        if (inWindow && !IsCaptureReady)
        {
            IsCaptureReady = true;
            OnCaptureReady?.Invoke();
            Debug.Log("[DinoHealth] Fenêtre de capture OUVERTE.");
        }
        else if (!inWindow && IsCaptureReady)
        {
            IsCaptureReady = false;
            OnCaptureExit?.Invoke();
            Debug.Log("[DinoHealth] Fenêtre de capture FERMÉE.");
        }
    }

    // ─────────────────────────────────────────────
    //  RÉGÉNÉRATION & ÉLIMINATION TRANQ
    // ─────────────────────────────────────────────
    void HandleRegen()
    {
        if (HpRegenRate > 0f && CurrentHP < MaxHP)
            CurrentHP = Mathf.Min(MaxHP, CurrentHP + HpRegenRate * Time.deltaTime);
    }

    void HandleTranqElim()
    {
        if (CurrentTranq > 0f)
            CurrentTranq = Mathf.Max(0f, CurrentTranq - TranqElimRate * Time.deltaTime);
    }

    // ─────────────────────────────────────────────
    //  KNOCKOUT (endormissement réussi)
    // ─────────────────────────────────────────────
    void KnockOut()
    {
        IsKnockedOut = true;
        IsCaptureReady = false;

        // Stoppe l'IA
        _ai?.OnDeath(); // Réutilise la même désactivation (animation différente possible)

        OnKnockedOut?.Invoke();
        Debug.Log("[DinoHealth] Dino ENDORMI — capture possible !");
    }

    // ─────────────────────────────────────────────
    //  MORT
    // ─────────────────────────────────────────────
    void Die(bool overdose = false)
    {
        if (IsDead) return;
        IsDead = true;
        IsCaptureReady = false;

        _ai?.OnDeath();
        OnDeath?.Invoke();

        Debug.Log(overdose
            ? "[DinoHealth] Dino mort par OVERDOSE (récompense de capture perdue)."
            : "[DinoHealth] Dino mort.");
    }

    // ─────────────────────────────────────────────
    //  GIZMO DEBUG (barre de vie dans la Scene view)
    // ─────────────────────────────────────────────
    void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;

        // Barre de vie au-dessus du dino
        Vector3 base3D = transform.position + Vector3.up * 3f;
        float   w      = 2f;

        // Fond gris
        Gizmos.color = Color.gray;
        Gizmos.DrawCube(base3D, new Vector3(w, 0.1f, 0.01f));

        // PV (vert→rouge)
        Gizmos.color = Color.Lerp(Color.red, Color.green, HPRatio);
        Gizmos.DrawCube(base3D + Vector3.left * (w * (1 - HPRatio) * 0.5f),
                        new Vector3(w * HPRatio, 0.1f, 0.02f));

        // Rage (orange)
        Gizmos.color = new Color(1f, 0.5f, 0f);
        Vector3 ragePos = base3D + Vector3.up * 0.15f;
        Gizmos.DrawCube(ragePos + Vector3.left * (w * (1 - RageRatio) * 0.5f),
                        new Vector3(w * RageRatio, 0.08f, 0.02f));

        // Tranq (bleu)
        Gizmos.color = Color.cyan;
        Vector3 tranqPos = base3D + Vector3.up * 0.28f;
        Gizmos.DrawCube(tranqPos + Vector3.left * (w * (1 - TranqRatio) * 0.5f),
                        new Vector3(w * TranqRatio, 0.08f, 0.02f));
    }
}
