using UnityEngine;

namespace Luvsic.Graybox
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class GrayboxPlayer : MonoBehaviour
    {
        [SerializeField] private GrayboxFlow flow;
        [SerializeField] private Camera viewCamera;
        [SerializeField] private Transform weapon;
        [SerializeField] private MeshRenderer blade;
        [SerializeField] private Material ironBlade;
        [SerializeField] private float moveSpeed = 4.4f;
        [SerializeField] private float mouseSensitivity = 2.1f;
        [SerializeField] private float interactionDistance = 3f;

        private CharacterController controller;
        private Vector3 weaponRest;
        private Quaternion weaponRotation;
        private Material oldBlade;
        private float pitch;
        private float verticalSpeed;
        private float attackCooldown;
        private float attackAnimation;
        private float hitFlash;
        private bool guard;

        public bool Guarding => guard && flow.Panel == DemoPanel.None && Cursor.lockState == CursorLockMode.Locked;
        public float HitFlash => hitFlash;
        public bool LastHitBlocked { get; private set; }

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            if (weapon != null)
            {
                weaponRest = weapon.localPosition;
                weaponRotation = weapon.localRotation;
                oldBlade = blade.sharedMaterial;
            }
        }

        private void Start()
        {
            if (flow.Panel == DemoPanel.None)
                CaptureCursor();
        }

        private void OnDisable() => ReleaseCursor();

        private void Update()
        {
            if (!flow.Paused)
            {
                attackCooldown = Mathf.Max(0f, attackCooldown - Time.deltaTime);
                attackAnimation = Mathf.Max(0f, attackAnimation - Time.deltaTime);
                hitFlash = Mathf.Max(0f, hitFlash - Time.deltaTime * 2.8f);
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                flow.Escape();
                return;
            }

            if (Input.GetKeyDown(KeyCode.Tab))
            {
                flow.TogglePack();
                return;
            }

            if (flow.Panel != DemoPanel.None)
            {
                if (Input.GetKeyDown(KeyCode.E))
                    UsePanelShortcut();
                return;
            }

            if (Cursor.lockState != CursorLockMode.Locked)
            {
                if (Input.GetMouseButtonDown(0))
                    CaptureCursor();
                return;
            }

            Look();
            Move();

            if (Input.GetKeyDown(KeyCode.E))
                TryInteract();

            if (flow.Panel != DemoPanel.None || !flow.Simulating)
                return;

            guard = Input.GetMouseButton(1) && attackAnimation <= 0f;
            if (Input.GetMouseButtonDown(0))
                TryAttack();

            if (Input.GetKeyDown(KeyCode.Q))
                flow.UsePotion();
        }

        private void LateUpdate()
        {
            if (weapon == null)
                return;

            blade.sharedMaterial = flow.HasSword ? ironBlade : oldBlade;
            float swing = Mathf.Sin((attackAnimation / 0.32f) * Mathf.PI);
            Vector3 position = Guarding ? new Vector3(-0.22f, 0.14f, 0.07f) : new Vector3(-0.24f * swing, 0.05f * swing, 0.24f * swing);
            Quaternion rotation = Guarding ? Quaternion.Euler(4f, -18f, 53f) : Quaternion.Euler(12f * swing, -70f * swing, 30f * swing);
            weapon.localPosition = weaponRest + position;
            weapon.localRotation = weaponRotation * rotation;
        }

        public GrayboxInteractable LookTarget()
        {
            Transform view = viewCamera.transform;
            if (!Physics.Raycast(view.position, view.forward, out RaycastHit hit, interactionDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                return null;

            return hit.collider.GetComponentInParent<GrayboxInteractable>();
        }

        private bool TryInteract() => flow.Interact(LookTarget());

        private bool TryAttack()
        {
            if (!flow.Simulating || Guarding || attackCooldown > 0f)
                return false;

            attackCooldown = flow.HasSword ? 0.6f : 0.7f;
            attackAnimation = 0.32f;
            Transform view = viewCamera.transform;
            if (Physics.SphereCast(view.position, 0.14f, view.forward, out RaycastHit hit, 2f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                var enemy = hit.collider.GetComponentInParent<DemoEnemy>();
                if (enemy != null)
                    enemy.TakeHit(flow.HasSword ? 28 : 20);
            }

            return true;
        }

        public void ShowHit(bool blocked)
        {
            hitFlash = 1f;
            LastHitBlocked = blocked;
        }

        public void CaptureCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        public void ReleaseCursor()
        {
            guard = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void UsePanelShortcut()
        {
            switch (flow.Panel)
            {
                case DemoPanel.Warehouse:
                case DemoPanel.Merchant:
                    flow.ClosePanel();
                    break;
                case DemoPanel.Departure:
                    flow.Depart();
                    break;
                case DemoPanel.Settlement:
                    flow.ReturnToCamp();
                    break;
            }
        }

        private void Look()
        {
            transform.Rotate(0f, Input.GetAxisRaw("Mouse X") * mouseSensitivity, 0f);
            pitch = Mathf.Clamp(pitch - Input.GetAxisRaw("Mouse Y") * mouseSensitivity, -85f, 85f);
            viewCamera.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        private void Move()
        {
            float side = (Input.GetKey(KeyCode.D) ? 1f : 0f) - (Input.GetKey(KeyCode.A) ? 1f : 0f);
            float forward = (Input.GetKey(KeyCode.W) ? 1f : 0f) - (Input.GetKey(KeyCode.S) ? 1f : 0f);
            Vector3 input = Vector3.ClampMagnitude(new Vector3(side, 0f, forward), 1f);

            if (controller.isGrounded)
                verticalSpeed = -2f;

            verticalSpeed += Physics.gravity.y * Time.deltaTime;
            float speed = Input.GetKey(KeyCode.LeftShift) ? moveSpeed * 1.35f : moveSpeed;
            Vector3 movement = transform.TransformDirection(input) * speed + Vector3.up * verticalSpeed;
            controller.Move(movement * Time.deltaTime);
        }
    }
}
