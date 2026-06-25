using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TextPopUp : MonoBehaviour
{
    public static TextPopUp instance;

    private TextMeshPro _TextMesh;

    [SerializeField] private Slider slider;
    [SerializeField] private TextMeshProUGUI sliderLabel;
    [SerializeField] private RectTransform staticLabelPrefabs;
    [SerializeField] private TextMeshProUGUI staticLabel;
    [SerializeField] private Vector3 offset = new Vector3(0f, -1.5f, 0f);
    [SerializeField] private string hint;
    [SerializeField] private Camera cam;

    [SerializeField] private Transform target;
    [SerializeField] private Canvas canvas;
    private RectTransform canvasRect;
    private RectTransform labelRect;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }

    private void Start()
    {
        _TextMesh = staticLabel.GetComponent<TextMeshPro>();
        canvasRect = canvas.GetComponent<RectTransform>();
        HideStaticLabel();
    }

    private void LateUpdate()
    {
        if (target == null) return;

        // Position dans le monde
        Vector3 worldPos = target.localPosition + offset;

        // Convertit en viewport (0-1)
        Vector3 viewportPos = cam.WorldToViewportPoint(worldPos);

        // Transforme en position locale du Canvas
        Vector2 localPos = new Vector2(
            (viewportPos.x - 0.5f) * canvasRect.sizeDelta.x,
            (viewportPos.y - 0.5f) * canvasRect.sizeDelta.y
        );

        labelRect.anchoredPosition = localPos;
    }

    public void ShowStaticLabel(string keyHint)
    {
        staticLabel.text = keyHint;
        staticLabelPrefabs.gameObject.SetActive(true);
    }
    public void ShowSliderLabel(float v)
    {
        slider.gameObject.SetActive(true);
        slider.value = v;
        sliderLabel.text = $"{v}%";
    }

    public void HideStaticLabel()
    {
        staticLabelPrefabs.gameObject.SetActive(false);
        slider.gameObject.SetActive(false);
        target = null;
    }
}
