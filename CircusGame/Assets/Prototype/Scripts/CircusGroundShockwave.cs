using UnityEngine;
namespace CircusPrototype
{
    public sealed class CircusGroundShockwave : MonoBehaviour
    {
        CircusSession session;CircusEnemy owner;LineRenderer ring;
        float speed,maxRadius,damage,width,clearance,previousDistance,previousHeight,dust;
        public CircusEnemy Owner=>owner;
        public float Radius {get;private set;}=.5f;
        public bool Resolved {get;private set;}
        public bool JumpAvoided {get;private set;}
        public static CircusGroundShockwave Create(CircusSession session,CircusEnemy owner,Vector3 center,float speed,float maxRadius,float damage,float width,float clearance)
        {
            var go=new GameObject("Jump over ground shockwave");go.transform.position=center;
            var w=go.AddComponent<CircusGroundShockwave>();w.session=session;w.owner=owner;w.speed=speed;w.maxRadius=maxRadius;w.damage=damage;w.width=width;w.clearance=clearance;
            w.previousDistance=Vector2.Distance(new Vector2(center.x,center.z),new Vector2(session.Player.transform.position.x,session.Player.transform.position.z));w.previousHeight=session.Player.transform.position.y;
            w.ring=go.AddComponent<LineRenderer>();w.ring.useWorldSpace=true;w.ring.loop=true;w.ring.positionCount=120;w.ring.widthMultiplier=width;w.ring.sharedMaterial=CircusCombatFx.Glow;w.ring.startColor=w.ring.endColor=new Color(1,.65f,.12f,.95f);w.ring.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;w.Draw();return w;
        }
        void Draw(){for(int i=0;i<120;i++)ring.SetPosition(i,transform.position+Vector3.up*.13f+Quaternion.Euler(0,i*3,0)*Vector3.forward*Radius);}
        void Update()
        {
            if(!session || !owner || owner.Health<=0 || session.State==ShowState.Menu || session.State==ShowState.Defeat || session.State==ShowState.Victory){Cancel();return;}
            if(!session.IsRunning)return;
            float previous=Radius;Radius+=Mathf.Max(.1f,speed)*Time.deltaTime;var p=session.Player.transform.position;
            float distance=Vector2.Distance(new Vector2(p.x,p.z),new Vector2(transform.position.x,transform.position.z));
            float before=previousDistance-previous,after=distance-Radius;
            bool crossed=Mathf.Abs(after)<=width*.5f || before*after<=0 || (before>width*.5f && after< -width*.5f);
            if(!Resolved && crossed){
                float t=Mathf.Abs(before-after)>.0001f?Mathf.Clamp01(before/(before-after)):1;
                float height=Mathf.Lerp(previousHeight,p.y,t)-transform.position.y;
                Resolved=true;JumpAvoided=height>=clearance;
                // Only this floor ring uses height; dash immunity cannot replace the jump.
                if(!JumpAvoided)session.Player.TakeDamage(damage,true);
            }
            previousDistance=distance;previousHeight=p.y;Draw();dust-=Time.deltaTime;
            if(dust<=0){dust=.25f;for(int i=0;i<4;i++)CircusCombatFx.Burst(transform.position+Quaternion.Euler(0,i*90+Radius*8,0)*Vector3.forward*Radius+Vector3.up*.1f,new Color(.7f,.55f,.3f,.4f),2,.16f,.6f);}
            if(Radius>=maxRadius)Cancel();
        }
        public void Cancel(){enabled=false;Destroy(gameObject);}
    }
}
