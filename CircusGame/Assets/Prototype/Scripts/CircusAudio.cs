using UnityEngine;
namespace CircusPrototype
{
    public enum CircusCue { Countdown, Start, ActChange, Clear, PlayerMelee, PlayerShot, PlayerHit, PlayerDeath, EnemyHit, EnemyDeath, BossArrival, BossMelee, BossShot, BossHit, BossDeath, FlameFlight, FlameImpact, Warning, FlameLaunch, UIClick, EnemyMelee, MagicCharge, PhaseShift, BossPrepare, SlamPrepare, BossSlam, DashPrepare, BossDash }
    public sealed class CircusAudio : MonoBehaviour
    {
        [SerializeField] AudioClip[] clips;
        [SerializeField] float[] cueVolumes;
        [SerializeField] bool effectsEnabled=false;
        [SerializeField] bool combatAccentsEnabled=true;
        [SerializeField,Range(0,1)] float meleeImpactVolume=.12f,slamImpactVolume=.2f;
        [SerializeField] AudioClip meleeImpactClip,slamImpactClip;
        float nextAccent;
        [Header("Boss entrance only")]
        [SerializeField] bool bossIntroSoundsEnabled=true;
        [SerializeField] AudioClip bossLandingClip,bossRoarClip;
        [SerializeField,Range(0,1)] float bossLandingVolume=.24f,bossRoarVolume=.15f;
        AudioSource introVoice;bool introPaused;
        public static void PlayBossIntro(bool roar,Vector3 point) {
            if(!instance || !instance.bossIntroSoundsEnabled)return;
            var clip=roar?instance.bossRoarClip:instance.bossLandingClip;if(!clip)return;
            instance.introVoice.transform.position=point;instance.introVoice.spatialBlend=.15f;
            instance.introVoice.clip=clip;instance.introVoice.volume=roar?instance.bossRoarVolume:instance.bossLandingVolume;instance.introVoice.Play();
        }
        public static void PlayAccent(bool slam,Vector3 position) {
            if(!instance || !instance.combatAccentsEnabled || Time.unscaledTime<instance.nextAccent)return;
            var clip=slam?instance.slamImpactClip:instance.meleeImpactClip;if(!clip)return;
            foreach(var voice in instance.voices)if(!voice.isPlaying) {
                voice.clip=clip;voice.volume=slam?instance.slamImpactVolume:instance.meleeImpactVolume;
                voice.spatialBlend=.2f;voice.transform.position=position;voice.Play();instance.nextAccent=Time.unscaledTime+.12f;break;
            }
        }
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
            introVoice=MakeSource("Boss entrance accent");voices = new AudioSource[maxVoices]; flights = new AudioSource[4];
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
            if (!instance || !instance.effectsEnabled || instance.clips==null) return;
            int index=(int)cue;
            if(index>=instance.clips.Length || !instance.clips[index] || Time.unscaledTime<instance.nextCue[index]) return;
            instance.nextCue[index]=Time.unscaledTime+((cue==CircusCue.EnemyHit || cue==CircusCue.FlameImpact) ? .12f : instance.minRepeatSeconds);
            int overlapping=0;
            foreach(var playing in instance.voices) if(playing.isPlaying && playing.clip==instance.clips[index]) overlapping++;
            if(overlapping>=2) return;
            foreach(var voice in instance.voices)
            {
                if(voice.isPlaying) continue;
                voice.transform.position=position ?? instance.transform.position;
                voice.spatialBlend=position.HasValue ? .35f : 0;
                float cueGain=instance.cueVolumes!=null && index<instance.cueVolumes.Length ? instance.cueVolumes[index] : 1;
                voice.volume=instance.effectsVolume*Mathf.Clamp01(gain)*cueGain;
                voice.pitch=1;
                voice.clip=instance.clips[index]; voice.Play(); return;
            }
        }
        public static AudioSource BeginFlight(Vector3 position)
        {
            if(!instance || !instance.effectsEnabled || instance.clips==null || instance.clips.Length <= (int)CircusCue.FlameFlight) return null;
            foreach(var voice in instance.flights)
            {
                if(voice.isPlaying) continue;
                voice.transform.position=position; voice.spatialBlend=.6f;
                if(!instance.clips[(int)CircusCue.FlameFlight]) return null;
                voice.volume=instance.effectsVolume*.06f; voice.clip=instance.clips[(int)CircusCue.FlameFlight]; voice.Play(); return voice;
            }
            return null;
        }
        public static void StopFlights() { if(instance && instance.flights!=null) foreach(var voice in instance.flights) voice.Stop(); }
        void Update()
        {
            var show=FindFirstObjectByType<CircusSession>();bool introPause=show && show.State==ShowState.Paused;
            if(introPause!=introPaused){if(introPause)introVoice.Pause();else introVoice.UnPause();introPaused=introPause;}
            if(show && (show.State==ShowState.Menu || show.State==ShowState.Victory || show.State==ShowState.Defeat))introVoice.Stop();
            if(!effectsEnabled)
            {
                foreach(var voice in voices) { if(voice.isPlaying && voice.clip!=meleeImpactClip && voice.clip!=slamImpactClip)voice.Stop();voice.mute=FindFirstObjectByType<CircusSession>()?.State==ShowState.Paused; }
                foreach(var voice in flights) if(voice.isPlaying)voice.Stop();
                return;
            }
            var session=FindFirstObjectByType<CircusSession>();
            bool pause=session && session.State==ShowState.Paused;
            foreach(var voice in voices) voice.mute=pause;
            foreach(var voice in flights) voice.mute=pause;
        }
        void OnDestroy() { if(instance==this) instance=null; }
    }
}
