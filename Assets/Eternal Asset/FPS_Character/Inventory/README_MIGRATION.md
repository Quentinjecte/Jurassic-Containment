# Nouveau système d'inventaire — Guide complet

Ce document décrit en détail chaque script du système d'inventaire, ses fonctions et la manière de le mettre en place.

---

## Table des matières

1. [Architecture globale](#1-architecture-globale)
2. [Couche Core — Données et logique](#2-couche-core--données-et-logique)
3. [Couche UI — Affichage et interaction](#3-couche-ui--affichage-et-interaction)
4. [Composants joueur et extensions](#4-composants-joueur-et-extensions)
5. [Mise en place pas à pas](#5-mise-en-place-pas-à-pas)
6. [Rétrocompatibilité et migration](#6-rétrocompatibilité-et-migration)

---

## 1. Architecture globale

```
Inventory/
├── Core/                          ← Données pures (sans UI)
│   ├── ItemStack.cs
│   ├── InventorySlot.cs
│   └── Inventory.cs
├── UI/                            ← Affichage et drag & drop
│   ├── InventorySlotUI.cs
│   ├── InventoryGridUI.cs
│   ├── InventoryDragHandler.cs
│   └── InventoryDropZone.cs
├── PlayerInventory.cs             ← Point d'entrée joueur
├── InventoryDiscardHandler.cs     ← Gestion du "jeter"
└── BackPackExtension.cs           ← Extension sac à dos
```

**Principe :** les données (Core) sont séparées de l'affichage (UI). Le `Inventory` ne connaît pas l'interface.

---

## 2. Couche Core — Données et logique

### ItemStack.cs

**Rôle :** Structure de données représentant un stack d'items (référence + quantité). Aucune dépendance UI.

| Propriété / Méthode | Description |
|---------------------|-------------|
| `Data` | Référence à `ObjectMotherData` (ScriptableObject de l'item) |
| `Quantity` | Nombre d'unités dans le stack |
| `ItemStack.Empty` | Stack vide statique |
| `IsEmpty` | `true` si pas de données ou quantité ≤ 0 |
| `SpaceLeft` | Place restante avant `StackMax` |
| `IsFull` | `true` si quantité = `StackMax` |
| `CanStackWith(ItemStack)` | Indique si deux stacks peuvent fusionner (même type) |
| `Add(int amount)` | Retourne un nouveau `ItemStack` avec la quantité augmentée (clampé à `StackMax`) |

**Usage :** Principalement utilisé par `Inventory` et `InventorySlot` pour lire ou construire des stacks. Rarement utilisé directement dans le jeu.

---

### InventorySlot.cs

**Rôle :** Un slot d'inventaire avec restrictions de type. Gère l'ajout, le retrait et l’échange pour ce slot uniquement.

| Propriété / Méthode | Description |
|---------------------|-------------|
| `Data` | Données de l'item actuel (lecture seule) |
| `Quantity` | Quantité actuelle (lecture seule) |
| `AllowedTypes` | Types acceptés (flags `ObjectType`). Par défaut : `Ressource`, `Consomable`, `autre` |
| `IsEmpty` | Slot vide ou non |
| `Stack` | Retourne un `ItemStack` du contenu |
| `AcceptsType(ObjectType)` | Vérifie si un type est accepté |
| `CanAdd(data, amount)` | Vérifie si l’ajout est possible |
| `TryAdd(data, amount)` | Tente d’ajouter. Retourne le **surplus** (0 = tout ajouté) |
| `TryRemove(amount)` | Retire des items. Retourne le **nombre effectivement retiré** |
| `Clear()` | Vide le slot |
| `Set(ItemStack)` | Remplace le contenu du slot |
| `SwapWith(InventorySlot)` | Échange le contenu avec un autre slot |

**Usage :** Créé et géré par `Inventory`. Pas utilisé directement dans la mise en place.

---

### Inventory.cs

**Rôle :** Conteneur d’inventaire : ensemble de slots + logique globale. **Aucune dépendance UI.**

| Propriété | Description |
|-----------|-------------|
| `SlotCount` | Nombre de slots |
| `OnSlotChanged` | `UnityEvent<int, ItemStack>` — appelé quand un slot change (index, nouveau contenu) |
| `OnInventoryChanged` | `UnityEvent` — appelé quand l’inventaire est modifié |
| `GetSlot(int index)` | Retourne le slot à l’index |
| `GetStack(int index)` | Retourne le `ItemStack` du slot |
| `IsValidIndex(int index)` | Vérifie si l’index est valide |

| Méthode | Description |
|---------|-------------|
| `Initialize(int slotCount = -1)` | Crée les slots. Si `slotCount < 0`, utilise la valeur par défaut. |
| `Resize(int newSlotCount)` | Change le nombre de slots (ex. sac à dos). Les anciens slots sont conservés. |
| `AddItem(ObjectMotherData, int)` | Ajoute des items (stacking auto). Retourne le **surplus** (0 = tout ajouté). |
| `RemoveItem(ObjectMotherData, int)` | Retire par type d’item. Retourne le **nombre retiré**. |
| `RemoveFromSlot(int index, int amount)` | Retire d’un slot précis. Retourne le nombre retiré. |
| `SwapSlots(int a, int b)` | Échange le contenu de deux slots. Retourne `true` si OK. |
| `MoveBetweenSlots(int from, int to, int amount = -1)` | Déplace entre slots (split/merge). `amount = -1` = tout. |
| `CountItem(ObjectMotherData)` | Nombre total d’un type d’item. |
| `HasItem(ObjectMotherData)` | Présence ou non de l’item. |
| `GetTotalMass()` | Masse totale de l’inventaire. |
| `SetSlotFilter(int index, ObjectType)` | Restreint les types acceptés pour un slot. |

**Usage :** Ajouté sur le joueur via `PlayerInventory`, ou sur un coffre / conteneur.

---

## 3. Couche UI — Affichage et interaction

### InventorySlotUI.cs

**Rôle :** Affiche un seul slot. Vue uniquement, pas de logique métier.

| Champ (Inspector) | Description |
|-------------------|-------------|
| `Icon Image` | Image affichant l’icône de l’item |
| `Quantity Text` | Texte pour la quantité (ex. "x5") |
| `Empty Sprite` | Sprite affiché quand le slot est vide |

| Propriété / Méthode | Description |
|---------------------|-------------|
| `SlotIndex` | Index du slot dans l’inventaire |
| `Inventory` | Inventaire lié |
| `Bind(Inventory, int index)` | Lie ce composant à un slot. À appeler par `InventoryGridUI`. |
| `Refresh()` | Met à jour l’affichage |
| `OnRightClick` | Événement déclenché au clic droit sur le slot |

**Mise en place :** Sur chaque GameObject représentant un slot (ou sur le prefab de slot).

---

### InventoryGridUI.cs

**Rôle :** Gère une grille de slots (instanciation ou réutilisation des enfants).

| Champ (Inspector) | Description |
|-------------------|-------------|
| `Inventory` | Inventaire à afficher |
| `Slot Prefab` | Prefab de slot (doit contenir `InventorySlotUI`) |
| `Slot Container` | Transform parent des slots (ex. Content d’un ScrollView) |
| `Create Slots At Runtime` | `true` = instancie le prefab pour chaque slot ; `false` = utilise les enfants existants |

| Propriété / Méthode | Description |
|---------------------|-------------|
| `Inventory` | Inventaire affiché |
| `SlotUIs` | Liste des `InventorySlotUI` utilisés |
| `Bind(Inventory)` | Lie l’inventaire et crée ou rebind les slots |
| `Unbind()` | Déconnecte les slots |
| `RefreshAll()` | Rafraîchit tous les slots |
| `OnSlotRightClick` | Événement quand un slot reçoit un clic droit |

**Mise en place :** Sur le GameObject du panneau d’inventaire (ou d’un onglet inventaire).

---

### InventoryDragHandler.cs

**Rôle :** Gère le drag d’un slot (interfaces `IBeginDragHandler`, `IDragHandler`, `IEndDragHandler`).

| Champ (Inspector) | Description |
|-------------------|-------------|
| `Slot UI` | Le `InventorySlotUI` associé. Auto-trouvé via `GetComponentInParent` si vide. |
| `Canvas Group` | Pour alpha/raycasts pendant le drag. Auto-trouvé si vide. |
| `Canvas` | Canvas utilisé pendant le drag. Cherche par tag "Canvas" si vide. |

| Propriété statique | Description |
|--------------------|-------------|
| `DraggedSlot` | Slot actuellement en cours de drag (ou `null`) |
| `IsDragging` | `true` pendant un drag actif |

**Comportement :**
- Au début du drag : bloque les raycasts, rend l’élément semi-transparent, le met au-dessus du Canvas.
- En fin de drag : restaure l’apparence et le parent si aucun drop valide.

**Mise en place :** Sur le même GameObject que `InventorySlotUI` ou sur un enfant cliquable (ex. zone de fond du slot).

---

### InventoryDropZone.cs

**Rôle :** Zone de dépôt pour les items glissés. Gère slots d’inventaire et zone « jeter ».

| Champ (Inspector) | Description |
|-------------------|-------------|
| `Zone Type` | `InventorySlot` = slot normal ; `DiscardZone` = jeter dans le monde |
| `Slot UI` | Pour `InventorySlot` : le slot cible. Auto-trouvé via `GetComponentInChildren` si vide. |
| `Target Inventory` | Pour le transfert entre inventaires distincts (ex. coffre ↔ joueur) |

**Comportement selon le type :**

- **InventorySlot :**
  - Même inventaire : `MoveBetweenSlots` (déplacement ou swap).
  - Inventaires différents : transfert via `Target Inventory`.

- **DiscardZone :**
  - Appelle `OnDiscardRequested`.
  - Cherche `InventoryDiscardHandler` dans les parents et appelle `OnDiscardSlot`.

**Mise en place :**
- Sur chaque slot : `Zone Type = InventorySlot`, `Slot UI` = ce slot.
- Sur la zone « jeter » : `Zone Type = DiscardZone`.

---

## 4. Composants joueur et extensions

### PlayerInventory.cs

**Rôle :** Inventaire du joueur et logique de drop dans le monde. Utilise `Inventory` en interne.

| Champ (Inspector) | Description |
|-------------------|-------------|
| `Base Slot Count` | Nombre de slots de base (ex. 8 pour la barre rapide) |
| `Drop Distance` | Distance devant le joueur pour le drop (m) |

| Propriété / Méthode | Description |
|---------------------|-------------|
| `Inventory` | Accès au `Inventory` interne |
| `ExtendSlots(int additionalSlots)` | Ajoute des slots (ex. sac à dos) |
| `ResetToBaseSize()` | Remet le nombre de slots à la valeur de base (retrait du sac) |
| `DropFromSlot(int index, int amount = -1)` | Jette l’item du slot dans le monde. `amount = -1` = tout. Retourne `true` si réussi. |
| `SpawnWorldItem(ObjectMotherData, int, Vector3)` | Méthode statique pour instancier un item dans le monde. |
| `GetTotalMass()` | Masse totale de l’inventaire. |

**Mise en place :** Sur le GameObject du joueur (avec `FPSController` et `Equipement`). Ajouté automatiquement par `Equipement` si absent.

---

### InventoryDiscardHandler.cs

**Rôle :** Réagit aux actions « jeter » (clic droit ou drop sur `DiscardZone`) en appelant `PlayerInventory.DropFromSlot`.

| Champ (Inspector) | Description |
|-------------------|-------------|
| `Player Inventory` | Inventaire joueur. Auto-trouvé dans les parents ou via `FindAnyObjectByType` si vide. |

| Méthode | Description |
|---------|-------------|
| `OnDiscardSlot(InventorySlotUI slotUI)` | Appelé quand un slot doit être jeté. Réalise le drop via `PlayerInventory`. |

**Mise en place :** Sur le joueur ou sur un parent commun de l’UI (pour être trouvé par `GetComponentInParent`). Référence `PlayerInventory` si besoin.

---

### BackPackExtension.cs

**Rôle :** Définit combien de slots ajoute un sac à dos et notifie le joueur à l’équipement/déséquipement.

| Champ (Inspector) | Description |
|-------------------|-------------|
| `Slot Count` | Nombre de slots ajoutés (ex. 12) |

| Méthode | Description |
|---------|-------------|
| `OnEquipped(GameObject owner)` | Appelée par `Equipement` à l’équipement. Appelle `PlayerInventory.ExtendSlots`. |
| `OnUnequipped(GameObject owner)` | Appelée à la déséquipe. Appelle `PlayerInventory.ResetToBaseSize`. |

**Mise en place :** Sur le prefab du sac à dos, en plus ou à la place de l’ancien `BackPack` selon la migration.

---

## 5. Mise en place pas à pas

### Étape 1 — Inventaire du joueur

1. Ouvrir le prefab du joueur (ex. `G_Operator`).
2. Vérifier qu’il a `FPSController` et `Equipement` (sinon les ajouter).
3. Si nécessaire : Add Component → **PlayerInventory**.
4. Configurer :
   - **Base Slot Count** : 8 (ou selon ton design).
   - **Drop Distance** : 1.5 (ou selon la scène).

`Equipement` ajoute automatiquement `PlayerInventory` s’il manque.

---

### Étape 2 — Prefab de slot

1. Créer un nouveau GameObject (ex. `InventorySlot`).
2. Hiérarchie recommandée :
   ```
   InventorySlot (RectTransform)
   ├── Background (Image) — fond du slot
   └── Content
       ├── Icon (Image) — icône de l’item
       └── Quantity (TextMeshPro) — quantité
   ```
3. Ajouter sur **InventorySlot** :
   - **InventorySlotUI**
     - Icon Image = `Content/Icon`
     - Quantity Text = `Content/Quantity`
     - Empty Sprite = sprite de slot vide (optionnel)
   - **InventoryDragHandler** (sur le même objet ou sur un enfant cliquable)
   - **InventoryDropZone**
     - Zone Type = `InventorySlot`
     - Slot UI = ce slot (ou laisser vide pour auto-détection)
4. Ajouter **CanvasGroup** sur l’objet draggable (pour alpha pendant le drag).
5. Taguer le Canvas principal avec le tag **"Canvas"**.
6. Sauvegarder en prefab (`InventorySlotPrefab`).

---

### Étape 3 — Panneau inventaire

1. Localiser le panneau d’inventaire (ex. sous `TabInterface`).
2. Créer ou réutiliser un conteneur pour les slots (ex. `Content` dans un ScrollView).
3. Sur le panneau ou sur ce conteneur : Add Component → **InventoryGridUI**.
4. Configurer :
   - **Inventory** : `PlayerInventory.Inventory` du joueur (référence directe ou via `PlayerInterfaceSystem.PlayerInventory.Inventory`).
   - **Slot Prefab** : le prefab créé à l’étape 2.
   - **Slot Container** : le Transform parent des slots.
   - **Create Slots At Runtime** : `true` pour génération automatique des slots.
5. S’assurer que `InventoryGridUI` est bien lié à l’inventaire au démarrage (ex. dans `Start` ou via référence).

---

### Étape 4 — Zone « Jeter »

1. Créer une zone visuelle (Image, zone cliquable, etc.) pour le drop « jeter ».
2. Sur cette zone : Add Component → **InventoryDropZone**.
   - Zone Type = **DiscardZone**
3. Sur le joueur ou sur un parent de l’UI : Add Component → **InventoryDiscardHandler**.
   - Référencer **Player Inventory** si l’auto-détection ne suffit pas.
4. Vérifier que le clic droit sur un slot appelle bien `OnDiscardSlot` (via `InventoryGridUI` et `HandleSlotRightClick`).

---

### Étape 5 — Sac à dos (optionnel)

1. Ouvrir le prefab du sac à dos.
2. Add Component → **BackPackExtension**.
3. Configurer **Slot Count** (ex. 12).
4. Garder l’ancien `BackPack` si tu utilises encore `ShowBackPackContentSlot` pour l’UI.

---

### Étape 6 — Vérifications

- Le joueur a `PlayerInventory` et `Inventory` fonctionnels.
- Les slots s’affichent correctement.
- Le drag & drop entre slots fonctionne.
- Le clic droit et le drop sur la zone « jeter » font apparaître l’item dans le monde.
- L’équipement/déséquipement du sac à dos ajoute/supprime des slots.
- `WorldEntity.Take_Interact` ajoute bien les items à l’inventaire du joueur.

---

## 6. Rétrocompatibilité et migration

### Comportement actuel

- **WorldEntity** : essaie `PlayerInventory` en premier, puis l’ancien `BackPack` si nécessaire.
- **Equipement** : stackables → `PlayerInventory` ; armes/équipements physiques → ancien flux `ItemSlot`.
- Les anciens `ItemSlot`, `DragItem`, `DropItem` peuvent coexister pendant la migration.

### Suppression de l’ancien système

Une fois le nouveau système en place et validé :

1. Remplacer les `ItemSlot` par `InventorySlotUI` dans tous les prefabs.
2. Retirer les références à `interfaceSystem.itemSlots`.
3. Adapter `PlayerInterfaceSystem` pour s’appuyer sur `InventoryGridUI` et `PlayerInventory`.
4. Supprimer ou archiver : `ItemSlot.cs`, `DragItem.cs`, `DropItem.cs`.

---

## Schéma des flux

```
[Ramassage d'un item dans le monde]
        ↓
   WorldEntity.Take_Interact
        ↓
   PlayerInventory.Inventory.AddItem()   OU   Equipement.EquipItem() (armes/gear)
        ↓
   OnSlotChanged / OnInventoryChanged
        ↓
   InventorySlotUI.Refresh() — Mise à jour de l’UI

[Jeter un item]
        ↓
   Clic droit OU Drop sur DiscardZone
        ↓
   InventoryDiscardHandler.OnDiscardSlot()
        ↓
   PlayerInventory.DropFromSlot()
        ↓
   SpawnWorldItem() + RemoveFromSlot()
```
