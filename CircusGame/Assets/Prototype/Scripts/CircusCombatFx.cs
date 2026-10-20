using UnityEngine;
namespace CircusPrototype
{
    // Small, shared presentation helpers; no damage or collision responsibilities.
    public sealed class CircusCombatFx : MonoBehaviour
    {
        [SerializeField] Material glowMaterial;
        static CircusCombatFx instance;
        public static Material Glow => instance ? instance.glowMaterial : null;
        void Awake() { instance=this; }
        void OnDestroy() { if(instance==this)instance=null; }
        public static ParticleSystem Particles(Transform parent,string name,Color color,float size,float lifetime)
        {
            var go=new GameObject(name);go.transform.SetParent(parent,false);
            var ps=go.AddComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=ps.main;main.playOnAwake=false;main.loop=false;main.duration=.3f;main.startLifetime=lifetime;
            main.startSize=new ParticleSystem.MinMaxCurve(size*.5f,size);main.startColor=color;main.startSpeed=new ParticleSystem.MinMaxCurve(.5f,2);
            main.simulationSpace=ParticleSystemSimulationSpace.World;main.scalingMode=ParticleSystemScalingMode.Shape;main.maxParticles=80;
            var emission=ps.emission;emission.enabled=false;
            var shape=ps.shape;shape.enabled=true;shape.shapeType=ParticleSystemShapeType.Sphere;shape.radius=.07f;
            var fade=ps.colorOverLifetime;fade.enabled=true;var gradient=new Gradient();
            gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(color,.6f)},new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(1,.12f),new GradientAlphaKey(0,1)});fade.color=gradient;
            var scale=ps.sizeOverLifetime;scale.enabled=true;scale.size=new ParticleSystem.MinMaxCurve(1,AnimationCurve.EaseInOut(0,1,1,0));
            var renderer=ps.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=Glow;renderer.renderMode=ParticleSystemRenderMode.Billboard;
            renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            return ps;
        }
        public static void Burst(Vector3 point,Color color,int count=12,float size=.16f,float speed=2)
        {
            if(!Glow)return;
            var root=new GameObject("Combat impact particles");root.transform.position=point;
            var ps=Particles(root.transform,"Sparks",color,size,.3f);
            var main=ps.main;main.startSpeed=new ParticleSystem.MinMaxCurve(speed*.3f,speed);ps.Play();ps.Emit(count);
            root.AddComponent<CircusFxLifetime>().seconds=.65f;
        }
        public static void Slam(Vector3 point,float radius)
        {
            Burst(point+Vector3.up*.12f,new Color(1,.65f,.25f),28,.32f,5);
            if(!Glow)return;
            var root=new GameObject("Slam ground dust");root.transform.position=point+Vector3.up*.12f;
            var ps=Particles(root.transform,"Dust",new Color(.7f,.55f,.38f,.35f),.7f,.65f);
            var main=ps.main;main.startSpeed=new ParticleSystem.MinMaxCurve(1.5f,3);main.gravityModifier=-.1f;
            var shape=ps.shape;shape.shapeType=ParticleSystemShapeType.Circle;shape.radius=radius*.35f;
            ps.transform.rotation=Quaternion.Euler(90,0,0);ps.Play();ps.Emit(24);root.AddComponent<CircusFxLifetime>().seconds=1;
        }
        public static void Slash(Vector3 origin,Vector3 forward,float radius,float seconds)
        {
            if(!Glow)return;
            var go=new GameObject("Brief melee slash");go.transform.SetPositionAndRotation(origin+Vector3.up*.95f,Quaternion.LookRotation(forward));
            var slash=go.AddComponent<CircusSlash>();slash.radius=radius;slash.seconds=seconds;
        }
    }
    public sealed class CircusFxLifetime : MonoBehaviour
    {
        public float seconds=1;
        void Update(){if((seconds-=Time.deltaTime)<=0)Destroy(gameObject);}
    }
    public sealed class CircusSlash : MonoBehaviour
    {
        public float radius=2.7f,seconds=.16f;
        float age;Mesh mesh;
        void Awake(){mesh=new Mesh();gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;var r=gameObject.AddComponent<MeshRenderer>();r.sharedMaterial=CircusCombatFx.Glow;r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;}
        void Update()
        {
            age+=Time.deltaTime;float t=Mathf.Clamp01(age/seconds);if(t>=1){Destroy(gameObject);return;}
            const int n=20;var v=new Vector3[(n+1)*2];var uv=new Vector2[v.Length];var colors=new Color[v.Length];var tris=new int[n*6];
            for(int i=0;i<=n;i++){
                float u=i/(float)n;float angle=Mathf.Lerp(-75,75,t)-55*(1-u);var dir=Quaternion.Euler(0,angle,0)*Vector3.forward;
                float width=.2f*Mathf.Sin(u*Mathf.PI);v[i*2]=dir*(radius-width);v[i*2+1]=dir*(radius+width);
                uv[i*2]=new Vector2(u,0);uv[i*2+1]=new Vector2(u,1);
                colors[i*2]=colors[i*2+1]=new Color(1,.85f,.5f,Mathf.Sin(u*Mathf.PI)*(1-t)*.8f);
                if(i<n){int a=i*2,j=i*6;tris[j]=a;tris[j+1]=a+1;tris[j+2]=a+2;tris[j+3]=a+1;tris[j+4]=a+3;tris[j+5]=a+2;}
            }
            mesh.Clear();mesh.vertices=v;mesh.uv=uv;mesh.colors=colors;mesh.triangles=tris;mesh.RecalculateBounds();
        }
        void OnDestroy(){if(mesh)Destroy(mesh);}
    }
}
