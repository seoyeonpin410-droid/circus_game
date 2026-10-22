using System;
using System.Collections.Generic;
using UnityEngine;
namespace CircusPrototype
{
    public enum BossAction { Entrance, Chase, MeleeWindup, RangedWindup, Recovery, Dead, GroundCast, Idle, MeleeAttack, RangedAttack, PhaseTransition, PatternSelect, SpecialWindup, SpecialAttack }
    public enum BossPattern { Swing, Slam, SingleFlame, FanFlame, HomingFlames, Charge, DoubleSwing, RingFlames, GroundSeals, SequentialFlames, MultiDash, SafeZones, OrbitVolley, Finale }
    [Serializable]
    public sealed class BossPatternSettings
    {
        [Min(0)] public float damage,windup,recovery,cooldown;
        [Min(.1f)] public float range,speed;
        [Range(1,12)] public int projectiles;
        [Range(0,180)] public float spread;
        public BossPatternSettings(float damage,float windup,float recovery,float cooldown,float range,float speed=8.5f,int projectiles=1,float spread=17)
        {this.damage=damage;this.windup=windup;this.recovery=recovery;this.cooldown=cooldown;this.range=range;this.speed=speed;this.projectiles=projectiles;this.spread=spread;}
    }
    [RequireComponent(typeof(CircusEnemy),typeof(CharacterController))]
    public sealed partial class CircusBoss : MonoBehaviour
    {
        [Header("Movement and phases")]
        [SerializeField] float moveSpeed=2.1f,rangedDistance=6,meleeHalfAngle=65;
        [SerializeField,Range(.1f,.95f)] float phaseTwoHealth=.7f,phaseThreeHealth=.35f;
        [SerializeField] float phaseTwoSpeed=1.15f,phaseThreeSpeed=1.2f,phaseTwoCooldown=.9f,phaseThreeCooldown=.85f;
        [SerializeField] float transitionSeconds=1.2f,entranceSeconds=4.1f,deathSeconds=1.5f,positionSeconds=.45f;
        [SerializeField] Vector3 arenaCenter=Vector3.zero;
        [SerializeField] float centerStopDistance=1.2f;
        [Header("Pattern damage / warning / recovery / cooldown / range / speed / count / spread")]
        [SerializeField] BossPatternSettings swing=new BossPatternSettings(24,.55f,.9f,2,3.8f);
        [SerializeField] BossPatternSettings slam=new BossPatternSettings(32,.8f,1.4f,7,4.2f);
        [SerializeField] BossPatternSettings singleFlame=new BossPatternSettings(14,.65f,.9f,3,16);
        [SerializeField] BossPatternSettings fanFlame=new BossPatternSettings(14,.7f,1.1f,6,16,8,3,20);
        [SerializeField] BossPatternSettings homingFlames=new BossPatternSettings(12,.75f,1.3f,10,16,6.5f,3,22);
        [SerializeField] BossPatternSettings dash=new BossPatternSettings(26,.75f,1.4f,9,8,12);
        [SerializeField] BossPatternSettings doubleSwing=new BossPatternSettings(22,.55f,1.15f,6,3.8f);
        [SerializeField] BossPatternSettings ringFlames=new BossPatternSettings(14,1.1f,1.8f,22,16,6,10,30);
        [SerializeField] BossPatternSettings groundSeals=new BossPatternSettings(18,1.15f,1.3f,16,1.8f,0,3);
        [Header("Projectile and charge tuning")]
        [SerializeField] float projectileLifetime=3.5f,homingSeconds=1,homingDegreesPerSecond=28,homingInterval=.3f;
        [SerializeField] float dashWidth=2.4f,comboGap=.3f,groundDuration=1.4f,groundInterval=.45f;
        CircusSession session;
        CircusEnemy enemy;
        CharacterController controller;
        CircusAttackArea area;
        CircusBossEntrance entrance;
        [Header("Attack silhouette and release timing")]
        [SerializeField] float meleeTwist=26,meleeFollowThrough=48,meleeStrikeSeconds=.1f,meleeVisualStep=.12f;
        [SerializeField] float rangedPullback=.18f,rangedKick=.12f,rangedReleaseHold=.16f,poseRecoverySeconds=.25f;
        [SerializeField] float dashCrouch=.16f,dashPullback=.16f;
        bool meleeContact;
        Vector3 recoveryPosition;Quaternion recoveryRotation;
        public bool MeleeContact=>meleeContact;
        public CircusBossEntrance Entrance=>entrance;
        readonly List<CircusCharge> charges=new List<CircusCharge>();
        readonly Dictionary<BossPattern,float> ready=new Dictionary<BossPattern,float>();
        readonly List<BossPattern> candidates=new List<BossPattern>();
        readonly List<BossPattern> history=new List<BossPattern>();
        Renderer[] renderers;
        MaterialPropertyBlock block;
        [SerializeField] float slamLift=.45f,slamDropSeconds=.1f,slamPoseRecovery=.3f;
        Vector3 modelRest;
        bool slamContact;
        public bool SlamContact=>slamContact;
        Vector3 baseScale,lockedDirection,attackOrigin,dashPrevious;
        float until,started,verticalSpeed,nextGround,nextRelease,dashTravel;
        int shotIndex,comboStep,selectionIndex;
        bool dashHit,retreating,centering;
        BossPattern? lastPattern,followup;
        public int Phase { get; private set; }=1;
        public int PhaseTransitions { get; private set; }
        public float EntranceSeconds=>GetComponent<CircusBossEntrance>()?GetComponent<CircusBossEntrance>().Duration:Mathf.Max(1,entranceSeconds);
        public float DeathSeconds=>Mathf.Max(.2f,deathSeconds);
        public BossAction Action { get; private set; }
        public BossPattern Pattern { get; private set; }
        public CircusAttackArea AttackArea=>area;
        public bool Charging=>charges.Exists(c=>c && c.gameObject.activeSelf);
        public bool IsInvulnerable=>Action==BossAction.Entrance || Action==BossAction.PhaseTransition || Action==BossAction.Dead;
        public int MeleeAttacks { get; private set; }
        public int RangedAttacks { get; private set; }
        public int GroundAttacks { get; private set; }
        public int LastVolleySize { get; private set; }
        public IReadOnlyList<BossPattern> PatternHistory=>history;
        public BossPatternSettings Settings(BossPattern p)
        {
            switch(p) {
                case BossPattern.Swing:return swing;case BossPattern.Slam:return slam;case BossPattern.SingleFlame:return singleFlame;
                case BossPattern.FanFlame:return fanFlame;case BossPattern.HomingFlames:return homingFlames;case BossPattern.Charge:return dash;
                case BossPattern.DoubleSwing:return doubleSwing;case BossPattern.RingFlames:return ringFlames;
                case BossPattern.SequentialFlames:return sequentialFlames;case BossPattern.MultiDash:return multiDash;case BossPattern.SafeZones:return safeZones;
                case BossPattern.OrbitVolley:return orbitVolley;case BossPattern.Finale:return finale;default:return groundSeals;
            }
        }
        float SpeedScale=>Phase==3?phaseThreeSpeed:Phase==2?phaseTwoSpeed:1;
        float CooldownScale=>Phase==3?phaseThreeCooldown:Phase==2?phaseTwoCooldown:1;
        public void Bind(CircusSession value,CircusEnemy owner)
        {
            session=value;enemy=owner;controller=GetComponent<CharacterController>();block=new MaterialPropertyBlock();
            area=CircusAttackArea.Create("Boss danger area",transform);renderers=enemy.Model.GetComponentsInChildren<Renderer>();baseScale=enemy.Model.localScale;modelRest=enemy.Model.localPosition;
            for(int i=0;i<12;i++)charges.Add(CircusCharge.Create(transform,Vector3.up*2.1f,true));
            Phase=1;Action=BossAction.Entrance;started=Time.time;until=Time.time+EntranceSeconds;nextGround=until+groundSeals.cooldown;
            entrance=GetComponent<CircusBossEntrance>();if(!entrance)entrance=gameObject.AddComponent<CircusBossEntrance>();entrance.Bind(session,enemy);ResetExpanded();
        }
        Vector3 ToPlayer { get { var v=session.Player.transform.position-transform.position;v.y=0;return v; } }
        void Glow(float intensity)
        {
            block.Clear();block.SetColor("_EmissionColor",new Color(.05f,1,.7f)*intensity);
            foreach(var r in renderers)if(r)r.SetPropertyBlock(block);
        }
        void Pose(float lean,float turn=0)
        { if(lockedDirection.sqrMagnitude>.001f)enemy.Model.rotation=Quaternion.LookRotation(lockedDirection)*Quaternion.Euler(lean,turn,0); }
        void HideWarnings() { area.Hide();foreach(var c in charges)if(c)c.Hide(); }
        bool ActiveHazards()
        {
            foreach(var p in FindObjectsByType<CircusProjectile>(FindObjectsSortMode.None))if(p.Owner==enemy)return true;
            foreach(var h in FindObjectsByType<CircusGroundFlame>(FindObjectsSortMode.None))if(h.Owner==enemy)return true;
            foreach(var f in FindObjectsByType<CircusSafeField>(FindObjectsSortMode.None))if(f.Owner==enemy)return true;
            return false;
        }
        public void ClearHazards()
        {
            if(ReferenceEquals(enemy,null))return;
            foreach(var p in FindObjectsByType<CircusProjectile>(FindObjectsSortMode.None))if(ReferenceEquals(p.Owner,enemy))p.Cancel();
            foreach(var h in FindObjectsByType<CircusGroundFlame>(FindObjectsSortMode.None))if(ReferenceEquals(h.Owner,enemy))h.Cancel();
            foreach(var f in FindObjectsByType<CircusSafeField>(FindObjectsSortMode.None))if(ReferenceEquals(f.Owner,enemy))f.Cancel();
        }
        void Update()
        {
            if(!enemy || !session)return;
            if(Action==BossAction.Dead)
            {
                if(session.State==ShowState.Paused)return;
                float t=Mathf.Clamp01((Time.time-started)/Mathf.Max(.1f,deathSeconds));
                enemy.Model.localScale=baseScale*Mathf.Lerp(1,.1f,t);Pose(t*80);Glow((1-t)*2);
                if(Time.time>=until)Destroy(gameObject);return;
            }
            if(Action==BossAction.Entrance)
            {
                if(session.State==ShowState.Paused)return;
                entrance.Tick();Glow(0);
                if(!session.IsRunning || !entrance.Complete)return;
                enemy.Model.localScale=baseScale;Action=BossAction.Idle;until=Time.time+.3f;
            }
            if(!session.IsRunning || enemy.Health<=0)return;
            // Float division may retain extra precision on Mono; include exact HP thresholds.
            float epsilon=Mathf.Max(.001f,enemy.MaxHealth*.00001f);
            int target=enemy.Health<=enemy.MaxHealth*phaseThreeHealth+epsilon?3:enemy.Health<=enemy.MaxHealth*phaseTwoHealth+epsilon?2:1;
            if(target>Phase) { BeginTransition(target);return; }
            if(Action==BossAction.PhaseTransition)
            {
                OrbitCharges(Phase+1,2.2f);Glow(1+Mathf.Sin(Time.time*10)*.4f);
                if(Time.time>=until){HideWarnings();Recover(.6f);}return;
            }
            if(Action==BossAction.Recovery)
            {
                Glow(.08f*(Phase-1));
                RelaxPose(Mathf.SmoothStep(0,1,(Time.time-started)/Mathf.Max(.05f,Pattern==BossPattern.Slam?slamPoseRecovery:poseRecoverySeconds)));
                if(Time.time<until || (ActiveHazards() && !(followup.HasValue && CanOverlapGround(followup.Value))))return;
                Action=BossAction.Chase;
                retreating=Phase==3 && (Pattern==BossPattern.Swing || Pattern==BossPattern.DoubleSwing);
                until=Time.time+(retreating?Mathf.Max(.8f,positionSeconds):positionSeconds);return;
            }
            if(betweenDashes) {
                RelaxPose(Mathf.SmoothStep(0,1,(Time.time-dashGapStarted)/Mathf.Max(.1f,multiDashGap)));
                if(Time.time>=until){betweenDashes=false;BeginWindup();}return;
            }
            if(Action==BossAction.Idle && Pattern==BossPattern.DoubleSwing && comboStep==1)
            { RelaxPose(Mathf.SmoothStep(0,1,(Time.time-(nextRelease-comboGap))/Mathf.Max(.01f,comboGap)));if(Time.time>=nextRelease)BeginWindup();return; }
            if(centering)
            {
                var toward=arenaCenter-transform.position;toward.y=0;
                lockedDirection=toward.sqrMagnitude>.01f?toward.normalized:lockedDirection;Pose(0);Glow(.3f);
                if(toward.magnitude<=centerStopDistance || Time.time>=until){centering=false;if(CustomPattern(Pattern))BeginCustomWindup();else BeginWindup();}
                else Move(toward.normalized*moveSpeed*SpeedScale);
                return;
            }
            if(Action==BossAction.Chase || Action==BossAction.Idle)
            {
                Glow(.15f*(Phase-1));
                var d=ToPlayer;lockedDirection=d.sqrMagnitude>.01f?d.normalized:transform.forward;Pose(0);
                if(Action==BossAction.Chase && !(Pattern==BossPattern.GroundSeals && followup==BossPattern.SingleFlame) && (d.magnitude>3 || retreating))Move((retreating?-1:1)*d.normalized*moveSpeed*SpeedScale);
                if(Time.time>=until)Action=BossAction.PatternSelect;return;
            }
            if(Action==BossAction.PatternSelect){SelectPattern();return;}
            if(CustomPattern(Pattern)){TickCustom();return;}
            var config=Settings(Pattern);
            if(Action==BossAction.MeleeWindup || Action==BossAction.SpecialWindup || Action==BossAction.RangedWindup)
            {
                float progress=1-Mathf.Clamp01((until-Time.time)/Mathf.Max(.1f,windupDuration));
                Glow(.08f+progress*.22f);
                if(Pattern==BossPattern.Swing || Pattern==BossPattern.DoubleSwing) {
                    float sign=comboStep==0?1:-1;Pose(-6*progress,-meleeTwist*sign*progress);Offset(-lockedDirection*.05f*progress);
                } else if(Pattern==BossPattern.Slam) {
                    enemy.Model.localPosition=modelRest+Vector3.up*(slamLift*Mathf.SmoothStep(0,1,progress));Pose(-18*progress);
                    area.ShowSlam(attackOrigin,config.range,progress);
                } else if(IsDash(Pattern)) {
                    Offset(-lockedDirection*dashPullback*progress-Vector3.up*dashCrouch*progress);Pose(18*progress);
                    enemy.Model.localScale=Vector3.Scale(baseScale,new Vector3(1,1-.025f*progress,1));
                } else if(Pattern==BossPattern.GroundSeals) {
                    Offset(-lockedDirection*.08f*progress);Pose(8*progress,12*progress);OrbitCharges(3,1.8f);
                } else {
                    Offset(-lockedDirection*rangedPullback*progress);Pose(-12*progress);ShowCharges(progress);
                }
                if(Time.time>=until)ReleasePattern();return;
            }
            if(Action==BossAction.MeleeAttack)
            {
                float t=Mathf.Clamp01((Time.time-started)/Mathf.Max(.03f,meleeStrikeSeconds));float sign=comboStep==0?1:-1;
                Pose(Mathf.Lerp(-6,10,t),Mathf.Lerp(-meleeTwist,meleeFollowThrough,t)*sign);Offset(lockedDirection*meleeVisualStep*Mathf.Sin(t*Mathf.PI));
                if(!meleeContact && t>=.65f) {
                    meleeContact=true;if(area.Contains(session.Player.transform.position))session.Player.TakeDamage(config.damage);
                    MeleeAttacks++;area.Hide();CircusAudio.Play(CircusCue.BossMelee,transform.position);
                }
                if(Time.time<until)return;
                if(Pattern==BossPattern.DoubleSwing && comboStep==0)
                { CacheRecoveryPose();comboStep=1;Action=BossAction.Idle;until=Time.time+comboGap;nextRelease=until;return; }
                FinishPattern();return;
            }
            if(Action==BossAction.RangedAttack)
            {
                float releasePose=1-Mathf.SmoothStep(0,1,(Time.time-(until-rangedReleaseHold))/Mathf.Max(.05f,rangedReleaseHold));
                Offset(lockedDirection*rangedKick*releasePose);Pose(8*releasePose);
                if(Pattern==BossPattern.HomingFlames && shotIndex<Mathf.Clamp(config.projectiles,1,5) && Time.time>=nextRelease)
                { FireHoming();nextRelease=Time.time+homingInterval; }
                if(Time.time>=until && (shotIndex>=Mathf.Clamp(config.projectiles,1,5) || Pattern!=BossPattern.HomingFlames))FinishPattern();return;
            }
            if(Action==BossAction.SpecialAttack)
            {
                if(IsDash(Pattern)){UpdateDash();return;}
                if(Pattern==BossPattern.Slam){
                    float t=Mathf.Clamp01((Time.time-started)/Mathf.Max(.03f,slamDropSeconds));
                    enemy.Model.localPosition=modelRest+Vector3.up*slamLift*(1-t*t*t);Pose(Mathf.Lerp(-18,16,t));
                    if(t>=1) {
                        enemy.Model.localPosition=modelRest;slamContact=true;
                        if(area.Contains(session.Player.transform.position))session.Player.TakeDamage(config.damage);
                        area.Hide();CircusBossVfx.Ring(session,attackOrigin,new Color(1,.65f,.2f),config.range,.35f);
                        CircusCombatFx.Slam(attackOrigin,config.range);
                        if(ToPlayer.magnitude<9)FindFirstObjectByType<CircusCamera>()?.Shake(.035f,.14f);
                        CircusAudio.PlayAccent(true,attackOrigin);MeleeAttacks++;FinishPattern();
                    }
                    return;
                }
                if(Time.time>=until)FinishPattern();return;
            }
            if(Action==BossAction.GroundCast)
            {
                if(shotIndex<groundPlan.Count && Time.time>=nextRelease)
                {
                    CircusGroundFlame.Create(session,enemy,groundPlan[shotIndex],config.range,hazardWarningSeconds,groundDuration,config.damage);
                    shotIndex++;nextRelease=Time.time+groundInterval;
                }
                if(shotIndex>=groundPlan.Count && Time.time>=nextRelease)FinishPattern();
            }
        }
        void BeginTransition(int phase)
        {
            CancelExpanded();CircusAudio.StopBossPatterns();Phase=phase;PhaseTransitions++;if(Phase==3)ultimateQueued=true;Action=BossAction.PhaseTransition;started=Time.time;until=Time.time+transitionSeconds;
            enemy.Model.localPosition=modelRest;enemy.Model.localScale=baseScale;HideWarnings();ClearHazards();comboStep=0;followup=null;centering=false;
            FindFirstObjectByType<CircusHud>()?.Announce($"도깨비 대장 · {Phase}페이즈",transitionSeconds+1);
            CircusBossVfx.Ring(session,transform.position,new Color(.2f,1,.7f),3,.8f);
            FindFirstObjectByType<CircusCamera>()?.Shake(Phase==3?.03f:.018f,.15f);CircusAudio.PlayBossPattern(CircusBossSound.PhaseShift,transform.position);CircusAudio.Play(CircusCue.PhaseShift,transform.position);CircusAudio.Play(CircusCue.Warning,transform.position,.4f);
        }
        bool IsFlame(BossPattern p)=>p==BossPattern.SingleFlame || p==BossPattern.FanFlame || p==BossPattern.HomingFlames || p==BossPattern.RingFlames;
        public bool CanUse(BossPattern p)
        {
            if(!session || !session.IsRunning || Action==BossAction.Dead || (lastPattern.HasValue && lastPattern.Value==p))return false;
            if(ready.TryGetValue(p,out var time) && Time.time<time)return false;
            if(Heavy(p) && Time.time<heavyBlockedUntil)return false;
            if(CustomPattern(p) || p==BossPattern.MultiDash)return ExpandedAllowed(p);
            float distance=ToPlayer.magnitude;
            if((p==BossPattern.HomingFlames || p==BossPattern.GroundSeals || p==BossPattern.DoubleSwing) && Phase<2)return false;
            if(p==BossPattern.RingFlames)return Phase==3 && distance>=3;
            if(p==BossPattern.Swing || p==BossPattern.DoubleSwing)return distance<=Settings(p).range-.2f;
            if(p==BossPattern.Slam)return distance<=slam.range;
            if(p==BossPattern.Charge)return distance>=3.5f && distance<=dash.range+1;
            if(p==BossPattern.GroundSeals)return Time.time>=nextGround && distance>=4;
            return distance>= (p==BossPattern.SingleFlame?rangedDistance:3.8f);
        }
        public bool TryStartPattern(BossPattern p)
        {
            if(betweenDashes)return false;
            if(Action==BossAction.Idle && Pattern==BossPattern.DoubleSwing && comboStep==1)return false;
            if(centering)return false;
            if(Action!=BossAction.PatternSelect && Action!=BossAction.Chase && Action!=BossAction.Idle)return false;
            if(Time.time<until || (ActiveHazards() && !CanOverlapGround(p)) || !CanUse(p))return false;
            Pattern=p;history.Add(p);lastPattern=p;comboStep=shotIndex=0;
            ready[p]=Time.time+(p==BossPattern.Finale?Mathf.Max(15,Settings(p).cooldown):Settings(p).cooldown*CooldownScale);
            dashPart=0;
            if(CustomPattern(p)){if(p==BossPattern.Finale)ultimateQueued=false;BeginCustom();return true;}
            if(p==BossPattern.GroundSeals)
            {
                BeginWindup();return true;
            }
            if(p==BossPattern.RingFlames)
            {
                var toward=arenaCenter-transform.position;toward.y=0;
                if(toward.magnitude>centerStopDistance){centering=true;Action=BossAction.Chase;until=Time.time+6;return true;}
            }
            BeginWindup();return true;
        }
        void SelectPattern()
        {
            if(Phase==3 && ultimateQueued && TryStartPattern(BossPattern.Finale))return;
            if(nextGround<=0 && TryStartPattern(BossPattern.GroundSeals))return;
            if(followup.HasValue){var p=followup.Value;if(TryStartPattern(p)){followup=null;CombosStarted++;return;}followup=null;}
            candidates.Clear();
            foreach(BossPattern p in Enum.GetValues(typeof(BossPattern)))if(CanUse(p))candidates.Add(p);
            if(candidates.Count>0)
            {
                // Rotate eligible attacks instead of repeatedly rolling the same attack.
                for(int i=0;i<candidates.Count;i++)if(TryStartPattern(candidates[(selectionIndex+i)%candidates.Count])){selectionIndex++;return;}
            }
            Action=BossAction.Chase;until=Time.time+.35f;
        }
        void BeginWindup()
        {
            var config=Settings(Pattern);HideWarnings();started=Time.time;windupDuration=WindupSeconds(config);until=Time.time+windupDuration;
            lockedDirection=ToPlayer.sqrMagnitude>.001f?ToPlayer.normalized:enemy.Model.forward;attackOrigin=transform.position;enemy.Model.localPosition=modelRest;enemy.Model.localScale=baseScale;Pose(0);
            if(Pattern==BossPattern.Swing || Pattern==BossPattern.DoubleSwing)
            {
                Action=BossAction.MeleeWindup;
                var forward=Quaternion.AngleAxis(Pattern==BossPattern.DoubleSwing?(comboStep==0?-20:20):0,Vector3.up)*lockedDirection;
                area.Show(attackOrigin,forward,config.range,meleeHalfAngle,new Color(1,.1f,.12f,.35f));
                CircusAudio.Play(CircusCue.BossPrepare,transform.position,.3f);
            }
            else if(Pattern==BossPattern.Slam)
            {
                Action=BossAction.SpecialWindup;slamContact=false;area.ShowSlam(attackOrigin,config.range,0);
                CircusAudio.Play(CircusCue.SlamPrepare,transform.position);
            }
            else if(IsDash(Pattern))
            {
                Action=BossAction.SpecialWindup;dashTravel=0;dashHit=false;CircusAudio.PlayBossPattern(CircusBossSound.DashPrepare,transform.position);
                area.ShowLane(attackOrigin,lockedDirection,config.range,dashWidth,new Color(1,.2f,.1f,.4f));
                CircusAudio.Play(CircusCue.DashPrepare,transform.position);
            }
            else if(Pattern==BossPattern.GroundSeals) {
                Action=BossAction.SpecialWindup;PlanGround();OrbitCharges(3,1.8f);CircusAudio.PlayBossPattern(CircusBossSound.GroundMark,transform.position);
                FindFirstObjectByType<CircusHud>()?.Announce("도깨비불 장판 · 보라색 원을 벗어나세요",2);
            }
            else
            {
                Action=Pattern==BossPattern.RingFlames?BossAction.SpecialWindup:BossAction.RangedWindup;
                if(Pattern==BossPattern.RingFlames)area.Show(attackOrigin,Vector3.forward,1.5f,180,new Color(.2f,1,.7f,.25f));
                ShowCharges(0);CircusAudio.Play(CircusCue.MagicCharge,transform.position);CircusAudio.Play(CircusCue.Warning,transform.position,.35f);
            }
        }
        void OrbitCharges(int count,float height)
        {
            for(int i=0;i<count;i++)
            {
                charges[i].transform.position=transform.position+Vector3.up*height+Quaternion.Euler(0,Time.time*65+i*360f/count,0)*Vector3.forward*1.5f;
                charges[i].Show(.6f);
            }
        }
        void ShowCharges(float progress)
        {
            int count=Pattern==BossPattern.SingleFlame?1:Mathf.Clamp(Settings(Pattern).projectiles,1,10);
            for(int i=0;i<count;i++)
            {
                var offset=Pattern==BossPattern.RingFlames?Quaternion.Euler(0,RingAngle(i,count),0)*Vector3.forward*1.5f:
                    lockedDirection*1.2f+Vector3.Cross(Vector3.up,lockedDirection)*(i-(count-1)*.5f)*.6f;
                charges[i].transform.position=transform.position+Vector3.up*1.7f+offset;charges[i].Show(progress);
            }
        }
        // Leave a broad, persistent escape gap rather than closing a complete projectile wall.
        public float RingAngle(int index,int count)=>Settings(BossPattern.RingFlames).spread+(360-Settings(BossPattern.RingFlames).spread*2)*index/Mathf.Max(1,count-1);
        void ReleasePattern()
        {
            var c=Settings(Pattern);
            if(Pattern==BossPattern.Swing || Pattern==BossPattern.DoubleSwing)
            {
                meleeContact=false;Action=BossAction.MeleeAttack;started=Time.time;until=Time.time+Mathf.Max(.03f,meleeStrikeSeconds);return;
            }
            if(Pattern==BossPattern.Slam)
            {
                Action=BossAction.SpecialAttack;started=Time.time;until=Time.time+slamDropSeconds;return;
            }
            if(IsDash(Pattern))
            { Offset(Vector3.zero);enemy.Model.localScale=baseScale;Pose(18);Action=BossAction.SpecialAttack;dashPrevious=transform.position;if(rushTrail){rushTrail.Clear();rushTrail.emitting=true;}CircusAudio.PlayBossPattern(CircusBossSound.DashRelease,transform.position);CircusAudio.Play(CircusCue.BossDash,transform.position);return; }
            if(Pattern==BossPattern.GroundSeals) {
                HideWarnings();GroundAttacks++;Action=BossAction.GroundCast;nextRelease=Time.time;nextGround=Time.time+groundSeals.cooldown;return;
            }
            if(Pattern==BossPattern.HomingFlames)
            { Action=BossAction.RangedAttack;FireHoming();nextRelease=Time.time+homingInterval;return; }
            int count=Pattern==BossPattern.SingleFlame?1:Mathf.Clamp(c.projectiles,1,10);LastVolleySize=count;
            var aim=(session.Player.transform.position+Vector3.up-charges[count/2].transform.position).normalized;
            for(int i=0;i<count;i++)
            {
                Vector3 direction=Pattern==BossPattern.RingFlames?Quaternion.Euler(0,RingAngle(i,count),0)*Vector3.forward:
                    Quaternion.AngleAxis((i-(count-1)*.5f)*c.spread,Vector3.up)*aim;
                Fire(charges[i].transform.position,direction,c,false);
            }
            RangedAttacks++;HideWarnings();RangedKick();Action=BossAction.RangedAttack;
            CircusAudio.Play(CircusCue.BossShot,transform.position);
        }
        void Fire(Vector3 origin,Vector3 direction,BossPatternSettings c,bool homing)
        {
            var shot=Instantiate(enemy.Projectile,origin,Quaternion.identity);
            shot.Launch(direction,session,c.damage,c.speed);shot.SetBossOwner(enemy,projectileLifetime,homing?homingSeconds:0,homingDegreesPerSecond);
        }
        void FireHoming()
        {
            var origin=charges[shotIndex].transform.position;
            Fire(origin,(session.Player.transform.position+Vector3.up-origin).normalized,homingFlames,true);
            charges[shotIndex].Hide();RangedKick();shotIndex++;RangedAttacks++;LastVolleySize=Mathf.Clamp(homingFlames.projectiles,1,5);
            CircusAudio.Play(CircusCue.BossShot,origin,.7f);
        }
        void Move(Vector3 planar)
        {
            if(!controller.enabled)return;
            if(controller.isGrounded && verticalSpeed<0)verticalSpeed=-2;verticalSpeed-=20*Time.deltaTime;
            controller.Move((planar+Vector3.up*verticalSpeed)*Time.deltaTime);
        }
        void UpdateDash()
        {
            float step=Mathf.Min(dash.speed*Time.deltaTime,Mathf.Max(0,dash.range-dashTravel));dashPrevious=transform.position;
            var flags=controller.Move((lockedDirection*step+Vector3.down*.05f));dashTravel+=Vector3.Distance(dashPrevious,transform.position);
            var p=session.Player.transform.position;var a=dashPrevious;var b=transform.position;p.y=a.y=b.y=0;
            var segment=b-a;float t=segment.sqrMagnitude>.0001f?Mathf.Clamp01(Vector3.Dot(p-a,segment)/segment.sqrMagnitude):0;
            if(!dashHit && area.Contains(session.Player.transform.position) && Vector3.Distance(p,a+segment*t)<=dashWidth*.5f)
            { session.Player.TakeDamage(dash.damage);dashHit=true; }
            if(dashTravel>=dash.range-.05f || (flags&CollisionFlags.Sides)!=0 || step<.001f)
            {FinishDash();}
        }
        void FinishPattern()
        {
            HideWarnings();comboStep=0;
            ChooseCombo();
            Recover(Mathf.Max(.35f,Settings(Pattern).recovery));
        }
        void Offset(Vector3 worldOffset){enemy.Model.localPosition=modelRest+enemy.Model.parent.InverseTransformVector(worldOffset);}
        void CacheRecoveryPose(){recoveryPosition=enemy.Model.localPosition;recoveryRotation=enemy.Model.localRotation;}
        void RelaxPose(float t){
            enemy.Model.localPosition=Vector3.Lerp(recoveryPosition,modelRest,t);
            var target=Quaternion.Inverse(enemy.Model.parent.rotation)*Quaternion.LookRotation(lockedDirection.sqrMagnitude>.001f?lockedDirection:transform.forward);
            enemy.Model.localRotation=Quaternion.Slerp(recoveryRotation,target,t);enemy.Model.localScale=Vector3.Lerp(enemy.Model.localScale,baseScale,t);
        }
        void RangedKick(){Offset(lockedDirection*rangedKick);Pose(8);until=Time.time+rangedReleaseHold;}
        void Recover(float seconds) {CacheRecoveryPose();started=Time.time;Action=BossAction.Recovery;until=Time.time+seconds;Glow(.08f*(Phase-1));}
        public void BeginDeath()
        {
            if(Action==BossAction.Dead)return;
            CancelExpanded();CircusAudio.StopBossPatterns();enemy.Model.localPosition=modelRest;enemy.Model.localScale=baseScale;Action=BossAction.Dead;started=Time.time;until=Time.time+DeathSeconds;HideWarnings();ClearHazards();followup=null;centering=false;
            if(controller)controller.enabled=false;
            CircusBossVfx.Ring(session,transform.position,new Color(.2f,1,.65f),5,deathSeconds);
            CircusAudio.Play(CircusCue.BossDeath,transform.position);
        }
        void OnDestroy(){CancelExpanded();ClearHazards();}
    }
}
