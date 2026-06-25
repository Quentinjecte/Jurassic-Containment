using GlobalEnum;
using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// État runtime d'un paramètre de survie.
/// Contient la valeur courante, les modificateurs actifs et les événements.
/// Créé et géré par SurvivalSystem.
/// </summary>
public class StatInstance
{
    // ─── Données ──────────────────────────────────────────────────────
    public StatSO       Config { get; private set; }
    public float        Value  { get; private set; }
    public float        Max    => Config.maxValue;
    public float        Min    => Config.minValue;
    public float        Ratio  => Max > 0 ? Value / Max : 0;
    public float        NombrePositif  => Max > 0 ? Value - Max : 0;

    // ─── Modificateurs temporaires ────────────────────────────────────
    // Chaque modificateur est une lambda (float currentRate) → float newRate
    // Exemple : x => x * 0.5f réduit la dégradation de 50%
    private float _decayModifier  = 1f;  // multiplicateur sur le taux de dégradation
    private float _regenModifier  = 1f;  // multiplicateur sur la régénération

    // Timer de mort différée
    private float _deathTimer = -1f;
    public  bool  IsDeathTimerActive => _deathTimer >= 0f;

    // ─── Événements ───────────────────────────────────────────────────
    public event Action<StatInstance, float>            OnValueChanged;   // (stat, delta)
    public event Action<StatInstance, CriticalLevel>    OnLevelChanged;   // (stat, niveau)
    public event Action<StatInstance>                   OnDeadReachedZero;
    public event Action                                 OnReachedZero;

    private CriticalLevel _lastLevel = CriticalLevel.Normal;

    // ═════════════════════════════════════════════════════════════════
    // INIT
    // ═════════════════════════════════════════════════════════════════

    public StatInstance(StatSO config)
    {
        Config = config;
        Value  = config.startValue;
    }

    // ═════════════════════════════════════════════════════════════════
    // TICK
    // ═════════════════════════════════════════════════════════════════

    public void Tick(float dt, float externalDecayBonus = 0f)
    {

        // Dégradation nette = (taux de base + bonus externe) × modificateur
        float decay = (Config.baseDecayRate + externalDecayBonus) * _decayModifier;
        float regen = Config.baseRegenRate * _regenModifier;

        float delta = (regen - decay) * dt;
        Modify(delta);
    }

    // ═════════════════════════════════════════════════════════════════
    // MODIFICATION
    // ═════════════════════════════════════════════════════════════════
    /// <summary>
    /// Applique une modification de la value actuelle.
    /// Par default la valeur est soustraite.
    /// </summary>
    public void Modify(float delta)
    {
        float oldValue = Value;
        Value = Mathf.Clamp(Value + delta, Min, Max);
        float actualDelta = Value - oldValue;

        if (Mathf.Abs(actualDelta) > 0.0001f)
        {
            OnValueChanged?.Invoke(this, actualDelta);
            CheckLevel();
        }
    }

    /// <summary>
    /// ???
    /// </summary>
    public void Set(float value)
    {
        float clamped = Mathf.Clamp(value, Min, Max);
        float delta   = clamped - Value;
        Value = clamped;
        if (Mathf.Abs(delta) > 0.0001f)
        {
            OnValueChanged?.Invoke(this, delta);
            CheckLevel();
        }
    }

    public void SetToMax() => Set(Max);

    // ─── Modificateurs de taux ────────────────────────────────────────

    /// <summary>
    /// Applique un modificateur multiplicatif sur le taux de dégradation.
    /// Appeler avec 0.5f pour réduire de 50%, 2f pour doubler.
    /// </summary>
    public void SetDecayModifier(float multiplier)
        => _decayModifier = Mathf.Max(0f, multiplier);

    public void SetRegenModifier(float multiplier)
        => _regenModifier = Mathf.Max(0f, multiplier);

    public void ResetModifiers()
    {
        _decayModifier = 1f;
        _regenModifier = 1f;
    }

    // ═════════════════════════════════════════════════════════════════
    // NIVEAU CRITIQUE
    // ═════════════════════════════════════════════════════════════════

    private void CheckLevel()
    {
        var newLevel = GetCurrentLevel();
        if (newLevel != _lastLevel)
        {
            _lastLevel = newLevel;
            OnLevelChanged?.Invoke(this, newLevel);
        }
    }

    public CriticalLevel GetCurrentLevel()
    {
        float ratio = Config.valueUI == ValueUI.@float ? Ratio : Value;
        if (ratio <= Config.dyingThreshold)    return CriticalLevel.Dying;
        if (ratio <= Config.criticalThreshold) return CriticalLevel.Critical;
        if (ratio <= Config.warningThreshold)  return CriticalLevel.Warning;
        return CriticalLevel.Normal;
    }
}