using UnityEngine;

namespace CatsMission
{
    // Two sources per scene: a quiet music bed and short action feedback.
    public sealed class GameAudio : MonoBehaviour
    {
        [SerializeField] private AudioClip music, ambience, collect, jump, life, warning, victory, gameOver;
        private AudioSource background, environment, effects;
        public void ConfigureAmbience(AudioClip clip) => ambience=clip;
        public void Configure(AudioClip bed, AudioClip pickup, AudioClip leap, AudioClip hurt,
            AudioClip lowTime, AudioClip success, AudioClip failure)
        { music=bed; collect=pickup; jump=leap; life=hurt; warning=lowTime; victory=success; gameOver=failure; }
        private void Awake()
        {
            background=gameObject.AddComponent<AudioSource>();
            background.playOnAwake=false; background.loop=true; background.volume=.16f;
            background.clip=music;
            effects=gameObject.AddComponent<AudioSource>(); effects.playOnAwake=false; effects.volume=.55f;
            if (music != null) background.Play();
            environment=gameObject.AddComponent<AudioSource>(); environment.playOnAwake=false;
            environment.clip=ambience; environment.loop=true; environment.volume=.12f;
            if (ambience != null) environment.Play();
        }
        private void Play(AudioClip clip) { if (clip != null) effects.PlayOneShot(clip); }
        public void Collect() => Play(collect);
        public void Jump() => Play(jump);
        public void LoseLife() => Play(life);
        public void Warning() => Play(warning);
        public void SetPhase(LevelPhase phase)
        {
            if (phase == LevelPhase.Paused) { background.Pause(); environment.Pause(); effects.Pause(); }
            else if (phase == LevelPhase.Playing) { background.UnPause(); environment.UnPause(); effects.UnPause(); }
            else { background.Stop(); environment.Stop(); Play(phase == LevelPhase.Complete ? victory : gameOver); }
        }
    }
}
