using UnityEngine;
using UnityEngine.UI;

namespace CatsMission
{
    public sealed class AdventurePresentation : MonoBehaviour
    {
        public Text objective;
        public Transform exitBeacon;
        public ParticleSystem celebration;
        private LevelController level;
        private FishCollectible[] treats;
        private bool celebrated;
        private void Start()
        {
            level = FindObjectOfType<LevelController>();
            treats = FindObjectsOfType<FishCollectible>();
        }
        private void Update()
        {
            if (level == null || level.State == null) return;
            bool ready = level.State.Fish >= level.State.RequiredFish;
            if (exitBeacon != null) exitBeacon.gameObject.SetActive(ready);
            if (objective != null)
            {
                string destination = level.LevelNumber == 3 ? "HOME" : "GATE";
                Vector3 target = exitBeacon != null ? exitBeacon.position : level.Player.transform.position;
                if (!ready)
                {
                    float nearest = float.MaxValue;
                    foreach (var treat in treats)
                    {
                        if (treat == null || !treat.gameObject.activeSelf) continue;
                        float distance = (treat.transform.position - level.Player.transform.position).sqrMagnitude;
                        if (distance < nearest) { nearest = distance; target = treat.transform.position; }
                    }
                }
                Vector3 relative = level.Player.transform.InverseTransformDirection(target - level.Player.transform.position);
                string direction = Mathf.Abs(relative.x) > Mathf.Abs(relative.z) ? (relative.x > 0 ? "RIGHT" : "LEFT") : (relative.z > 0 ? "AHEAD" : "BEHIND");
                objective.text = ready ? "Reach the destination" : "Collect fish: " + level.State.Fish + " / " + level.State.RequiredFish;
                objective.gameObject.SetActive(level.CanPlay);
            }
            if (level.State.Phase == LevelPhase.Complete && !celebrated)
            {
                celebrated = true;
                if (celebration != null) celebration.Play();
            }
        }
    }
}
