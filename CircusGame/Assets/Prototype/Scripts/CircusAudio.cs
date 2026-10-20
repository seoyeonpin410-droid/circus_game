using UnityEngine;
namespace CircusPrototype
{
    public enum CircusCue { Countdown, Start, ActChange, Clear, PlayerMelee, PlayerShot, PlayerHit, PlayerDeath, EnemyHit, EnemyDeath, BossArrival, BossMelee, BossShot, BossHit, BossDeath, FlameFlight, FlameImpact, Warning, FlameLaunch, UIClick }
    public sealed class CircusAudio : MonoBehaviour
    {
        [SerializeField] AudioClip[] clips;
        [SerializeField] float[] cueVolumes;
        [SerializeField, Range(0,1)] float effectsVolume = .55f;
        [SerializeField] int maxVoices = 12;
        [SerializeField] float minRepeatSeconds = .075f;
        static CircusAudio instance;
        AudioSource[] voices, flights;
        float[] nextCue;
        public static int AvailableCues => instance && instance.clips != null ? instance.clips.Length : 0;
        void Awake()
        {
            instance = this; nextCue = new float[System.Enum.GetValues(typeof(CircusCue)).Length];
            voices = new AudioSource[maxVoices]; flights = new AudioSource[4];
            for(int i=0;i<voices.Length;i++) voices[i] = MakeSource("Effect " + i);
            for(int i=0;i<flights.Length;i++) { flights[i] = MakeSource("Flame flight " + i); flights[i].loop=true; }
        }
        AudioSource MakeSource(string title)
        {
            var go=new GameObject(title); go.transform.SetParent(transform,false);
            var source=go.AddComponent<AudioSource>(); source.playOnAwake=false; source.dopplerLevel=0;
            source.minDistance=4; source.maxDistance=28; return source;
        }
        public static void Play(CircusCue cue, Vector3? position = null, float gain = 1)
        {
            if (!instance || instance.clips==null) return;
            int index=(int)cue;
            if(index>=instance.clips.Length || !instance.clips[index] || Time.unscaledTime<instance.nextCue[index]) return;
            instance.nextCue[index]=Time.unscaledTime+instance.minRepeatSeconds;
            foreach(var voice in instance.voices)
            {
                if(voice.isPlaying) continue;
                voice.transform.position=position ?? instance.transform.position;
                voice.spatialBlend=position.HasValue ? .35f : 0;
                float cueGain=instance.cueVolumes!=null && index<instance.cueVolumes.Length ? instance.cueVolumes[index] : 1;
                voice.volume=instance.effectsVolume*Mathf.Clamp01(gain)*cueGain;
                bool combat=cue==CircusCue.PlayerMelee || cue==CircusCue.PlayerShot || cue==CircusCue.EnemyHit || cue==CircusCue.BossHit;
                voice.pitch=combat ? UnityEngine.Random.Range(.96f,1.04f) : cue==CircusCue.BossMelee ? .8f : 1;
                voice.clip=instance.clips[index]; voice.Play(); return;
            }
        }
        public static AudioSource BeginFlight(Vector3 position)
        {
            if(!instance || instance.clips==null || instance.clips.Length <= (int)CircusCue.FlameFlight) return null;
            foreach(var voice in instance.flights)
            {
                if(voice.isPlaying) continue;
                voice.transform.position=position; voice.spatialBlend=.6f;
                voice.volume=instance.effectsVolume*.16f; voice.clip=instance.clips[(int)CircusCue.FlameFlight]; voice.Play(); return voice;
            }
            return null;
        }
        public static void StopFlights() { if(instance && instance.flights!=null) foreach(var voice in instance.flights) voice.Stop(); }
        void Update()
        {
            var session=FindFirstObjectByType<CircusSession>();
            bool pause=session && session.State==ShowState.Paused;
            foreach(var voice in voices) voice.mute=pause;
            foreach(var voice in flights) voice.mute=pause;
        }
        void OnDestroy() { if(instance==this) instance=null; }
    }
}
