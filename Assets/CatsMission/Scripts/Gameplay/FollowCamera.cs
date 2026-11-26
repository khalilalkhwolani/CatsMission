using UnityEngine;

namespace CatsMission
{
    public sealed class FollowCamera : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset = new Vector3(0, 6.5f, -9);
        private Vector3 velocity;
        public Transform Target => target;
        public void Configure(Transform player) { target = player; }
        private void Start() => Snap();
        public void Snap()
        {
            if (target == null) return;
            transform.position = target.position + offset;
            transform.LookAt(target.position + Vector3.up * .7f);
            velocity = Vector3.zero;
        }
        private void LateUpdate()
        {
            if (target == null || Time.timeScale == 0) return;
            Vector3 focus = target.position + Vector3.up * .7f;
            Vector3 desired = target.position + offset;
            RaycastHit hit;
            Vector3 direction = desired - focus;
            if (Physics.SphereCast(focus, .22f, direction.normalized, out hit, direction.magnitude, ~0, QueryTriggerInteraction.Ignore)
                && hit.collider.GetComponentInParent<PlayerCat>() == null)
                desired = focus + direction.normalized * Mathf.Max(1.6f, hit.distance - .2f);
            transform.position = Vector3.SmoothDamp(transform.position, desired, ref velocity, .12f);
            transform.LookAt(focus);
        }
    }
}
