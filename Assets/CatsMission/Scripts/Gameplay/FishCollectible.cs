using UnityEngine;

namespace CatsMission
{
    public sealed class FishCollectible : MonoBehaviour
    {
        [SerializeField] private int id;
        [SerializeField] private LevelController level;
        [SerializeField] private Transform visual;
        private float baseHeight;
        public int Id => id;
        public void Configure(int key, LevelController owner, Transform mesh) { id = key; level = owner; visual = mesh; }
        private void Start() { baseHeight = visual.localPosition.y; }
        private void Update()
        {
            if (level == null || !level.CanPlay) return;
            visual.Rotate(0, 65 * Time.deltaTime, 0, Space.World);
            visual.localPosition = new Vector3(0, baseHeight + Mathf.Sin(Time.time * 3 + id) * .13f, 0);
        }
        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponent<PlayerCat>() != null) TryCollect();
        }
        public bool TryCollect()
        {
            if (!gameObject.activeSelf || !level.Collect(id)) return false;
            gameObject.SetActive(false);
            return true;
        }
    }
}
