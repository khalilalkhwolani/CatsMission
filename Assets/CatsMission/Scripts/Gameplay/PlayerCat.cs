using UnityEngine;

namespace CatsMission
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerCat : MonoBehaviour
    {
        [SerializeField] private float speed = 5.5f;
        [SerializeField] private float jumpHeight = 1.8f;
        [SerializeField] private float gravity = -22;
        [SerializeField] private Transform model;
        [SerializeField] private Transform[] paws;
        [SerializeField] private Transform tail;
        private CharacterController controller;
        private LevelController level;
        private Transform view;
        private float verticalSpeed, coyote, jumpBuffer;
        private Vector3[] pawOrigins;
        // A short probe keeps the grounded state stable on CharacterController's skin margin.
        public bool IsGrounded => controller != null && (controller.isGrounded || (verticalSpeed <= 0 &&
            Physics.SphereCast(transform.position + Vector3.up * .4f, .22f, Vector3.down,
                out _, .24f, ~0, QueryTriggerInteraction.Ignore)));
        public Vector3 Velocity => controller == null ? Vector3.zero : controller.velocity;
        public void Configure(Transform visual, Transform[] feet, Transform catTail)
        { model = visual; paws = feet; tail = catTail; }
        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            pawOrigins = new Vector3[paws == null ? 0 : paws.Length];
            for (int i = 0; i < pawOrigins.Length; i++) pawOrigins[i] = paws[i].localPosition;
        }
        public void Initialize(LevelController owner, Transform camera) { level = owner; view = camera; }
        private void Update()
        {
            if (level == null || !level.CanPlay) return;
            Step(new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")),
                Input.GetKeyDown(KeyCode.Space), Time.deltaTime);
            if (transform.position.y < -5) level.Fall();
        }
        // The same entry point serves keyboard input and deterministic movement smoke tests.
        public void Step(Vector2 input, bool jump, float delta)
        {
            if (delta <= 0 || controller == null || (level != null && !level.CanPlay)) return;
            Vector3 forward = view == null ? Vector3.forward : Vector3.ProjectOnPlane(view.forward, Vector3.up).normalized;
            Vector3 right = view == null ? Vector3.right : Vector3.ProjectOnPlane(view.right, Vector3.up).normalized;
            Vector3 motion = Vector3.ClampMagnitude(right * input.x + forward * input.y, 1);
            if (IsGrounded) { coyote = .12f; if (verticalSpeed < 0) verticalSpeed = -2; }
            else coyote -= delta;
            jumpBuffer = jump ? .12f : jumpBuffer - delta;
            if (jumpBuffer > 0 && coyote > 0)
            { verticalSpeed = Mathf.Sqrt(jumpHeight * -2 * gravity); jumpBuffer = 0; coyote = 0;
                if (level != null) level.JumpFeedback(); }
            verticalSpeed += gravity * delta;
            controller.Move((motion * speed + Vector3.up * verticalSpeed) * delta);
            if (model != null)
            {
                if (motion.sqrMagnitude > .01f)
                    model.rotation = Quaternion.Slerp(model.rotation, Quaternion.LookRotation(motion), 14 * delta);
                model.localPosition = new Vector3(0, controller.isGrounded ? Mathf.Sin(Time.time * 16) * .025f * motion.magnitude : .06f, 0);
                for (int i = 0; i < pawOrigins.Length; i++)
                    paws[i].localPosition = pawOrigins[i] + Vector3.up * Mathf.Sin(Time.time * 15 + (i % 2) * Mathf.PI) * .09f * motion.magnitude;
                if (tail != null) tail.localRotation = Quaternion.Euler(25, Mathf.Sin(Time.time * 3) * 12, -20);
            }
        }
        public void Teleport(Vector3 position)
        {
            controller.enabled = false;
            transform.position = position;
            controller.enabled = true;
            verticalSpeed = 0; coyote = 0; jumpBuffer = 0;
        }
    }
}
