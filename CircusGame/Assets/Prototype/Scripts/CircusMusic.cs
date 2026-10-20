using UnityEngine;
namespace CircusPrototype
{
    public sealed class CircusMusic : MonoBehaviour
    {
        [SerializeField] AudioClip combatLoop,bossLoop,finalPercussion;
        [SerializeField,Range(0,.5f)] float musicVolume=.16f;
        [SerializeField,Range(0,1)] float finalLayerVolume=.45f;
        [SerializeField,Min(.1f)] float fadeSeconds=1.5f;
        [SerializeField,Min(.1f)] float introFadeOutSeconds=.4f,introBossFadeSeconds=.85f;
        AudioSource combat,boss,percussion;
        CircusSession session;
        bool paused;
        public float CombatVolume=>combat?combat.volume:0;
        public float BossVolume=>boss?boss.volume:0;
        public float PercussionVolume=>percussion?percussion.volume:0;
        void Awake()
        {
            session=GetComponent<CircusSession>();
            combat=Make("Ordinary performance",combatLoop);boss=Make("Final performance",bossLoop);percussion=Make("Final percussion",finalPercussion);
        }
        AudioSource Make(string title,AudioClip clip)
        {
            var go=new GameObject(title);go.transform.SetParent(transform,false);var source=go.AddComponent<AudioSource>();
            source.playOnAwake=false;source.loop=true;source.clip=clip;source.volume=0;source.spatialBlend=0;return source;
        }
        void Update()
        {
            if(!session)return;
            bool pause=session.State==ShowState.Paused;
            if(pause!=paused)
            {
                foreach(var source in new[]{combat,boss,percussion}) {if(pause)source.Pause();else source.UnPause();}
                paused=pause;
            }
            if(pause)return;
            var entry=session.Boss?session.Boss.GetComponent<CircusBossEntrance>():null;
            bool bossTime=session.Round==3 && (session.IsRunning || (session.State==ShowState.BossIntro && entry && entry.MusicAllowed));
            bool normal=session.Round<3 && (session.IsRunning || (session.State==ShowState.Intermission && session.Round==1));
            float main=normal?musicVolume:0,final=bossTime?musicVolume:0;
            var ai=session.Boss?session.Boss.GetComponent<CircusBoss>():null;
            float layer=bossTime && ai && ai.Phase==3?musicVolume*finalLayerVolume:0;
            Fade(combat,main,session.State==ShowState.BossIntro?introFadeOutSeconds:fadeSeconds);Fade(boss,final,session.State==ShowState.BossIntro?introBossFadeSeconds:fadeSeconds);Fade(percussion,layer,fadeSeconds);
        }
        void Fade(AudioSource source,float target,float seconds)
        {
            if(musicVolume<=0){source.volume=0;source.Stop();return;}
            if(target>0 && !source.isPlaying && source.clip)
            {
                if(source==percussion && boss.isPlaying)source.timeSamples=boss.timeSamples%source.clip.samples;
                source.Play();
            }
            source.volume=Mathf.MoveTowards(source.volume,target,Time.deltaTime*musicVolume/Mathf.Max(.1f,seconds));
            if(target==0 && source.volume<.0001f && source.isPlaying){source.Stop();source.time=0;}
        }
    }
}
