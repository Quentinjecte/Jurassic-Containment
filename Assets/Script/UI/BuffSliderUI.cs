using UnityEngine;
using UnityEngine.UI;

public class BuffSliderUI : MonoBehaviour
{
    public StatInstance survival;
    public Slider slider;
    public Image statImg;         // Image du fill de la jauge (pour changer la couleur)
    public Image buffImg;         // Image du fill de la jauge (pour changer la couleur)
    public Image fill;         // Image du fill de la jauge (pour changer la couleur)

    private float duration;
    // ─── Couleurs par niveau ──────────────────────────────────────────
    private readonly Color BuffMain = ColorUtility.TryParseHtmlString("#00FF03", out var c1) ? c1 : Color.green;
    private readonly Color BuffFill = ColorUtility.TryParseHtmlString("#268E0064", out var c2) ? c2 : Color.green;

    private readonly Color DebuffMain = ColorUtility.TryParseHtmlString("#FF0000", out var c3) ? c3 : Color.red;
    private readonly Color DebuffFill = ColorUtility.TryParseHtmlString("#FF000064", out var c4) ? c4 : Color.red;

    public void Init(StatInstance survival, Sprite stats, Sprite buff, float duration, bool isBuff)
    {
        this.survival = survival;

        statImg.sprite = stats;
        buffImg.sprite = buff;
        slider.maxValue = duration;
        this.duration = duration;

        if (isBuff)
        {
            statImg.color = BuffMain;
            buffImg.color = BuffMain;
            fill.color = BuffFill;
        }
        else
        {
            statImg.color = DebuffMain;
            buffImg.color = DebuffMain;
            fill.color = DebuffFill;
        }
    }
    void LateUpdate()
    {
        duration -= Time.deltaTime;
        if (duration > 0)
            slider.value = duration;
        if (duration <= 0)
            DestroyImmediate(gameObject);
    }
}
