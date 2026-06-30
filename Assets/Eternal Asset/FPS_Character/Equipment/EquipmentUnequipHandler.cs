using UnityEngine;

/// <summary>
/// Gère le déséquipement depuis l'UI.
/// Délègue entièrement à PlayerLoadout — ne contient plus de logique métier.
/// </summary>
public class EquipmentUnequipHandler : MonoBehaviour
{
    private PlayerLoadout _loadout;

    private void Awake()
    {
        _loadout = GetComponentInParent<PlayerLoadout>()
                ?? FindAnyObjectByType<PlayerLoadout>();
    }

    /// <summary>Clic droit sur un slot équipement → retour inventaire.</summary>
    public void UnequipSlot(EquipmentSlotUI slotUI)
    {
        if (slotUI?.EquipSlot == null || _loadout == null) return;
        _loadout.UnequipToInventory(slotUI.EquipSlot);
        slotUI.Refresh();
        Debug.Log("UnequipSlot");
    }

    /// <summary>Drop vers la zone "jeter" → spawn monde.</summary>
    public void UnequipSlotToWorld(EquipmentSlotUI slotUI)
    {
        if (slotUI?.EquipSlot == null || _loadout == null) return;
        _loadout.UnequipToWorld(slotUI.EquipSlot);
        slotUI.Refresh();
        Debug.Log("UnequipSlotToWorld");
    }
}
