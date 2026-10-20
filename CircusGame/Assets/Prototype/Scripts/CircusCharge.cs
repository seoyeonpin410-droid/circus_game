using UnityEngine;
namespace CircusPrototype
{
    // Decoration only: no collider or damage until the release frame.
    public sealed class CircusCharge : MonoBehaviour
    {
        Material material;
        public static CircusCharge Create(Transform owner,Vector3 offset)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Sphere); go.name="Dokkaebi fire charge";
            var collider=go.GetComponent<Collider>();collider.enabled=false;Destroy(collider);
            go.transform.SetParent(owner,false); go.transform.localPosition=offset;
            var renderer=go.GetComponent<Renderer>();
            renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            var effect=go.AddComponent<CircusCharge>();
            effect.material=new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            effect.material.SetColor("_BaseColor",new Color(.15f,1,.8f)); renderer.sharedMaterial=effect.material;
            effect.Hide(); return effect;
        }
        public void Show(float progress) { gameObject.SetActive(true);transform.localScale=Vector3.one*Mathf.Lerp(.18f,.65f,progress); }
        public void Hide() { gameObject.SetActive(false); }
        void OnDestroy() { if(material) Destroy(material); }
    }
}
