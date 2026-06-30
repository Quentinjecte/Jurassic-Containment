// Designed by KINEMATION, 2025.

using KINEMATION.KAnimationCore.Runtime.Core;
using Demo.Scripts.Runtime.Item;
using KINEMATION.TacticalShooterPack.Scripts;
using UnityEngine;


[AddComponentMenu("KINEMATION/Tactical Shooter Pack/Mag Animator")]
public class MagAnimator : MonoBehaviour
{
    [Header("MagazineLenght")]
    [SerializeField, Min(0)] protected int magCapacity;
    [SerializeField, Min(0f)] protected float timeStep;
    [SerializeField, Min(0f)] protected float interpSpeed;
    [SerializeField, Range(0f, 1f)] protected float magProgress;
    public int _ammo;
        
    protected Animator _animator;
    protected Weapon _weapon;

    protected float _bulletsAnimLength;
    public AnimatorStateName Animator_MagProgress = new AnimatorStateName("MagProgress");

    private void Start()
    {
        _weapon = transform.GetComponentInParent<Weapon>();
        _animator = GetComponent<Animator>();
        //magCapacity = GetComponent<WorldEntity>().data is MagazineSO mag ? mag.MagCapacity : 30;
        _bulletsAnimLength = _animator.runtimeAnimatorController.animationClips[0].length;
    }

    private void Update()
    {
        if (_animator == null) return;
            
        if (_weapon != null)
        {
            int activeAmmo = _weapon.GetAmmo();

            if (activeAmmo >= magCapacity)
            {
                magProgress = 0f;
            }
            else
            {
                magProgress = KMath.FloatInterp(magProgress, (magCapacity - activeAmmo) * timeStep / _bulletsAnimLength, 
                    interpSpeed, Time.deltaTime);
                magProgress = Mathf.Clamp01(magProgress);
            }
        }
            
        _animator.SetFloat(Animator_MagProgress.hash, magProgress);
    }
}
