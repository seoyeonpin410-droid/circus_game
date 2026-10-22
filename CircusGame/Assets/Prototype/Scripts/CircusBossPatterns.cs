using System;
using System.Collections.Generic;
using UnityEngine;
namespace CircusPrototype
{
    [Serializable]
    public sealed class CircusBossCombo
    {
        public BossPattern first,second;
        [Range(2,3)] public int minimumPhase=2;
        public CircusBossCombo(BossPattern first,BossPattern second,int phase=2){this.first=first;this.second=second;minimumPhase=phase;}
    }
    public sealed partial class CircusBoss
    {
        [Header("Arena patterns: damage / windup / recovery / cooldown / range / speed / count / angle")]
        [SerializeField] BossPatternSettings sequentialFlames=new BossPatternSettings(9,.7f,.8f,8,18,8,4);
        [SerializeField] BossPatternSettings multiDash=new BossPatternSettings(16,.7f,1.35f,13,7,10);
        [SerializeField] BossPatternSettings safeZones=new BossPatternSettings(18,1.8f,1.2f,20,14,0,2);
        [SerializeField] BossPatternSettings orbitVolley=new BossPatternSettings(9,.8f,1,12,18,7.5f,5);
        [SerializeField] BossPatternSettings finale=new BossPatternSettings(20,1.8f,1.9f,26,5,6,10,40);
        [Header("Fair arena / safety")]
        [SerializeField,Min(8)] float arenaRadius=14;
        [SerializeField,Range(2.5f,4)] float safeRadius=3;
        [SerializeField,Range(1,2)] int safeAreaCount=2;
        [SerializeField] Material safeFieldMaterial;
        [SerializeField,Min(.3f)] float safetyReactionSeconds=.55f;
        [SerializeField,Min(2)] float minimumProjectileDistance=2.75f;
        [Header("Sequenced attacks")]
        [SerializeField,Min(.25f)] float sequentialInterval=.34f,orbitInterval=.32f;
        [SerializeField,Range(1,3)] int phaseTwoDashCount=2,phaseThreeDashCount=3;
        [SerializeField,Min(.3f)] float multiDashGap=.4f;
        [SerializeField,HideInInspector] int hazardAreaCount=4; // Retained for older setup scripts; phase-specific counts are used now.
        [SerializeField,Min(.8f)] float hazardWarningSeconds=1.1f;
        [Header("Finale timing / response windows")]
        [SerializeField,Min(.5f)] float ultimateFloorGap=.6f,ultimateRingWarning=.6f,ultimateRingLife=1.05f,ultimateShockWarning=1.05f;
        [SerializeField,Min(2)] float postUltimateHeavyGrace=3.5f;
        [SerializeField] float phaseTwoAttackSpeed=1,phaseThreeAttackSpeed=1.03f;
        [Header("Only predefined, checked combinations")]
        [SerializeField] CircusBossCombo[] combos={
            new CircusBossCombo(BossPattern.SequentialFlames,BossPattern.Charge),
            new CircusBossCombo(BossPattern.GroundSeals,BossPattern.SingleFlame),
            new CircusBossCombo(BossPattern.FanFlame,BossPattern.Slam),
            new CircusBossCombo(BossPattern.GroundSeals,BossPattern.MultiDash,3)};
        readonly List<Vector3> groundPlan=new List<Vector3>();
        readonly Vector3[] hoverPositions=new Vector3[12];
        CircusSafeField safeField;
        TrailRenderer rushTrail;
        float windupDuration,expandedStarted,expandedWarning,heavyBlockedUntil,dashGapStarted;
        int expandedStage,expandedShots,dashPart,comboIndex;
        bool betweenDashes,ultimateQueued,jumpPatternPending;
        Vector3 safeA,safeB,ringGapDirection;
        public int ExpandedShots {get;private set;}
        public int DashSegments {get;private set;}
        public int UltimateExecutions {get;private set;}
        public int CombosStarted {get;private set;}
        public int UltimateStage=>Pattern==BossPattern.Finale?expandedStage:0;
        public bool UltimateActive=>Pattern==BossPattern.Finale && (Action==BossAction.SpecialWindup || Action==BossAction.SpecialAttack);
        public bool MusicEmphasis=>Pattern==BossPattern.Finale && Action==BossAction.SpecialWindup;
        public float ArenaRadius=>arenaRadius;
        public float HeavyBlockedUntil=>heavyBlockedUntil;
        public IReadOnlyList<Vector3> GroundPlan=>groundPlan;
        public float ReadyAt(BossPattern pattern)=>ready.TryGetValue(pattern,out var value)?value:0;
        bool CustomPattern(BossPattern p)=>p==BossPattern.SequentialFlames || p==BossPattern.OrbitVolley || p==BossPattern.SafeZones || p==BossPattern.Finale || p==BossPattern.GroundShockwave;
        bool IsDash(BossPattern p)=>p==BossPattern.Charge || p==BossPattern.MultiDash;
        bool Heavy(BossPattern p)=>p==BossPattern.Slam || p==BossPattern.MultiDash || p==BossPattern.GroundSeals || p==BossPattern.RingFlames || p==BossPattern.SafeZones || p==BossPattern.Finale || p==BossPattern.GroundShockwave;
        [Header("Jump shockwave and stronger staged ground seals")]
        [SerializeField] BossPatternSettings groundShockwave=new BossPatternSettings(14,1,1.3f,16,16.2f,8,1);
        [SerializeField,Range(6,8)] int phaseTwoGroundCount=7;
        [SerializeField,Range(8,10)] int phaseThreeGroundCount=9;
        [SerializeField,Range(2,3)] int groundBatchSize=3;
        [SerializeField,Min(.5f)] float groundBatchGap=.55f;
        [SerializeField,Min(.8f)] float secondShockwaveGap=.95f;
        [SerializeField,Range(.3f,.7f)] float shockwaveWidth=.55f,shockwaveJumpClearance=.35f;
        [SerializeField] bool phaseThreeDoubleShockwave=true;
        float WindupSeconds(BossPatternSettings c)=>Mathf.Max(.4f,c.windup/Mathf.Max(.5f,Phase==3?phaseThreeAttackSpeed:Phase==2?phaseTwoAttackSpeed:1));
        bool ExpandedAllowed(BossPattern p)
        {
            if(p==BossPattern.GroundShockwave)return Phase>=2;
            if(p==BossPattern.SafeZones || p==BossPattern.Finale)return Phase==3;
            if(p==BossPattern.MultiDash)return Phase>=2 && ToPlayer.magnitude>=3.5f && ToPlayer.magnitude<=multiDash.range+2;
            return Phase>=2 && ToPlayer.magnitude>=4 && ToPlayer.magnitude<=Settings(p).range;
        }
        void ResetExpanded()
        {
            groundPlan.Clear();betweenDashes=false;expandedStage=expandedShots=dashPart=0;
            ultimateQueued=false;jumpPatternPending=false;heavyBlockedUntil=0;
            var go=new GameObject("Short boss rush wake");go.transform.SetParent(transform,false);go.transform.localPosition=Vector3.up*.4f;
            rushTrail=go.AddComponent<TrailRenderer>();rushTrail.sharedMaterial=CircusCombatFx.Glow;rushTrail.time=.12f; rushTrail.minVertexDistance=.12f;
            rushTrail.widthCurve=AnimationCurve.Linear(0,.35f,1,0);rushTrail.startColor=new Color(1,.55f,.2f,.6f);rushTrail.endColor=Color.clear;
            rushTrail.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;rushTrail.emitting=false;
        }
        void CancelExpanded()
        {
            betweenDashes=false;expandedStage=expandedShots=dashPart=0;groundPlan.Clear();
            if(rushTrail){rushTrail.emitting=false;rushTrail.Clear();}
            if(safeField)safeField.Cancel();safeField=null;
        }
        Vector3 ClampArena(Vector3 point,float margin)
        {
            var d=point-arenaCenter;d.y=0;d=Vector3.ClampMagnitude(d,Mathf.Max(1,arenaRadius-margin));
            return new Vector3(arenaCenter.x+d.x,transform.position.y,arenaCenter.z+d.z);
        }
        static float SegmentDistance(Vector3 point,Vector3 start,Vector3 end)
        {
            point.y=start.y=end.y=0;var d=end-start;
            float t=d.sqrMagnitude>.001f?Mathf.Clamp01(Vector3.Dot(point-start,d)/d.sqrMagnitude):0;
            return Vector3.Distance(point,start+d*t);
        }
        void PlanGround()
        {
            groundPlan.Clear();float radius=groundSeals.range;
            var player=ClampArena(session.Player.transform.position,radius+.5f);
            var outward=player-arenaCenter;outward.y=0;
            var escape=outward.magnitude>arenaRadius*.5f?-outward.normalized:Vector3.Cross(Vector3.up,lockedDirection).normalized;
            if(escape.sqrMagnitude<.01f)escape=Vector3.right;
            var safe=ClampArena(player+escape*5,radius+.5f);groundPlan.Add(player);
            var options=new List<Vector3>();
            for(int i=0;i<5;i++)options.Add(player+Quaternion.AngleAxis(90+i*45,Vector3.up)*escape*5);
            for(int i=0;i<16;i++)options.Add(arenaCenter+Quaternion.Euler(0,i*22.5f,0)*Vector3.forward*(arenaRadius*.6f));
            for(int ring=0;ring<3;ring++)for(int i=0;i<28;i++)options.Add(arenaCenter+Quaternion.Euler(0,i*360f/28+ring*12,0)*Vector3.forward*(4+ring*3.5f));
            foreach(var option in options){
                if(groundPlan.Count>=Mathf.Clamp(Phase==3?phaseThreeGroundCount:phaseTwoGroundCount,6,10))break;
                var point=ClampArena(option,radius+.5f);
                if(SegmentDistance(point,player,safe)<radius+1)continue;
                bool overlaps=false;foreach(var used in groundPlan)if(Vector3.Distance(point,used)<radius*2+.4f)overlaps=true;
                if(!overlaps)groundPlan.Add(point);
            }
        }
        bool CanOverlapGround(BossPattern next)
        {
            if(next!=BossPattern.SingleFlame || Pattern!=BossPattern.GroundSeals || followup!=next || ToPlayer.magnitude<7)return false;
            foreach(var p in FindObjectsByType<CircusProjectile>(FindObjectsSortMode.None))if(p.Owner==enemy)return false;
            foreach(var f in FindObjectsByType<CircusSafeField>(FindObjectsSortMode.None))if(f.Owner==enemy)return false;
            var hazards=FindObjectsByType<CircusGroundFlame>(FindObjectsSortMode.None);var from=session.Player.transform.position;
            foreach(var h in hazards)if(h.Owner==enemy && Vector3.Distance(h.Center,from)<h.Area.Radius+.6f)return false;
            for(int direction=0;direction<12;direction++){
                var target=from+Quaternion.Euler(0,direction*30,0)*Vector3.forward*3.5f;
                if(Vector3.Distance(ClampArena(target,.7f),target)>.1f)continue;
                bool blocked=false;foreach(var h in hazards)if(h.Owner==enemy && SegmentDistance(h.Center,from,target)<h.Area.Radius+.6f)blocked=true;
                if(!blocked)return true;
            }
            return false;
        }
        void ChooseCombo()
        {
            followup=null;if(Phase<2 || Pattern==BossPattern.Finale || Pattern==BossPattern.SafeZones || combos==null)return;
            var eligible=new List<CircusBossCombo>();foreach(var combo in combos)if(combo!=null && combo.first==Pattern && Phase>=combo.minimumPhase)eligible.Add(combo);
            if(eligible.Count>0)followup=eligible[(comboIndex++)%eligible.Count].second;
        }
        void FinishDash()
        {
            area.Hide();DashSegments++;if(rushTrail){rushTrail.emitting=false;rushTrail.Clear();}
            dashPart++;
            int count=Phase==3?phaseThreeDashCount:phaseTwoDashCount;
            if(Pattern==BossPattern.MultiDash && dashPart<Mathf.Clamp(count,1,3)){
                CacheRecoveryPose();betweenDashes=true;dashGapStarted=Time.time;until=Time.time+multiDashGap;Action=BossAction.Idle;return;
            }
            FinishPattern();
        }
        void BeginCustom()
        {
            if(Pattern==BossPattern.Finale || Pattern==BossPattern.GroundShockwave){
                var d=arenaCenter-transform.position;d.y=0;
                if(d.magnitude>centerStopDistance){centering=true;Action=BossAction.Chase;until=Time.time+8;return;}
            }
            BeginCustomWindup();
        }
        void PlaceSafeAreas()
        {
            var player=session.Player.transform.position;var d=player-transform.position;d.y=0;
            var side=Vector3.Cross(Vector3.up,d.sqrMagnitude>.01f?d.normalized:Vector3.forward);
            safeA=ClampArena(player+side*5.5f,safeRadius+.6f);
            var relative=safeA-arenaCenter;relative.y=0;
            if(relative.magnitude<safeRadius+1.3f)safeA=ClampArena(arenaCenter+(relative.sqrMagnitude>.01f?relative.normalized:side)*(safeRadius+1.3f),safeRadius+.6f);
            safeB=safeAreaCount==1?safeA:ClampArena(arenaCenter-(safeA-arenaCenter).normalized*7.5f,safeRadius+.6f);
            float distance=Mathf.Max(0,Mathf.Min(Vector3.Distance(player,safeA),Vector3.Distance(player,safeB))-safeRadius+.5f);
            expandedWarning=Mathf.Max(Settings(Pattern).windup,1.5f,distance*1.25f/Mathf.Max(.5f,session.Player.MoveSpeed)+safetyReactionSeconds);
            ringGapDirection=(safeA-transform.position);ringGapDirection.y=0;ringGapDirection.Normalize();
            safeField=CircusSafeField.Create(session,enemy,safeFieldMaterial,arenaCenter+Vector3.up*transform.position.y,arenaRadius,safeA,safeB,safeRadius,expandedWarning,.45f,Settings(Pattern).damage*(Pattern==BossPattern.Finale?.7f:1));
            FindFirstObjectByType<CircusHud>()?.Announce(Pattern==BossPattern.Finale?"최종 공연 · 초록 원 안으로 이동하세요":"초록 원이 안전지대입니다",expandedWarning);
        }
        void BeginCustomWindup()
        {
            HideWarnings();expandedStarted=Time.time;expandedStage=0;expandedShots=0;started=Time.time;
            lockedDirection=ToPlayer.sqrMagnitude>.01f?ToPlayer.normalized:transform.forward;attackOrigin=transform.position;
            expandedWarning=WindupSeconds(Settings(Pattern));Action=BossAction.SpecialWindup;
            if(Pattern==BossPattern.GroundShockwave){FindFirstObjectByType<CircusHud>()?.Announce("지면 충격파 · SPACE로 뛰어넘으세요",expandedWarning+1.5f);}
            else if(Pattern==BossPattern.SafeZones || Pattern==BossPattern.Finale)PlaceSafeAreas();else ShowCustomCharges();
            until=Time.time+expandedWarning;
            CircusAudio.PlayBossPattern(Pattern==BossPattern.Finale?CircusBossSound.UltimateCharge:Pattern==BossPattern.OrbitVolley?CircusBossSound.OrbitPrepare:Pattern==BossPattern.SafeZones?CircusBossSound.GroundMark:CircusBossSound.SequencePrepare,transform.position);
        }
        void ShowCustomCharges()
        {
            if(Pattern==BossPattern.SequentialFlames){charges[0].transform.position=transform.position+Vector3.up*1.9f+lockedDirection*.9f;charges[0].Show(.7f);return;}
            int count=Mathf.Clamp(Settings(Pattern).projectiles,1,12);
            for(int i=expandedShots;i<count;i++){
                var direction=Quaternion.Euler(0,-Settings(Pattern).spread*.5f+i*Settings(Pattern).spread/Mathf.Max(1,count-1),0)*lockedDirection;
                var position=transform.position+Vector3.up*(Pattern==BossPattern.OrbitVolley?2.5f:1.9f)+direction*(Pattern==BossPattern.OrbitVolley?1.8f:.9f);
                if(Pattern==BossPattern.OrbitVolley && Action==BossAction.SpecialAttack)position=hoverPositions[i]+Vector3.up*Mathf.Sin(Time.time*4+i)*.07f;
                charges[i].transform.position=position;charges[i].Show(Mathf.Clamp01((Time.time-expandedStarted)/Mathf.Max(.1f,expandedWarning)));if(Action!=BossAction.SpecialAttack)hoverPositions[i]=position;
            }
        }
        void FireStraight(Vector3 origin,BossPatternSettings profile)
        {
            var target=session.Player.transform.position+Vector3.up;
            if(Vector3.Distance(origin,target)>=minimumProjectileDistance)Fire(origin,(target-origin).normalized,profile,false);
            else CircusCombatFx.Burst(origin,new Color(.2f,1,.7f),5,.12f,.5f);
            ExpandedShots++;RangedAttacks++;RangedKick();
        }
        void TickCustom()
        {
            var config=Settings(Pattern);
            if(Pattern==BossPattern.GroundShockwave){TickGroundShockwave();return;}
            if(Action==BossAction.SpecialWindup){
                float t=Mathf.Clamp01((Time.time-expandedStarted)/Mathf.Max(.1f,expandedWarning));
                if(Pattern==BossPattern.SafeZones || Pattern==BossPattern.Finale){Offset(Vector3.up*.12f*t);Pose(-10*t);enemy.Model.localScale=baseScale*(1+.04f*t);Glow(.12f+t*.3f);}
                else {Offset(-lockedDirection*rangedPullback*t);Pose(-12*t);ShowCustomCharges();Glow(.1f+t*.2f);}
                if(Time.time<until)return;
                enemy.Model.localScale=baseScale;Offset(Vector3.zero);
                if(Pattern==BossPattern.SafeZones || Pattern==BossPattern.Finale){
                    if(safeField)safeField.Detonate();Pose(14);expandedStage=1;nextRelease=Time.time+(Pattern==BossPattern.Finale?ultimateFloorGap:.45f);
                }else {nextRelease=Time.time;Action=BossAction.SpecialAttack;}
                Action=BossAction.SpecialAttack;return;
            }
            if(Action!=BossAction.SpecialAttack)return;
            if(Pattern==BossPattern.SafeZones){if(Time.time>=nextRelease)FinishPattern();return;}
            if(Pattern==BossPattern.Finale){TickFinale();return;}
            ShowCustomCharges();
            float release=1-Mathf.SmoothStep(0,1,(Time.time-(until-rangedReleaseHold))/Mathf.Max(.05f,rangedReleaseHold));Offset(lockedDirection*rangedKick*release);Pose(8*release);
            int count=Mathf.Clamp(config.projectiles,1,12);
            if(expandedShots<count && Time.time>=nextRelease){
                int index=Pattern==BossPattern.SequentialFlames?0:expandedShots;
                var origin=charges[index].transform.position;charges[index].Hide();FireStraight(origin,config);expandedShots++;
                nextRelease=Time.time+(Pattern==BossPattern.OrbitVolley?orbitInterval:sequentialInterval);
                CircusAudio.PlayBossPattern(Pattern==BossPattern.OrbitVolley?CircusBossSound.OrbitRelease:CircusBossSound.SequenceRelease,origin);
            }
            if(expandedShots>=count && Time.time>=nextRelease){HideWarnings();FinishPattern();}
        }
        void TickGroundShockwave(){
            var c=groundShockwave;
            if(Action==BossAction.SpecialWindup){float t=Mathf.Clamp01((Time.time-expandedStarted)/expandedWarning);Offset(Vector3.up*(slamLift*1.3f)*t);Pose(-26*t);area.ShowSlam(attackOrigin,.8f,t);Glow(.2f+t*.5f);if(Time.time>=until){Action=BossAction.SpecialAttack;expandedStage=0;started=Time.time;}return;}
            if(Action!=BossAction.SpecialAttack)return;
            if(expandedStage==0){float t=Mathf.Clamp01((Time.time-started)/Mathf.Max(.05f,slamDropSeconds));Offset(Vector3.up*(slamLift*1.3f)*(1-t*t*t));Pose(Mathf.Lerp(-26,18,t));if(t<1)return;Offset(Vector3.zero);area.Hide();CircusCombatFx.Slam(transform.position,2);CircusAudio.PlayAccent(true,transform.position);FindFirstObjectByType<CircusCamera>()?.Shake(.03f,.12f);LaunchGroundWave();expandedStage=1;nextRelease=Time.time+secondShockwaveGap;if(Phase<3 || !phaseThreeDoubleShockwave)FinishPattern();return;}
            if(Time.time>=nextRelease){LaunchGroundWave();FinishPattern();}
        }
        void LaunchGroundWave(){CircusGroundShockwave.Create(session,enemy,transform.position,groundShockwave.speed,groundShockwave.range,groundShockwave.damage,shockwaveWidth,shockwaveJumpClearance);}
        void TickFinale()
        {
            if(expandedStage==1 && Time.time>=nextRelease){expandedStage=2;started=Time.time;nextRelease=Time.time+ultimateRingWarning;FindFirstObjectByType<CircusHud>()?.Announce("투사체 사이의 틈을 찾으세요",1.2f);}
            if(expandedStage==2){
                float charge=Mathf.Clamp01((Time.time-started)/ultimateRingWarning);Pose(-8*charge);Offset(-lockedDirection*.1f*charge);
                int count=Mathf.Clamp(finale.projectiles,8,12);for(int i=0;i<count;i++){charges[i].transform.position=transform.position+Vector3.up*1.7f+Quaternion.Euler(0,i*360f/count,0)*Vector3.forward*1.3f;charges[i].Show(Mathf.Clamp01((Time.time-started)/ultimateRingWarning));}
                if(Time.time>=nextRelease){
                    for(int i=0;i<count;i++){
                        float angle=finale.spread+(360-2*finale.spread)*i/Mathf.Max(1,count-1);
                        var direction=Quaternion.AngleAxis(angle,Vector3.up)*ringGapDirection;
                        var shot=Instantiate(enemy.Projectile,transform.position+Vector3.up*1.5f+direction*1.3f,Quaternion.identity);
                        shot.Launch(direction,session,finale.damage*.4f,finale.speed);shot.SetBossOwner(enemy,ultimateRingLife,0,0);
                    }
                    LastVolleySize=count;RangedAttacks++;HideWarnings();expandedStage=3;nextRelease=Time.time+ultimateRingLife+.05f;
                    CircusAudio.PlayBossPattern(CircusBossSound.UltimateBurst,transform.position);
                }
                return;
            }
            if(expandedStage==3 && Time.time>=nextRelease){
                foreach(var shot in FindObjectsByType<CircusProjectile>(FindObjectsSortMode.None))if(shot.Owner==enemy)shot.Cancel();
                expandedStage=4;started=Time.time;until=Time.time+ultimateShockWarning;FindFirstObjectByType<CircusHud>()?.Announce("마지막 충격파 · 보스에게서 떨어지세요",ultimateShockWarning);
            }
            if(expandedStage==4){
                float t=Mathf.Clamp01((Time.time-started)/ultimateShockWarning);area.ShowSlam(transform.position,finale.range,t);Offset(Vector3.up*.35f*t);Pose(-14*t);
                if(Time.time>=until){expandedStage=5;started=Time.time;}return;
            }
            if(expandedStage==5){
                float t=Mathf.Clamp01((Time.time-started)/Mathf.Max(.05f,slamDropSeconds));Offset(Vector3.up*.35f*(1-t*t*t));Pose(Mathf.Lerp(-14,16,t));
                if(t<1)return;
                if(area.Contains(session.Player.transform.position))session.Player.TakeDamage(finale.damage);
                Offset(Vector3.zero);area.Hide();CircusCombatFx.Slam(transform.position,finale.range);CircusBossVfx.Ring(session,transform.position,new Color(1,.5f,.2f),finale.range,.45f);
                FindFirstObjectByType<CircusCamera>()?.Shake(.04f,.17f);CircusAudio.PlayBossPattern(CircusBossSound.UltimateImpact,transform.position);
                UltimateExecutions++;heavyBlockedUntil=Time.time+postUltimateHeavyGrace;followup=null;FinishPattern();
            }
        }
    }
}
