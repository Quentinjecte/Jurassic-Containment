using GlobalEnum;
using System;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Conteneur d'inventaire - logique pure, pas d'UI.
/// Gère l'ajout, le retrait et le swap d'items.
/// </summary>
public class Inventory : MonoBehaviour
{
    [SerializeField] private int _slotCount = 20;
    [SerializeField] private ValidateType _defaultAllowedTypes = ValidateType.autre | ValidateType.Ressource | ValidateType.Consomable;

    [SerializeField] private InventorySlot[] _slots;
    private bool _initialized;

    [Serializable]
    public class SlotChangedEvent : UnityEvent<int, ItemStack> { }
    public SlotChangedEvent OnSlotChanged = new SlotChangedEvent();

    [Serializable]
    public class InventoryChangedEvent : UnityEvent { }
    public InventoryChangedEvent OnInventoryChanged = new InventoryChangedEvent();

    public int SlotCount => _slots?.Length ?? 0;
    public InventorySlot GetSlot(int index) => IsValidIndex(index) ? _slots[index] : null;
    public ItemStack GetStack(int index) => GetSlot(index)?.Stack ?? ItemStack.Empty;
    public bool IsValidIndex(int index) => _slots != null && index >= 0 && index < _slots.Length;

    private void Awake()
    {
        Initialize();
    }

    public void Initialize(int slotCount = -1)
    {
        if (_initialized && slotCount < 0) return;

        int count = slotCount >= 0 ? slotCount : _slotCount;
        _slots = new InventorySlot[count];

        for (int i = 0; i < count; i++)
        {
            _slots[i] = new InventorySlot
            {
                AllowedTypes = _defaultAllowedTypes
            };
        }

        _slotCount = count;
        _initialized = true;
    }

    /// <summary>
    /// Redimensionne l'inventaire (ex: nouveau sac à dos).
    /// Les slots existants sont préservés jusqu'au nouveau count.
    /// </summary>
    public void Resize(int newSlotCount)
    {
        if (newSlotCount <= 0) return;

        var newSlots = new InventorySlot[newSlotCount];
        int copyCount = Mathf.Min(_slots?.Length ?? 0, newSlotCount);

        for (int i = 0; i < copyCount; i++)
        {
            newSlots[i] = _slots[i];
        }

        for (int i = copyCount; i < newSlotCount; i++)
        {
            newSlots[i] = new InventorySlot { AllowedTypes = _defaultAllowedTypes };
        }

        _slots = newSlots;
        _slotCount = newSlotCount;
        OnInventoryChanged?.Invoke();
    }

    /// <summary>
    /// Ajoute des items. Retourne le surplus (0 = tout a été ajouté).
    /// </summary>
    public int AddItem(ObjectMotherData data, int amount)
    {
        if (data == null || amount <= 0) return amount;

        int remaining = amount;

        // 1. Empiler dans les slots existants du même type
        for (int i = 0; i < _slots.Length && remaining > 0; i++)
        {
            if (_slots[i].CanAdd(data, remaining))
            {
                remaining = _slots[i].TryAdd(data, remaining);
                NotifySlotChanged(i);
            }
        }

        // 2. Remplir les slots vides
        for (int i = 0; i < _slots.Length && remaining > 0; i++)
        {
            if (_slots[i].IsEmpty && _slots[i].AcceptsType(data.ObjectType))
            {
                remaining = _slots[i].TryAdd(data, remaining);
                NotifySlotChanged(i);
            }
        }

        if (remaining != amount)
            OnInventoryChanged?.Invoke();

        return remaining;
    }

    /// <summary>
    /// Retire des items par type. Retourne le nombre effectivement retiré.
    /// </summary>
    public int RemoveItem(ObjectMotherData data, int amount)
    {
        if (data == null || amount <= 0) return 0;

        int removed = 0;
        string itemName = data.GetName();

        for (int i = 0; i < _slots.Length && removed < amount; i++)
        {
            if (!_slots[i].IsEmpty && _slots[i].Data.GetName() == itemName)
            {
                int toRemove = Mathf.Min(amount - removed, _slots[i].Quantity);
                _slots[i].TryRemove(toRemove);
                removed += toRemove;
                NotifySlotChanged(i);
            }
        }

        if (removed > 0)
            OnInventoryChanged?.Invoke();

        return removed;
    }

    /// <summary>
    /// Retire des items d'un slot précis.
    /// </summary>
    public int RemoveFromSlot(int slotIndex, int amount)
    {
        if (!IsValidIndex(slotIndex)) return 0;


        int removed = _slots[slotIndex].TryRemove(amount);
        if (removed > 0)
        {
            NotifySlotChanged(slotIndex);
            OnInventoryChanged?.Invoke();
        }
        return removed;
    }

    /// <summary>
    /// Place directement des données dans un slot précis, sans passer par AddItem.
    /// Utilisé pour les swaps inter-inventaires et les drops sur slot ciblé.
    /// Retourne false si le slot refuse le type.
    /// </summary>
    public bool SetSlotDirect(int index, ObjectMotherData data, int quantity)
    {
        if (!IsValidIndex(index) || data == null) return false;
        if (!_slots[index].AcceptsType(data.ObjectType)) return false;

        // Vider proprement le slot avant d'écrire
        _slots[index].Clear();
        _slots[index].TryAdd(data, quantity);

        NotifySlotChanged(index);
        OnInventoryChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// Échange le contenu de deux slots.
    /// </summary>
    public bool SwapSlots(int indexA, int indexB)
    {
        if (!IsValidIndex(indexA) || !IsValidIndex(indexB) || indexA == indexB)
            return false;

        var slotA = _slots[indexA];
        var slotB = _slots[indexB];

        if (!slotA.IsEmpty && !slotB.AcceptsType(slotA.Data.ObjectType)) return false;
        if (!slotB.IsEmpty && !slotA.AcceptsType(slotB.Data.ObjectType)) return false;

        slotA.SwapWith(slotB);
        NotifySlotChanged(indexA);
        NotifySlotChanged(indexB);
        OnInventoryChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// Déplace une quantité d'un slot vers un autre (split/merge).
    /// </summary>
    public bool MoveBetweenSlots(int fromIndex, int toIndex, int amount = -1)
    {
        if (!IsValidIndex(fromIndex) || !IsValidIndex(toIndex) || fromIndex == toIndex)
            return false;

        var from = _slots[fromIndex];
        var to = _slots[toIndex];

        if (from.IsEmpty) return false;

        int toMove = amount < 0 ? from.Quantity : Mathf.Min(amount, from.Quantity);
        if (toMove <= 0) return false;

        if (!to.CanAdd(from.Data, toMove))
        {
            if (to.IsEmpty || from.Data.GetName() != to.Data.GetName())
                return SwapSlots(fromIndex, toIndex);
            return false;
        }

        int surplus = to.TryAdd(from.Data, toMove);
        int actuallyMoved = toMove - surplus;
        from.TryRemove(actuallyMoved);

        NotifySlotChanged(fromIndex);
        NotifySlotChanged(toIndex);
        OnInventoryChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// Compte le total d'un type d'item.
    /// </summary>
    public int CountItem(ObjectMotherData data)
    {
        if (data == null) return 0;
        string name = data.GetName();
        int count = 0;
        foreach (var slot in _slots)
        {
            if (!slot.IsEmpty && slot.Data.GetName() == name)
                count += slot.Quantity;
        }
        return count;
    }

    public bool HasItem(ObjectMotherData data) => CountItem(data) > 0;

    public float GetTotalMass()
    {
        float mass = 0;
        foreach (var slot in _slots)
        {
            if (!slot.IsEmpty)
                mass += slot.Data.Mass * slot.Quantity;
        }
        return mass;
    }

    private void NotifySlotChanged(int index)
    {
        OnSlotChanged?.Invoke(index, GetStack(index));
    }

    /// <summary>
    /// Définit les types autorisés pour un slot.
    /// </summary>
    public void SetSlotFilter(int index, ValidateType allowedTypes)
    {
        if (IsValidIndex(index))
            _slots[index].AllowedTypes = allowedTypes;
    }
}
