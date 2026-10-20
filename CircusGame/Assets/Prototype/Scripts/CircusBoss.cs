using UnityEngine;
namespace CircusPrototype
{
    public enum BossAction { Entrance, Chase, MeleeWindup, RangedWindup, Recovery, Dead }
    [RequireComponent(typeof(CircusEnemy),typeof(CharacterController))]
    public sealed class CircusBoss : MonoBehaviour
    {
        [SerializeField] float moveSpeed=2.1f, meleeRadius=3.8f, meleeHalfAngle=65, meleeDamage=24;
        [SerializeField] float windupSeconds=.5f, meleeCooldown=1.4f, rangedCooldown=2.1f;
        [SerializeField] float rangedDistance=6, projectileSpeed=8.5f, projectileDamage=14;
        CircusSession session;
        CircusEnemy enemy;
        CharacterController controller;
        CircusAttackArea area;
        Renderer[] renderers;
        MaterialPropertyBlock block;
        float until, verticalSpeed, nextRanged;
        public BossAction Action { get; private set; }
        public CircusAttackArea AttackArea=>area;
        public int MeleeAttacks { get; private set; }
        public int RangedAttacks { get; private set; }
        public void Bind(CircusSession value,CircusEnemy owner)
        {
            session=value; enemy=owner; controller=GetComponent<CharacterController>();
            block=new MaterialPropertyBlock();
            area=CircusAttackArea.Create("Boss danger sector",transform);
            renderers=enemy.Model.GetComponentsInChildren<Renderer>();
            Action=BossAction.Entrance; until=Time.time+1.4f; nextRanged=until;
        }
        void Glow(float intensity)
        {
            block.SetColor("_EmissionColor",new Color(.08f,1,.8f)*intensity);
            foreach(var renderer in renderers) if (renderer) renderer.SetPropertyBlock(block);
        }
        void Update()
        {
            if(!enemy || enemy.Health<=0) { Action=BossAction.Dead; return; }
            if(!session || !session.IsRunning) { area.Hide(); Glow(0); return; }
            var direction=session.Player.transform.position-transform.position; direction.y=0;
            if(Action==BossAction.MeleeWindup)
            {
                area.Show(area.transform.position-Vector3.up*.09f,area.transform.forward,meleeRadius,meleeHalfAngle,new Color(1,.1f,.13f,Mathf.Lerp(.6f,.25f,(until-Time.time)/windupSeconds)));
                Glow(1);
                if(Time.time<until) return;
                if(area.Contains(session.Player.transform.position)) session.Player.TakeDamage(meleeDamage);
                MeleeAttacks++; CircusAudio.Play(CircusCue.BossMelee,transform.position);
                area.Hide(); Recover(meleeCooldown); return;
            }
            if(Action==BossAction.RangedWindup)
            {
                Glow(1.5f+Mathf.Sin(Time.time*30));
                if(Time.time<until) return;
                var origin=transform.position+Vector3.up*2;
                var shot=Instantiate(enemy.Projectile,origin,Quaternion.identity);
                shot.Launch((session.Player.transform.position+Vector3.up-origin).normalized,session,projectileDamage,projectileSpeed);
                RangedAttacks++; CircusAudio.Play(CircusCue.BossShot,origin); nextRanged=Time.time+rangedCooldown;
                Recover(.65f); return;
            }
            if(Action==BossAction.Entrance || Action==BossAction.Recovery)
            {
                Glow(Action==BossAction.Entrance ? .7f : 0);
                if(Time.time<until) return; Action=BossAction.Chase;
            }
            Glow(0);
            if(direction.sqrMagnitude>.01f) enemy.Model.rotation=Quaternion.LookRotation(direction);
            if(direction.magnitude<=meleeRadius-.2f)
            {
                Action=BossAction.MeleeWindup; until=Time.time+windupSeconds;
                area.Show(transform.position,direction,meleeRadius,meleeHalfAngle,new Color(1,.1f,.13f,.25f));
                CircusAudio.Play(CircusCue.Warning,transform.position); return;
            }
            if(direction.magnitude>=rangedDistance && Time.time>=nextRanged)
            {
                Action=BossAction.RangedWindup; until=Time.time+windupSeconds;
                CircusAudio.Play(CircusCue.Warning,transform.position); return;
            }
            if(controller.isGrounded && verticalSpeed<0) verticalSpeed=-2;
            verticalSpeed-=20*Time.deltaTime;
            controller.Move((direction.normalized*moveSpeed+Vector3.up*verticalSpeed)*Time.deltaTime);
        }
        void Recover(float seconds) { Glow(0); Action=BossAction.Recovery; until=Time.time+seconds; }
    }
}
