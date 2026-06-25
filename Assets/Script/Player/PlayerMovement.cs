using GlobalEnum;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Assets.Script.Player
{
    public class PlayerMovement : NetworkBehaviour
    {
        public delegate bool ActionConditionDelegate();
        public ActionConditionDelegate _sprintActionCondition;
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        [Header("References")]
        [SerializeField] private Camera _mainCamera;
        [SerializeField] private CharacterController _controller;
        [SerializeField] private Animator _animator;
        [SerializeField] private PlayerCondition _condition;

        [Header("Mouvement")]
        public MovementState _movementState;
        public PoseState _poseState;

        [Header("Settings")]
        [SerializeField] private Vector2 angleLimite;
        [SerializeField] private float crouchSpeed = 1.75f;
        [SerializeField] private float walkSpeed = 3.5f;
        [SerializeField] private float sprintSpeed = 7f;
        [SerializeField] private float acceleration = 12f;
        [SerializeField] private float deceleration = 10f;
        [SerializeField] private float airControl = 0.5f;
        [SerializeField] private float speed = 1f;

        [Header("Souris")]
        [SerializeField] private float sensivity = 1f;

        [Header("Jump")]
        [SerializeField] private float jumpHeight = 1.4f;
        [SerializeField] private float jump = 1f;
        [SerializeField] private float gravity = -22f;

        [Header("Ground Check")]
        [SerializeField] private float groundRadius = 0.35f;
        [SerializeField] private float groundOffset = 0.1f;
        [SerializeField] private LayerMask groundMask;
        public bool isGrounded;

        private PlayerInput _playerInput;
        private Vector3 velocity;
        private Vector3 _originalCenter;
        private Vector2 inputDir;
        private Vector2 _lookDeltaInput;
        private float _originalHeight;
        private float _pitch;

        private static readonly int InAir = Animator.StringToHash("InAir");
        private static readonly int MoveX = Animator.StringToHash("MoveX");
        private static readonly int MoveY = Animator.StringToHash("MoveY");
        private static readonly int Velocity = Animator.StringToHash("Velocity");
        private static readonly int Moving = Animator.StringToHash("Moving");
        private static readonly int Crouching = Animator.StringToHash("Crouching");
        private static readonly int Sprinting = Animator.StringToHash("Sprinting");

        private void Awake()
        {
            _mainCamera = Camera.main;

            _animator = GetComponent<Animator>();
            _playerInput = GetComponent<PlayerInput>();
            _controller = GetComponent<CharacterController>();
            _condition = GetComponent<PlayerCondition>();

            _playerInput.actions["Move"].performed += OnMove;
            _playerInput.actions["Move"].canceled += OnMove;
            _playerInput.actions["Jump"].performed += OnJump;
            _playerInput.actions["Jump"].canceled += OnJump;
            _playerInput.actions["Sprint"].performed += OnSprint;
            _playerInput.actions["Sprint"].canceled += OnSprint;
            _playerInput.actions["Crouch"].performed += OnCrouch;
            _playerInput.actions["Crouch"].canceled += OnCrouch;
            _playerInput.actions["Look"].performed += OnLook;
            _playerInput.actions["Look"].canceled += OnLook;
        }

        private void OnDestroy()
        {
            _playerInput.actions["Move"].performed -= OnMove;
            _playerInput.actions["Move"].canceled -= OnMove;
            _playerInput.actions["Jump"].performed -= OnJump;
            _playerInput.actions["Jump"].canceled -= OnJump;
            _playerInput.actions["Sprint"].performed -= OnSprint;
            _playerInput.actions["Sprint"].canceled -= OnSprint;
            _playerInput.actions["Crouch"].performed -= OnCrouch;
            _playerInput.actions["Crouch"].canceled -= OnCrouch;
            _playerInput.actions["Look"].performed -= OnLook;
            _playerInput.actions["Look"].canceled -= OnLook;
        }

        public bool IsMoving() => !Mathf.Approximately(inputDir.normalized.magnitude, 0f);

        public bool CanSprint()
        {
            bool conditionCheck = false;
            if (_sprintActionCondition != null)
            {
                conditionCheck = _sprintActionCondition.Invoke();
            }

            return _poseState == PoseState.Standing && conditionCheck;
        }

        private void Start()
        {
            _originalHeight = _controller.height;
            _originalCenter = _controller.center;
            _poseState = PoseState.Standing;

            //SkillManager.instance.OnEffectChanged += Stat;
        }
        private void Stat(StatInstance instance, float delta)
        {
            switch (instance.Config.startValue)
            {
                case (float)PlayerStat.Speed: Apply(ref speed, delta); break;
                case (float)PlayerStat.JumpForce: Apply(ref jump, delta); break;
                case (float)PlayerStat.Sens: Apply(ref sensivity, delta); break;
            }
        }

        private void Apply(ref float stat, float delta)
        {
            stat = delta;
        }

        private void Update()
        {
            //if(!IsOwner) return; // <--------- Probleme ici

            GroundCheck();
            Move();
            ApplyGravity();

            UpdateLookInput();
            UpdateMovementState();
            UpdateConsume();
            UpdateMovement();
            UpdateAnimatorParams();
        }

        private void UpdateLookInput()
        {
            transform.rotation *= Quaternion.Euler(0f, _lookDeltaInput.x * sensivity, 0f);

            _pitch -= _lookDeltaInput.y * sensivity;
            _pitch = Mathf.Clamp(_pitch, angleLimite.x, angleLimite.y);
            _mainCamera.transform.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
        }

        public void OnLook(InputAction.CallbackContext ctx)
        {
            if (ctx.performed)
                _lookDeltaInput = ctx.ReadValue<Vector2>();

            if (ctx.canceled)
                _lookDeltaInput = Vector2.zero;
        }

        public void OnMove(InputAction.CallbackContext ctx)
        {
            if (ctx.ReadValue<Vector2>().magnitude > Vector2.zero.magnitude)
                inputDir = ctx.ReadValue<Vector2>();
            if (ctx.ReadValue<Vector2>() == Vector2.zero)
                inputDir = Vector2.zero;
        }

        private void UpdateAnimatorParams()
        {
            var animatorVelocity = inputDir;
            //animatorVelocity *= MovementState == MovementState.InAir ? 0f : 1f;

            _animator.SetFloat(MoveX, animatorVelocity.x);
            _animator.SetFloat(MoveY, animatorVelocity.y);
            _animator.SetFloat(Velocity, animatorVelocity.magnitude);
            _animator.SetBool(InAir, !isGrounded);
            _animator.SetBool(Moving, IsMoving());

            float sprintWeight = _animator.GetFloat(Sprinting);
            //float t = Mathf.Lerp(0, 1,);
            sprintWeight = Mathf.Lerp(sprintWeight, _movementState == MovementState.Sprinting ? 1f : 0f, Time.deltaTime);
            _animator.SetFloat(Sprinting, sprintWeight);

            /*        _inputController.SetValue(FPSANames.MoveInput,
                        new Vector4(AnimatorVelocity.x, AnimatorVelocity.y));*/
        }

        public void OnJump(InputAction.CallbackContext ctx)
        {
            if (ctx.performed && isGrounded)
            {
                float _jump = jumpHeight * jump;
                velocity.y = Mathf.Sqrt(_jump * -2f * gravity);
                _condition.OnConsume(PlayerStat.Energie, 10);
                _movementState = MovementState.InAir;
            }
        }

        private void UpdateMovementState()
        {
            if (_movementState == MovementState.InAir)
            {
                return;
            }
            //Debug.Log(!GetComponent<SurvivalSystem>().IsBusy);
            // If still can sprint, keep the sprinting state.
            if (_movementState == MovementState.Sprinting
                && inputDir.y > 0f && Mathf.Approximately(inputDir.x, 0f))
            {
                Debug.Log("Sprinting");
                return;
            }

            if (!IsMoving())
            {
                _movementState = MovementState.Idle;
                return;
            }

            _movementState = MovementState.Walking;
        }

        private void UpdateConsume()
        {
            float delta = (_poseState, _movementState) switch
            {
                (PoseState.Standing, MovementState.Idle) => -0.05f,
                (PoseState.Crouching, MovementState.Idle) => -0.075f,
                (PoseState.Standing, MovementState.Walking) => 0f,
                (PoseState.Crouching, MovementState.Walking) => -0.01f,
                (_, MovementState.Sprinting) => 0.15f,
                (_, MovementState.InAir) => -0.025f,
                _ => 0f
            };

            _condition.OnConsume(PlayerStat.Energie, delta);
        }


        private void UpdateMovement() => _controller.Move(Time.deltaTime * velocity);

        private void ApplyGravity()
        {
            velocity.y += gravity * Time.deltaTime;
        }

        private void GroundCheck()
        {
            Vector3 origin = transform.position;

            isGrounded = Physics.CheckSphere(
                origin,
                groundRadius,
                groundMask,
                QueryTriggerInteraction.Ignore
            );

            if (isGrounded && velocity.y < 0)
                velocity.y = -2f; // stick to ground
        }

        private void Move()
        {
            float moveSpeed = (_poseState, _movementState) switch
            {
                (PoseState.Crouching, _) => crouchSpeed,
                (PoseState.Standing, MovementState.Sprinting) => sprintSpeed,
                _ => walkSpeed
            };

            Vector3 targetVelocity = transform.TransformDirection(new Vector3(inputDir.x, 0, inputDir.y)) * moveSpeed;

            float lerpSpeed = inputDir.magnitude > 0.1f
                ? acceleration
                : deceleration;

            if (!isGrounded)
                lerpSpeed *= airControl;

            lerpSpeed *= speed;

            Vector3 horizontalVel = new Vector3(velocity.x, 0, velocity.z);
            Vector3 newHorizontal = Vector3.Lerp(horizontalVel, targetVelocity, lerpSpeed * Time.deltaTime);

            velocity.x = newHorizontal.x;
            velocity.z = newHorizontal.z;
        }

        public void OnCrouch(InputAction.CallbackContext ctx)
        {
            /*if (!ctx.performed)
                return;*/

            if (_poseState != PoseState.Crouching)
            {
                Crouch();
                return;
            }

            /*if (!CanUnCrouch())
            {
                return;
            }*/
            _poseState = PoseState.Standing;
            UnCrouch();
        }

        private void UnCrouch()
        {
            _controller.height = _originalHeight;
            _controller.center = _originalCenter;

            //_movementState = MovementState.Idle;

            _animator.SetBool(Crouching, false);
        }

        private void Crouch()
        {
            float crouchedHeight = _originalHeight * .75f;
            float heightDifference = _originalHeight - crouchedHeight;

            _controller.height = crouchedHeight;

            // Adjust the center position so the bottom of the capsule remains at the same position
            Vector3 crouchedCenter = _originalCenter;
            crouchedCenter.y -= heightDifference / 2;
            _controller.center = crouchedCenter;

            _poseState = PoseState.Crouching;

            _animator.SetBool(Crouching, true);
        }

        public void OnSprint(InputAction.CallbackContext ctx)
        {
            //bool enableSprint = CanSprint();
            bool enableSprint = ctx.performed && !GetComponent<PlayerCondition>().IsBusy;
            Debug.Log(enableSprint);
            if (enableSprint)
            {
                Debug.Log(enableSprint);
                _movementState = MovementState.Sprinting;
                return;
            }

            _movementState = MovementState.Walking;
        }
    }
}