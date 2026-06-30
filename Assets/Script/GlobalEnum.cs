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

    // ═══════════════════════════════════════════════════════════════════════════
    // ENUMS — Système de quêtes
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Définit comment une quête se comporte au changement de scène et entre sessions.
    ///
    /// Permanent     : complétée une fois → reste complétée pour toujours (ex: quête histoire)
    /// SceneReset    : se réinitialise à chaque fois que la scène est chargée (ex: tutoriel)
    /// Repeatable    : peut être refaite, l'état est gardé entre scènes mais peut être relancée
    ///                 manuellement (ex: cueillir des plantes, contrats quotidiens)
    /// </summary>
    public enum QuestPersistenceMode
    {
        Permanent,      // jamais réinitialisée — état sauvegardé en JSON
        SceneReset,     // reset à chaque entrée de scène — jamais sauvegardé
        Repeatable,     // peut être relancée — dernier état sauvegardé en JSON
    }

    /// <summary>Type de quête.</summary>
    public enum QuestType
    {
        MainStory,      // Histoire principale — linéaire, échec possible
        SideQuest,      // Quête annexe — optionnelle, pas d'échec permanent
        SurvivalGoal,   // Objectif de survie — déclenché automatiquement
        Contract,       // Contrat PNJ — donné par un personnage
    }

    /// <summary>État d'une quête pour le joueur.</summary>
    public enum QuestState
    {
        Locked,         // Conditions non remplies, pas visible
        Available,      // Débloquée, pas encore acceptée
        Active,         // En cours
        Completed,      // Terminée avec succès
        Failed,         // Échouée (conséquences possibles)
    }

    /// <summary>État d'un objectif individuel.</summary>
    public enum ObjectiveState
    {
        Inactive,       // Pas encore actif (objectif suivant)
        Active,         // En cours
        Completed,      // Rempli
        Failed,         // Échoué
        Skipped,        // Ignoré (objectif optionnel non requis)
    }

    /// <summary>Type d'objectif — définit comment la progression est mesurée.</summary>
    public enum ObjectiveType
    {
        KillEnemies,        // Tuer X ennemis (filtrables par faction/type)
        CollectItems,       // Collecter X d'un item
        ReachLocation,      // Atteindre une zone (trigger)
        SurviveTime,        // Survivre X secondes
        AnalyzeObject,      // Analyser un objet au labo
        CraftItem,          // Fabriquer un objet
        InterceptMessage,   // Trouver/lire un message/document
        TalkToNPC,          // Parler à un PNJ
        Custom,             // Événement personnalisé (déclenché par code)
    }

    /// <summary>Comment la quête se déclenche.</summary>
    public enum QuestTriggerType
    {
        Manual,             // Acceptée par le joueur auprès d'un PNJ
        ZoneEnter,          // Entrer dans une zone
        TimeSurvived,       // Après X secondes de survie
        QuestCompleted,     // Une autre quête terminée
        ItemPickup,         // Ramasser un item spécifique
        RandomEvent,        // Événement aléatoire (probabilité)
        GameStart,          // Au démarrage de la partie
    }

    /// <summary>Type de récompense.</summary>
    public enum RewardType
    {
        Item,               // Items / ressources
        UnlockCraft,        // Déblocage de recette de craft
        UnlockZone,         // Déblocage d'une zone de la map
        StoryProgression,   // Chapitre suivant de l'histoire
        SkillXP,            // XP pour une compétence
    }

    /// <summary>Conséquence d'un échec de quête.</summary>
    public enum FailConsequenceType
    {
        None,               // Aucune conséquence
        BlockFollowUp,      // Bloque les quêtes qui en dépendaient
        WorldStateChange,   // Change l'état du monde (PNJ mort, zone hostile)
        LoseItems,          // Perd des items
    }

    [System.Flags]
    public enum MagazineType
    {
        AR = 1 << 0,
        AK = 1 << 1,
        Remigton = 1 << 2,
        SRM = 1 << 3,
        Pistol = 1 << 4,
        All = 1 << 5
    }
}