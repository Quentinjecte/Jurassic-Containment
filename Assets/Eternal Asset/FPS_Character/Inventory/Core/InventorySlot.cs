using System;
using UnityEngine;
using GlobalEnum;

/// <summary>
/// Un slot d'inventaire - données pures uniquement.
/// Supporte les restrictions de type (ex: slot Ressource uniquement).
/// </summary>
[System.Serializable]
public class InventorySlot
{
    [SerializeField] private ObjectMotherData _data;
    [SerializeField] private int _quantity;

    public ObjectMotherData Data
    {
        get => _data;
        private set => _data = value;
    }

    public int Quantity
    {
        get => _quantity;
        private set => _quantity = Mathf.Max(0, value);
    }

    public ValidateType AllowedTypes { get; set; } = ValidateType.autre | ValidateType.Ressource | ValidateType.Consomable;

    public bool IsEmpty => _data == null || _quantity <= 0;
    public ItemStack Stack => new ItemStack(_data, _quantity);

    public bool AcceptsType(ObjectType itemType)
    {
        return ((int)AllowedTypes & (int)itemType) != 0;
    }

    public bool CanAdd(ObjectMotherData data, int amount)
    {
        if (data == null || amount <= 0) return false;
        if (!AcceptsType(data.ObjectType)) return false;

        if (IsEmpty) return true;
        if (_data.GetName() != data.GetName()) return false;

        return _quantity + amount <= data.StackMax;
    }

    /// <summary>
    /// Tente d'ajouter des items. Retourne le surplus (0 = tout a été ajouté).
    /// </summary>
    public int TryAdd(ObjectMotherData data, int amount)
    {
        if (!CanAdd(data, amount))
            return amount;

        if (IsEmpty)
        {
            int _toAdd = Mathf.Min(amount, data.StackMax);
            _data = data;
            _quantity = _toAdd;
            return amount - _toAdd;
        }

        int spaceLeft = _data.StackMax - _quantity;
        int toAdd = Mathf.Min(amount, spaceLeft);
        _quantity += toAdd;
        return amount - toAdd;
    }

    /// <summary>
    /// Retire des items. Retourne le nombre effectivement retiré.
    /// </summary>
    public int TryRemove(int amount)
    {
        int removed = Mathf.Min(amount, _quantity);
        _quantity -= removed;
        if (_quantity <= 0)
        {
            _data = null;
            _quantity = 0;
        }
        return removed;
    }

    public void Clear()
    {
        _data = null;
        _quantity = 0;
    }

    public void Set(ItemStack stack)
    {
        _data = stack.Data;
        _quantity = stack.Quantity;
    }

    public void SwapWith(InventorySlot other)
    {
        var tempData = _data;
        var tempQty = _quantity;
        _data = other._data;
        _quantity = other._quantity;
        other._data = tempData;
        other._quantity = tempQty;
    }
}
