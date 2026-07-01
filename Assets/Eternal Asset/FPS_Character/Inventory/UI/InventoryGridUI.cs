using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Affiche une grille de slots d'inventaire.
/// Peut instancier les slots dynamiquement ou utiliser des enfants existants.
/// </summary>
public class InventoryGridUI : MonoBehaviour
{
    public Inventory _inventory;
    public Equipment _equipment;
    [SerializeField] private GameObject _slotPrefab;
    [SerializeField] private Transform _slotContainer;
    [SerializeField] private bool _createSlotsAtRuntime = true;

    private List<InventorySlotUI> _inventorySlotUIs = new();
    private List<EquipmentSlotUI> _equipementSlotUIs = new();

    public IReadOnlyList<InventorySlotUI> InventorySlotUIs => _inventorySlotUIs;
    public IReadOnlyList<EquipmentSlotUI> EquipementSlotUIs => _equipementSlotUIs;

    private void Start()
    {
        if (_inventory != null)
            Bind(_inventory);
    }

    private void OnDestroy()
    {
        Unbind();
    }

    public void Bind(Inventory inventory)
    {
        Unbind();
        _inventory = inventory;

        if (_inventory == null) return;

        Transform container = _slotContainer != null ? _slotContainer : transform;
        int slotCount = _inventory.SlotCount;

        if (_createSlotsAtRuntime && _slotPrefab != null)
        {
            for (int i = 0; i < slotCount; i++)
            {
                var go = Instantiate(_slotPrefab, container);
                if(go.TryGetComponent(out InventorySlotUI inventorySlotUI))
                if (inventorySlotUI != null)
                {
                    inventorySlotUI.Bind(_inventory, i);
                    inventorySlotUI.OnRightClick += HandleSlotRightClick;
                    _inventorySlotUIs.Add(inventorySlotUI);
                }
                if (go.TryGetComponent(out EquipmentSlotUI equipmentSlotUI))
                if (equipmentSlotUI != null)
                {
                    equipmentSlotUI.Bind(_equipment, equipmentSlotUI.EquipSlot);
                    _equipementSlotUIs.Add(equipmentSlotUI);
                }
            }
        }
        else
        {
            var existing = container.GetComponentsInChildren<InventorySlotUI>(true);
            for (int i = 0; i < Mathf.Min(slotCount, existing.Length); i++)
            {
                existing[i].Bind(_inventory, i);
                existing[i].OnRightClick += HandleSlotRightClick;
                _inventorySlotUIs.Add(existing[i]);
            }
        }
    }

    public void Unbind()
    {
        foreach (var slotUI in _inventorySlotUIs)
        {
            if (slotUI != null)
                slotUI.OnRightClick -= HandleSlotRightClick;
        }
        _inventorySlotUIs.Clear();
    }

    public void DestroyBind()
    {
        foreach (var slotUI in _inventorySlotUIs)
            Destroy(slotUI.gameObject);

        _inventorySlotUIs.Clear();
    }


    public void RefreshAll()
    {
        foreach (var slotUI in _inventorySlotUIs)
            slotUI?.Refresh();
    }

    private void HandleSlotRightClick(InventorySlotUI slotUI)
    {
        OnSlotRightClick?.Invoke(slotUI);
        var discardHandler = GetComponentInParent<InventoryDiscardHandler>();
        if (discardHandler != null) discardHandler.OnDiscardSlot(_inventory, slotUI);
    }

    public event System.Action<InventorySlotUI> OnSlotRightClick;
}
