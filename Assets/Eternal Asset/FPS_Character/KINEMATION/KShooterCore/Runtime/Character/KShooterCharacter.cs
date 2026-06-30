// Designed by KINEMATION, 2025.

using KINEMATION.KShooterCore.Runtime.Weapon;
using UnityEngine;

namespace KINEMATION.KShooterCore.Runtime.Character
{
    public abstract class KShooterCharacter : MonoBehaviour
    {
        public virtual Weapon.Weapon GetActiveShooterWeapon()
        {
            return null;
        }
    }
}