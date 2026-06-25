using Assets.Script.Player;
using GlobalEnum;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Affiche les jauges de survie dans le HUD.
/// Chaque jauge est liée à un PlayerStat.
/// Change de couleur selon le niveau critique.
/// Affiche des alertes textuelles pour les états critiques.
/// </summary>
public class HUDController : MonoBehaviour
{
    [Header("Références")]
    [SerializeField] private PlayerCondition _playerCondition;
    public Sprite iconBuff;
    public Sprite iconDebuff;

    [Header("Jauges (assigner dans l'Inspector)")]
    [SerializeField] private Transform _buffContainer;
    [SerializeField] private BuffSliderUI buffSlider;
    [SerializeField] private SurvivalBarUI[] _bars;

    [Header("Alertes")]
    [SerializeField] private GameObject _pendingAlerte;
    [SerializeField] private float _alertDisplayDuration = 3f;

    private float _alertTimer;

    // ═════════════════════════════════════════════════════════════════
    // INIT
    // ═════════════════════════════════════════════════════════════════

    private void Start()
    {
        if (_playerCondition == null)
            _playerCondition = GetComponentInParent<PlayerCondition>();

        //_pendingAlerte?.gameObject.SetActive(false);

        if (_playerCondition == null) return;

        _playerCondition.OnStatChanged += OnStatChanged;
        _playerCondition.OnStatLevelChanged += OnStatLevelChanged;
        _playerCondition.OnBuffAdditive += OnBuffApply;

        // Initialiser les jauges
        foreach (var bar in _bars)
        {
            var stat = _playerCondition.Get(bar.statType);
            Debug.Log(stat.Config.statType);
            if (stat != null) bar.Refresh(stat, bar._colorNormal);
        }
    }

    private void OnDestroy()
    {
        if (_playerCondition == null) return;
        _playerCondition.OnStatChanged -= OnStatChanged;
        _playerCondition.OnStatLevelChanged -= OnStatLevelChanged;
        _playerCondition.OnBuffAdditive -= OnBuffApply;
    }

    // ═════════════════════════════════════════════════════════════════
    // UPDATE
    // ═════════════════════════════════════════════════════════════════

    private void Update()
    {
        // Rafraîchir toutes les jauges chaque frame (valeurs changent en continu)
        if (_playerCondition == null) return;

        foreach (var bar in _bars)
        {
            var stat = _playerCondition.Get(bar.statType);
            if (stat == null) continue;

            Color c = bar.LevelToColor(stat.GetCurrentLevel());
            bar.Refresh(stat, c);
        }

        // Timer d'alerte
        if (_alertTimer > 0f)
        {
            _alertTimer -= Time.deltaTime;
            if (_alertTimer <= 0f)
                _pendingAlerte?.gameObject.SetActive(false);
        }
    }

    // ═════════════════════════════════════════════════════════════════
    // HANDLERS
    // ═════════════════════════════════════════════════════════════════

    private void OnStatChanged(StatInstance stat, float delta) { }
    private SurvivalBarUI GetSurvivalBar(PlayerStat survivalStat) => _bars.First(b => b.statType == survivalStat);

    private void OnBuffApply(StatInstance stat, float duration, bool isBuff)
    {
        var icon = isBuff == true ? iconBuff : iconDebuff;

        foreach (Transform _buff in _buffContainer.transform)
        {
            if (!_buff.TryGetComponent(out BuffSliderUI buffSlider)) continue;
            if (buffSlider.survival == stat)
            {
                buffSlider.Init(stat, stat.Config.icon, icon, duration, isBuff);
                return;
            }
        }

        var buff = Instantiate(buffSlider, _buffContainer);
        buff.Init(stat, stat.Config.icon, icon, duration, isBuff);
    }

    private void OnStatLevelChanged(StatInstance stat, CriticalLevel level)
    {
        if (level == CriticalLevel.Normal) return;

        // Afficher une alerte
        int lang = TranslationManager.instance?.index ?? 0;
        string statName = stat.Config.GetName(lang);

        string message = level switch
        {
            CriticalLevel.Warning => $"⚠ {statName} faible",
            CriticalLevel.Critical => $"⚠ {statName} critique !",
            CriticalLevel.Dying => $"☠ {statName} en danger !",
            _ => ""
        };

        ShowAlert(message, GetSurvivalBar(stat.Config.statType).LevelToColor(level));
    }

    private void ShowAlert(string message, Color color)
    {
        TextMeshProUGUI _alertText = _pendingAlerte.GetComponent<TextMeshProUGUI>();
        if (_alertText == null) return;
        _alertText.text = message;
        _alertText.color = color;
        _alertText.gameObject.SetActive(true);
        _alertTimer = _alertDisplayDuration;
    }
}

/// <summary>Configuration d'une jauge individuelle dans le HUD.</summary>
[System.Serializable]
public class SurvivalBarUI
{
    public ValueUI valueUI;
    public PlayerStat statType;
    public ValueConvert valueConvert; //Definie comment le nombre est rendu
    public Slider slider;
    public Sprite[] newFill;        // Image du fill de la jauge (pour changer la couleur)
    public Image fill;          // Image du fill de la jauge (pour changer la couleur)
    public TextMeshProUGUI valueText; // ex: "75 / 100" (optionnel)

    // ─── Couleurs par niveau ──────────────────────────────────────────
    [Header("Couleurs")]
    public Color _colorNormal = Color.green;
    public Color _colorWarning = Color.yellow;
    public Color _colorCritical = Color.red;
    public Color _colorDying = new Color(0.8f, 0f, 0f);
    public void Refresh(StatInstance stat, Color color)
    {
        switch (valueUI)
        { 
            case ValueUI.@float:
                if (slider != null) slider.value = stat.Ratio;
                if (fill != null) fill.color = color;
                if (valueText != null)
                {
                    valueText.text = ConvertValueText(stat.Value, stat.Max);
                    valueText.color = color;
                }
                break;
            case ValueUI.@int:
                if (newFill.Length > 0) fill.sprite = newFill[(int)stat.Value];
                if (fill != null) fill.color = color;
                /*if (valueText != null)
                    valueText.text = ConvertValueText(stat.Value, stat.Max);*/
                break;
        }
    }
    public Color LevelToColor(CriticalLevel level) => level switch
    {
        CriticalLevel.Warning => _colorWarning,
        CriticalLevel.Critical => _colorCritical,
        CriticalLevel.Dying => _colorDying,
        _ => _colorNormal
    };

    private string ConvertValueText(float value, float max)
    {
        switch (valueConvert)
        {
            case ValueConvert.pourcentage:
                if (max <= 0) return "0%"; // évite division par zéro
                int percent = Mathf.RoundToInt(value);
                return $"{percent}%";
            case ValueConvert.Degres:
                int degres = Mathf.RoundToInt(value);
                return $"{degres}°";
            default:
                return $"{Mathf.RoundToInt(value)} / {max}";
        }
    }
}
