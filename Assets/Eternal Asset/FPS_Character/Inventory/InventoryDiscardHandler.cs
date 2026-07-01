using UnityEngine;

/// <summary>
/// Gère le "jeter" d'items depuis l'inventaire vers le monde.
/// Passe désormais par PlayerLoadout.
/// </summary>
public class InventoryDiscardHandler : MonoBehaviour
{
    private PlayerLoadout _loadout;

    private void Awake()
    {
        _loadout = GetComponentInParent<PlayerLoadout>()
                ?? FindAnyObjectByType<PlayerLoadout>();
    }

    public void OnDiscardSlot(Inventory _targetInventory, InventorySlotUI slotUI)
    {
        if (slotUI == null || _loadout == null) return;
        _loadout.DropFromInventorySlot(_targetInventory, slotUI.SlotIndex);
    }
}
