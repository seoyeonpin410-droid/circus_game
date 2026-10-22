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
        CircusAttackArea spawnArea;
        CircusCharge charge;
        Renderer[] modelRenderers;
        Canvas healthCanvas;
        MaterialPropertyBlock flashBlock;
        readonly System.Collections.Generic.List<Material> flashMaterials=new System.Collections.Generic.List<Material>();
        float spawnUntil, flashUntil;
        bool wasFlashing;
        Vector3 shotDirection,modelRest,attackFacing;
        [SerializeField] float rangedLean=10,rangedPullback=.1f,rangedRecoil=.07f,rangedRecovery=.24f;
        float recoilStart=-100,emergeStarted=-100;
        public bool Winding=>winding;
        float nextAttack, maxHealth, verticalSpeed, windupUntil;
        bool dead, winding;
        public CircusCombatArea CombatArea {get;private set;}
        public void BindArea(CircusCombatArea area){CombatArea=area;}
        public bool IsRanged=>ranged;
        public bool IsBoss=>GetComponent<CircusBoss>()!=null;
        public float Health=>health;
        public float MaxHealth=>maxHealth;
        public CircusProjectile Projectile=>projectile;
        public Transform Model=>model;
        public bool Spawning=>spawnArea && Time.time<spawnUntil;
        public bool Flashing=>Time.time<flashUntil;
        public void Bind(CircusSession value, float healthMultiplier=1, float intervalMultiplier=1)
        {
            session=value; modelRest=model.localPosition;controller=GetComponent<CharacterController>();
            health*=healthMultiplier; maxHealth=health; attackInterval*=intervalMultiplier;
            nextAttack=Time.time+Random.Range(1.5f,2.5f);
            area=CircusAttackArea.Create("Enemy melee warning",transform);
            modelRenderers=model.GetComponentsInChildren<Renderer>(); flashBlock=new MaterialPropertyBlock();
            healthCanvas=healthFill ? healthFill.GetComponentInParent<Canvas>(true) : null;
            // Imported artist materials remain untouched; enable emission on owned runtime copies.
            foreach(var renderer in modelRenderers)
            {
                var materials=renderer.sharedMaterials;bool changed=false;
                for(int i=0;i<materials.Length;i++)
                {
                    if(!materials[i] || !materials[i].HasProperty("_EmissionColor") || materials[i].IsKeywordEnabled("_EMISSION"))continue;
                    var copy=new Material(materials[i]);copy.EnableKeyword("_EMISSION");copy.SetColor("_EmissionColor",Color.black);
                    flashMaterials.Add(copy);materials[i]=copy;changed=true;
                }
                if(changed)renderer.sharedMaterials=materials;
            }
            if(ranged && !IsBoss) charge=CircusCharge.Create(transform,Vector3.up*1.65f,true);
            if(warning) warning.enabled=false;
            if(IsBoss) GetComponent<CircusBoss>().Bind(session,this);
        }
        public void PrepareSpawn(float seconds)
        {
            spawnUntil=Time.time+seconds;
            spawnArea=CircusAttackArea.Create("Enemy spawn warning",transform);
            spawnArea.Show(transform.position,Vector3.forward,1,180,new Color(.65f,.35f,1,.35f));
            foreach(var renderer in modelRenderers) renderer.enabled=false;
            if(controller) controller.enabled=false;
            if(healthCanvas) healthCanvas.gameObject.SetActive(false);
        }
        void Update()
        {
            if(spawnArea)
            {
                if(!session || !session.IsRunning || Time.time<spawnUntil) return;
                foreach(var renderer in modelRenderers) renderer.enabled=true;
                if(controller) controller.enabled=true;
                if(healthCanvas) healthCanvas.gameObject.SetActive(true);
                Destroy(spawnArea.gameObject); spawnArea=null;emergeStarted=Time.time;CircusCombatFx.Burst(transform.position+Vector3.up*.1f,new Color(.65f,.35f,1),8,.18f,1);
                nextAttack=Mathf.Max(nextAttack,Time.time+.6f);
            }
            if(!session || !session.Player || !session.IsRunning || dead) { if(area) area.Hide(); if(warning) warning.enabled=false; return; }
            if(IsBoss) return;
            if(CombatArea && (CombatArea.State!=CircusAreaState.Battle || !CombatArea.Contains(session.Player.transform.position))){area.Hide();if(charge)charge.Hide();return;}
            var direction=session.Player.transform.position-transform.position; direction.y=0;
            if(winding)
            {
                if(!ranged) area.Show(area.transform.position-Vector3.up*.09f,area.transform.forward,meleeRange,180,new Color(1,.13f,.08f,.25f+.25f*(1-(windupUntil-Time.time)/windupSeconds)));
                if(ranged) {
                    float t=Mathf.Clamp01(1-(windupUntil-Time.time)/Mathf.Max(.01f,windupSeconds));
                    model.rotation=Quaternion.LookRotation(attackFacing)*Quaternion.Euler(-rangedLean*t,0,0);
                    model.localPosition=modelRest+transform.InverseTransformDirection(attackFacing)*(-rangedPullback*t);
                    if(charge)charge.Show(t);
                }
                if(Time.time<windupUntil) return;
                winding=false; nextAttack=Time.time+attackInterval;
                if(ranged)
                {
                    var origin=charge ? charge.transform.position : transform.position+Vector3.up*1.1f;
                    var shot=Instantiate(projectile,origin,Quaternion.identity);
                    shot.Launch(shotDirection,session);recoilStart=Time.time;
                    if(charge) charge.Hide();
                    CircusAudio.Play(CircusCue.FlameLaunch,origin);
                }
                else
                {
                    CircusAudio.Play(CircusCue.EnemyMelee,transform.position);
                    if(area.Contains(session.Player.transform.position)) session.Player.TakeDamage(12);
                }
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
                movement=Vector3.ClampMagnitude(movement,1);
                if(CombatArea && movement.sqrMagnitude>.01f && Physics.SphereCast(transform.position+Vector3.up,.35f,movement.normalized,out var obstacle,1.2f,1<<0,QueryTriggerInteraction.Ignore)){
                    var left=Quaternion.Euler(0,-70,0)*movement;var right=Quaternion.Euler(0,70,0)*movement;
                    bool clearLeft=!Physics.SphereCast(transform.position+Vector3.up,.35f,left.normalized,out var a,1.2f,1<<0,QueryTriggerInteraction.Ignore);
                    bool clearRight=!Physics.SphereCast(transform.position+Vector3.up,.35f,right.normalized,out var b,1.2f,1<<0,QueryTriggerInteraction.Ignore);
                    movement=clearLeft && clearRight?(Vector3.Dot(left,direction)>Vector3.Dot(right,direction)?left:right):clearLeft?left:clearRight?right:Vector3.zero;
                }
                var step=movement*moveSpeed*Time.deltaTime;
                if(CombatArea){var target=CombatArea.Clamp(transform.position+step,.8f);step.x=target.x-transform.position.x;step.z=target.z-transform.position.z;}
                controller.Move(step+Vector3.up*verticalSpeed*Time.deltaTime);
            }
            if(model && direction.sqrMagnitude>.001f) model.rotation=Quaternion.LookRotation(direction);
            if(Time.time<nextAttack || (!ranged && direction.magnitude>meleeRange)) return;
            winding=true; windupUntil=Time.time+windupSeconds;attackFacing=direction.sqrMagnitude>.001f?direction.normalized:model.forward;
            if(ranged)
            {
                if(charge) { charge.transform.position=transform.position+Vector3.up*1.65f+model.forward*.85f;charge.Show(.4f); }
                var origin=charge ? charge.transform.position : transform.position+Vector3.up*1.1f;
                shotDirection=(session.Player.transform.position+Vector3.up-origin).normalized;
                CircusAudio.Play(CircusCue.MagicCharge,transform.position,.25f);
                CircusAudio.Play(CircusCue.Warning,transform.position,.3f);
            }
            if(!ranged) area.Show(transform.position,direction,meleeRange,180,new Color(1,.16f,.08f,.25f));
            if(ranged && warning) warning.enabled=false;
        }
        public void TakeDamage(float damage)
        {
            if(dead || Spawning || !session || !session.IsRunning || damage<=0) return;
            var boss=GetComponent<CircusBoss>();
            if(boss && boss.IsInvulnerable)return;
            health=Mathf.Max(0,health-damage);
            flashUntil=Time.time+.1f;
            CircusImpact.Create(transform.position+Vector3.up*1.1f,new Color(1,.8f,.35f));
            if(healthFill) healthFill.fillAmount=health/maxHealth;
            CircusAudio.Play(IsBoss ? CircusCue.BossHit : CircusCue.EnemyHit,transform.position,.7f);
            if(health>0) return;
            dead=true;session.EnemyDefeated(this);
            if(boss) { boss.BeginDeath();return; }
            CircusAudio.Play(CircusCue.EnemyDeath,transform.position);Destroy(gameObject);
        }
        void LateUpdate()
        {
            if(healthCanvas && Camera.main) healthCanvas.transform.rotation=Camera.main.transform.rotation;
            if(ranged && !IsBoss && !winding && session && session.IsRunning) {
                float t=Mathf.Clamp01((Time.time-recoilStart)/Mathf.Max(.01f,rangedRecovery));
                float kick=t<.2f?Mathf.Lerp(-rangedPullback,rangedRecoil,t/.2f):Mathf.Lerp(rangedRecoil,0,Mathf.SmoothStep(0,1,(t-.2f)/.8f));
                model.localPosition=modelRest+transform.InverseTransformDirection(attackFacing)*kick;
                if(t<1)model.rotation=Quaternion.LookRotation(attackFacing)*Quaternion.Euler(Mathf.Sin(t*Mathf.PI)*6,0,0);
            }
            if(!ranged && Time.time-emergeStarted>=.25f)model.localPosition=modelRest;
            if(Time.time-emergeStarted<.25f)model.localPosition=modelRest-Vector3.up*(.65f*(1-Mathf.SmoothStep(0,1,(Time.time-emergeStarted)/.25f)));
            bool flashing=Flashing;
            if(modelRenderers!=null && (flashing || wasFlashing))
                foreach(var renderer in modelRenderers)
                {
                    if(!renderer) continue;
                    renderer.GetPropertyBlock(flashBlock);
                    flashBlock.SetColor("_EmissionColor",flashing ? new Color(.65f,.5f,.25f) : Color.black);
                    if(renderer.sharedMaterial && renderer.sharedMaterial.HasProperty("_BaseColor"))
                        flashBlock.SetColor("_BaseColor",flashing ? new Color(1,.95f,.65f) : renderer.sharedMaterial.GetColor("_BaseColor"));
                    renderer.SetPropertyBlock(flashBlock);
                }
            wasFlashing=flashing;
        }
        void OnDestroy() { foreach(var material in flashMaterials) if(material) Destroy(material); }
    }
}
