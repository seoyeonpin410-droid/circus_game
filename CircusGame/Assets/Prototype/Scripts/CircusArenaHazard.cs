using UnityEngine;
namespace CircusPrototype
{
    public sealed class CircusArenaHazard : MonoBehaviour
    {
        public CircusCombatArea arena;
        public Light[] stageLights;
        [Min(8)] public float interval=10;
        [Min(1)] public float warningSeconds=1.1f;
        [Range(1.5f,3)] public float radius=2.4f;
        [Range(1,15)] public float damage=8;
        CircusSession session;CircusAttackArea warning;Vector3 center;
        float timer,age;bool fired;
        public int Blasts {get;private set;}
        public bool Active=>session && session.SameStageShow && session.Round==2 && session.ActiveArea && session.ActiveArea.State==CircusAreaState.Battle && session.WaveTotal>0 && session.Remaining>0 && session.State==ShowState.Playing;
        public bool WarningVisible=>warning && warning.Visible;
        public Vector3 WarningCenter=>center;
        void Awake(){session=FindFirstObjectByType<CircusSession>();timer=interval;}
        public void Cancel(){if(warning)Destroy(warning.gameObject);warning=null;timer=interval;age=0;fired=false;}
        void Update()
        {
            if(!session)return;if(session.State==ShowState.Paused)return;
            bool second=session.Round==2 && session.State!=ShowState.Menu && session.State!=ShowState.Countdown;
            foreach(var light in stageLights)if(light)light.color=Color.Lerp(light.color,second?new Color(1,.3f,.18f):new Color(1,.68f,.35f),Time.deltaTime*1.5f);
            if(!Active){
                bool waveGap=session.SameStageShow && session.Round==2 && session.IsRunning && session.ActiveArea && session.ActiveArea.State==CircusAreaState.Battle;
                if(waveGap){if(warning){Destroy(warning.gameObject);warning=null;timer=interval;}age=0;fired=false;}else Cancel();
                return;
            }
            if(!warning){timer-=Time.deltaTime;if(timer>0)return;center=arena.Clamp(session.Player.transform.position,radius+1);warning=CircusAttackArea.Create("Act 2 stage danger",transform);age=0;fired=false;}
            age+=Time.deltaTime;warning.ShowSlam(center,radius,Mathf.Clamp01(age/warningSeconds));
            if(!fired && age>=warningSeconds){fired=true;Blasts++;if(warning.Contains(session.Player.transform.position))session.Player.TakeDamage(damage);CircusCombatFx.Slam(center,radius);}
            if(age>=warningSeconds+.25f)Cancel();
        }
        void OnDisable(){Cancel();}
    }
}
