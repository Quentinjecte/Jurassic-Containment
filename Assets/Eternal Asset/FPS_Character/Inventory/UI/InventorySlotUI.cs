using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Affichage UI d'un slot d'inventaire - vue uniquement, pas de logique métier.
/// </summary>
public class InventorySlotUI : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private Image _iconImage;
    [SerializeField] private TMP_Text _quantityText;
    [SerializeField] private Sprite _emptySprite;
/*    [SerializeField] private bool _isPersistant = false;
    private GameObject _gameObject;
*/
    private int _slotIndex;
    private Inventory _inventory;

    public int SlotIndex => _slotIndex;
    public Inventory Inventory => _inventory;
    public Image IconImage => _iconImage;
    public TMP_Text QuantityText => _quantityText;
/*    public bool IsPersistant => _isPersistant;

    public GameObject GameObject => _gameObject; 
*/
    public void Bind(Inventory inventory, int slotIndex)
    {
        _inventory = inventory;
        _slotIndex = slotIndex;

        if (_inventory != null)
        {
            _inventory.OnSlotChanged.RemoveListener(OnSlotChanged);
            _inventory.OnSlotChanged.AddListener(OnSlotChanged);
        }

        Refresh();
    }

    private void OnDestroy()
    {
        if (_inventory != null)
            _inventory.OnSlotChanged.RemoveListener(OnSlotChanged);
    }

    private void OnSlotChanged(int index, ItemStack stack)
    {
        if (index == _slotIndex)
            Refresh();
    }

    public void Refresh()
    {
        if (_inventory == null || !_inventory.IsValidIndex(_slotIndex)) return;

        var stack = _inventory.GetStack(_slotIndex);

        if (stack.IsEmpty)
        {
            if (_iconImage != null)
                _iconImage.sprite = _emptySprite != null ? _emptySprite : null;
            if (_quantityText != null)
            {
                _quantityText.text = "";
                _quantityText.enabled = false;
            }
        }
        else
        {
            if (_iconImage != null && stack.Data.Icon != null)
                _iconImage.sprite = stack.Data.Icon;

            if (_quantityText != null)
            {
                _quantityText.text = stack.Quantity > 1 ? stack.Quantity.ToString() : "";
                _quantityText.enabled = stack.Quantity > 1;
            }
        }

        EventTrigger trigger = GetComponent<EventTrigger>();
        // ENTER
        EventTrigger.Entry eventEnter = new EventTrigger.Entry();
        eventEnter.eventID = EventTriggerType.PointerEnter;
        eventEnter.callback.AddListener((data) =>
        {
            var _str = Inventory.GetSlot(SlotIndex)?.Data.GetName() ?? "";
            TextPopUp.instance.ShowDynamicLabel(transform, _str);
        });
        trigger.triggers.Add(eventEnter);

        trigger.AddTrigger(EventTriggerType.PointerExit, hideLabel);
        trigger.AddTrigger(EventTriggerType.PointerClick, hideLabel);

    }
    // Callback commun
    readonly UnityAction<BaseEventData> hideLabel = (data) =>
    {
        TextPopUp.instance.HideDynamicLabel();
    };

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            OnRightClick?.Invoke(this);
        }
    }

    public event System.Action<InventorySlotUI> OnRightClick;
}
