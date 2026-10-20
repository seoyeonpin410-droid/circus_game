using UnityEngine;
namespace CircusPrototype
{
    public enum BossAction { Entrance, Chase, MeleeWindup, RangedWindup, Recovery, Dead, GroundCast, Idle, MeleeAttack, RangedAttack, PhaseTransition }
    [RequireComponent(typeof(CircusEnemy),typeof(CharacterController))]
    public sealed class CircusBoss : MonoBehaviour
    {
        [SerializeField] float moveSpeed=2.1f, meleeRadius=3.8f, meleeHalfAngle=65, meleeDamage=24;
        [SerializeField] float windupSeconds=.65f, meleeCooldown=1.4f, rangedCooldown=2.1f;
        [SerializeField] float rangedDistance=6, projectileSpeed=8.5f, projectileDamage=14;
        [SerializeField] float groundCooldown=7.5f, groundRadius=1.8f, groundWarning=1.15f, groundDuration=1.4f, groundDamage=18;
        [SerializeField] int groundCount=3;
        [SerializeField] float phaseTwoSpeed=1.15f, phaseTwoCooldown=.8f, transitionSeconds=1.2f;
        CircusSession session;
        CircusEnemy enemy;
        CharacterController controller;
        CircusAttackArea area;
        CircusCharge charge;
        Renderer[] renderers;
        MaterialPropertyBlock block;
        float until, verticalSpeed, nextRanged, nextGround, nextSeal;
        int sealsRemaining, comboStep;
        bool comboRecovery;
        Vector3 lockedShotDirection;
        public int Phase { get; private set; }=1;
        public int GroundAttacks { get; private set; }
        public BossAction Action { get; private set; }
        public CircusAttackArea AttackArea=>area;
        public bool Charging=>charge && charge.gameObject.activeSelf;
        public int MeleeAttacks { get; private set; }
        public int RangedAttacks { get; private set; }
        public int LastVolleySize { get; private set; }
        float Windup=>Mathf.Clamp(windupSeconds,.5f,.8f);
        float CooldownScale=>Phase==2 ? phaseTwoCooldown : 1;
        public void Bind(CircusSession value,CircusEnemy owner)
        {
            session=value; enemy=owner; controller=GetComponent<CharacterController>();
            block=new MaterialPropertyBlock();
            area=CircusAttackArea.Create("Boss danger sector",transform);
            charge=CircusCharge.Create(transform,Vector3.up*2.1f);
            renderers=enemy.Model.GetComponentsInChildren<Renderer>();
            Action=BossAction.Entrance; until=Time.time+2.5f; nextRanged=until;
            nextGround=until+6;
        }
        void Glow(float intensity)
        {
            block.Clear(); block.SetColor("_EmissionColor",new Color(.08f,1,.8f)*intensity);
            foreach(var renderer in renderers) if(renderer) renderer.SetPropertyBlock(block);
        }
        void Update()
        {
            if(!enemy || enemy.Health<=0) { Action=BossAction.Dead; return; }
            if(!session || !session.IsRunning) return;
            var direction=session.Player.transform.position-transform.position; direction.y=0;
            if(Phase==1 && enemy.Health<=enemy.MaxHealth*.5f)
            {
                Phase=2; Action=BossAction.PhaseTransition; until=Time.time+transitionSeconds;
                area.Hide(); charge.Hide(); comboRecovery=false; comboStep=0; sealsRemaining=0;
                FindFirstObjectByType<CircusHud>()?.Announce("도깨비 대장 · 2페이즈",transitionSeconds+1);
                CircusAudio.Play(CircusCue.PhaseShift,transform.position); return;
            }
            if(Action==BossAction.PhaseTransition)
            {
                Glow(1+Mathf.PingPong(Time.time*4,2));
                if(Time.time>=until) { nextGround=Mathf.Max(nextGround,Time.time+2); Recover(.6f); }
                return;
            }
            if(Action==BossAction.GroundCast)
            {
                Glow(1.2f);
                if(sealsRemaining>0 && Time.time>=nextSeal)
                {
                    CircusGroundFlame.Create(session,enemy,session.Player.transform.position,groundRadius,groundWarning,groundDuration,groundDamage);
                    sealsRemaining--; nextSeal=Time.time+.45f;
                }
                if(sealsRemaining==0 && Time.time>=nextSeal) Recover(.7f);
                return;
            }
            if(Action==BossAction.MeleeWindup)
            {
                area.Show(area.transform.position-Vector3.up*.09f,area.transform.forward,meleeRadius,meleeHalfAngle,
                    new Color(1,.1f,.13f,Mathf.Lerp(.6f,.25f,Mathf.Clamp01((until-Time.time)/Windup))));
                Glow(1);
                if(Time.time<until) return;
                if(area.Contains(session.Player.transform.position)) session.Player.TakeDamage(meleeDamage);
                MeleeAttacks++; CircusAudio.Play(CircusCue.BossMelee,transform.position);
                area.Hide(); Action=BossAction.MeleeAttack; until=Time.time+.1f; return;
            }
            if(Action==BossAction.MeleeAttack)
            {
                if(Time.time<until) return;
                comboRecovery=Phase==2 && comboStep==0; comboStep++;
                Recover(comboRecovery ? .32f : meleeCooldown*CooldownScale); return;
            }
            if(Action==BossAction.RangedWindup)
            {
                Glow(1.5f+Mathf.Sin(Time.time*20)*.5f);
                charge.Show(1-Mathf.Clamp01((until-Time.time)/Windup));
                if(Time.time<until) return;
                var origin=charge.transform.position; charge.Hide();
                LastVolleySize=Phase==2 ? 3 : 1;
                for(int i=0;i<LastVolleySize;i++)
                {
                    var shot=Instantiate(enemy.Projectile,origin,Quaternion.identity);
                    var aim=Quaternion.AngleAxis(LastVolleySize==1 ? 0 : (i-1)*17,Vector3.up)*lockedShotDirection;
                    shot.Launch(aim,session,projectileDamage,projectileSpeed);
                }
                RangedAttacks++; CircusAudio.Play(CircusCue.BossShot,origin);
                nextRanged=Time.time+rangedCooldown*CooldownScale;
                Action=BossAction.RangedAttack; until=Time.time+.12f; return;
            }
            if(Action==BossAction.RangedAttack) { if(Time.time>=until) Recover(.8f*CooldownScale); return; }
            if(Action==BossAction.Entrance || Action==BossAction.Recovery || Action==BossAction.Idle)
            {
                Glow(Action==BossAction.Entrance ? .7f : 0);
                if(Time.time<until) return;
                if(comboRecovery)
                {
                    comboRecovery=false;
                    if(direction.magnitude<=meleeRadius-.2f) { BeginMelee(direction); return; }
                }
                Action=BossAction.Chase;
            }
            Glow(0);
            if(direction.sqrMagnitude>.01f) enemy.Model.rotation=Quaternion.LookRotation(direction);
            if(Time.time>=nextGround)
            {
                Action=BossAction.GroundCast; GroundAttacks++; sealsRemaining=Mathf.Max(1,groundCount);
                nextSeal=Time.time; nextGround=Time.time+groundCooldown;
                FindFirstObjectByType<CircusHud>()?.Announce("도깨비불 장판 · 보라색 원을 벗어나세요",2);
                CircusAudio.Play(CircusCue.MagicCharge,transform.position); return;
            }
            if(direction.magnitude<=meleeRadius-.2f) { comboStep=0; BeginMelee(direction); return; }
            if(direction.magnitude>=rangedDistance && Time.time>=nextRanged)
            {
                Action=BossAction.RangedWindup; until=Time.time+Windup;
                charge.transform.position=transform.position+Vector3.up*2.1f+enemy.Model.forward*1.2f;
                lockedShotDirection=(session.Player.transform.position+Vector3.up-charge.transform.position).normalized;
                charge.Show(0); CircusAudio.Play(CircusCue.MagicCharge,transform.position);
                CircusAudio.Play(CircusCue.Warning,transform.position,.3f); return;
            }
            if(controller.isGrounded && verticalSpeed<0) verticalSpeed=-2;
            verticalSpeed-=20*Time.deltaTime;
            controller.Move((direction.normalized*moveSpeed*(Phase==2?phaseTwoSpeed:1)+Vector3.up*verticalSpeed)*Time.deltaTime);
        }
        void BeginMelee(Vector3 direction)
        {
            if(direction.sqrMagnitude<.001f) direction=enemy.Model.forward;
            Action=BossAction.MeleeWindup; until=Time.time+Windup;
            area.Show(transform.position,direction,meleeRadius,meleeHalfAngle,new Color(1,.1f,.13f,.25f));
        }
        void Recover(float seconds) { Glow(0); Action=BossAction.Recovery; until=Time.time+seconds; }
    }
}
