using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Demo.Scripts.Runtime.Item;

/// <summary>
/// Affiche un slot d'équipement (arme, armure, sac, etc.).
/// Lit directement depuis Equipment.EquipSlot.
/// </summary>
public class EquipmentSlotUI : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private Image _iconImage;
    [SerializeField] private Sprite _emptySprite;

    [SerializeField]  private Equipment _equipement;
    [SerializeField]  private EquipSlot _equipSlot;

    public Equipment Equipement => _equipement;
    public EquipSlot EquipSlot => _equipSlot;

    public bool IsEmpty => _equipSlot == null || _equipSlot.item == null;

    /// <summary>
    /// Lie ce composant à un EquipSlot.
    /// </summary>
    public void Bind(Equipment equipement, EquipSlot equipSlot)
    {
        _equipement = equipement;
        _equipSlot = equipSlot;
        Refresh();
    }

    public void Refresh()
    {
        if (_iconImage == null) return;

        if (IsEmpty)
        {
            _iconImage.sprite = _emptySprite != null ? _emptySprite : null;
            _iconImage.enabled = _emptySprite != null;
        }
        else
        {
            var worldEntity = _equipSlot.item.GetComponent<WorldEntity>();

            if (worldEntity != null && worldEntity.data != null && worldEntity.data.Icon != null)
            {
                _iconImage.sprite = worldEntity.data.Icon;
                _iconImage.enabled = true;
            }
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
            OnRightClick?.Invoke(this);
    }

    public event System.Action<EquipmentSlotUI> OnRightClick;
}
