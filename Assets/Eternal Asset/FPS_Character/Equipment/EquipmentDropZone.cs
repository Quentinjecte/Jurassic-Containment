using UnityEngine;
using UnityEngine.EventSystems;
using Demo.Scripts.Runtime.Item;

/// <summary>
/// Zone de drop pour les slots d'équipement.
/// Cas gérés :
///   - Inventaire → EquipSlot  (équiper)
///   - EquipSlot  → EquipSlot  (swap équipement)
///   - EquipSlot  → InventorySlot est géré côté InventoryDropZone
/// </summary>
public class EquipmentDropZone : MonoBehaviour, IDropHandler
{
    [SerializeField] private EquipmentSlotUI _slotUI;

    private void Awake()
    {
        if (_slotUI == null) _slotUI = GetComponent<EquipmentSlotUI>();
    }

    public void OnDrop(PointerEventData eventData)
    {
        // 1. Drop depuis un slot inventaire → équiper dans ce slot
        var invSlot = InventoryDragHandler.DraggedSlot;
        if (invSlot != null)
        {
            TryEquipFromInventory(invSlot);
            return;
        }

        // 2. Drop depuis un autre slot équipement → swap
        var equipSlot = EquipmentDragHandler.DraggedSlot;
        if (equipSlot != null)
            TrySwapEquipment(equipSlot);
    }

    // ─── Inventaire → EquipSlot ──────────────────────────────────────────
    private void TryEquipFromInventory(InventorySlotUI invSlot)
    {
        if (_slotUI?.Equipement == null || invSlot.Inventory == null) return;

        var stack = invSlot.Inventory.GetStack(invSlot.SlotIndex);
        if (stack.IsEmpty || stack.Data?.Prefab == null) return;

        var targetSlot = _slotUI.EquipSlot;
        if (targetSlot == null || !SlotAccepts(targetSlot, stack.Data.ObjectType)) return;

        var loadout = _slotUI.Equipement.GetComponent<PlayerLoadout>();
        if (loadout == null) return;

        // Si le slot cible est déjà occupé → swap : l'ancien item va dans l'inventaire source
        if (targetSlot.item != null)
        {
            var occupantWe = targetSlot.item.GetComponent<WorldEntity>();
            if (occupantWe == null) return;

            // Vérifier que l'inventaire source peut accepter l'item occupant
            // (on ne bloque pas si l'inventaire est plein — le surplus ira dans le monde)
            _slotUI.Equipement.UnEquipItem(targetSlot.item, false);
            int surplus = invSlot.Inventory.AddItem(occupantWe.data, occupantWe.Count);
            if (surplus > 0)
                loadout.SpawnInWorld(occupantWe.data, surplus);

            targetSlot.item.SetActive(false);
            Object.Destroy(targetSlot.item);
        }

        // Instancier et équiper le nouvel item
        var instance = Instantiate(stack.Data.Prefab,
            _slotUI.Equipement.transform.position,
            Quaternion.identity);

        if (instance.TryGetComponent<WorldEntity>(out var we))
            we.Init(stack.Data, 1);

        invSlot.Inventory.RemoveFromSlot(invSlot.SlotIndex, 1);
        _slotUI.Equipement.EquipItem(instance, 1);

        invSlot.Refresh();
        _slotUI.Refresh();
    }

    // ─── EquipSlot → EquipSlot ───────────────────────────────────────────
    private void TrySwapEquipment(EquipmentSlotUI sourceSlot)
    {
        if (_slotUI == null || sourceSlot == null || sourceSlot == _slotUI) return;

        var sourceEquipSlot = sourceSlot.EquipSlot;
        var targetEquipSlot = _slotUI.EquipSlot;

        if (sourceEquipSlot?.item == null || targetEquipSlot == null) return;

        var sourceWe = sourceEquipSlot.item.GetComponent<WorldEntity>();
        if (sourceWe == null) return;

        // Vérifier compatibilité des types dans les deux sens
        if (!SlotAccepts(targetEquipSlot, sourceWe.data.ObjectType)) return;

        if (targetEquipSlot.item != null)
        {
            var targetWe = targetEquipSlot.item.GetComponent<WorldEntity>();
            if (targetWe != null && !SlotAccepts(sourceEquipSlot, targetWe.data.ObjectType))
                return;
        }

        _slotUI.Equipement.SwapWeapons(sourceEquipSlot.item, targetEquipSlot);

        sourceSlot.Refresh();
        _slotUI.Refresh();
    }

    public static bool SlotAcceptsStatic(EquipSlot slot, ObjectType itemType)
        => !Utils.FlagsTypeValide((int)slot.type, (int)itemType);

    // Garder aussi la version privée pour usage interne :
    private static bool SlotAccepts(EquipSlot slot, ObjectType itemType)
        => SlotAcceptsStatic(slot, itemType);
}