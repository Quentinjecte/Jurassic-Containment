using GlobalEnum;
using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

/// <summary>
/// COORDINATEUR CENTRAL — point d'entrée unique pour tout ce qui touche
/// à l'inventaire et à l'équipement du joueur.
///
/// Remplace PlayerInventory. Tous les handlers passent par ici.
/// Règle de décision : Equipment d'abord → InventorySocket ensuite → Monde si surplus.
/// </summary>
[RequireComponent(typeof(Inventory))]
[RequireComponent(typeof(Equipment))]
public class PlayerLoadout : MonoBehaviour
{
    // ─── Config ───────────────────────────────────────────────────────────
    [Header("Configuration")]
    [SerializeField] private int _baseSlotCount = 8;
    [SerializeField] private float _dropDistance = 1.5f;
    public InventoryGridUI InventoryUI;

    // ─── Références ───────────────────────────────────────────────────────
    public Inventory InventoryBackPack;
    public Inventory InventorySocket { get; private set; }
    public Equipment Equipment { get; private set; }

    // Taille de base mémorisée pour le reset (retrait du sac à dos)
    private int _baseSlots;

    // ─── Événements ───────────────────────────────────────────────────────
    /// <summary>Déclenché après tout équipement réussi (équip ou inventaire).</summary>
    public event Action<ObjectMotherData, int, EquipResult> OnItemReceived;

    /// <summary>Déclenché après tout déséquipement.</summary>
    public event Action<ObjectMotherData, int> OnItemReleased;

    // ─── Init ─────────────────────────────────────────────────────────────
    private void Awake()
    {
        InventorySocket = GetComponent<Inventory>();
        Equipment = GetComponent<Equipment>();

        InventorySocket.Initialize(_baseSlotCount);
        _baseSlots = _baseSlotCount;
    }

    // ═════════════════════════════════════════════════════════════════════
    // API PUBLIQUE — les handlers n'utilisent QUE ces méthodes
    // ═════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Point d'entrée unique pour recevoir un item (ramassage, craft, trade...).
    /// Décision automatique : slot équipement → inventaire → surplus monde.
    /// </summary>
    /// <param name="data">Données de l'item.</param>
    /// <param name="quantity">Quantité.</param>
    /// <param name="prefabInstance">
    ///     Instance GameObject déjà existante dans la scène (optionnel).
    ///     Si null et qu'un slot d'équipement est libre, instancie depuis data.Prefab.
    /// </param>
    public EquipResult TryReceiveItem(ObjectMotherData data, int quantity, GameObject prefabInstance = null)
    {
        if (data == null || quantity <= 0) return EquipResult.Failed;

        // ── 1. Slot d'équipement physique ────────────────────────────────
        var equipSlot = Equipment.CanEquipeItem(data.ObjectType);
        if (equipSlot != null)
        {
            var instance = prefabInstance;
            if (instance == null && data.Prefab != null)
            {
                instance = Instantiate(data.Prefab,
                    transform.position + transform.forward * _dropDistance,
                    Quaternion.identity);
                if (instance.TryGetComponent<WorldEntity>(out var we))
                    we.Init(data, quantity);
            }

            if (instance != null)
            {
                Equipment.EquipItem(instance, quantity);
                OnItemReceived?.Invoke(data, quantity, EquipResult.Equipped);
                return EquipResult.Equipped;
            }
        }

        // ── 2. Sac à dos (si équipé) ─────────────────────────────────────
        int remaining = quantity;

        if (InventoryBackPack != null)
            remaining = InventoryBackPack.AddItem(data, remaining);

        // ── 3. Slots rapides (InventorySocket) ───────────────────────────
        if (remaining > 0)
            remaining = InventorySocket.AddItem(data, remaining);

        int added = quantity - remaining;
        if (added > 0)
            OnItemReceived?.Invoke(data, added, EquipResult.AddedToInventory);

        // ── 4. Surplus → monde ───────────────────────────────────────────

        if (remaining > 0)
        {
            SpawnInWorld(data, remaining);
            return EquipResult.PartialOverflow;
        }
        if (remaining == 0)
            DestroyImmediate(prefabInstance);

        return added > 0 ? EquipResult.AddedToInventory : EquipResult.Full;
    }

    /// <summary>
    /// Déséquipe un slot d'équipement → tente l'inventaire, sinon monde.
    /// </summary>
    public void UnequipToInventory(EquipSlot slot)
    {
        if (slot?.item == null) return;

        var we = slot.item.GetComponent<WorldEntity>();
        if (we == null) return;

        var data = we.data;
        var count = we.Count;

        Equipment.UnEquipItem(slot.item, false);
        Equipment.UpdateMass(-(data.Mass * count));

        // ── Tente BackPack d'abord, puis InventorySocket ─────────────────
        int remaining = count;

        if (InventoryBackPack != null)
            remaining = InventoryBackPack.AddItem(data, remaining);

        if (remaining > 0)
            remaining = InventorySocket.AddItem(data, remaining);

        if (remaining > 0)
            SpawnInWorld(data, remaining);

        slot.item.SetActive(false);
        Destroy(slot.item);

        OnItemReleased?.Invoke(data, count);
    }

    /// <summary>
    /// Déséquipe un slot d'équipement directement dans le monde (jet).
    /// </summary>
    public void UnequipToWorld(EquipSlot slot)
    {
        if (slot.item == null) return;

        var we = slot.item.GetComponent<WorldEntity>();
        if (we == null) return;

        var data = we.data;
        var count = we.Count;

        Equipment.UpdateMass(-(data.Mass * count));
        SpawnInWorld(slot.item);
        Equipment.UnEquipItem(slot.item, false);

        //slot.item.SetActive(false);
        //Destroy(slot.item);

        OnItemReleased?.Invoke(data, count);
    }

    /// <summary>
    /// Jette un item depuis un slot de l'inventaire dans le monde.
    /// </summary>
    public bool DropFromInventorySlot(Inventory _targetInventory,int slotIndex, int amount = -1)
    {
        if (!_targetInventory.IsValidIndex(slotIndex)) return false;

        var stack = _targetInventory.GetStack(slotIndex);
        if (stack.IsEmpty || stack.Data?.Prefab == null) return false;

        int toDrop = amount < 0 ? stack.Quantity : Mathf.Min(amount, stack.Quantity);
        if (toDrop <= 0) return false;

        SpawnInWorld(stack.Data, toDrop);
        _targetInventory.RemoveFromSlot(slotIndex, toDrop);

        OnItemReleased?.Invoke(stack.Data, toDrop);
        return true;
    }

    // ─── Sac à dos ────────────────────────────────────────────────────────

    /// <summary>Étend l'inventaire quand un sac à dos est équipé.</summary>
    public void ExtendInventory(int additionalSlots)
    {
        if (additionalSlots <= 0) return;
        InventoryBackPack.Resize(_baseSlots + additionalSlots);
    }

    /// <summary>Réinitialise l'inventaire à la taille de base (retrait du sac).</summary>
    public void ResetInventorySize()
    {
        InventoryBackPack.Resize(_baseSlots);
    }

    // ─── Spawn monde ──────────────────────────────────────────────────────

    /// <summary>
    /// Instancie un item dans le monde à la position du joueur (+ dropDistance).
    /// Méthode statique overload disponible avec position explicite.
    /// </summary>
    public GameObject SpawnInWorld(ObjectMotherData data, int qty, Vector3? position = null)
    {
        if (data?.Prefab == null) return null;

        Vector3 pos = position ?? (transform.position + transform.forward * _dropDistance + Vector3.up);
        var go = Instantiate(data.Prefab, pos, Quaternion.identity);

        if (go.TryGetComponent<WorldEntity>(out var we)) we.Init(data, qty);
        if (go.TryGetComponent<Rigidbody>(out var rb)) { rb.useGravity = true; rb.isKinematic = false; }
        if (go.TryGetComponent<BoxCollider>(out var col)) col.isTrigger = false;

        return go;
    }
    public GameObject SpawnInWorld(GameObject item, Vector3? position = null)
    {
        if (item == null) return null;
        item.transform.parent = null;

        Vector3 pos = position ?? (transform.position + transform.forward * _dropDistance + Vector3.up);
        item.transform.SetPositionAndRotation(pos, Quaternion.identity);

        if (item.TryGetComponent<Rigidbody>(out var rb)) { rb.useGravity = true; rb.isKinematic = false; }
        if (item.TryGetComponent<BoxCollider>(out var col)) col.isTrigger = false;
        item.SetActive(true);
        Debug.Log("SpawnInWorld");

        return item;
    }

    // ─── Utilitaires ──────────────────────────────────────────────────────
    /// <summary>
    /// Cherche le meilleur chargeur disponible dans l'inventaire pour ce type d'arme.
    /// Priorité : chargeur avec des balles > chargeur vide.
    /// Cherche dans BackPack d'abord, puis InventorySocket.
    /// Retourne null si aucun chargeur compatible trouvé.
    /// </summary>
    public (MagazineSO data, Inventory source, int slotIndex) FindBestMagazine(MagazineType weaponMagType)
    {
        // Chercher dans BackPack d'abord, puis InventorySocket
        var inventories = new List<Inventory>();
        if (InventoryBackPack != null) inventories.Add(InventoryBackPack);
        inventories.Add(InventorySocket);

        (MagazineSO, Inventory, int) bestFull = (null, null, -1); // chargeur avec balles
        (MagazineSO, Inventory, int) bestEmpty = (null, null, -1); // chargeur vide

        foreach (var inv in inventories)
        {
            for (int i = 0; i < inv.SlotCount; i++)
            {
                var stack = inv.GetStack(i);
                if (stack.IsEmpty) continue;
                if (stack.Data is not MagazineSO mag) continue;
                if (Utils.FlagsTypeValide((int)mag.magazineType, (int)weaponMagType) == true) continue;

                // Chargeur avec balles → priorité maximale
                if (mag.CurrentAmmo > 0 && bestFull.Item1 == null)
                    bestFull = (mag, inv, i);

                // Chargeur vide → fallback
                if (mag.CurrentAmmo == 0 && bestEmpty.Item1 == null)
                    bestEmpty = (mag, inv, i);
            }
        }

        // Retourner le meilleur trouvé
        return bestFull.Item1 != null ? bestFull : bestEmpty;
    }

    /// <summary>
    /// Consomme un chargeur de l'inventaire après rechargement.
    /// </summary>
    public void ConsumeMagazine(MagazineSO magazine,Inventory source, int slotIndex)
    {
        if (source == null || !source.IsValidIndex(slotIndex)) return;
        source.RemoveFromSlot(slotIndex, 1);
        OnItemReleased?.Invoke(source.GetStack(slotIndex).Data, 1);
    }
    public float GetTotalMass() => (InventorySocket?.GetTotalMass() ?? 0f) + (Equipment?.MassTotal ?? 0f);
}

/// <summary>Résultat d'une tentative de réception d'item.</summary>
public enum EquipResult
{
    Equipped,           // Placé dans un slot d'équipement physique
    AddedToInventory,   // Placé dans l'inventaire
    PartialOverflow,    // Partiellement accepté, le surplus a été spawné dans le monde
    Full,               // Aucune place disponible
    Failed              // Données invalides
}
