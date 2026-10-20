using UnityEngine;
namespace CircusPrototype
{
    // Decoration only: no collider or damage until the release frame.
    public sealed class CircusCharge : MonoBehaviour
    {
        Material material;
        ParticleSystem soft;
        public static CircusCharge Create(Transform owner,Vector3 offset,bool softFlame=false)
        {
            var go=softFlame?new GameObject():GameObject.CreatePrimitive(PrimitiveType.Sphere); go.name="Dokkaebi fire charge";
            var collider=go.GetComponent<Collider>();if(collider){collider.enabled=false;Destroy(collider);}
            go.transform.SetParent(owner,false); go.transform.localPosition=offset;
            var renderer=go.GetComponent<Renderer>();
            var effect=go.AddComponent<CircusCharge>();
            if(softFlame) {
                effect.soft=CircusCombatFx.Particles(go.transform,"Gathering spirit flame",new Color(.15f,1,.8f),.4f,.18f);
                var main=effect.soft.main;main.loop=true;main.duration=1;main.startSpeed=.2f;
                var emission=effect.soft.emission;emission.enabled=true;emission.rateOverTime=45;effect.Hide();return effect;
            }
            renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            effect.material=new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            effect.material.SetColor("_BaseColor",new Color(.15f,1,.8f)); renderer.sharedMaterial=effect.material;
            effect.Hide(); return effect;
        }
        public void Show(float progress) {
            gameObject.SetActive(true);
            if(soft){var main=soft.main;main.startSize=Mathf.Lerp(.18f,.48f,progress);if(!soft.isPlaying)soft.Play();}
            else transform.localScale=Vector3.one*Mathf.Lerp(.18f,.65f,progress);
        }
        public void Hide() { gameObject.SetActive(false); }
        void OnDestroy() { if(material) Destroy(material); }
    }
}
