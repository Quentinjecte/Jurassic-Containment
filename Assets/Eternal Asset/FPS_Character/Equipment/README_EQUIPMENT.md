# Système d'équipement UI

## Scripts

| Script | Rôle |
|--------|------|
| **EquipmentSlotUI** | Affiche un slot (icône de l'item équipé) |
| **EquipmentGridUI** | Grille de slots, crée ou bind les EquipmentSlotUI |
| **EquipmentDragHandler** | Drag & drop depuis un slot équipement |
| **EquipmentDropZone** | Zone de drop sur un slot (équiper depuis inventaire ou swap) |
| **EquipmentUnequipHandler** | Déséquipe vers inventaire (clic droit) ou monde (zone jeter) |

## Mise en place

### 1. Prefab de slot équipement

1. Créer un GameObject avec Image (icône)
2. Ajouter **EquipmentSlotUI** (réf. Image, Empty Sprite)
3. Ajouter **EquipmentDragHandler** (sur le même objet ou parent)
4. Ajouter **EquipmentDropZone** (Slot UI = ce slot)
5. Sauvegarder en prefab

### 2. Grille d'équipement

1. Sur le panneau équipement : **EquipmentGridUI**
2. Référencer **Equipement** (du joueur)
3. Slot Prefab, Slot Container, Create Slots At Runtime
4. Optionnel : **Slot Sources** pour choisir l'ordre (Weapon 0,1,2, Gear 0,1,2,3, Backpack)
5. Si vide : ordre par défaut = armes, gear, sac

### 3. Déséquipement

1. **EquipmentUnequipHandler** sur le joueur ou parent de l'UI
2. Référencer Equipement et PlayerInventory
3. Clic droit sur un slot = déséquipe vers inventaire
4. Drop sur zone "Jeter" = jette dans le monde

### 4. Flux

- **Inventaire → Équipement** : Glisser un item (avec Prefab) sur un slot équipement
- **Équipement → Équipement** : Glisser pour swap entre slots compatibles
- **Équipement → Inventaire** : Clic droit
- **Équipement → Monde** : Glisser sur zone Jeter
