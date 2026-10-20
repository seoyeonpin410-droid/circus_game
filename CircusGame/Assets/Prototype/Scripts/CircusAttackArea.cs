using UnityEngine;
namespace CircusPrototype
{
    // Rendered sector and damage predicate share these dimensions.
    public sealed class CircusAttackArea : MonoBehaviour
    {
        Mesh mesh;
        MeshRenderer surface;
        Material material;LineRenderer rim;Material rimMaterial;
        bool lane;
        float laneLength,laneWidth;
        public float Radius { get; private set; }
        public float HalfAngle { get; private set; }
        public bool Visible => surface && surface.enabled;
        public static CircusAttackArea Create(string name, Transform owner)
        {
            var go = new GameObject(name); go.transform.SetParent(owner, false);
            return go.AddComponent<CircusAttackArea>();
        }
        void Awake()
        {
            mesh = new Mesh { name = "Attack sector" };
            gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            surface = gameObject.AddComponent<MeshRenderer>();
            material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            material.SetFloat("_Surface", 1); material.SetFloat("_Blend", 0);
            material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0); material.SetFloat("_Cull", 0);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); material.renderQueue = 3000;
            surface.sharedMaterial = material;
            surface.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            surface.receiveShadows = false; Hide();
        }
        public void Show(Vector3 origin, Vector3 forward, float radius, float halfAngle, Color color)
        {
            if(rim)rim.enabled=false;
            origin.y += .09f;
            transform.SetPositionAndRotation(origin, Quaternion.LookRotation(Vector3.ProjectOnPlane(forward, Vector3.up).normalized));
            if (lane || !Mathf.Approximately(Radius, radius) || !Mathf.Approximately(HalfAngle, halfAngle))
            {
                lane=false;
                Radius = radius; HalfAngle = halfAngle;
                const int count = 64;
                var points = new Vector3[count + 2]; var triangles = new int[count * 3];
                for (int i = 0; i <= count; i++)
                {
                    points[i + 1] = Quaternion.Euler(0, Mathf.Lerp(-halfAngle, halfAngle, i / (float)count), 0) * Vector3.forward * radius;
                    if (i < count) { triangles[i * 3] = 0; triangles[i * 3 + 1] = i + 1; triangles[i * 3 + 2] = i + 2; }
                }
                mesh.Clear(); mesh.vertices = points; mesh.triangles = triangles; mesh.RecalculateBounds();
            }
            material.SetColor("_BaseColor", color); surface.enabled = true;
        }
        public bool Contains(Vector3 position, float maxVertical = 1.6f)
        {
            var delta = position - transform.position;
            if (Mathf.Abs(delta.y) > maxVertical) return false;
            if(lane)
            {
                var local=transform.InverseTransformPoint(position);
                return Mathf.Abs(local.x)<=laneWidth*.5f && local.z>=0 && local.z<=laneLength;
            }
            delta.y = 0;
            return delta.sqrMagnitude <= Radius * Radius && (delta.sqrMagnitude < .001f
                || Vector3.Dot(transform.forward, delta.normalized) >= Mathf.Cos(HalfAngle * Mathf.Deg2Rad));
        }
        public void ShowSlam(Vector3 origin,float radius,float progress) {
            float emphasis=progress>.85f?1:0;
            Show(origin,Vector3.forward,radius,180,new Color(1,.25f,.08f,Mathf.Lerp(.055f,.16f,progress)+emphasis*.06f));
            if(!rim) {
                rim=gameObject.AddComponent<LineRenderer>();rim.useWorldSpace=true;rim.loop=true;rim.positionCount=96;
                rimMaterial=new Material(material);rim.sharedMaterial=rimMaterial;rim.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            rimMaterial.SetColor("_BaseColor",new Color(1,.35f,.12f,Mathf.Lerp(.2f,.8f,progress)));
            rim.widthMultiplier=Mathf.Lerp(.025f,.07f,progress)+emphasis*.025f;
            for(int i=0;i<96;i++)rim.SetPosition(i,origin+Vector3.up*.105f+Quaternion.Euler(0,i*360f/96,0)*Vector3.forward*radius);
            rim.enabled=true;
        }
        public void Hide() { if (surface) surface.enabled = false;if(rim)rim.enabled=false; }
        public void ShowLane(Vector3 origin,Vector3 forward,float length,float width,Color color)
        {
            if(rim)rim.enabled=false;lane=true;laneLength=length;laneWidth=width;
            transform.SetPositionAndRotation(origin+Vector3.up*.09f,Quaternion.LookRotation(forward));
            mesh.Clear();mesh.vertices=new[]{new Vector3(-width*.5f,0,0),new Vector3(width*.5f,0,0),new Vector3(-width*.5f,0,length),new Vector3(width*.5f,0,length)};
            mesh.triangles=new[]{0,2,1,1,2,3};mesh.RecalculateBounds();
            material.SetColor("_BaseColor",color);surface.enabled=true;
        }
        void OnDestroy() { if (mesh) Destroy(mesh); if (material) Destroy(material);if(rimMaterial)Destroy(rimMaterial); }
    }
}
