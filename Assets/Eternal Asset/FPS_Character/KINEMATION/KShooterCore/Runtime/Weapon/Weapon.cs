// Designed by KINEMATION, 2025.

using KINEMATION.ProceduralRecoilAnimationSystem.Runtime;
using UnityEngine;

namespace KINEMATION.KShooterCore.Runtime.Weapon
{
    public abstract class Weapon : MonoBehaviour
    {
        public virtual string GetWeaponName()
        {
            return string.Empty;
        }

        public virtual int GetActiveAmmo()
        {
            return 0;
        }

        public virtual int GetMaxAmmo()
        {
            return 0;
        }

        public virtual FireMode GetFireMode()
        {
            return FireMode.Semi;
        }
    }
}