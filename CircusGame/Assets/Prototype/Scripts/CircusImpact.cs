using UnityEngine;
namespace CircusPrototype
{
    public sealed class CircusImpact : MonoBehaviour
    {
        float age;
        public static void Create(Vector3 point,Color color)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Sphere); go.name="Dokkaebi flame impact";
            var collider=go.GetComponent<Collider>();collider.enabled=false;Destroy(collider); go.transform.position=point;
            var renderer=go.GetComponent<Renderer>();
            var material=new Material(Shader.Find("Universal Render Pipeline/Unlit")); material.SetColor("_BaseColor",color);
            renderer.sharedMaterial=material; renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            go.AddComponent<CircusImpact>();
        }
        void Update()
        {
            var session=FindFirstObjectByType<CircusSession>();
            if(!session || !session.IsRunning) { if(!session || session.State!=ShowState.Paused) Destroy(gameObject); return; }
            age+=Time.deltaTime; transform.localScale=Vector3.one*Mathf.Lerp(.15f,.85f,age/.18f);
            if(age>=.18f) Destroy(gameObject);
        }
        void OnDestroy() { var renderer=GetComponent<Renderer>(); if(renderer) Destroy(renderer.sharedMaterial); }
    }
}
