using System.Globalization;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Assets.Script.Player 
{
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(PlayerInput))]
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(PlayerCondition))]

    public class PlayerSystem : NetworkBehaviour
    {
        [Header("References")]
        [SerializeField] private Camera _mainCamera;
        [SerializeField] private GameObject Option;

        [Header("Settings")]
        [SerializeField] private float interactRange;

        private RaycastHit[] results = new RaycastHit[2];  // Pre-allocated array for // 2 for skip if i hit my weapon in hand

        private IInteract currentInteract;
        private bool isHoldingInteract;

        public bool showGizmos = true;
        public Color gizmoColor = Color.cyan;

        private PlayerInput _playerInput;

        private void Awake()
        {
            _playerInput = GetComponent<PlayerInput>();
            _mainCamera = Camera.main;

            _playerInput.actions["Interact"].performed += OnInteract;
            _playerInput.actions["Interact"].canceled += OnInteract;
            _playerInput.actions["Escape"].performed += OnEscape;
            _playerInput.actions["Escape"].canceled += OnEscape;

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void OnDestroy()
        {
            _playerInput.actions["Interact"].performed -= OnInteract;
            _playerInput.actions["Interact"].canceled -= OnInteract;
            _playerInput.actions["Escape"].performed -= OnEscape;
            _playerInput.actions["Escape"].canceled -= OnEscape;
        }


        private void Update()
        {
            if (!IsOwner) return;

            FindNearestInteractable();
            UpdateInteract();
        }

        private void FindNearestInteractable() // Affiche un text si le raycast touche un objet interactible.
        {
            if (UIManager.IsPointerOverUI() || _playerInput.currentActionMap.name == "UI")
            {
                TextPopUp.instance.HideStaticLabel();
                return;
            }

            int hitCount = Physics.RaycastNonAlloc(
                _mainCamera.transform.position,
                _mainCamera.transform.forward,
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

                if (!hit.collider.TryGetComponent(out IInteract interact))
                    continue;

                currentInteract = interact;

                interact.Show();
                return; // On prend le premier valide
            }
            TextPopUp.instance.HideStaticLabel();
            currentInteract = null;
        }

        private void UpdateInteract()
        {
            if (!isHoldingInteract)
                return;

/*            Ray ray = new(
                _mainCamera.transform.position,
                _mainCamera.transform.forward);

            if (!Physics.Raycast(ray, out RaycastHit hit, interactRange))
                return;

            IInteract interact =
                hit.collider.GetComponentInParent<IInteract>();

            if (interact == null)
                return;*/

            //currentInteract = interact;

            currentInteract.HoldInteract(gameObject);
        }


        private void OnInteract(InputAction.CallbackContext ctx)
        {
            if (ctx.performed)
            {
               /*Ray ray = new Ray(
                    _mainCamera.transform.position,
                    _mainCamera.transform.forward);

                if (!Physics.Raycast(ray, out RaycastHit hit, interactRange))
                    return;

                IInteract interact =
                    hit.collider.GetComponentInParent<IInteract>();

                if (interact == null)
                    return;

                currentInteract = interact;*/

                currentInteract.Interact(gameObject);

                isHoldingInteract = true;
            }

            if (ctx.canceled)
            {
                isHoldingInteract = false;

                currentInteract?.EndInteract(gameObject);

                currentInteract = null;
            }
        }

        public void OnEscape(InputAction.CallbackContext ctx)
        {
            if (!ctx.performed)
                return;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Option.SetActive(true);
        }

        public void OnSpell(InputAction.CallbackContext ctx)
        {

        }

        private void OnDrawGizmos()
        {
            if (!showGizmos) return;

            // Flèche montrant la direction "sortante" du socket
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(transform.position, Vector3.down * 0.1f);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, .15f);
        }

        /*
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position + Vector3.down * 0.1f, 0.1f);
        }*/
    }
}
