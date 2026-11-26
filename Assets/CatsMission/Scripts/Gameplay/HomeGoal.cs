using UnityEngine;

namespace CatsMission
{
    public sealed class HomeGoal : MonoBehaviour
    {
        [SerializeField] private LevelController level;
        public void Configure(LevelController owner) { level = owner; }
        private void OnTriggerEnter(Collider other)
        { if (other.GetComponent<PlayerCat>() != null) level.ReachHome(); }
        private void OnTriggerStay(Collider other)
        { if (level.CanPlay && level.State.Fish >= level.State.RequiredFish && other.GetComponent<PlayerCat>() != null) level.ReachHome(); }
    }
}
