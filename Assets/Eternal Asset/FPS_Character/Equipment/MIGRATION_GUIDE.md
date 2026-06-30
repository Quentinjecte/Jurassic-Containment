# Guide de migration — Centralisation vers PlayerLoadout
==========================================================

## Vue d'ensemble des changements

```
AVANT                          APRÈS
──────────────────────────     ──────────────────────────────────
PlayerInventory                PlayerLoadout  ← point d'entrée unique
Equipment                      Equipment      ← simplifié, sans InventorySlot
BackPackExtension              BackPackExtension  ← parle à PlayerLoadout
InventoryDiscardHandler        InventoryDiscardHandler  ← délègue à PlayerLoadout
EquipmentUnequipHandler        EquipmentUnequipHandler  ← délègue à PlayerLoadout
EquipmentDropZone              EquipmentDropZone  ← AcceptsItem() corrigé
```

---

## ÉTAPE 1 — Préparer le GameObject joueur

1. Sur le GameObject du joueur, **supprimer** le composant `PlayerInventory`
2. **Ajouter** le composant `PlayerLoadout`
3. Vérifier que `Inventory` et `Equipment` sont aussi sur ce même GameObject
   (PlayerLoadout les récupère via GetComponent dans Awake)

**Inspector de PlayerLoadout :**
- Base Slot Count : 8 (ou ta valeur actuelle)
- Drop Distance : 1.5

---

## ÉTAPE 2 — Remplacer les scripts modifiés

Copier les fichiers suivants dans ton projet (écraser les anciens) :

| Fichier              | Changement principal                              |
|----------------------|---------------------------------------------------|
| PlayerLoadout.cs     | NOUVEAU — remplace PlayerInventory                |
| Equipment.cs         | InventorySlot retiré de EquipSlot, bugfix slots   |
| BackPackExtension.cs | PlayerInventory → PlayerLoadout                  |
| InventoryDiscardHandler.cs | PlayerInventory → PlayerLoadout            |
| EquipmentUnequipHandler.cs | Logique retirée, délègue à PlayerLoadout   |
| EquipmentDropZone.cs | AcceptsItem() corrigé + référence loadout         |

---

## ÉTAPE 3 — Supprimer PlayerInventory.cs

Une fois les autres scripts en place, `PlayerInventory.cs` peut être supprimé.

⚠️ Avant de supprimer : chercher dans tout le projet les références à
`PlayerInventory` avec Ctrl+Shift+F dans VS Code ou l'IDE.

Remplacements à faire :
```csharp
// AVANT
PlayerInventory inv = GetComponent<PlayerInventory>();
inv.DropFromSlot(index);
PlayerInventory.SpawnWorldItem(data, qty, pos);

// APRÈS
PlayerLoadout loadout = GetComponent<PlayerLoadout>();
loadout.DropFromInventorySlot(index);
loadout.SpawnInWorld(data, qty, pos);
```

---

## ÉTAPE 4 — Vérifier EquipSlot dans l'Inspector

`EquipSlot` n'a plus de champ `inventorySlot`. Si Unity affiche des
erreurs de sérialisation sur les EquipSlot existants, c'est normal :
les anciennes données de `inventorySlot` seront ignorées.

Aucune réassignation nécessaire dans l'Inspector — les autres champs
(type, equipmentSlotUI, slotTransform) sont conservés.

---

## ÉTAPE 5 — Tester par ordre

1. **Lancer la scène** → vérifier absence d'erreurs dans la console
2. **Ramasser un item** → doit aller en inventaire ou équipement
3. **Clic droit sur slot inventaire** → doit dropper dans le monde
4. **Clic droit sur slot équipement** → doit aller en inventaire
5. **Drop vers zone "jeter"** → doit spawner dans le monde
6. **Équiper un sac à dos** → l'inventaire doit s'étendre
7. **Déséquiper le sac à dos** → l'inventaire doit revenir à la taille de base
8. **Dépasser la capacité** → le surplus doit spawner dans le monde

---

## Bugs corrigés au passage

### Bug 1 — weaponSlots[2] jamais bindé (Equipment.cs)
```csharp
// AVANT (bug silencieux)
weaponSlots[1].equipmentSlotUI.Bind(this, weaponSlots[1]);
weaponSlots[1].equipmentSlotUI.Bind(this, weaponSlots[2]); // ← weaponSlots[1] bindé 2x !

// APRÈS
for (int i = 0; i < weaponSlots.Length; i++)
    weaponSlots[i].equipmentSlotUI?.Bind(this, weaponSlots[i]);
```

### Bug 2 — AcceptsItem() inversé (EquipmentDropZone.cs)
```csharp
// AVANT — retournait true quand le slot N'acceptait PAS
private static bool AcceptsItem(EquipSlot slot, ObjectType itemType)
    => !Utils.FlagsTypeValide(...);  // logique à vérifier selon Utils

// APRÈS — nommé SlotAccepts(), même logique mais intention claire
private static bool SlotAccepts(EquipSlot slot, ObjectType itemType)
    => !Utils.FlagsTypeValide((int)slot.type, (int)itemType);
```
⚠️ Si tes drops ne fonctionnaient pas du tout avant, c'est ce bug.
   Si ça fonctionnait, la logique de FlagsTypeValide était peut-être
   déjà inversée en interne — à vérifier avec ton implémentation de Utils.

### Bug 3 — EquipmentInventory() récursion infinie possible (Equipment.cs)
```csharp
// AVANT — rappelait EquipItem() si remaining > 0, boucle possible
if (remaining > 0 && newSlot != null)
    EquipItem(item, remaining);  // ← pouvait boucler

// APRÈS — EquipmentInventory() supprimé, logique dans PlayerLoadout.TryReceiveItem()
```

---

## Architecture finale

```
PlayerLoadout (coordinateur)
├── Inventory     — stockage données (slots, stacks)
└── Equipment     — slots physiques (GameObject, Transform)

Flux d'un item ramassé :
WorldItem → PlayerLoadout.TryReceiveItem()
              ├─ slot équipement libre ?  → Equipment.EquipItem()
              ├─ sinon                   → Inventory.AddItem()
              └─ surplus                → PlayerLoadout.SpawnInWorld()

Flux d'un déséquipement (clic droit) :
EquipmentSlotUI.OnRightClick
  → EquipmentUnequipHandler.UnequipSlot()
    → PlayerLoadout.UnequipToInventory()
      ├─ Equipment.UnEquipItem()
      ├─ Inventory.AddItem()
      └─ surplus → SpawnInWorld()
```
