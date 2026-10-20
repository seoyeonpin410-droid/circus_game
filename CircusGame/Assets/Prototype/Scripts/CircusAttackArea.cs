using UnityEngine;
namespace CircusPrototype
{
    // Rendered sector and damage predicate share these dimensions.
    public sealed class CircusAttackArea : MonoBehaviour
    {
        Mesh mesh;
        MeshRenderer surface;
        Material material;
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
            origin.y += .09f;
            transform.SetPositionAndRotation(origin, Quaternion.LookRotation(Vector3.ProjectOnPlane(forward, Vector3.up).normalized));
            if (!Mathf.Approximately(Radius, radius) || !Mathf.Approximately(HalfAngle, halfAngle))
            {
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
            delta.y = 0;
            return delta.sqrMagnitude <= Radius * Radius && (delta.sqrMagnitude < .001f
                || Vector3.Dot(transform.forward, delta.normalized) >= Mathf.Cos(HalfAngle * Mathf.Deg2Rad));
        }
        public void Hide() { if (surface) surface.enabled = false; }
        void OnDestroy() { if (mesh) Destroy(mesh); if (material) Destroy(material); }
    }
}
