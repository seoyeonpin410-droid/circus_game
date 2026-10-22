using UnityEngine;
namespace CircusPrototype
{
    // The shader's circular cutouts and damage predicate share the same world-space data.
    public sealed class CircusSafeField : MonoBehaviour
    {
        CircusSession session;CircusEnemy owner;Material material;Mesh mesh;
        Vector3 a,b;float radius,safeRadius,warning,linger,damage,age;
        LineRenderer[] rims;
        public CircusEnemy Owner=>owner;
        public Vector3 SafeA=>a;
        public Vector3 SafeB=>b;
        public float SafeRadius=>safeRadius;
        public float WarningSeconds=>warning;
        public float Age=>age;
        public bool Fired {get;private set;}
        public bool ContainsDanger(Vector3 point){var p=new Vector2(point.x,point.z);return Vector2.Distance(p,new Vector2(transform.position.x,transform.position.z))<=radius && !IsSafe(point);}
        public bool IsSafe(Vector3 point){point.y=a.y;return (point-a).sqrMagnitude<=safeRadius*safeRadius || (point-b).sqrMagnitude<=safeRadius*safeRadius;}
        public static CircusSafeField Create(CircusSession show,CircusEnemy boss,Material source,Vector3 center,float arenaRadius,Vector3 safeA,Vector3 safeB,float safeRadius,float warning,float linger,float damage)
        {
            var go=new GameObject("Boss safe-zone field");go.transform.position=center;
            var field=go.AddComponent<CircusSafeField>();field.session=show;field.owner=boss;field.radius=arenaRadius;field.a=safeA;field.b=safeB;
            field.safeRadius=safeRadius;field.warning=Mathf.Max(1.5f,warning);field.linger=linger;field.damage=damage;
            field.material=source?new Material(source):new Material(Shader.Find("Circus/ArenaWarning"));field.material.SetVector("_CenterRadius",new Vector4(center.x,center.z,arenaRadius,0));
            field.material.SetVector("_SafeA",new Vector4(safeA.x,safeA.z,0,0));field.material.SetVector("_SafeB",new Vector4(safeB.x,safeB.z,0,0));field.material.SetFloat("_SafeRadius",safeRadius);
            field.mesh=new Mesh();float r=arenaRadius;field.mesh.vertices=new[]{new Vector3(-r,.1f,-r),new Vector3(r,.1f,-r),new Vector3(-r,.1f,r),new Vector3(r,.1f,r)};
            field.mesh.triangles=new[]{0,2,1,1,2,3};field.mesh.RecalculateBounds();go.AddComponent<MeshFilter>().sharedMesh=field.mesh;
            var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=field.material;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            field.rims=new LineRenderer[(safeA-safeB).sqrMagnitude<.01f?1:2];var centers=new[]{safeA,safeB};
            for(int n=0;n<field.rims.Length;n++){
                var safe=CircusAttackArea.Create("GREEN SAFE AREA",go.transform);safe.Show(centers[n],Vector3.forward,safeRadius,180,new Color(.05f,1,.5f,.16f));
                var rim=safe.gameObject.AddComponent<LineRenderer>();rim.loop=true;rim.useWorldSpace=true;rim.positionCount=80;rim.widthMultiplier=.11f;
                rim.sharedMaterial=CircusCombatFx.Glow;rim.startColor=rim.endColor=new Color(.2f,1,.55f,.9f);rim.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
                for(int i=0;i<80;i++)rim.SetPosition(i,centers[n]+Vector3.up*.13f+Quaternion.Euler(0,i*360f/80,0)*Vector3.forward*safeRadius);field.rims[n]=rim;
            }
            return field;
        }
        public void Detonate()
        {
            if(Fired || !session || !session.IsRunning || !owner || owner.Health<=0)return;
            Fired=true;age=Mathf.Max(age,warning);
            if(ContainsDanger(session.Player.transform.position))session.Player.TakeDamage(damage);
            CircusAudio.PlayBossPattern(CircusBossSound.GroundImpact,transform.position);
            foreach(var center in new[]{a,b})CircusBossVfx.Ring(session,center,new Color(.2f,1,.5f),safeRadius,.4f);
            CircusCombatFx.Burst(transform.position+Vector3.up*.2f,new Color(1,.35f,.15f),24,.35f,4);
            FindFirstObjectByType<CircusCamera>()?.Shake(.025f,.12f);
        }
        public void Cancel(){enabled=false;Fired=true;Destroy(gameObject);}
        void Update()
        {
            if(!session || !owner || owner.Health<=0 || session.State==ShowState.Menu || session.State==ShowState.Victory || session.State==ShowState.Defeat || session.State==ShowState.Intermission){Destroy(gameObject);return;}
            if(!session.IsRunning)return;age+=Time.deltaTime;
            if(age>=warning)Detonate();
            float t=Mathf.Clamp01(age/warning);material.SetColor("_BaseColor",new Color(1,.12f,.07f,Fired?.42f:Mathf.Lerp(.08f,.3f,t)));
            foreach(var rim in rims)rim.widthMultiplier=.09f+.045f*t;
            if(age>=warning+linger)Destroy(gameObject);
        }
        void OnDestroy(){if(material)Destroy(material);if(mesh)Destroy(mesh);}
    }
}
