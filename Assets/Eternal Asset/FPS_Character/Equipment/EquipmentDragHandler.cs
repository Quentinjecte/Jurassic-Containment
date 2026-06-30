using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Demo.Scripts.Runtime.Item;

/// <summary>
/// Gère le drag des slots d'équipement.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class EquipmentDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private EquipmentSlotUI _slotUI;
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private Canvas _canvas;

    private RectTransform _rectTransform;
    private Transform _originalParent;

    public static EquipmentSlotUI DraggedSlot { get; private set; }
    public static bool IsDragging => DraggedSlot != null;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        if (_slotUI == null) _slotUI = GetComponentInParent<EquipmentSlotUI>();
        if (_canvasGroup == null) _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvas == null)
        {
            var canvasGo = GameObject.FindGameObjectWithTag("Canvas");
            if (canvasGo != null) _canvas = canvasGo.GetComponent<Canvas>();
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (_slotUI == null || _slotUI.IsEmpty) return;

        DraggedSlot = _slotUI;
        _originalParent = transform.parent;

        if (_canvas != null)
            transform.SetParent(_canvas.transform, true);

        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 0.6f;
            _canvasGroup.blocksRaycasts = false;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (DraggedSlot == null) return;
        if (_rectTransform != null && _canvas != null)
            _rectTransform.anchoredPosition += eventData.delta / _canvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 1f;
            _canvasGroup.blocksRaycasts = true;
        }

        if (_originalParent != null && transform.parent == _canvas?.transform)
        {
            transform.SetParent(_originalParent);
            if (_rectTransform != null)
                _rectTransform.anchoredPosition = Vector2.zero;
        }

        DraggedSlot = null;
    }
}
