using UnityEngine;

/// <summary>
/// Représentation immutable d'un stack d'items (données pures, pas de GameObject).
/// Utilisé pour les ressources, consommables, etc.
/// </summary>
[System.Serializable]
public struct ItemStack
{
    public ObjectMotherData Data;
    public int Quantity;

    public static ItemStack Empty => new() { Data = null, Quantity = 0 };

    public readonly bool IsEmpty => Data == null || Quantity <= 0;
    public readonly int SpaceLeft => Data != null ? Mathf.Max(0, Data.StackMax - Quantity) : 0;
    public readonly bool IsFull => Data != null && Quantity >= Data.StackMax;

    public ItemStack(ObjectMotherData data, int quantity)
    {
        Data = data;
        Quantity = Mathf.Clamp(quantity, 0, data != null ? data.StackMax : 0);
    }

    public readonly bool CanStackWith(ItemStack other)
    {
        if (IsEmpty || other.IsEmpty) return true;
        return Data != null && other.Data != null && Data.GetName() == other.Data.GetName();
    }

    public readonly ItemStack Add(int amount)
    {
        if (Data == null) return Empty;
        int newQty = Mathf.Min(Quantity + amount, Data.StackMax);
        int added = newQty - Quantity;
        return new ItemStack(Data, newQty);
    }
}
