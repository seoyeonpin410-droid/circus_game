using UnityEngine;
namespace CircusPrototype
{
    public sealed class CircusProjectileVfx : MonoBehaviour
    {
        [SerializeField] Color color=new Color(.12f,1,.75f);
        [SerializeField] float coreSize=.42f,tailLifetime=.18f;
        ParticleSystem core,embers;
        void Start()
        {
            var mesh=GetComponent<MeshRenderer>();if(mesh)mesh.enabled=false;
            var trail=GetComponent<TrailRenderer>();if(trail){trail.enabled=false;trail.Clear();}
            core=CircusCombatFx.Particles(transform,"Spirit flame core",color,coreSize,.12f);
            var m=core.main;m.loop=true;m.startSpeed=.15f;m.duration=1;
            var e=core.emission;e.enabled=true;e.rateOverTime=65;
            embers=CircusCombatFx.Particles(transform,"Soft flame wake",color,coreSize*.65f,tailLifetime);
            m=embers.main;m.loop=true;m.duration=1;m.startSpeed=.25f;
            e=embers.emission;e.enabled=true;e.rateOverTime=45;
            core.Play();embers.Play();CircusCombatFx.Burst(transform.position,color,8,.16f,1);
        }
    }
}
