using UnityEngine;

/// <summary>
/// Inventaire du joueur - combine le stockage de données et la logique de drop dans le monde.
/// S'étend quand un sac à dos est équipé.
/// </summary>
[RequireComponent(typeof(Inventory))]
public class PlayerInventory : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private int _baseSlotCount = 8;
    [SerializeField] private float _dropDistance = 1.5f;

    [SerializeField] private Inventory _inventory;
    private int _baseSlots;

    public Inventory Inventory => _inventory ??= GetComponent<Inventory>();

    private void Awake()
    {
        _inventory = GetComponent<Inventory>();
        if (_inventory == null) _inventory = gameObject.AddComponent<Inventory>();

        _inventory.Initialize(_baseSlotCount);
        _baseSlots = _baseSlotCount;
    }

    /// <summary>
    /// Étend l'inventaire (ex: équipement d'un sac à dos).
    /// </summary>
    public void ExtendSlots(int additionalSlots)
    {
        if (additionalSlots <= 0) return;
        Inventory.Resize(_baseSlots + additionalSlots);
    }

    /// <summary>
    /// Réinitialise à la taille de base (ex: retrait du sac à dos).
    /// Les items en surplus sont perdus ou à gérer selon le game design.
    /// </summary>
    public void ResetToBaseSize()
    {
        Inventory.Resize(_baseSlots);
    }

    /// <summary>
    /// Jette un item du slot dans le monde (instancie le prefab).
    /// </summary>
    public bool DropFromSlot(int slotIndex, int amount = -1)
    {
        if (!Inventory.IsValidIndex(slotIndex)) return false;

        var stack = Inventory.GetStack(slotIndex);
        if (stack.IsEmpty || stack.Data?.Prefab == null) return false;

        int toDrop = amount < 0 ? stack.Quantity : Mathf.Min(amount, stack.Quantity);
        if (toDrop <= 0) return false;

        Vector3 dropPos = transform.position + transform.forward * _dropDistance + Vector3.up;
        SpawnWorldItem(stack.Data, toDrop, dropPos);

        Inventory.RemoveFromSlot(slotIndex, toDrop);
        return true;
    }

    /// <summary>
    /// Spawn un item dans le monde à partir des données.
    /// </summary>
    public static GameObject SpawnWorldItem(ObjectMotherData data, int quantity, Vector3 position)
    {
        if (data?.Prefab == null) return null;

        var go = Instantiate(data.Prefab, position, Quaternion.identity);
        var worldEntity = go.GetComponent<WorldEntity>();
        if (worldEntity != null)
            worldEntity.Init(data, quantity);

        var rb = go.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.useGravity = true;
            rb.isKinematic = false;
        }

        var col = go.GetComponent<Collider>();
        if (col != null && col is BoxCollider box)
            box.isTrigger = false;

        return go;
    }

    public float GetTotalMass() => Inventory?.GetTotalMass() ?? 0;
}
