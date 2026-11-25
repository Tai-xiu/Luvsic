using UnityEngine;

namespace Luvsic.Graybox
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class GrayboxPlayer : MonoBehaviour
    {
        [SerializeField] private GrayboxFlow flow;
        [SerializeField] private Camera viewCamera;
        [SerializeField] private float moveSpeed = 4.4f;
        [SerializeField] private float mouseSensitivity = 2.1f;
        [SerializeField] private float interactionDistance = 3f;

        private CharacterController controller;
        private float pitch;
        private float verticalSpeed;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
        }

        private void Start()
        {
            CaptureCursor();
        }

        private void OnDisable()
        {
            ReleaseCursor();
        }

        private void Update()
        {
            if (flow.WarehouseOpen)
            {
                if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Escape))
                    flow.CloseWarehouse();
                return;
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                ReleaseCursor();
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
        }

        public GrayboxInteractable LookTarget()
        {
            Transform view = viewCamera.transform;
            if (!Physics.Raycast(view.position, view.forward, out RaycastHit hit, interactionDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                return null;

            return hit.collider.GetComponentInParent<GrayboxInteractable>();
        }

        private bool TryInteract()
        {
            return flow.Interact(LookTarget());
        }

        public void CaptureCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        public void ReleaseCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
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
            Vector3 movement = transform.TransformDirection(input) * moveSpeed + Vector3.up * verticalSpeed;
            controller.Move(movement * Time.deltaTime);
        }
    }
}
