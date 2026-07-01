using UnityEngine;

/// <summary>
/// À placer sur le prefab du sac à dos.
/// Quand équipé, étend l'inventaire du joueur.
/// Remplace l'ancien BackPack pour l'intégration au nouveau système.
/// </summary>
public class BackPackExtension : MonoBehaviour
{
    [SerializeField] private int _slotCount = 12;
    public int SlotCount => _slotCount;

    /// <summary>
    /// Appelé par Equipment quand le sac est équipé.
    /// </summary>
    public void OnEquipped(GameObject owner)
    {
        if (!owner.TryGetComponent(out PlayerLoadout loadout)) return;
        loadout.InventoryUI.Bind(GetComponent<Inventory>());
        loadout.InventoryBackPack = GetComponent<Inventory>();
    }

    /// <summary>
    /// Appelé par Equipment quand le sac est déséquipé.
    /// </summary>
    public void OnUnequipped(GameObject owner)
    {
        if (owner.TryGetComponent(out PlayerLoadout loadout))
            loadout.InventoryUI.DestroyBind();
        loadout.InventoryBackPack = null;

    }
}
