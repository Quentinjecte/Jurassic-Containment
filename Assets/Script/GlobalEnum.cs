namespace GlobalEnum
{
    // ═══════════════════════════════════════════════════════════════════════════
    // ENUMS — Etat
    // ═══════════════════════════════════════════════════════════════════════════
    public enum MovementState
    {
        Idle,
        Walking,
        Sprinting,
        InAir,
        Sliding
    }

    public enum PoseState
    {
        Standing,
        Crouching,
    }
    public enum DinoState
    {
        Passif,
        Alerte,
        Agressif,
        BlesseEnrage
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ENUMS — Stat du joueur
    // ═══════════════════════════════════════════════════════════════════════════
    /// <summary>Identifiant unique de chaque paramètre de survie.</summary>
    public enum PlayerStat
    {
        Health,
        Energie,
        Speed,
        Sens,
        JumpForce
    }

    /// <summary>Sévérité d'un état critique — utilisé pour les effets visuels et sons.</summary>
    public enum CriticalLevel
    {
        Normal,     // > 50%
        Warning,    // 25–50%
        Critical,   // 10–25%
        Dying,      // < 10%
    }

    /// <summary>Type d'effet d'un consommable ou d'un soin.</summary>
    public enum ConsumeEffectType
    {
        RestoreFlat,        // restaurer une valeur int
        RestorePercent,     // restaurer un % du max
        ModifyDecayRate,    // modifier temporairement le taux de dégradation
        ApplyBuff,          // buff temporaire (ex: anti-radiation)
        ApplyDebuff,        // debuff (ex: nourriture avariée)
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ENUMS — Contenu converti
    // ═══════════════════════════════════════════════════════════════════════════
    public enum ValueConvert
    {
        normal,
        pourcentage,
        Degres
    }

    public enum ValueUI
    {
        @float,
        @int
    }
}