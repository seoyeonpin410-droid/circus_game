using UnityEngine;
namespace CircusPrototype
{
    public sealed class CircusBossVfx : MonoBehaviour
    {
        CircusSession session;
        LineRenderer ring;
        Material material;
        float age,duration,radius;
        public static void Ring(CircusSession session,Vector3 point,Color color,float radius,float seconds)
        {
            var go=new GameObject("Dokkaebi shockwave");go.transform.position=point+Vector3.up*.12f;
            var fx=go.AddComponent<CircusBossVfx>();fx.session=session;fx.radius=radius;fx.duration=Mathf.Max(.1f,seconds);
            fx.ring=go.AddComponent<LineRenderer>();fx.ring.useWorldSpace=false;fx.ring.loop=true;fx.ring.positionCount=64;fx.ring.widthMultiplier=.12f;
            fx.material=new Material(Shader.Find("Universal Render Pipeline/Unlit"));fx.material.SetColor("_BaseColor",color);
            fx.ring.sharedMaterial=fx.material;fx.ring.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        void Update()
        {
            if(!session || session.State==ShowState.Menu || session.State==ShowState.Victory || session.State==ShowState.Defeat){Destroy(gameObject);return;}
            if(session.State==ShowState.Paused)return;
            age+=Time.deltaTime;float t=Mathf.Clamp01(age/duration);
            for(int i=0;i<64;i++)ring.SetPosition(i,Quaternion.Euler(0,i*360f/64,0)*Vector3.forward*Mathf.Lerp(.25f,radius,t));
            ring.widthMultiplier=Mathf.Lerp(.16f,.015f,t);
            if(age>=duration)Destroy(gameObject);
        }
        void OnDestroy(){if(material)Destroy(material);}
    }
}
