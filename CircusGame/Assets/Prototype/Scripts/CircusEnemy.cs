using UnityEngine;
namespace CircusPrototype
{
    public sealed class CircusEnemy : MonoBehaviour
    {
        [SerializeField] bool ranged;
        [SerializeField] float health=50, attackInterval=2.3f, meleeRange=2.2f, moveSpeed=2.6f;
        [SerializeField] float windupSeconds=.45f;
        [SerializeField] CircusProjectile projectile;
        [SerializeField] Transform model;
        [SerializeField] UnityEngine.UI.Image healthFill;
        [SerializeField] LineRenderer warning;
        CircusSession session;
        CharacterController controller;
        CircusAttackArea area;
        float nextAttack, maxHealth, verticalSpeed, windupUntil;
        bool dead, winding;
        public bool IsRanged=>ranged;
        public bool IsBoss=>GetComponent<CircusBoss>()!=null;
        public float Health=>health;
        public float MaxHealth=>maxHealth;
        public CircusProjectile Projectile=>projectile;
        public Transform Model=>model;
        public void Bind(CircusSession value, float healthMultiplier=1, float intervalMultiplier=1)
        {
            session=value; controller=GetComponent<CharacterController>();
            health*=healthMultiplier; maxHealth=health; attackInterval*=intervalMultiplier;
            nextAttack=Time.time+Random.Range(1.5f,2.5f);
            area=CircusAttackArea.Create("Enemy melee warning",transform);
            if(warning) warning.enabled=false;
            if(IsBoss) GetComponent<CircusBoss>().Bind(session,this);
        }
        void Update()
        {
            if(!session || !session.Player || !session.IsRunning || dead) { if(area) area.Hide(); if(warning) warning.enabled=false; return; }
            if(IsBoss) return;
            var direction=session.Player.transform.position-transform.position; direction.y=0;
            if(winding)
            {
                if(!ranged) area.Show(area.transform.position-Vector3.up*.09f,area.transform.forward,meleeRange,180,new Color(1,.13f,.08f,.25f+.25f*(1-(windupUntil-Time.time)/windupSeconds)));
                if(Time.time<windupUntil) return;
                winding=false; nextAttack=Time.time+attackInterval;
                if(ranged)
                {
                    var origin=transform.position+Vector3.up*1.1f;
                    var shot=Instantiate(projectile,origin,Quaternion.identity);
                    shot.Launch((session.Player.transform.position+Vector3.up-origin).normalized,session);
                    CircusAudio.Play(CircusCue.FlameLaunch,origin);
                }
                else if(area.Contains(session.Player.transform.position)) session.Player.TakeDamage(12);
                area.Hide(); if(warning) warning.enabled=false; return;
            }
            if(!ranged && controller)
            {
                var movement=direction.magnitude>meleeRange*.72f ? direction.normalized : Vector3.zero;
                foreach(var other in session.Enemies)
                {
                    if(!other || other==this) continue;
                    var away=transform.position-other.transform.position; away.y=0;
                    if(away.sqrMagnitude>.001f && away.sqrMagnitude<1.44f) movement+=away.normalized*(1.2f-away.magnitude);
                }
                if(controller.isGrounded && verticalSpeed<0) verticalSpeed=-2;
                verticalSpeed-=20*Time.deltaTime;
                controller.Move((Vector3.ClampMagnitude(movement,1)*moveSpeed+Vector3.up*verticalSpeed)*Time.deltaTime);
            }
            if(model && direction.sqrMagnitude>.001f) model.rotation=Quaternion.LookRotation(direction);
            if(Time.time<nextAttack || (!ranged && direction.magnitude>meleeRange)) return;
            winding=true; windupUntil=Time.time+windupSeconds;
            if(!ranged) area.Show(transform.position,direction,meleeRange,180,new Color(1,.16f,.08f,.25f));
            if(ranged && warning) { warning.enabled=true; warning.SetPosition(0,transform.position+Vector3.up*.2f); warning.SetPosition(1,session.Player.transform.position+Vector3.up*.2f); }
        }
        public void TakeDamage(float damage)
        {
            if(dead || !session || !session.IsRunning) return;
            health=Mathf.Max(0,health-damage);
            if(healthFill) healthFill.fillAmount=health/maxHealth;
            CircusAudio.Play(IsBoss ? CircusCue.BossHit : CircusCue.EnemyHit,transform.position,.7f);
            if(health>0) return;
            dead=true; CircusAudio.Play(IsBoss ? CircusCue.BossDeath : CircusCue.EnemyDeath,transform.position);
            session.EnemyDefeated(this); Destroy(gameObject);
        }
        void LateUpdate() { if(healthFill && healthFill.canvas && Camera.main) healthFill.canvas.transform.rotation=Camera.main.transform.rotation; }
    }
}
