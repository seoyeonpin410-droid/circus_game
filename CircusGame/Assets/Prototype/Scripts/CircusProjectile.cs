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
        float age, damage=10;
        bool playerShot;
        AudioSource flight;
        public bool IsPlayerShot=>playerShot;
        public void Launch(Vector3 direction,CircusSession value,float amount=10,float travelSpeed=0)
        { damage=amount; velocity=direction.normalized*(travelSpeed>0 ? travelSpeed : speed); session=value; StartFlight(); }
        public void LaunchPlayer(Vector3 direction,CircusSession value,float amount)
        { playerShot=true; damage=amount; velocity=direction.normalized*18; session=value; life=2; StartFlight(); }
        void StartFlight() { flight=CircusAudio.BeginFlight(transform.position); }
        void Update()
        {
            if(!session || !session.IsRunning) return;
            var step=velocity*Time.deltaTime;
            if(Physics.SphereCast(transform.position,hitRadius,step.normalized,out var hit,step.magnitude,
                (1<<0)|(1<<(playerShot ? 9 : 8)),QueryTriggerInteraction.Ignore))
            {
                if(playerShot) hit.collider.GetComponentInParent<CircusEnemy>()?.TakeDamage(damage);
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
