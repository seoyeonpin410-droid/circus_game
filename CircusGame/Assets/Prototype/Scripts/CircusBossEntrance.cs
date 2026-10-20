using UnityEngine;
namespace CircusPrototype
{
    // Presentation timeline only. Boss AI and the session retain combat ownership.
    public sealed class CircusBossEntrance : MonoBehaviour
    {
        [Header("Timeline (seconds)")]
        [SerializeField,Min(.1f)] float quietSeconds=.65f,fallSeconds=.5f,landingHold=.5f,closeupSeconds=.55f,roarSeconds=1.05f,returnSeconds=.85f;
        [Header("Whole-model motion")]
        [SerializeField,Min(2)] float fallHeight=9;
        [SerializeField,Range(.9f,1)] float landingSquash=.94f;
        [SerializeField,Range(0,.06f)] float roarScale=.025f;
        [SerializeField] float roarLean=12;
        [Header("Camera framing")]
        [SerializeField] float wideDistance=10,wideHeight=3.5f,closeDistance=6.2f,closeHeight=2.4f,lookHeight=1.9f,cinematicFov=52;
        CircusSession session;CircusEnemy enemy;CharacterController controller;
        Vector3 restPosition,restScale;Quaternion facing;
        bool controllerWasEnabled,landingPlayed,roarPlayed;
        public float Elapsed {get;private set;}
        public bool Complete=>Elapsed>=Duration;
        public bool Landed=>landingPlayed;
        public bool Roared=>roarPlayed;
        public float QuietSeconds=>quietSeconds;
        public float FallEnd=>quietSeconds+fallSeconds;
        public float CloseStart=>FallEnd+landingHold;
        public float NameAt=>CloseStart+closeupSeconds;
        public float ReturnAt=>NameAt+roarSeconds;
        public float Duration=>ReturnAt+returnSeconds;
        public float NameAge=>Elapsed-NameAt;
        public bool HealthVisible=>Elapsed>=NameAt+.16f;
        public bool MusicAllowed=>Elapsed>=ReturnAt;
        public float CloseBlend=>Mathf.SmoothStep(0,1,Mathf.InverseLerp(CloseStart,NameAt,Elapsed));
        public float ReturnBlend=>Mathf.SmoothStep(0,1,Mathf.InverseLerp(ReturnAt,Duration,Elapsed));
        public float WideDistance=>wideDistance;
        public float WideHeight=>wideHeight;
        public float CloseDistance=>closeDistance;
        public float CloseHeight=>closeHeight;
        public float LookHeight=>lookHeight;
        public float Fov=>cinematicFov;
        public void Bind(CircusSession value,CircusEnemy owner)
        {
            session=value;enemy=owner;controller=GetComponent<CharacterController>();controllerWasEnabled=controller && controller.enabled;
            if(controller)controller.enabled=false;
            restPosition=enemy.Model.localPosition;restScale=enemy.Model.localScale;
            var toward=session.Player.transform.position-transform.position;toward.y=0;
            facing=Quaternion.LookRotation(toward.sqrMagnitude>.01f?toward.normalized:Vector3.back);
            enemy.Model.rotation=facing;enemy.Model.localPosition=restPosition+Vector3.up*fallHeight;
            enemy.Model.gameObject.SetActive(false);Elapsed=0;landingPlayed=roarPlayed=false;
        }
        public void Tick()
        {
            if(!session || !enemy || session.State==ShowState.Paused || Complete)return;
            Elapsed=Mathf.Min(Duration,Elapsed+Time.deltaTime);
            if(Elapsed<quietSeconds)return;
            enemy.Model.gameObject.SetActive(true);
            if(Elapsed<FallEnd){
                float t=(Elapsed-quietSeconds)/Mathf.Max(.01f,fallSeconds);
                enemy.Model.localPosition=restPosition+Vector3.up*fallHeight*(1-t*t);
                enemy.Model.rotation=facing*Quaternion.Euler(8*t,0,0);return;
            }
            if(!landingPlayed){
                landingPlayed=true;enemy.Model.localPosition=restPosition;
                CircusCombatFx.Slam(transform.position,4.5f);
                CircusBossVfx.Ring(session,transform.position,new Color(1,.65f,.25f),5,.55f);
                FindFirstObjectByType<CircusCamera>()?.Shake(.05f,.2f);
                CircusAudio.PlayBossIntro(false,transform.position);
            }
            if(Elapsed<NameAt){
                float squash=1-Mathf.SmoothStep(0,1,(Elapsed-FallEnd)/.28f);
                enemy.Model.localScale=Vector3.Scale(restScale,new Vector3(1+.025f*squash,Mathf.Lerp(1,landingSquash,squash),1+.025f*squash));
                enemy.Model.rotation=facing*Quaternion.Euler(12*(1-Mathf.SmoothStep(0,1,(Elapsed-FallEnd)/landingHold)),0,0);
            } else {
                float t=Mathf.Clamp01((Elapsed-NameAt-.18f)/Mathf.Max(.1f,roarSeconds-.18f));
                float strength=Mathf.Sin(t*Mathf.PI);
                enemy.Model.localScale=restScale*(1+roarScale*strength);
                enemy.Model.rotation=facing*Quaternion.Euler(-roarLean*strength,0,0);
                if(!roarPlayed && Elapsed>=NameAt+.28f){
                    roarPlayed=true;CircusAudio.PlayBossIntro(true,transform.position);
                    CircusCombatFx.Burst(transform.position+Vector3.up*1.7f,new Color(.18f,.85f,.65f),14,.23f,1.5f);
                    CircusBossVfx.Ring(session,transform.position,new Color(.2f,.85f,.6f),2.8f,.4f);
                    FindFirstObjectByType<CircusCamera>()?.Shake(.025f,.16f);
                }
            }
            if(Complete)Restore();
        }
        void Restore(){if(!enemy)return;enemy.Model.gameObject.SetActive(true);enemy.Model.localPosition=restPosition;enemy.Model.localScale=restScale;enemy.Model.rotation=facing;if(controller)controller.enabled=controllerWasEnabled;}
    }
}
