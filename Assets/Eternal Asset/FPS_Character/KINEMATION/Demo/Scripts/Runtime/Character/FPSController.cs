// Designed by KINEMATION, 2025.
// Ajusted by Quentinjecte, 2025.

using Demo.Scripts.Runtime.Item;
using KINEMATION.FPSAnimationFramework.Runtime.Core;
using KINEMATION.FPSAnimationFramework.Runtime.Recoil;
using KINEMATION.KAnimationCore.Runtime.Input;
using KINEMATION.KAnimationCore.Runtime.Rig;
using System;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Interactions;

namespace Demo.Scripts.Runtime.Character
{
    public enum FPSAimState
    {
        None,
        Ready,
        Aiming,
        PointAiming
    }

    public enum FPSActionState
    {
        None,
        PlayingAnimation,
        WeaponChange,
        Inspect
    }

    [RequireComponent(typeof(CharacterController), typeof(FPSMovement))]
    //[RequireComponent(typeof(PlayerInterfaceSystem))]
    public class FPSController : MonoBehaviour
    {
        [SerializeField] private FPSControllerSettings settings;

        private FPSMovement _movementComponent;

        [Header("Interracting"), Space(5f)]
        private RaycastHit[] results = new RaycastHit[2];  // Pre-allocated array for // 2 for skip if i hit my weapon in hand
        public float interactRange = 1.5f;
        public LayerMask hiting;


        [Header("Ohter"), Space(5f)]
        public Transform _weaponBone;
        private Vector2 _playerInput;

        public int _pendingItemIndex;

        public FPSItem _activeItem;
        public FPSItem _previousItem;

        private FPSAimState _aimState;
        [SerializeField] private FPSActionState _actionState;

        private Animator _animator;

        // ~Scriptable Animation System Integration
        private FPSAnimator _fpsAnimator;
        private UserInputController _userInput;
        public UserInputController UserInput => _userInput;

        public event Action OnEnableAim;
        public event Action OnDisableAim;

        // ~Scriptable Animation System Integration

        [SerializeField] private FPSItem _unArmed;

        private Vector2 _lookDeltaInput;

        private RecoilPattern _recoilPattern;
        private int _sensitivityMultiplierPropertyIndex;
        public bool onHand; // definie l'arme dans les mais (c'est pour eviter qui le GO soit en setactive(false)).
        public bool Unarmed; // definie l'arme dans les mais (c'est pour eviter qui le GO soit en setactive(false)).
        private AudioSource audioSource;
        [SerializeField] private AudioResource leanSFX;
        [SerializeField] private AudioResource crouchSFX;
        [SerializeField] private AudioResource proneSFX;

        #region Static
        private static readonly int _fullBodyWeightHash = Animator.StringToHash("FullBodyWeight");
        private static readonly int _proneWeightHash = Animator.StringToHash("ProneWeight");
        private static readonly int _slideHash = Animator.StringToHash("Sliding");
        #endregion

        #region Player
        private PlayerInput _playerInputSystem;
        //public Equipment Equipement;

        #endregion
            float _pressTime = 0;
        private void PlayTransitionMotion(FPSAnimatorLayerSettings layerSettings)
        {
            if (layerSettings == null)
            {
                return;
            }
            
            _fpsAnimator.LinkAnimatorLayer(layerSettings);
        }

        private bool IsSprinting()
        {
            return _movementComponent.MovementState == FPSMovementState.Sprinting;
        }
        
        private bool HasActiveAction()
        {
            return _actionState != FPSActionState.None && _actionState != FPSActionState.Inspect;
        }
        private bool HasChangeAction()
        {
            return _actionState == FPSActionState.WeaponChange;
        }
        private bool HasIspectAction()
        {
            return _actionState != FPSActionState.Inspect;
        }

        private bool IsAiming()
        {
            return _aimState is FPSAimState.Aiming or FPSAimState.PointAiming;
        }

        private void InitializeMovement()
        {
            _movementComponent = GetComponent<FPSMovement>();
            
            _movementComponent.onJump = () => { PlayTransitionMotion(settings.jumpingMotion); };
            _movementComponent.onLanded = () => { PlayTransitionMotion(settings.jumpingMotion); };

            _movementComponent.onCrouch = OnCrouch;
            _movementComponent.onUncrouch = OnUncrouch;

            _movementComponent.onSprintStarted = OnSprintStarted;
            _movementComponent.onSprintEnded = OnSprintEnded;

            _movementComponent.onSlideStarted = OnSlideStarted;

            _movementComponent._slideActionCondition += () => !HasActiveAction();
            _movementComponent._sprintActionCondition += () => !HasActiveAction();
            _movementComponent._proneActionCondition += () => !HasActiveAction();
            
            _movementComponent.onStopMoving = () =>
            {
                PlayTransitionMotion(settings.stopMotion);
            };
            
            _movementComponent.onProneEnded = () =>
            {
                _userInput.SetValue(FPSANames.PlayablesWeight, 1f);
            };
        }
        private void InitializeBindingKey()
        {
            _playerInputSystem.actions["Fire"].performed += OnFire;
            _playerInputSystem.actions["Aim"].performed += OnAim;
            _playerInputSystem.actions["ThrowGrenade"].performed += OnThrowGrenade;
            _playerInputSystem.actions["Unarmed"].performed += OnUnarmed;
            _playerInputSystem.actions["Reload"].performed += OnReload;
            _playerInputSystem.actions["CheckMagazine"].performed += OnCheckMagazine;
            _playerInputSystem.actions["CycleScope"].performed += OnCycleScope;
            _playerInputSystem.actions["ChangeFireMode"].performed += OnChangeFireMode;
            _playerInputSystem.actions["ChangeWeapon"].performed += OnChangeWeapon;
            _playerInputSystem.actions["Interact"].performed += OnInteract;
            _playerInputSystem.actions["Inspect"].performed += OnInspect;
            _playerInputSystem.actions["Look"].performed += OnLook;
            _playerInputSystem.actions["Lean"].performed += OnLean;
            _playerInputSystem.actions["DigitAxis"].performed += OnDigitAxis;

            _playerInputSystem.actions["Fire"].canceled += OnFire;
            _playerInputSystem.actions["Aim"].canceled += OnAim;
            _playerInputSystem.actions["Look"].canceled += OnLook;
            _playerInputSystem.actions["Lean"].canceled += OnLean;
        }
        private void OnDestroy()
        {
            _playerInputSystem.actions["Fire"].performed -= OnFire;
            _playerInputSystem.actions["Aim"].performed -= OnAim;
            _playerInputSystem.actions["ThrowGrenade"].performed -= OnThrowGrenade;
            _playerInputSystem.actions["Unarmed"].performed -= OnUnarmed;
            _playerInputSystem.actions["Reload"].performed -= OnReload;
            _playerInputSystem.actions["CheckMagazine"].performed -= OnCheckMagazine;
            _playerInputSystem.actions["CycleScope"].performed -= OnCycleScope;
            _playerInputSystem.actions["ChangeFireMode"].performed -= OnChangeFireMode;
            _playerInputSystem.actions["ChangeWeapon"].performed -= OnChangeWeapon;
            _playerInputSystem.actions["Interact"].performed -= OnInteract;
            _playerInputSystem.actions["Inspect"].performed -= OnInspect;
            _playerInputSystem.actions["Look"].performed -= OnLook;

            _playerInputSystem.actions["Fire"].canceled -= OnFire;
            _playerInputSystem.actions["Aim"].canceled -= OnAim;
            _playerInputSystem.actions["Look"].canceled -= OnLook;
            _playerInputSystem.actions["Lean"].canceled -= OnLean;
        }

        private void InitializeWeapons()
        {
            int weaponIndex = 0;

            foreach (var prefab in settings.weaponPrefabs)
            {
                weaponIndex++;

                if (prefab == null)
                    continue;

                var weapon = Instantiate(prefab, transform.position, Quaternion.identity);

                var weaponTransform = weapon.transform;

                weaponTransform.parent = _weaponBone;
                weaponTransform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

                //GetComponent<PlayerLoadout>().TryReceiveItem(weapon.GetComponent<WorldEntity>().data, 1, weapon);

                weapon.SetActive(false);
            }

            var unArmed = Instantiate(settings.unArmedPrefabs, transform.position, Quaternion.identity);
            var unArmedTransform = unArmed.transform;
            unArmedTransform.parent = _weaponBone;
            unArmedTransform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            _unArmed = unArmed.GetComponent<FPSItem>();
            _unArmed.gameObject.SetActive(false);
            //_activeItem = Equipement.GetFPSItem() ?? _unArmed;
            Unarmed = _unArmed ? true : false;
        }

        private void Start()
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;

            _fpsAnimator = GetComponent<FPSAnimator>();
            _fpsAnimator.Initialize();

            _weaponBone = GetComponentInChildren<KRigComponent>().GetRigTransform(settings.weaponBone);
            _animator = GetComponent<Animator>();

            _userInput = GetComponent<UserInputController>();
            _recoilPattern = GetComponent<RecoilPattern>();

            _playerInputSystem = GetComponent<PlayerInput>();
           // Equipement = GetComponent<Equipment>();
            audioSource = GetComponent<AudioSource>();

            InitializeWeapons();
            InitializeMovement();
            InitializeBindingKey();

            _actionState = FPSActionState.None;
            EquipItem();

            _sensitivityMultiplierPropertyIndex = _userInput.GetPropertyIndex("SensitivityMultiplier");
        }

        private void UnequipItem()
        {
            DisableAim();
            _actionState = FPSActionState.WeaponChange;
            GetActiveItem().OnUnEquip();
        }

        public void ResetActionState()
        {
            _actionState = FPSActionState.None;
        }

        public void OnUnarmed(InputAction.CallbackContext ctx)
        {
            StartWeaponChange(settings.unArmedPrefabs.GetComponent<FPSItem>());
            Debug.Log("Unarmed");
        }

        private void EquipItem()
        {
            if(_pendingItemIndex == 0) 
                //if (Equipement.AllEquipSlot(ObjectType.p_Arme).Length == 0) return;

            if (!onHand && _previousItem != null)
                    _previousItem.gameObject.SetActive(false);
            onHand = false;

            GetActiveItem().gameObject.SetActive(true);
            GetActiveItem().OnEquip(gameObject);

            _actionState = FPSActionState.None;
            _actionState = FPSActionState.None;
        }

        private void DisableAim()
        {
            if (GetActiveItem().OnAimReleased()) _aimState = FPSAimState.None;
        }
        
        private void OnFirePressed()
        {
            //if (Equipement.AllEquipSlot(ObjectType.p_Arme).Length== 0 || HasActiveAction()) return;
            GetActiveItem().OnFirePressed();
        }

        private void OnFireReleased()
        {
            //if (Equipement.AllEquipSlot(ObjectType.p_Arme).Length== 0) return;
            GetActiveItem().OnFireReleased();
        }

        private FPSItem GetActiveItem() => /*Equipement.AllEquipSlot(ObjectType.p_Arme).Length == 0 ? null :*/ _activeItem;
        /*{
            if (_instantiatedWeapons.Length== 0) return null;
            return _activeItem;
        }*/

        private void OnSlideStarted()
        {
            _animator.CrossFade(_slideHash, 0.2f);
        }
        
        private void OnSprintStarted()
        {
            OnFireReleased();
            DisableAim();

            _aimState = FPSAimState.None;
            
            _userInput.SetValue(FPSANames.StabilizationWeight, 0f);
            _userInput.SetValue("LookLayerWeight", 0.3f);
        }

        private void OnSprintEnded()
        {
            _userInput.SetValue(FPSANames.StabilizationWeight, 1f);
            _userInput.SetValue("LookLayerWeight", 1f);
        }

        private void OnCrouch()
        {
            PlayTransitionMotion(settings.crouchingMotion);
            if (crouchSFX != null)
            {
                audioSource.resource = crouchSFX;
                audioSource.Play();
            }
        }

        private void OnUncrouch()
        {
            PlayTransitionMotion(settings.crouchingMotion); 
            if(crouchSFX  != null)
            {
                audioSource.resource = crouchSFX;
                audioSource.Play();
            }
        }

        private bool _isLeaning;

        private void StartWeaponChange(FPSItem item)
        {
            if (item == _activeItem)
                return;

            UnequipItem();
            OnFireReleased();

            _previousItem = _activeItem;
            _activeItem = item; 

            Invoke(nameof(EquipItem), settings.equipDelay);

            /*
            je swap 2 passe aolrs a 1
            dans le modulo alors il commence a 1 puis rajoute 1
            donc revient a l'arme blanche
             */
        }

        private void UpdateLookInput()
        {
            float scale = _userInput.GetValue<float>(_sensitivityMultiplierPropertyIndex);
            
            float deltaMouseX = _lookDeltaInput.x * settings.sensitivity * scale;
            float deltaMouseY = -_lookDeltaInput.y * settings.sensitivity * scale;
            
            _playerInput.y += deltaMouseY;
            _playerInput.x += deltaMouseX;
            
            if (_recoilPattern != null)
            {
                _playerInput += _recoilPattern.GetRecoilDelta();
                deltaMouseX += _recoilPattern.GetRecoilDelta().x;
            }
            
            float proneWeight = _animator.GetFloat(_proneWeightHash);
            Vector2 pitchClamp = Vector2.Lerp(new Vector2(-90f, 90f), new Vector2(-30, 0f), proneWeight);

            _playerInput.y = Mathf.Clamp(_playerInput.y, pitchClamp.x, pitchClamp.y);
            
            transform.rotation *= Quaternion.Euler(0f, deltaMouseX, 0f);
            
            _userInput.SetValue(FPSANames.MouseDeltaInput, new Vector4(deltaMouseX, deltaMouseY));
            _userInput.SetValue(FPSANames.MouseInput, new Vector4(_playerInput.x, _playerInput.y));
        }

        private void OnMovementUpdated()
        {
            float playablesWeight = 1f - _animator.GetFloat(_fullBodyWeightHash);
            _userInput.SetValue(FPSANames.PlayablesWeight, playablesWeight);
        }

        private void Update()
        {
            Time.timeScale = settings.timeScale;
            UpdateLookInput();
            OnMovementUpdated();
            _playerInputSystem.actions.FindActionMap("UI").Enable();
        }

        private void FixedUpdate()
        {
            FindNearestInteractable();
        }

        private void FindNearestInteractable()
        {
            /*if (InputManager.IsPointerOverUI() || _playerInputSystem.currentActionMap.name == "UI")
            {
                TextPopUp.instance.HideStaticLabel();
                return;
            }*/

            int hitCount = Physics.RaycastNonAlloc(
                Camera.main.transform.position,
                Camera.main.transform.forward,
                results,
                interactRange
            );

            if (hitCount == 0)
            {
                TextPopUp.instance.HideStaticLabel();
                return;
            }

            for (int i = 0; i < hitCount; i++)
            {
                var hit = results[i];

                if (hit.collider == null)
                    continue;

                if (_activeItem != null &&
                    hit.collider.gameObject == _activeItem.gameObject)
                    continue;

                /*if (!hit.collider.TryGetComponent(out IInteractible interact))
                    continue;*/

                //TextPopUp.instance.ShowStaticLabel(interact.InteractionPrompt);
                return; // On prend le premier valide
            }
            TextPopUp.instance.HideStaticLabel();
        }

        public void UnEquipItemOnStuff(FPSItem item)
        {
            if (_activeItem != _unArmed)
            {
                if (_activeItem == item)
                {
                    onHand = true;
                    _actionState = FPSActionState.None;
                    ChangeWeapon();
                }
            }
        }

#if ENABLE_INPUT_SYSTEM
        public void OnReload(InputAction.CallbackContext ctx)
        {
            if (IsSprinting() || HasActiveAction() || !GetActiveItem().OnReload()) return;
            _actionState = FPSActionState.PlayingAnimation;
        }
        public void OnCheckMagazine(InputAction.CallbackContext ctx)
        {
            if (IsSprinting() || HasActiveAction() || !GetActiveItem()) return;
            if (!GetActiveItem().HasMagazine) return;

            _actionState = FPSActionState.PlayingAnimation;
            var weapon = GetActiveItem() as Weapon;
            if (weapon != null)
                weapon.OnMagCheck();
            _actionState = FPSActionState.PlayingAnimation;
        }       

        public void OnThrowGrenade(InputAction.CallbackContext ctx)
        {
            if (IsSprinting()|| HasActiveAction() || !GetActiveItem().OnGrenadeThrow()) return;
            _actionState = FPSActionState.PlayingAnimation;
        }
        
        public void OnFire(InputAction.CallbackContext ctx)
        {
            if (IsSprinting()) return;
            
            if (ctx.performed)
            {
                OnFirePressed();
                return;
            }
            
            OnFireReleased();
        }

        public void OnAim(InputAction.CallbackContext ctx)
        {
            if (IsSprinting()) return;
            if (HasChangeAction()) return;

            if (ctx.performed && !IsAiming())
            {
                if (GetActiveItem().OnAimPressed()) _aimState = FPSAimState.Aiming;
                PlayTransitionMotion(settings.aimingMotion);
                OnDisableAim?.Invoke();
                return;
            }

            if (ctx.canceled && IsAiming())
            {
                DisableAim();
                PlayTransitionMotion(settings.aimingMotion);
                OnEnableAim?.Invoke();
            }
        }

        public void OnChangeWeapon(InputAction.CallbackContext ctx) => ChangeWeapon();

        public void ChangeWeapon(int nxtWeapon = -1)
        {
            /*if (_movementComponent.PoseState == FPSPoseState.Prone) return;
            if (HasActiveAction() || Equipement.AllEquipSlot(ObjectType.p_Arme).Length == 0) return;

            if(nxtWeapon == -1)
                nxtWeapon = Math.Max(Array.IndexOf(Equipement.AllEquipSlot(ObjectType.p_Arme), _activeItem), 0);

            for (int i = 0; i < Equipement.AllEquipSlot(ObjectType.p_Arme).Length; i++)
            {
                FPSItem candidate = Equipement.AllEquipSlot(ObjectType.p_Arme)[(nxtWeapon + i) % Equipement.AllEquipSlot(ObjectType.p_Arme).Length].FPSItem;
                Debug.Log(candidate + "" + _activeItem);
                if (_activeItem == candidate) continue;
                if (candidate != null)
                {
                    Unarmed = false;
                    StartWeaponChange(candidate);
                    return;
                }
            }
            
            bool unarmed = Array.TrueForAll(Equipement.AllEquipSlot(ObjectType.p_Arme), w => w.item == null);
            if (!unarmed)
                return;

            if (Array.IndexOf(Equipement.AllEquipSlot(ObjectType.p_Arme), _activeItem) == -1)
                StartWeaponChange(_unArmed); //Aucune arme possédé
            Unarmed = true;*/

            /* Logic
             
            on commence avec 0 - 2
            swap 0 à 1
            on a 1 - 2

            appel de la foction

            suivant = 2
            précédant = 1

            appel de la foction

            suivant = 1
            précédant = 2


            _activeWeaponIndex = 1
            _previousWeaponIndex = 0 // default

            1 passe a 2
            2 = vrai
            _activeWeaponIndex = 2
            _previousWeaponIndex = 1


            ----------
            _activeWeaponIndex = 2
            _previousWeaponIndex = 1

            2 passe a 0
            0 = null 
            0 passe a 1 
            1 = vrai

            _activeWeaponIndex = 1
            _activeWeaponIndex = 2

             */
        }

        public void OnLook(InputAction.CallbackContext ctx)
        {
            if (ctx.performed)
                _lookDeltaInput = ctx.ReadValue<Vector2>();

            if (ctx.canceled)
                _lookDeltaInput = Vector2.zero;
        }

        public void OnLean(InputAction.CallbackContext ctx)
        {
            _userInput.SetValue(FPSANames.LeanInput, ctx.ReadValue<float>() * settings.leanAngle);
            audioSource.resource = leanSFX;
            audioSource.Play();
            PlayTransitionMotion(settings.leanMotion);
        }

        public void OnCycleScope(InputAction.CallbackContext ctx)
        {
            if (!IsAiming()) return;
            
            GetActiveItem().OnCycleScope();
            PlayTransitionMotion(settings.aimingMotion);
        }

        public void OnChangeFireMode(InputAction.CallbackContext ctx)
        {
            GetActiveItem().OnChangeFireMode();
        }

        public void OnInspect(InputAction.CallbackContext ctx)
        {
            if (HasActiveAction() && _actionState != FPSActionState.Inspect) return;

            _actionState = FPSActionState.Inspect;

            GetActiveItem().OnInspect();
            //PlayTransitionMotion(settings.);
        }

        /*public void OnDigitAxis(InputValue value)
        {
            if (!value.isPressed || _actionState != FPSActionState.Inspect) return;
            GetActiveItem().OnAttachmentChanged((int) value.Get<float>(), (int)value.Get<float>());
        }*/

        private void OnInteract(InputAction.CallbackContext ctx)
        {
            Debug.Log("press");
            if (!ctx.performed)
                return;

            Debug.Log("perform");
            Ray ray = new Ray(Camera.main.transform.position, Camera.main.transform.forward);

            Debug.Log(ray);
            // 1. Raycast principal
            if (!Physics.Raycast(ray, out RaycastHit hit, interactRange))
                return;

            Debug.Log(hit);
            Vector3 start = hit.point;
            Vector3 direction = Camera.main.transform.forward;

            // 2. SphereCast au bout du ray
            RaycastHit[] sphereHits = Physics.SphereCastAll(start, .5f, direction, 0.1f);

            Debug.Log(sphereHits);
            if (sphereHits.Length == 0)
                return;

            // 3. Trier une seule fois
            Array.Sort(sphereHits, (a, b) => a.distance.CompareTo(b.distance));

            // 4. Prendre le plus proche
            /*RaycastHit closest = sphereHits.First(w => w.collider.TryGetComponent<IInteractible>(out _));

            Debug.Log(closest);
            // 5. Ignorer l'objet tenu
            if (!Unarmed && closest.collider.gameObject == _activeItem.gameObject)
                return;

            GameObject go = hit.collider.gameObject;
            var interactibles = go.GetComponents<IInteractible>();

            Debug.Log(interactibles.Length);
            if (interactibles.Length == 0)
                return;

            // 6. Interaction
            foreach (var interactible in interactibles)
            {
                if (ctx.interaction is TapInteraction)
                    interactible.Use_Interact(gameObject);
                else if (ctx.interaction is HoldInteraction)
                {
                    Debug.Log("Hold");
                    interactible.Take_Interact(gameObject);
                }
            }*/
        }


        // A voir avec UIStackManager pour l'inputSystem diseable
        // 2 condition pour l'inventaire
        /*
         * S'active et ce désactive en appuyant sur l'a touche d'inventaire
         * Ce désactive en appuyant sur echappe
         */

        public void OnDigitAxis(InputAction.CallbackContext ctx) // Pad car _inputAsset ne repere pas les chiffre mod
        {
            /*var slot = GetComponent<PlayerLoadout>();
            var item = slot.InventorySocket.GetSlot((int)ctx.ReadValue<float>());

            if (item == null) return;
            Debug.Log("axis");
            var go = Instantiate(item.Data.Prefab, _weaponBone);

            var fpsitem = go.GetComponent<FPSItem>();
            if (fpsitem == null) 
            {
                DestroyImmediate(go);
                StartWeaponChange(_unArmed);
                return;
            }

            StartWeaponChange(fpsitem);*/
        }
#endif
    }
}