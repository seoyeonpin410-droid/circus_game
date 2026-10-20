using UnityEngine;

namespace CircusPrototype
{
    public enum GroundFlamePhase { Warning, Burning, Finished }

    public sealed class CircusGroundFlame : MonoBehaviour
    {
        CircusSession session;
        CircusEnemy owner;
        CircusAttackArea area;
        LineRenderer rim;
        Material rimMaterial;
        float age, warningSeconds, burnSeconds, damage, nextDamage;
        public GroundFlamePhase Phase { get; private set; }
        public CircusEnemy Owner=>owner;
        public CircusAttackArea Area => area;
        public Vector3 Center => transform.position;
        public static CircusGroundFlame Create(CircusSession session, CircusEnemy owner,
            Vector3 position, float radius, float warningSeconds, float burnSeconds, float damage)
        {
            var go = new GameObject("Boss Ground Flame");
            // Keep the marker on the arena floor even if the player is jumping.
            position.y = owner.transform.position.y;
            go.transform.position = position;
            var hazard = go.AddComponent<CircusGroundFlame>();
            hazard.session = session; hazard.owner = owner;
            hazard.warningSeconds = warningSeconds; hazard.burnSeconds = burnSeconds; hazard.damage = damage;
            hazard.area = CircusAttackArea.Create("Ground Flame Radius", go.transform);
            hazard.area.Show(position, Vector3.forward, radius, 180, new Color(.7f,.2f,1,.18f));
            hazard.rim = go.AddComponent<LineRenderer>();
            hazard.rimMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            hazard.rim.sharedMaterial = hazard.rimMaterial;
            hazard.rim.useWorldSpace = true; hazard.rim.loop = true;
            hazard.rim.widthMultiplier = .065f; hazard.rim.positionCount = 64;
            hazard.rim.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            for (int i=0;i<64;i++)
                hazard.rim.SetPosition(i, position + Vector3.up*.12f + Quaternion.Euler(0,i*360f/64,0)*Vector3.forward*radius);
            hazard.rimMaterial.SetColor("_BaseColor",new Color(.8f,.35f,1));
            return hazard;
        }
        void Update()
        {
            if (!session || !owner || owner.Health<=0 || session.State==ShowState.Menu
                || session.State==ShowState.Victory || session.State==ShowState.Defeat || session.State==ShowState.Intermission)
            { Phase=GroundFlamePhase.Finished; Destroy(gameObject); return; }
            if (!session.IsRunning) return;
            age += Time.deltaTime;
            if (age < warningSeconds)
            {
                Phase=GroundFlamePhase.Warning;
                float alpha=Mathf.Lerp(.15f,.42f,age/warningSeconds);
                area.Show(Center,Vector3.forward,area.Radius,180,new Color(.65f,.15f,1,alpha));
                return;
            }
            if(age>=warningSeconds+burnSeconds) { Phase=GroundFlamePhase.Finished; Destroy(gameObject); return; }
            if(Phase!=GroundFlamePhase.Burning) CircusImpact.Create(Center+Vector3.up*.15f,new Color(1,.4f,.07f));
            Phase=GroundFlamePhase.Burning;
            area.Show(Center,Vector3.forward,area.Radius,180,new Color(1,.22f,.07f,.45f+.08f*Mathf.Sin(age*16)));
            rimMaterial.SetColor("_BaseColor",new Color(1,.55f,.12f));
            rim.widthMultiplier=.12f;
            if (age>=nextDamage)
            {
                nextDamage=age+.8f;
                if (area.Contains(session.Player.transform.position)) session.Player.TakeDamage(damage);
            }
        }
        void OnDestroy() { if(rimMaterial) Destroy(rimMaterial); }
    }
}
