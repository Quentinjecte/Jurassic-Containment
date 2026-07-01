using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Zone de drop pour l'inventaire.
/// Cas gérés :
///   - InventorySlot → InventorySlot  (déplacer/merge dans le même inventaire)
///   - InventorySlot → InventorySlot  (transférer entre deux inventaires différents)
///   - EquipSlot     → InventorySlot  (déséquiper vers un slot précis)  ← NOUVEAU
///   - InventorySlot → DiscardZone    (jeter dans le monde)
///   - EquipSlot     → DiscardZone    (jeter équipement dans le monde)
/// </summary>
public class InventoryDropZone : MonoBehaviour, IDropHandler
{
    public enum DropZoneType { InventorySlot, DiscardZone }

    [SerializeField] private DropZoneType _zoneType = DropZoneType.InventorySlot;
    [SerializeField] private InventorySlotUI _slotUI;
    [SerializeField] private Inventory _targetInventory;

    private PlayerLoadout _loadout;

    private void Start()
    {
        if (_slotUI == null) _slotUI = GetComponent<InventorySlotUI>();
        if (_targetInventory == null)
        {
            var grid = GetComponentInParent<InventoryGridUI>();
            if (grid != null) _targetInventory = grid._inventory;
        }

        _loadout = FindAnyObjectByType<PlayerLoadout>();
    }

    public void OnDrop(PointerEventData eventData)
    {
        var invSlot = InventoryDragHandler.DraggedSlot;
        var equipSlot = EquipmentDragHandler.DraggedSlot;

        // ── Zone "jeter" ─────────────────────────────────────────────────
        if (_zoneType == DropZoneType.DiscardZone)
        {
            if (invSlot != null)
            {
                OnDiscardRequested?.Invoke(invSlot);
                var handler = GetComponentInParent<InventoryDiscardHandler>()
                           ?? FindAnyObjectByType<InventoryDiscardHandler>();

                // Résoudre l'inventaire cible
                var inventory = ResolveInventory(invSlot);
                handler?.OnDiscardSlot(inventory, invSlot);
            }
            else if (equipSlot != null)
            {
                var unequipHandler = GetComponentInParent<EquipmentUnequipHandler>()
                                  ?? FindAnyObjectByType<EquipmentUnequipHandler>();
                unequipHandler?.UnequipSlotToWorld(equipSlot);
            }
            return;
        }

        // ── Zone slot inventaire ──────────────────────────────────────────

        // Cas A : EquipSlot → InventorySlot (déséquiper vers slot précis)
        Debug.Log("Before equipe");
        if (equipSlot != null && _slotUI != null)
        {
            Debug.Log("equipe");
            TryUnequipToInventorySlot(equipSlot);

            return;
        }

        // Cas B : InventorySlot → InventorySlot
        if (invSlot == null || invSlot.Inventory == null) return;

        var stack = invSlot.Inventory.GetStack(invSlot.SlotIndex);
        if (stack.IsEmpty) return;

        if (_slotUI != null && _slotUI.Inventory != null)
        {
            int targetIndex = _slotUI.SlotIndex;

            if (invSlot.Inventory == _slotUI.Inventory)
            {
                // Même inventaire → déplacer/merge
                if (invSlot.SlotIndex == targetIndex) return;
                invSlot.Inventory.MoveBetweenSlots(invSlot.SlotIndex, targetIndex);
            }
            else
            {
                // Inventaires différents → transférer vers le slot précis
                TransferToSlot(invSlot.Inventory, invSlot.SlotIndex, _slotUI.Inventory, targetIndex);
            }
        }
        else if (_targetInventory != null)
        {
            // Pas de slot UI ciblé → ajouter dans le premier slot libre de _targetInventory
            int surplus = _targetInventory.AddItem(stack.Data, stack.Quantity);
            int moved = stack.Quantity - surplus;
            if (moved > 0)
                invSlot.Inventory.RemoveFromSlot(invSlot.SlotIndex, moved);
        }
    }

    // ─── EquipSlot → InventorySlot précis ────────────────────────────────
    private void TryUnequipToInventorySlot(EquipmentSlotUI equipSlotUI)
    {
        if (_loadout == null || equipSlotUI?.EquipSlot?.item == null) return;

        var slot = equipSlotUI.EquipSlot;
        var we = slot.item.GetComponent<WorldEntity>();
        Debug.Log(we);
        if (we == null) return;

        var targetInventory = _slotUI.Inventory;
        Debug.Log(targetInventory);
        if (targetInventory == null) return;

        int targetIndex = _slotUI.SlotIndex;
        var targetStack = targetInventory.GetStack(targetIndex);
        Debug.Log(targetStack);

        // Si le slot cible est occupé → tenter un swap inventaire ↔ équipement
        if (!targetStack.IsEmpty)
        {
            // L'item de l'inventaire doit être compatible avec le slot équipement
            var equipSlot = slot;
            Debug.Log(equipSlot);
            if (!EquipmentDropZone.SlotAcceptsStatic(equipSlot, targetStack.Data.ObjectType))
                return; // types incompatibles, on annule

            // Swap : item inventaire → équipement, item équipement → slot inventaire
            var loadout = _loadout;
            Debug.Log(loadout);

            // 1. Déséquiper l'item actuel sans le détruire (on va le mettre en inventaire)
            var oldItem = slot.item;
            var oldData = we.data;
            var oldCount = we.Count;

            loadout.Equipment.UnEquipItem(oldItem, false);
            loadout.Equipment.UpdateMass(-(oldData.Mass * oldCount));
            Debug.Log(oldCount);

            // 2. Mettre l'ancien item dans le slot inventaire cible
            targetInventory.RemoveFromSlot(targetIndex, targetStack.Quantity);
            targetInventory.SetSlotDirect(targetIndex, oldData, oldCount);

            oldItem.SetActive(false);
            Object.Destroy(oldItem);
            Debug.Log(oldCount);

            // 3. Équiper le nouvel item depuis l'inventaire
            if (targetStack.Data.Prefab != null)
            {
                var instance = Object.Instantiate(targetStack.Data.Prefab,
                    loadout.Equipment.transform.position, Quaternion.identity);
                if (instance.TryGetComponent<WorldEntity>(out var newWe))
                    newWe.Init(targetStack.Data, targetStack.Quantity);

                loadout.Equipment.EquipItem(instance, targetStack.Quantity);
            }
        }
        else
        {
            // Slot cible vide → déséquiper directement vers ce slot
            var oldItem = slot.item;
            var oldData = we.data;
            var oldCount = we.Count;
            var loadout = _loadout;
            Debug.Log(loadout);

            loadout.Equipment.UnEquipItem(oldItem, false);
            loadout.Equipment.UpdateMass(-(oldData.Mass * oldCount));

            // Placer dans le slot précis
            bool placed = targetInventory.SetSlotDirect(targetIndex, oldData, oldCount);
            Debug.Log(placed);
            if (!placed)
            {
                // Slot refusé (type incompatible) → tenter AddItem puis monde
                int surplus = targetInventory.AddItem(oldData, oldCount);
                if (surplus > 0) _loadout.SpawnInWorld(oldData, surplus);
            }

            oldItem.SetActive(false);
            Object.Destroy(oldItem);
        }

        equipSlotUI.Refresh();
        _slotUI.Refresh();
    }

    // ─── Transfert entre inventaires vers un slot précis ─────────────────
    private void TransferToSlot(Inventory source, int sourceIndex, Inventory target, int targetIndex)
    {
        var sourceStack = source.GetStack(sourceIndex);
        var targetStack = target.GetStack(targetIndex);

        if (sourceStack.IsEmpty) return;

        if (targetStack.IsEmpty)
        {
            // Slot cible vide → déplacer directement
            bool ok = target.SetSlotDirect(targetIndex, sourceStack.Data, sourceStack.Quantity);
            if (ok)
                source.RemoveFromSlot(sourceIndex, sourceStack.Quantity);
            else
            {
                // Type refusé → AddItem classique
                int surplus = target.AddItem(sourceStack.Data, sourceStack.Quantity);
                source.RemoveFromSlot(sourceIndex, sourceStack.Quantity - surplus);
            }
        }
        else if (sourceStack.Data.GetName() == targetStack.Data.GetName())
        {
            // Même item → merge
            int surplus = target.AddItem(sourceStack.Data, sourceStack.Quantity);
            source.RemoveFromSlot(sourceIndex, sourceStack.Quantity - surplus);
        }
        else
        {
            // Items différents → swap entre les deux inventaires
            bool sourceAccepts = source.GetSlot(sourceIndex).AcceptsType(targetStack.Data.ObjectType);
            bool targetAccepts = target.GetSlot(targetIndex).AcceptsType(sourceStack.Data.ObjectType);
            if (!sourceAccepts || !targetAccepts) return;

            source.SetSlotDirect(sourceIndex, targetStack.Data, targetStack.Quantity);
            target.SetSlotDirect(targetIndex, sourceStack.Data, sourceStack.Quantity);
        }
    }

    private Inventory ResolveInventory(InventorySlotUI slot)
        => slot?.Inventory ?? _targetInventory;

    public event System.Action<InventorySlotUI> OnDiscardRequested;
}