using UnityEngine;
namespace CircusPrototype
{
    public sealed class CircusProjectile : MonoBehaviour
    {
        [SerializeField] float speed=7, life=6;
        [SerializeField] float hitRadius=.23f;
        [SerializeField] Color flameColor=new Color(.1f,1,.75f);
        CircusSession session;
        Vector3 velocity;
        float age, damage=10, bossDamageScale=1;
        bool playerShot,bossOwned;
        AudioSource flight;
        CircusEnemy owner;
        float homingUntil,turnDegrees;
        public CircusEnemy Owner=>owner;
        public Vector3 Direction=>velocity.normalized;
        public bool Homing=>owner && age<homingUntil;
        public void SetBossOwner(CircusEnemy value,float lifetime,float trackingSeconds,float degreesPerSecond)
        { owner=value;bossOwned=true;life=Mathf.Max(.5f,lifetime);homingUntil=Mathf.Clamp(trackingSeconds,0,1.2f);turnDegrees=Mathf.Clamp(degreesPerSecond,0,45); }
        public bool IsPlayerShot=>playerShot;
        public void Launch(Vector3 direction,CircusSession value,float amount=10,float travelSpeed=0)
        { damage=amount; velocity=direction.normalized*(travelSpeed>0 ? travelSpeed : speed); session=value; StartFlight(); }
        public void LaunchPlayer(Vector3 direction,CircusSession value,float amount,float bossScale=1)
        { bossDamageScale=bossScale;playerShot=true; damage=amount; velocity=direction.normalized*18; session=value; life=2; StartFlight(); }
        void StartFlight() { flight=CircusAudio.BeginFlight(transform.position); }
        void Update()
        {
            if(bossOwned && (!owner || owner.Health<=0)) { Destroy(gameObject);return; }
            if(!session || !session.IsRunning) return;
            if(Homing)
            {
                var desired=(session.Player.transform.position+Vector3.up-transform.position).normalized;
                velocity=Vector3.RotateTowards(velocity.normalized,desired,turnDegrees*Mathf.Deg2Rad*Time.deltaTime,0)*velocity.magnitude;
            }
            var step=velocity*Time.deltaTime;
            if(Physics.SphereCast(transform.position,hitRadius,step.normalized,out var hit,step.magnitude,
                (1<<0)|(1<<(playerShot ? 9 : 8)),QueryTriggerInteraction.Ignore))
            {
                if(playerShot) {var enemy=hit.collider.GetComponentInParent<CircusEnemy>();if(enemy)enemy.TakeDamage(damage*(enemy.IsBoss?bossDamageScale:1));}
                else hit.collider.GetComponentInParent<CircusPlayer>()?.TakeDamage(damage);
                CircusAudio.Play(CircusCue.FlameImpact,hit.point,.65f);
                CircusImpact.Create(hit.point,flameColor); Destroy(gameObject); return;
            }
            transform.position+=step;
            if(flight) flight.transform.position=transform.position;
            age+=Time.deltaTime; if(age>=life) Destroy(gameObject);
        }
        void OnDestroy() { if(flight) flight.Stop(); }
    }
}
