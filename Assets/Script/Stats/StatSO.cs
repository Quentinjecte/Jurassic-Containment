using GlobalEnum;
using UnityEngine;

/// <summary>
/// Configuration d'un paramètre de survie (faim, soif, température, etc.).
/// Créer un asset par paramètre via Assets > Create > Survival > Stat.
/// </summary>
[CreateAssetMenu(menuName = "Stat")]
public class StatSO : ScriptableObject
{
    [Header("Identité")]
    public PlayerStat   statType;
    public string[]     displayName;        // localisé (fr/en/...)
    public Sprite       icon;
    public ValueUI      valueUI;
    //public Sprite       iconBuff;
    //public Sprite       iconDebuff;

    [Header("Valeurs de base")]
    public float minValue       = 0f;
    public float maxValue       = 100f;
    public float startValue     = 100f;     // valeur au démarrage
    /*public int  minValue        = 0;
    public int  maxValue        = 100;
    public int  startValue      = 100;     // valeur au démarrage*/

    [Header("Dégradation automatique (par seconde en temps réel)")]
    [Tooltip("Valeur perdue par seconde au repos. 0 = pas de dégradation auto.")]
    public float baseDecayRate  = 0.01f;    // ex: 0.01 = 1% toutes les 100s

    [Header("Seuils critiques (en % du max)")]
    public float warningThreshold = 0.50f;      // 50% → alerte jaune
    public float criticalThreshold = 0.25f;     // 25% → alerte rouge
    public float dyingThreshold = 0.10f;        // 10% → effet mourant
    /*public int warningThreshold = 2;            // 50% → alerte jaune
    public int criticalThreshold = 1;           // 25% → alerte rouge
    public int dyingThreshold = 0;              // 10% → effet mourant*/

    [Header("Régénération")]
    [Tooltip("Régénération naturelle par seconde quand au-dessus du seuil critique.")]
    public float baseRegenRate  = 0f;

    [Header("Deadth")]
    [Tooltip("Cause la mort si la valeur tombe a zero")]
    public bool causesDeathAtZero;

    // ─── Helpers ──────────────────────────────────────────────────────
    public string GetName(int langIndex = 0)
        => displayName != null && displayName.Length > langIndex
            ? displayName[langIndex]
            : statType.ToString();
}