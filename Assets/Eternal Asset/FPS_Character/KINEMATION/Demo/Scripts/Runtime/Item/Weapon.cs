// Designed by KINEMATION, 2025.

using Demo.Scripts.Runtime.AttachmentSystem;
using Demo.Scripts.Runtime.Character;
using GlobalEnum;
using KINEMATION.FPSAnimationFramework.Runtime.Camera;
using KINEMATION.FPSAnimationFramework.Runtime.Core;
using KINEMATION.FPSAnimationFramework.Runtime.Playables;
using KINEMATION.FPSAnimationFramework.Runtime.Recoil;
using KINEMATION.KAnimationCore.Runtime.Input;
using KINEMATION.ProceduralRecoilAnimationSystem.Runtime;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Demo.Scripts.Runtime.Item
{
    [RequireComponent(typeof(AudioSource), typeof(BoxCollider), typeof(Rigidbody))]
    [RequireComponent(typeof(Bullet))]
    public class Weapon : FPSItem
    {
        [Header("General")]
        public MagazineType magazineType;
        [SerializeField] [Range(0f, 120f)] private float fieldOfView = 90f;
        
        [Header("Firing"), Space(5f)]
        [SerializeField] private FPSAnimationAsset fireClip;
        [SerializeField] private FPSAnimationAsset emptyFireClip;
        public event Action<Transform> onFireEvent;
        
        [Header("Reloading"), Space(5f)]
        [SerializeField] private Transform magazineTransform;
        [SerializeField] private MagAnimator _magAnimator;
        [SerializeField] private MagAnimator _previousMagAnimator;
        [SerializeField] private bool ReloadIncremental;
        [SerializeField] private FPSAnimationAsset tacReloadClip;
        [SerializeField] private FPSAnimationAsset emptyReloadClip;
        [SerializeField] private FPSAnimationAsset noMagReloadClip;
        [SerializeField] private FPSAnimationAsset reloadLoop;
        [SerializeField] private FPSAnimationAsset reloadEnd;

        [SerializeField] private FPSCameraAnimation cameraReloadAnimation;
        
        [Header("Inspecting"), Space(5f)]
        [SerializeField] private FPSAnimationAsset inspectClip;
        [SerializeField] private FPSAnimationAsset magCheckClip;
        [SerializeField] private FPSCameraAnimation cameraGrenadeAnimation;

        [Header("Recoil"), Space(5f)]
        [SerializeField] private RecoilAnimData recoilData;
        [SerializeField] private RecoilPatternSettings recoilPatternSettings;
        [SerializeField] private FPSCameraShake cameraShake;
        [Min(0f)] [SerializeField] private float fireRate;

        [SerializeField] private bool supportsAuto;
        [SerializeField] private bool supportsBurst;
        [SerializeField] private int burstLength;

        [Header("SFX/Effect"), Space(5f)]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip fireSound;
        [SerializeField] private AudioClip fireMode;
        [SerializeField] private AudioClip Equip;
        [SerializeField] private AudioClip Unequip;
        [SerializeField] private AudioClip AimIn;
        [SerializeField] private AudioClip AimOut;
        [SerializeField] private Transform onFireEffect;
        [SerializeField] private Transform canon;

        [Header("Extra"), Space(5f)]
        [SerializeField] private int MagazineLenght;
        [SerializeField] private int _ammo;
        [SerializeField] private float mass = 2.3f; // En KG // Egal a la mass de l'arme plus le nombre de balle ( pas dynamic commence par l'arme)
        [SerializeField] protected bool _skipFirstShell;
        [SerializeField] protected bool _skipFirstReload;

        [Header("Attachments"), Space(5f)] 
        [SerializeField]
        public AttachmentGroup<BaseAttachment> barrelAttachments = new AttachmentGroup<BaseAttachment>();
        
        [SerializeField]
        public AttachmentGroup<BaseAttachment> gripAttachments = new AttachmentGroup<BaseAttachment>();
        
        [SerializeField]
        public List<AttachmentGroup<ScopeAttachment>> scopeGroups = new List<AttachmentGroup<ScopeAttachment>>();
        
        //~ Controller references

        private FPSController _fpsController;
        private Animator _controllerAnimator;
        private UserInputController _userInputController;
        private IPlayablesController _playablesController;
        private FPSCameraController _fpsCameraController;
        
        private FPSAnimator _fpsAnimator;
        private FPSAnimatorEntity _fpsAnimatorEntity;

        private RecoilAnimation _recoilAnimation;
        private RecoilPattern _recoilPattern;
        
        //~ Controller references
        
        //private PlayerLoadout _loadout;
        //private MagazineSO _mag;
        private Animator _weaponAnimator;

        private int _scopeIndex;

        public event Action<bool> OnCameraActive;
        private float _lastRecoilTime;

        private int _bursts;
        private FireMode _fireMode = FireMode.Semi;

        private static readonly int CurveEquip = Animator.StringToHash("CurveEquip");
        private static readonly int CurveUnequip = Animator.StringToHash("CurveUnequip");
        private static readonly int TacReloadInsertHash = Animator.StringToHash("TacReloadInsert");
        private static readonly int ReloadInsertHash = Animator.StringToHash("ReloadInsert");
        private static readonly int ReloadLoopHash = Animator.StringToHash("ReloadLoop");
        private static readonly int ReloadEndHash = Animator.StringToHash("ReloadEnd");

        private void Start()
        {
            OnCameraActive += UpdateCamera;

            audioSource = GetComponent<AudioSource>();
            if(magazineTransform != null)
                _magAnimator = magazineTransform.GetComponentInChildren<MagAnimator>();

            if(barrelAttachments.attachments.Count > 1)
            {
                fireSound = barrelAttachments.attachments[0].fireSound;
                canon = barrelAttachments.attachments[0].Canon;
            }
        }

        public void OnActionEnded()
        {
            if (_fpsController == null) return;
            _fpsController.ResetActionState();
        }

        protected void UpdateTargetFOV(bool isAiming)
        {
            float fov = fieldOfView;
            float sensitivityMultiplier = 1f;
            
            if (isAiming && scopeGroups.Count != 0)
            {
                var scope = scopeGroups[_scopeIndex].GetActiveAttachment();
                fov *= scope.aimFovZoom;

                sensitivityMultiplier = scopeGroups[_scopeIndex].GetActiveAttachment().sensitivityMultiplier;

                var _cam = scopeGroups[_scopeIndex].GetActiveAttachment().Camera;
            }

            _userInputController.SetValue("SensitivityMultiplier", sensitivityMultiplier);
            _fpsCameraController.UpdateTargetFOV(fov);
        }

        private void UpdateCamera(bool b)
        {
            var _cam = scopeGroups[_scopeIndex].GetActiveAttachment().Camera;
            if(_cam != null)
                _cam.SetActive(b);
        }

        protected void UpdateAimPoint()
        {
            if (scopeGroups.Count == 0) return;

            var scope = scopeGroups[_scopeIndex].GetActiveAttachment().aimPoint;
            _fpsAnimatorEntity.defaultAimPoint = scope;
        }
        
        protected void InitializeAttachments()
        {
            foreach (var attachmentGroup in scopeGroups)
            {
                attachmentGroup.Initialize(_fpsAnimator);
            }
            
            _scopeIndex = 0;
            if (scopeGroups.Count == 0) return;

            //audioSource.volume = MasterVolumeManager.instance.GetEffectVolum();

            UpdateAimPoint();
            UpdateTargetFOV(false);
        }
        
        public override void OnEquip(GameObject parent)
        {
            if (parent == null) return;

            if(Equip != null)
                audioSource.PlayOneShot(Equip);

            //_loadout = parent.GetComponent<PlayerLoadout>(); // ← ajouter

            _fpsAnimator = parent.GetComponent<FPSAnimator>();
            _fpsAnimatorEntity = GetComponent<FPSAnimatorEntity>();
            
            _fpsController = parent.GetComponent<FPSController>();
            _weaponAnimator = GetComponentInChildren<Animator>();
            
            _controllerAnimator = parent.GetComponent<Animator>();
            _userInputController = parent.GetComponent<UserInputController>();
            _playablesController = parent.GetComponent<IPlayablesController>();
            _fpsCameraController = parent.GetComponentInChildren<FPSCameraController>();

            if (overrideController != _controllerAnimator.runtimeAnimatorController)
            {
                _playablesController.UpdateAnimatorController(overrideController);
            }
            
            InitializeAttachments();
            
            _recoilAnimation = parent.GetComponent<RecoilAnimation>();
            _recoilPattern = parent.GetComponent<RecoilPattern>();
            
            _fpsAnimator.LinkAnimatorProfile(gameObject);
            
            barrelAttachments.Initialize(_fpsAnimator);
            gripAttachments.Initialize(_fpsAnimator);
            
            _recoilAnimation.Init(recoilData, fireRate, _fireMode);

            if (_recoilPattern != null)
            {
                _recoilPattern.Init(recoilPatternSettings);
            }
            
            _fpsAnimator.LinkAnimatorLayer(equipMotion);
        }

        public override void OnUnEquip()
        {
            if(Unequip != null)
                audioSource.PlayOneShot(Unequip);
            _fpsAnimator.LinkAnimatorLayer(unEquipMotion);
        }

        public override bool OnAimPressed()
        {
            _playablesController.StopAnimation(0f);
            if(AimIn != null)
                audioSource.PlayOneShot(AimIn);
            OnCameraActive?.Invoke(true);
            _userInputController.SetValue(FPSANames.IsAiming, true);
            UpdateTargetFOV(true);
            _recoilAnimation.isAiming = true;
            
            return true;
        }

        public override bool OnAimReleased()
        {
            if(AimOut != null && _recoilAnimation.isAiming == true)
                audioSource.PlayOneShot(AimOut);
            OnCameraActive?.Invoke(false);
            _userInputController.SetValue(FPSANames.IsAiming, false);
            UpdateTargetFOV(false);
            _recoilAnimation.isAiming = false;
            
            return true;
        }

        public override bool OnFirePressed()
        {
            // Do not allow firing faster than the allowed fire rate.
            if (Time.unscaledTime - _lastRecoilTime < 60f / fireRate)
            {
                return false;
            }
            
            _playablesController.StopAnimation(0.1f);
            _lastRecoilTime = Time.unscaledTime;
            _bursts = burstLength;

            OnFire();
            
            return true;
        }

        public override bool OnFireReleased()
        {
            if (_recoilAnimation != null)
            {
                _recoilAnimation.Stop();
            }
            
            if (_recoilPattern != null)
            {
                _recoilPattern.OnFireEnd();
            }
            
            CancelInvoke(nameof(OnFire));
            return true;
        }

        public override bool OnReload()
        {
            if (!FPSAnimationAsset.IsValid(tacReloadClip)) return false;

            Debug.Log(HasMagazine);

            // ── 1. Chercher un chargeur compatible ────────────────────────────
            /*if (_loadout == null)
            {
                Debug.LogWarning("[Weapon] PlayerLoadout introuvable.");
                return false;
            }
            if (magazineTransform != null)
            {
                var (mag, source, slotIndex) = _loadout.FindBestMagazine(magazineType);

                if (mag == null)
                {
                    // Aucun chargeur disponible — jouer animation à vide si besoin
                    Debug.Log("[Weapon] Aucun chargeur compatible en inventaire.");
                    return false;
                }

                // ── 2. Mettre à jour MagazineLenght et _ammo depuis le chargeur trouvé ──
                int newAmmo = mag.CurrentAmmo > 0 ? mag.CurrentAmmo : mag.MagCapacity;

                // ── 3. Éjecter le chargeur actuel si non vide (rechargement tactique)
                // Le chargeur éjecté repart en inventaire avec les balles restantes
                ReturnPartialMagToInventory();

                // ── 4. Consommer le chargeur de l'inventaire ──────────────────────
                _loadout.ConsumeMagazine(mag, source, slotIndex);
                MagazineLenght = mag.MagCapacity;
                _mag = mag;
            }*/
            //_magAnimator.gameObject 
            if (ReloadIncremental)
            {
                ReloadInsert();
                return true;
            }

            FPSAnimationAsset _reloadClip = _ammo > 0 ? tacReloadClip : emptyReloadClip;
            FPSAnimationAsset reloadClip = _magAnimator == null  ? noMagReloadClip : _reloadClip;

            string str_reloadClip = _ammo == 0 ? "ReloadEmpty" : "Reload";
            string str_noMagReloadClip = _magAnimator == null ? "InsertMagazine" : str_reloadClip;


            if (_previousMagAnimator != null)
                Destroy(_previousMagAnimator.gameObject, 1);

            if (audioSource != null)
            {
                audioSource.PlayOneShot(reloadClip.audioClip);
            }

            _playablesController.PlayAnimation(reloadClip, 0f);
            
            if (_weaponAnimator != null)
            {
                _weaponAnimator.Rebind();
                _weaponAnimator.Play(str_noMagReloadClip, 0);
            }

            if (_fpsCameraController != null)
            {
                _fpsCameraController.PlayCameraAnimation(cameraReloadAnimation);
            }

            Invoke(nameof(OnActionEnded), reloadClip.animClip.length * 0.85f);

            _ammo = MagazineLenght;

            OnFireReleased();
            return true;
        }
        public void LoadMagazine()
        {
            /*var go = Instantiate(_mag.Prefab, magazineTransform);
            _previousMagAnimator = _magAnimator;
            _magAnimator = go.GetComponent<MagAnimator>();
            HasMagazine = true;*/
        }
        /// <summary>
        /// Remet le chargeur partiel dans l'inventaire quand le joueur recharge
        /// avec des balles restantes (rechargement tactique).
        /// </summary>
        private void ReturnPartialMagToInventory()
        {
            /*MagazineSO _currentMagData = magazineTransform != null
                ? _magAnimator?.GetComponent<WorldEntity>().data as MagazineSO
                : null;

            if (_loadout == null || _currentMagData == null ) return;

            // Créer une copie du chargeur partiel avec les balles restantes
            // et le remettre en inventaire
            var partialMag = _currentMagData; // référence au SO du chargeur actif
                                              // Note : si tu veux des chargeurs vraiment distincts (instances),
                                              // il faudra créer des instances de MagazineSO au runtime.
                                              // Pour simplifier, on utilise le même SO avec CurrentAmmo mis à jour.
            _loadout.TryReceiveItem(partialMag, 1);*/
        }

        private void ReloadInsert()
        {
            if (_ammo == MagazineLenght) return;

            _skipFirstShell = _ammo > 0;

            var reloadClip = _ammo == 0 ? emptyReloadClip : tacReloadClip;

            int animName = _ammo == 0
                ? TacReloadInsertHash
                : ReloadInsertHash;

            _playablesController.PlayAnimation(reloadClip, 0);
            _weaponAnimator.Play(animName, 0, 0f);

            if (audioSource != null)
                audioSource.PlayOneShot(reloadClip.audioClip);
        }

        public void ReloadWeapon()
        {
            if (!_skipFirstShell) _ammo++; 
            _skipFirstShell = false;

            bool isFull = _ammo == MagazineLenght;
            var reloadClip = isFull ? reloadEnd : reloadLoop;

            var animName = isFull
                ? ReloadEndHash
                : ReloadLoopHash;

            _playablesController.PlayAnimation(reloadClip, 0);
            _weaponAnimator.Play(animName, 0, 0f);

            if (!isFull)
            {
                if (reloadClip.audioClip != null)
                    audioSource.PlayOneShot(reloadClip.audioClip);
            }

            OnActionEnded();
            OnFireReleased();
        }

        /*private IEnumerator OnInsertEnded()
        {
            bool firstInsert = _ammo == 0;

            while (_ammo < MagazineLenght)
            {
                var reloadClip = firstInsert ? emptyReloadClip : tacReloadClip;
                string animName = firstInsert ? "TacReloadInsert" : "ReloadInsert";

                if (audioSource != null)
                    audioSource.PlayOneShot(reloadClip.audioClip);

                _playablesController.PlayAnimation(reloadClip, 0f);

                if (_weaponAnimator != null)
                    _weaponAnimator.Play(animName, 0, 0f);

                if (_fpsCameraController != null)
                    _fpsCameraController.PlayCameraAnimation(cameraReloadAnimation);

                _ammo++;

                yield return new WaitForSeconds(reloadClip.animClip.length);

                firstInsert = false;
            }

            var reloadClipEnd = reloadEnd;

            _playablesController.PlayAnimation(reloadClipEnd, 0f);

            if (_weaponAnimator != null)
                _weaponAnimator.Play("ReloadEnd", 0, 0f);

            if (_fpsCameraController != null)
                _fpsCameraController.PlayCameraAnimation(cameraReloadAnimation);

            Invoke(nameof(OnActionEnded), reloadClipEnd.animClip.length * 0.85f);

            OnFireReleased();
        }*/

        public override bool OnGrenadeThrow()
        {
            if (!FPSAnimationAsset.IsValid(inspectClip))
            {
                return false;
            }

            _playablesController.PlayAnimation(inspectClip, 0f);
            
            if (_fpsCameraController != null)
            {
                _fpsCameraController.PlayCameraAnimation(cameraGrenadeAnimation);
            }
            
            Invoke(nameof(OnActionEnded), inspectClip.animClip.length * 0.8f);
            return true;
        }
        
        private void OnFire()
        {
            var _fireclip = _ammo > 0 ? fireSound : emptyFireClip.audioClip;

            audioSource.volume = UnityEngine.Random.Range(0.95f, 1);
            audioSource.PlayOneShot(_fireclip);

            if (_ammo <= 0)
            {
                _recoilAnimation?.Stop();
                _recoilPattern?.OnFireEnd();
                OnFireReleased();
                return;
            }

            _ammo--;

            if(onFireEffect != null)
                foreach (ParticleSystem particle in onFireEffect.GetComponentsInChildren<ParticleSystem>())
                {
                    particle.Play();
                }

            string str_fireclip = _ammo > 0 ? "Fire" : "FireEmpty";

            if (_weaponAnimator != null)
            {
                _weaponAnimator.Play(str_fireclip, 0, 0f);
            }

            onFireEvent?.Invoke(canon);
            
            _fpsCameraController.PlayCameraShake(cameraShake);
            
            if(fireClip != null) _playablesController.PlayAnimation(fireClip);

            if (_recoilAnimation != null && recoilData != null)
            {
                _recoilAnimation.Play();
            }

            if (_recoilPattern != null)
            {
                _recoilPattern.OnFireStart();
            }


            if (_recoilAnimation.fireMode == FireMode.Semi)
            {
                Invoke(nameof(OnFireReleased), 60f / fireRate);
                return;
            }
            
            if (_recoilAnimation.fireMode == FireMode.Burst)
            {
                _bursts--;
                
                if (_bursts == 0)
                {
                    OnFireReleased();
                    return;
                }
            }

            Invoke(nameof(OnFire), 60f / fireRate);
        }

        public override void OnCycleScope()
        {
            if (scopeGroups.Count == 0) return;
            
            _scopeIndex++;
            _scopeIndex = _scopeIndex > scopeGroups.Count - 1 ? 0 : _scopeIndex;
            
            UpdateAimPoint();
            UpdateTargetFOV(true);
        }

        private void CycleFireMode()
        {
            if (_fireMode == FireMode.Semi && supportsBurst)
            {
                _fireMode = FireMode.Burst;
                _bursts = burstLength;
                return;
            }

            if (_fireMode != FireMode.Auto && supportsAuto)
            {
                _fireMode = FireMode.Auto;
                return;
            }

            _fireMode = FireMode.Semi;
        }
        
        public override void OnChangeFireMode()
        {
            CycleFireMode();
            _recoilAnimation.fireMode = _fireMode;
            audioSource.PlayOneShot(fireMode);
        }
        
        public override void OnInspect()
        {
            if (FPSAnimationAsset.IsValid(inspectClip) == false) return;

            if (_playablesController != null)
            {
                _playablesController.PlayAnimation(inspectClip, 0f);
            }
            if (_weaponAnimator != null)
            {
                _weaponAnimator.Play("Inspect", 0, 0);
            }

            audioSource.PlayOneShot(inspectClip.audioClip);
            Invoke(nameof(OnActionEnded), inspectClip.animClip.length * 0.85f);
        }

        public override void OnAttachmentChanged(int attachmentTypeIndex, int attachmentIndex)
        {
            if (attachmentTypeIndex == 2)
            {
                gripAttachments.NewAttachments(_fpsAnimator, attachmentIndex);
                return;
            }
            if (attachmentTypeIndex == 3)
            {
                barrelAttachments.NewAttachments(_fpsAnimator, attachmentIndex);
                fireSound = barrelAttachments.attachments[attachmentIndex].fireSound;
                onFireEffect.position = barrelAttachments.attachments[attachmentIndex].Canon.position; 
                //canon = barrelAttachments.attachments[attachmentIndex].Canon;
                return;
            }

            if (scopeGroups.Count == 0) return;
            scopeGroups[attachmentTypeIndex].NewAttachments(_fpsAnimator, attachmentIndex);
            UpdateAimPoint();
        }

        public int GetAmmo() => _ammo;

        internal void OnMagCheck()
        {
            if (FPSAnimationAsset.IsValid(magCheckClip) == false) return;

            if (_playablesController != null)
            {
                _playablesController.PlayAnimation(magCheckClip, 0f);
            }
            if (_weaponAnimator != null)
            {
                _weaponAnimator.Play("MagInspect", 0, 0);
            }

            audioSource.PlayOneShot(magCheckClip.audioClip);
            Invoke(nameof(OnActionEnded), magCheckClip.animClip.length * 0.85f);
        }
    }
}