using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CircusPrototype;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;
public static class CircusArenaVerification
{
    static readonly BindingFlags F=BindingFlags.NonPublic|BindingFlags.Instance;
    static readonly List<string> report=new List<string>();
    static CircusSession session;static CircusEnemy owner;static CircusBoss boss;static bool running;
    static void Set(object o,string field,object value){o.GetType().GetField(field,F).SetValue(o,value);}
    static T Get<T>(object o,string field)=>(T)o.GetType().GetField(field,F).GetValue(o);
    static void Check(bool ok,string text){report.Add((ok?"PASS ":"FAIL ")+text);File.WriteAllLines("Temp/arena-verification.txt",report);}
    static void Move(Transform t,Vector3 p){var c=t.GetComponent<CharacterController>();if(c)c.enabled=false;t.position=p;if(c)c.enabled=true;Physics.SyncTransforms();}
    static IEnumerator Wait(Func<bool> condition,float seconds=12){float end=Time.realtimeSinceStartup+seconds;while(!condition() && Time.realtimeSinceStartup<end)yield return null;}
    static CircusProjectile[] Shots()=>Object.FindObjectsByType<CircusProjectile>(FindObjectsSortMode.None).Where(p=>p.Owner==owner).ToArray();
    static CircusSafeField Field()=>Object.FindObjectsByType<CircusSafeField>(FindObjectsSortMode.None).FirstOrDefault(p=>p.Owner==owner);
    static IEnumerator Start(BossPattern pattern,float distance=8)
    {
        boss.ClearHazards();boss.SendMessage("CancelExpanded");boss.SendMessage("HideWarnings");yield return null;
        Set(boss,"centering",false);Set(boss,"betweenDashes",false);Set(boss,"followup",null);Set(boss,"lastPattern",null);Set(boss,"heavyBlockedUntil",0f);Set(boss,"ultimateQueued",false);Set(boss,"nextGround",pattern==BossPattern.GroundSeals?0f:float.MaxValue);
        Get<Dictionary<BossPattern,float>>(boss,"ready").Clear();Set(boss,"until",0f);Set(boss,"<Action>k__BackingField",BossAction.PatternSelect);
        owner.Model.localPosition=Get<Vector3>(boss,"modelRest");owner.Model.localScale=Get<Vector3>(boss,"baseScale");
        Move(session.Player.transform,owner.transform.position+Vector3.back*distance);session.Player.RestoreHealth(100);Set(session.Player,"lastDamage",-100f);
        bool started=boss.TryStartPattern(pattern);Check(started,"Eligible pattern starts: "+pattern);
    }
    static IEnumerator Walk(Vector3 goal,Func<bool> stop,float timeout=4)
    {
        float end=Time.realtimeSinceStartup+timeout;var cc=session.Player.GetComponent<CharacterController>();
        while(!stop() && Time.realtimeSinceStartup<end){var d=goal-session.Player.transform.position;d.y=0;if(d.magnitude>.1f)cc.Move(d.normalized*Mathf.Min(d.magnitude,session.Player.MoveSpeed*Time.deltaTime));yield return null;}
    }
    public static string Run(){if(running)return "Already running";running=true;report.Clear();File.WriteAllText("Temp/arena-verification.txt","RUNNING");session=Object.FindFirstObjectByType<CircusSession>();session.StartCoroutine(Verify());return "Arena fairness and pattern verification started";}
    static IEnumerator Verify()
    {
        Application.runInBackground=true;Time.timeScale=1;session.BeginShow();yield return new WaitForSeconds(3.3f);
        session.StopCoroutine(Get<Coroutine>(session,"roundRoutine"));Set(session,"round",2);Set(session,"<CountdownText>k__BackingField","");
        Move(session.Player.transform,new Vector3(0,.06f,-8));
        owner=Object.Instantiate(AssetDatabase.LoadAssetAtPath<CircusEnemy>("Assets/Prototype/Prefabs/DokkaebiBoss.prefab"),new Vector3(0,.06f,0),Quaternion.identity);owner.Bind(session);
        Get<List<CircusEnemy>>(session,"enemies").Add(owner);Set(session,"<Boss>k__BackingField",owner);boss=owner.GetComponent<CircusBoss>();Set(boss,"nextGround",float.MaxValue);
        yield return Wait(()=>!boss.IsInvulnerable);
        Check(owner.MaxHealth==1000 && session.Player.BossDamageScale==.4f,"Moderate HP and boss-only player damage tuning are serialized");
        Check(boss.CanUse(BossPattern.Charge) && boss.CanUse(BossPattern.FanFlame) && boss.CanUse(BossPattern.SingleFlame),"Phase 1 teaches dash, fan and ordinary shot");
        Check(!boss.CanUse(BossPattern.SequentialFlames) && !boss.CanUse(BossPattern.GroundSeals) && !boss.CanUse(BossPattern.MultiDash) && !boss.CanUse(BossPattern.OrbitVolley) && !boss.CanUse(BossPattern.SafeZones) && !boss.CanUse(BossPattern.Finale),"Phase 1 locks arena extensions");
        owner.TakeDamage(owner.MaxHealth*.31f);yield return null;yield return null;float hp=owner.Health;owner.TakeDamage(25);
        Check(boss.Phase==2 && boss.PhaseTransitions==1 && owner.Health==hp && boss.IsInvulnerable,"70 percent transition is protected and occurs once");
        session.Pause();var action=boss.Action;yield return new WaitForSecondsRealtime(.15f);Check(boss.Action==action,"Pause freezes phase transition");session.Resume();yield return Wait(()=>!boss.IsInvulnerable);
        yield return Start(BossPattern.SequentialFlames);int count=boss.ExpandedShots;float previous=Time.time;CircusProjectile first=null;
        yield return new WaitForSeconds(.3f);Check(Shots().Length==0 && boss.Charging,"Sequential volley warns before firing");
        bool spaced=true,straight=true,reaimed=true;
        for(int i=0;i<4;i++){
            yield return Wait(()=>boss.ExpandedShots>count+i);
            var newest=Shots().OrderBy(p=>Vector3.Distance(p.transform.position,owner.transform.position)).FirstOrDefault();
            if(i>0)spaced&=Time.time-previous>=.29f;previous=Time.time;
            if(newest){reaimed&=Vector3.Angle(newest.Direction,(session.Player.transform.position+Vector3.up-newest.transform.position).normalized)<3;if(i==0)first=newest;straight&=!newest.Homing;}
            var old=first?first.Direction:Vector3.zero;Move(session.Player.transform,owner.transform.position+new Vector3(i%2==0?4:-4,.0f,-8));yield return null;
            if(first)straight&=Vector3.Angle(old,first.Direction)<.01f;
            Check(!boss.TryStartPattern(BossPattern.Charge),"Sequential cast cannot be interrupted by another pattern: shot "+(i+1));
        }
        Check(spaced && reaimed && straight && boss.ExpandedShots==count+4,"Four shots are spaced, re-aim before launch, and never track in flight");
        yield return Wait(()=>boss.Action==BossAction.Recovery);
        yield return Start(BossPattern.OrbitVolley);count=boss.ExpandedShots;yield return new WaitForSeconds(.35f);
        var charges=owner.GetComponentsInChildren<CircusCharge>(true);Check(charges.Count(c=>c.gameObject.activeSelf)==5 && Shots().Length==0,"Hover pattern displays five staged flames before launch");
        session.Pause();float age=Time.time;var positions=charges.Select(c=>c.transform.position).ToArray();yield return new WaitForSecondsRealtime(.15f);
        Check(Time.time==age && charges.Select((c,i)=>Vector3.Distance(c.transform.position,positions[i])<.001f).All(v=>v),"Pause freezes staged flame positions and release timer");session.Resume();
        yield return Wait(()=>boss.ExpandedShots>=count+5);Check(boss.ExpandedShots==count+5 && Shots().All(p=>!p.Homing),"Five hovering flames launch one by one as straight projectiles");
        yield return Start(BossPattern.MultiDash,7);count=boss.DashSegments;bool locked=true,repeatedWarnings=true;
        for(int i=0;i<2;i++){
            yield return Wait(()=>boss.Action==BossAction.SpecialWindup);repeatedWarnings&=boss.AttackArea.Visible;
            var direction=boss.AttackArea.transform.forward;var from=owner.transform.position;var side=Vector3.Cross(Vector3.up,direction);
            Move(session.Player.transform,from+direction*4+side*4);yield return Wait(()=>boss.Action==BossAction.SpecialAttack);yield return new WaitForSeconds(.09f);
            var movement=owner.transform.position-from;movement.y=0;locked&=movement.sqrMagnitude>.01f && Vector3.Angle(movement,direction)<.5f;
            Move(session.Player.transform,from+direction*4-side*4);yield return Wait(()=>boss.DashSegments>=count+i+1);
            if(i==0)Check(!boss.TryStartPattern(BossPattern.SingleFlame),"Multi-dash inter-leg pause cannot start an unrelated attack");
        }
        Check(locked && repeatedWarnings && boss.DashSegments==count+2 && boss.Action==BossAction.Recovery,"Phase 2 has two straight dashes, fresh warnings and final recovery");
        yield return new WaitForSeconds(.9f);Check(boss.Action==BossAction.Recovery,"Multi-dash offers more than one second of counterattack time");
        Move(owner.transform,new Vector3(0,.06f,0));yield return Start(BossPattern.GroundSeals,10);var planned=boss.GroundPlan.ToArray();
        Check(planned.Length>=3 && planned.Length<=5 && planned.SelectMany((p,i)=>planned.Skip(i+1).Select(q=>Vector3.Distance(p,q))).Min()>boss.Settings(BossPattern.GroundSeals).range*2,"Ground plan places 3-5 separate circles instead of covering the arena");
        Move(session.Player.transform,new Vector3(3,.06f,-7));hp=session.Player.Health;
        yield return Wait(()=>Object.FindObjectsByType<CircusGroundFlame>(FindObjectsSortMode.None).Length>0);yield return new WaitForSeconds(.35f);
        Check(session.Player.Health==hp && Object.FindObjectsByType<CircusGroundFlame>(FindObjectsSortMode.None).All(h=>h.Phase==GroundFlamePhase.Warning),"Marked floor has a real warning interval without damage");
        yield return new WaitForSeconds(.8f);Check(Object.FindObjectsByType<CircusGroundFlame>(FindObjectsSortMode.None).All(h=>planned.Any(p=>Vector3.Distance(p,h.Center)<.01f)),"Floor positions remain fixed after player movement");
        Check(!boss.TryStartPattern(BossPattern.MultiDash),"Live floor hazards cannot overlap a multi-dash");
        int combo=boss.CombosStarted;yield return Wait(()=>boss.CombosStarted>combo,10);
        Check(boss.CombosStarted>combo && boss.Pattern==BossPattern.SingleFlame,"Only the predefined safe floor-to-single-shot combo is selected");
        owner.TakeDamage(owner.Health-owner.MaxHealth*.34f);yield return null;yield return null;yield return Wait(()=>boss.Phase==3 && !boss.IsInvulnerable);
        Check(boss.PhaseTransitions==2,"35 percent produces one further phase transition");
        Move(owner.transform,new Vector3(0,.06f,0));yield return Start(BossPattern.SafeZones);var field=Field();
        Check(field && field.WarningSeconds>=1.5f && field.SafeRadius>=2.5f && !field.IsSafe(session.Player.transform.position),"Arena warning exposes two broad, reachable green safe areas");
        Check(field.IsSafe(field.SafeA) && field.IsSafe(field.SafeB) && field.ContainsDanger(Vector3.zero) && !field.ContainsDanger(field.SafeA),"Safe cutouts and damage predicate agree");
        float distance=Mathf.Min(Vector3.Distance(field.SafeA,session.Player.transform.position),Vector3.Distance(field.SafeB,session.Player.transform.position))-field.SafeRadius;
        Check(distance/session.Player.MoveSpeed+.3f<field.WarningSeconds,"At least one safe area is reachable at actual player speed");
        hp=session.Player.Health;yield return new WaitForSeconds(.35f);Check(session.Player.Health==hp && !field.Fired,"Safe-zone field cannot damage immediately");
        yield return Walk(field.SafeA,()=>field.Fired);Check(field.IsSafe(session.Player.transform.position) && session.Player.Health==hp,"Actual CharacterController movement reaches safety before detonation");
        yield return Start(BossPattern.SafeZones);field=Field();hp=session.Player.Health;yield return Wait(()=>field.Fired);
        Check(session.Player.Health==hp-boss.Settings(BossPattern.SafeZones).damage,"Remaining in red floor receives one nonlethal hit");
        yield return new WaitForSeconds(.3f);Check(session.Player.Health==hp-boss.Settings(BossPattern.SafeZones).damage,"Safe-zone blast does not secretly tick repeated damage");
        yield return Start(BossPattern.MultiDash,7);count=boss.DashSegments;yield return Wait(()=>boss.DashSegments>=count+3,10);
        Check(boss.DashSegments==count+3 && boss.Action==BossAction.Recovery,"Phase 3 caps multi-dash at three segments");
        Move(owner.transform,new Vector3(0,.06f,0));yield return Start(BossPattern.Finale);yield return Wait(()=>boss.Action==BossAction.SpecialWindup);field=Field();
        float start=Time.time;hp=session.Player.Health;int ult=boss.UltimateExecutions;
        yield return new WaitForSeconds(.5f);var music=session.GetComponent<CircusMusic>();
        Check(boss.MusicEmphasis && Get<AudioLowPassFilter>(music,"bossFilter").cutoffFrequency<18000,"Finale preparation gently ducks and filters existing boss music");
        yield return Walk(field.SafeA,()=>field.Fired);Check(session.Player.Health==hp,"Finale's first floor stage can be escaped on foot");
        yield return Wait(()=>boss.UltimateStage==2);Check(Shots().Length==0 && boss.Charging,"Finale ring stage has a separate visible reaction window");
        yield return Wait(()=>boss.UltimateStage==3);var ring=Shots();var gap=Get<Vector3>(boss,"ringGapDirection");
        Check(ring.Length==10 && ring.All(p=>!p.Homing && Vector3.Angle(p.Direction,gap)>=boss.Settings(BossPattern.Finale).spread-.2f),"Finale ring leaves a broad marked escape gap and uses straight shots");
        Move(session.Player.transform,owner.transform.position+gap*7);yield return Wait(()=>boss.UltimateStage==4);
        Check(Shots().Length==0 && boss.AttackArea.Visible && session.Player.Health==hp,"Ring expires before final shockwave warning, preventing an unavoidable overlap");
        yield return Wait(()=>boss.UltimateExecutions>ult);
        Check(Time.time-start>=4 && Time.time-start<=6 && session.Player.Health==hp,"Three-stage finale lasts 4-6 seconds and can be fully avoided");
        Check(boss.Action==BossAction.Recovery && boss.ReadyAt(BossPattern.Finale)-Time.time>=15 && boss.HeavyBlockedUntil>Time.time,"Finale enforces long cooldown, recovery and a heavy-attack lockout");
        yield return new WaitForSeconds(1.55f);Check(boss.Action==BossAction.Recovery,"Finale leaves a large 1.9-second counterattack window");
        yield return Start(BossPattern.Finale);yield return new WaitForSeconds(.3f);owner.TakeDamage(10000);yield return null;yield return null;
        Check(boss.Action==BossAction.Dead && !Field() && Shots().Length==0 && !boss.Charging,"Death cancels an active finale, field, staged flames and projectiles");
        int events=boss.ExpandedShots+boss.UltimateExecutions;yield return new WaitForSeconds(.25f);Check(events==boss.ExpandedShots+boss.UltimateExecutions,"No delayed cast survives the death state");
        session.ReturnToMenu();yield return null;yield return null;
        Check(!Object.FindFirstObjectByType<CircusSafeField>() && !Object.FindFirstObjectByType<CircusGroundFlame>() && !Object.FindFirstObjectByType<CircusProjectile>() && Time.timeScale==1,"Menu/retry cleanup leaves no damaging objects or frozen time scale");
        report.Add(report.Any(s=>s.StartsWith("FAIL"))?"RESULT: FAILED":"RESULT: ALL PASSED");File.WriteAllLines("Temp/arena-verification.txt",report);running=false;
    }
    public static string Record(){session=Object.FindFirstObjectByType<CircusSession>();session.StartCoroutine(Capture());return "Recording real arena finale";}
    static IEnumerator Capture()
    {
        session.BeginShow();yield return new WaitForSeconds(3.3f);session.StopCoroutine(Get<Coroutine>(session,"roundRoutine"));Set(session,"round",2);Set(session,"<CountdownText>k__BackingField","");
        Move(session.Player.transform,new Vector3(0,.06f,-8));
        owner=Object.Instantiate(AssetDatabase.LoadAssetAtPath<CircusEnemy>("Assets/Prototype/Prefabs/DokkaebiBoss.prefab"),new Vector3(0,.06f,0),Quaternion.identity);owner.Bind(session);
        Get<List<CircusEnemy>>(session,"enemies").Add(owner);Set(session,"<Boss>k__BackingField",owner);boss=owner.GetComponent<CircusBoss>();Set(boss,"nextGround",float.MaxValue);
        yield return Wait(()=>!boss.IsInvulnerable);owner.TakeDamage(660);yield return null;yield return Wait(()=>boss.Phase==3 && !boss.IsInvulnerable);
        yield return Start(BossPattern.Finale);var field=Field();
        var rig=Object.FindFirstObjectByType<CircusCamera>();rig.SetFirstPerson(false);rig.enabled=false;var camera=Camera.main;camera.transform.position=new Vector3(0,13,-19);camera.transform.LookAt(new Vector3(0,.6f,0));camera.fieldOfView=60;
        EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor")).Focus();
        Directory.CreateDirectory("Temp/ArenaCapture");Time.captureFramerate=30;
        var small=new Texture2D(960,540,TextureFormat.RGB24,false);var rt=new RenderTexture(960,540,0);var cc=session.Player.GetComponent<CharacterController>();
        for(int i=0;i<210;i++){
            if(field && !field.Fired){var d=field.SafeA-session.Player.transform.position;d.y=0;if(d.magnitude>.1f)cc.Move(d.normalized*Mathf.Min(d.magnitude,session.Player.MoveSpeed*Time.deltaTime));}
            if(boss.UltimateStage==4)Move(session.Player.transform,owner.transform.position+Get<Vector3>(boss,"ringGapDirection")*7);
            yield return new WaitForEndOfFrame();var source=ScreenCapture.CaptureScreenshotAsTexture();var previous=RenderTexture.active;Graphics.Blit(source,rt);RenderTexture.active=rt;small.ReadPixels(new Rect(0,0,960,540),0,0);small.Apply();RenderTexture.active=previous;
            if(i%2==0)File.WriteAllBytes("Temp/ArenaCapture/"+i.ToString("D3")+".png",small.EncodeToPNG());Object.Destroy(source);
        }
        rt.Release();Object.Destroy(rt);Object.Destroy(small);Time.captureFramerate=0;rig.enabled=true;session.ReturnToMenu();File.WriteAllText("Temp/ArenaCapture/complete.txt","COMPLETE: 210 real Play Mode frames, sampled at 15 fps");
    }
}
