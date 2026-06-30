using Demo.Scripts.Runtime.Character;
using Demo.Scripts.Runtime.Item;
using GlobalEnum;
using System;
using System.Linq;
using UnityEngine;

[Serializable]
public class EquipSlot
{
    public ValidateType type;                   // Types d'items autorisés
    public EquipmentSlotUI equipmentSlotUI;     // UI liée à ce slot
    public Transform slotTransform;             // Position physique sur le joueur
    public GameObject item;                     // Objet actuellement équipé
    public FPSItem FPSItem;

    // InventorySlot retiré - le stockage de données passe par InventorySocket via PlayerLoadout
}

public class Equipment : MonoBehaviour
{
    // ---- Masse --------------------------------------------------------------------
    [Header("Masse totale")]
    [SerializeField] private float _massTotal;

    /// <summary>Exposé en lecture pour PlayerLoadout.GetTotalMass().</summary>
    public float MassTotal => _massTotal;

    // ---- Slots --------------------------------------------------------------------
    [Header("Items rapides"), Space(5)]
    [SerializeField] private EquipSlot[] equipmentDummys;
    public EquipSlot[] EquipmentDommys => equipmentDummys;


    // ---- Références internes ------------------------------------------------
    private FPSController _fpsController;
    private PlayerLoadout _loadout;        
    private bool _isHeavy;

    [Header("Debug / optionnel")]
    public GameObject debugHeavyText;

    // ========================================================================
    // INIT
    // ========================================================================

    private void Awake()
    {
        _fpsController = GetComponent<FPSController>();
        _loadout = GetComponent<PlayerLoadout>();  // même GameObject

        // Sécurité : PlayerLoadout doit être présent
        if (_loadout == null)
            Debug.LogError("[Equipment] PlayerLoadout introuvable sur " + gameObject.name);
    }

    private void Start()    
    {
        InitializeSlots();
    }

    private void InitializeSlots()
    {
        // --- Items rapides ------------------------------------------------─
        for (int i = 0; i < equipmentDummys.Length; i++)
        {
            int v = i;
            equipmentDummys[i].equipmentSlotUI.Bind(this, equipmentDummys[v]);
        }
    }

    // ========================================================================
    // VÉRIFICATIONS
    // ========================================================================

    public bool IsHeavy() => _massTotal > 50f;
    public bool HasEquipment(int i) => equipmentDummys[i].item != null;
    public bool HasBackpack() 
        => equipmentDummys.FirstOrDefault(w => w.type == ValidateType.Backpack && w.item != null) != null;
    public bool IsEmpty(int index) 
        => equipmentDummys[index] == null;

    /// <summary>
    /// Cherche le premier slot capable d'accueillir ce type d'item.
    /// Priorité : armes -> gear -> items rapides -> sac à dos.
    /// </summary>
    public EquipSlot CanEquipeItem(ObjectType type)
        => Array.Find(equipmentDummys, w => SlotAccepts(w, type) && w.item == null);
    public EquipSlot[] AllEquipSlot(ObjectType type)
        => Array.FindAll(equipmentDummys, w => SlotAccepts(w, type));

    private static bool SlotAccepts(EquipSlot slot, ObjectType type)
        => !Utils.FlagsTypeValide((int)slot.type, (int)type);

    // ========================================================================
    // MASSE
    // ========================================================================

    public void UpdateMass(float delta)
    {
        _massTotal += delta;

        bool heavy = IsHeavy();
        if (heavy == _isHeavy) return;  // pas de changement -> pas d'appel inutile

        _isHeavy = heavy;
        GetComponent<FPSMovement>()?.Encombrant(_isHeavy);
        // debugHeavyText?.SetActive(_isHeavy);
    }

    // ========================================================================
    // ÉQUIPEMENT
    // ========================================================================

    /// <summary>
    /// Équipe un GameObject dans le bon slot.
    /// Appelé uniquement depuis PlayerLoadout.TryReceiveItem().
    /// </summary>
    public void EquipItem(GameObject item, int count = 1)
    {
        if (item == null) return;

        if (!item.TryGetComponent<WorldEntity>(out var we)) return;

        EquipSlot slot = CanEquipeItem(we.data.ObjectType);
        if (slot == null) return;

        // Masquer si une arme est déjà active
        if (_fpsController._activeItem != null)
            item.SetActive(false);

        // Sac à dos : étend l'inventaire
        if (SlotAccepts(slot, we.data.ObjectType) && !HasBackpack())
        {
            if (item.TryGetComponent<BackPackExtension>(out var backpackExt))
            {
                item.SetActive(true);
                backpackExt.OnEquipped(gameObject);  // passe par PlayerLoadout.ExtendInventory
                GetComponent<PlayerInterfaceSystem>().ShowBackPackContentSlot(true);
                item.SetActive(true);
            }
        }

        // Assigner au slot
        slot.item = item;
        slot.FPSItem = item.GetComponent<FPSItem>();

        UpdateMass(we.data.Mass * count);

        // Physique
        if (item.TryGetComponent<Rigidbody>(out var rb)) { rb.useGravity = false; rb.isKinematic = true; }
        if (item.TryGetComponent<BoxCollider>(out var col)) col.isTrigger = true;

        // Attacher au transform du slot
        AttachItemToSlot(item, slot);

        slot.equipmentSlotUI.Refresh();
    }

    /// <summary>
    /// Déséquipe un item. Ne gère PAS où il va ensuite (c'est PlayerLoadout qui décide).
    /// </summary>
    public void UnEquipItem(GameObject item, bool drop = false)
    {
        EquipSlot slot = GetEquipSlot(item);
        if (slot == null) return;

        var fpsitem = slot.FPSItem;

        // Si c'est le sac à dos, réinitialiser l'inventaire
        if (!Utils.FlagsTypeValide((int)slot.type, (int)ValidateType.Backpack))
            UnEquipBackPack();

        slot.item = null;
        slot.FPSItem = null;
        slot.equipmentSlotUI.Refresh();

        _fpsController.UnEquipItemOnStuff(fpsitem);
    }

    /// <summary>
    /// Swap UI : déplace un item vers un slot précis.
    /// </summary>
    public void EquipItemToSlot(GameObject item, EquipSlot targetSlot)
    {
        if (item == null || targetSlot == null) return;
        if (!item.TryGetComponent<WorldEntity>(out var we)) return;

        // ── 1. Récupérer les données AVANT de toucher aux slots ──────────
        EquipSlot sourceSlot = GetEquipSlot(item);
        GameObject previousItem = targetSlot.item;
        FPSItem sourceFPS = sourceSlot?.FPSItem;
        FPSItem targetFPS = targetSlot.FPSItem;

        // ── 2. Swap des données en mémoire (sans appeler UnEquipItem) ────
        // Vider les deux slots
        if (sourceSlot != null)
        {
            sourceSlot.item = null;
            sourceSlot.FPSItem = null;
        }
        targetSlot.item = null;
        targetSlot.FPSItem = null;

        // Remplir le slot cible avec l'item source
        targetSlot.item = item;
        targetSlot.FPSItem = item.GetComponent<FPSItem>();

        // Remplir le slot source avec l'ancien occupant (si swap)
        if (previousItem != null && sourceSlot != null)
        {
            sourceSlot.item = previousItem;
            sourceSlot.FPSItem = previousItem.GetComponent<FPSItem>();
        }

        // ── 3. Physique & parenting pour l'item déplacé ──────────────────
        AttachItemToSlot(item, targetSlot);

        if (previousItem != null && sourceSlot != null)
            AttachItemToSlot(previousItem, sourceSlot);

        // ── 4. Notifier le FPS controller UNE FOIS, après le swap ────────
        // Si l'item actif était l'un des deux, on lui dit de changer
        if (_fpsController._activeItem == sourceFPS || _fpsController._activeItem == targetFPS)
        {
            _fpsController.UnEquipItemOnStuff(sourceFPS);
        }

        // ── 5. Refresh UI ─────────────────────────────────────────────────
        sourceSlot?.equipmentSlotUI?.Refresh();
        targetSlot.equipmentSlotUI?.Refresh();
    }

    // Helper : physique + parenting d'un item dans son slot
    private void AttachItemToSlot(GameObject item, EquipSlot slot)
    {
        if (item == null) return;

        if (slot.slotTransform != null)
        {
            item.transform.SetParent(slot.slotTransform);
            item.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        }
    }

    public void SwapWeapons(GameObject item, EquipSlot targetSlot)
    {

        // ── 1. Récupérer les données AVANT de toucher aux slots ──────────
        EquipSlot sourceSlot = GetEquipSlot(item);
        GameObject previousItem = targetSlot.item;
        FPSItem sourceFPS = sourceSlot?.FPSItem;
        FPSItem targetFPS = targetSlot.FPSItem;

        // ── 2. Swap des données en mémoire (sans appeler UnEquipItem) ────
        // Vider les deux slots
        if (sourceSlot != null)
        {
            sourceSlot.item = null;
            sourceSlot.FPSItem = null;
        }
        targetSlot.item = null;
        targetSlot.FPSItem = null;

        // Remplir le slot cible avec l'item source
        targetSlot.item = item;
        targetSlot.FPSItem = item.GetComponent<FPSItem>();

        // Remplir le slot source avec l'ancien occupant (si swap)
        if (previousItem != null && sourceSlot != null)
        {
            sourceSlot.item = previousItem;
            sourceSlot.FPSItem = previousItem.GetComponent<FPSItem>();
        }

        sourceSlot.equipmentSlotUI.Refresh();
        targetSlot.equipmentSlotUI.Refresh();
    }

    // ========================================================================
    // GETTERS
    // ========================================================================

    public EquipSlot GetEquipSlot(GameObject go)
        => Array.Find(equipmentDummys, w => w.item == go);
    public EquipSlot GetEquipSlot(int index)
        => equipmentDummys[index];
    public EquipSlot GetEquipSlot(ObjectType type)
        => Array.Find(equipmentDummys, w => (int)w.type == (int)type);

    public int GetIndexEquipSlot(GameObject go)
    {
        int i = Array.FindIndex(equipmentDummys, w => w.item == go);
        return i >= 0 ? i : Array.FindIndex(equipmentDummys, w => w.item == go);
    }

    public int GetEmptyPrimarySlot(ValidateType type, EquipSlot[] slots = null)
        => Array.FindIndex(slots ?? equipmentDummys, w => w.item == null && w.type == type);

    public GameObject GetWeapon(int i) => equipmentDummys[i].item;
    public FPSItem GetFPSItem() =>
        equipmentDummys.FirstOrDefault(w => w.FPSItem != null)?.FPSItem;

    // ========================================================================
    // UTILITAIRES
    // ========================================================================

    public void UnEquipBackPack()
    {
        if (GetEquipSlot(ObjectType.Backpack).item.TryGetComponent(out BackPackExtension ext))
            ext.OnUnequipped(gameObject);  // passe par PlayerLoadout.ResetInventorySize()
        GetComponent<PlayerInterfaceSystem>().ShowBackPackContentSlot(false);
    }

    public void ClearAllWeapons()
    {
        foreach (var slot in equipmentDummys)
            if (slot.item != null) UnEquipItem(slot.item, true);
    }

    public void ClearAllGear()
    {
        foreach (var slot in equipmentDummys)
            slot.item = null;
    }
}
