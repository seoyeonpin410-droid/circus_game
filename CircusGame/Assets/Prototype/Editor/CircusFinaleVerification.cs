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
public static class CircusFinaleVerification
{
    static readonly List<string> report=new List<string>();
    static readonly BindingFlags Flags=BindingFlags.NonPublic|BindingFlags.Instance;
    static CircusSession session;
    static CircusEnemy owner;
    static CircusBoss boss;
    static bool running;
    static void Check(bool ok,string text){report.Add((ok?"PASS ":"FAIL ")+text);File.WriteAllLines("Temp/finale-verification.txt",report);}
    static void Move(Transform t,Vector3 p){var c=t.GetComponent<CharacterController>();bool on=c && c.enabled;if(on)c.enabled=false;t.position=p;if(on)c.enabled=true;Physics.SyncTransforms();}
    static IEnumerator Wait(Func<bool> condition,float timeout=10){float end=Time.realtimeSinceStartup+timeout;while(!condition() && Time.realtimeSinceStartup<end){session.Player.RestoreHealth(100);yield return null;}}
    static IEnumerator Create(int phase,Vector3 player)
    {
        if(owner)Object.Destroy(owner.gameObject);yield return null;
        Move(session.Player.transform,player);session.Player.RestoreHealth(100);
        owner=Object.Instantiate(AssetDatabase.LoadAssetAtPath<CircusEnemy>("Assets/Prototype/Prefabs/DokkaebiBoss.prefab"),new Vector3(0,.06f,5),Quaternion.identity);owner.Bind(session);boss=owner.GetComponent<CircusBoss>();
        typeof(CircusBoss).GetField("nextGround",Flags).SetValue(boss,float.MaxValue);
        yield return Wait(()=>!boss.IsInvulnerable);
        if(phase>1){owner.TakeDamage(owner.MaxHealth*(phase==2?.4f:.7f));yield return null;yield return Wait(()=>boss.Phase==phase && !boss.IsInvulnerable);}
    }
    static IEnumerator Start(BossPattern p){bool started=false;yield return Wait(()=>started=boss.TryStartPattern(p),12);Check(started && boss.Pattern==p,"Selected eligible pattern: "+p);}
    public static string Run()
    {
        if(running)return "Finale verification is already running";
        running=true;report.Clear();File.WriteAllText("Temp/finale-verification.txt","RUNNING "+DateTime.Now.ToString("O"));
        session=Object.FindFirstObjectByType<CircusSession>();session.StartCoroutine(Verify());return "Finale pattern verification started";
    }
    static IEnumerator Verify()
    {
        session.BeginShow();yield return new WaitForSeconds(3.6f);
        session.StopCoroutine((Coroutine)typeof(CircusSession).GetField("roundRoutine",Flags).GetValue(session));
        yield return new WaitForSeconds(.6f);foreach(var e in session.Enemies.ToArray())e.TakeDamage(10000);yield return null;
        yield return Create(1,new Vector3(0,.06f,2.4f));
        Check(!boss.CanUse(BossPattern.Charge) && !boss.CanUse(BossPattern.DoubleSwing) && !boss.CanUse(BossPattern.RingFlames),"Phase 1 locks rush, combo, homing/ring extensions");
        yield return Start(BossPattern.Swing);float hp=session.Player.Health;int count=boss.MeleeAttacks;
        yield return new WaitForSeconds(.25f);Check(session.Player.Health==hp && boss.MeleeAttacks==count && boss.AttackArea.Visible,"Swing warns before dealing damage");
        Check(boss.AttackArea.Contains(owner.transform.position+boss.AttackArea.transform.forward*3.79f) && !boss.AttackArea.Contains(owner.transform.position+boss.AttackArea.transform.forward*3.81f),"Swing visual and damage share exact sector edge");
        Move(session.Player.transform,owner.transform.position+boss.AttackArea.transform.forward*4.5f);
        yield return new WaitForSeconds(.5f);Check(session.Player.Health==hp && boss.MeleeAttacks>count,"Escaping swing telegraph avoids damage");
        yield return new WaitForSeconds(.1f);Check(boss.Action==BossAction.Recovery && !boss.TryStartPattern(BossPattern.Slam),"Recovery blocks other attacks and offers counterattack time");
        yield return Create(1,new Vector3(0,.06f,2.4f));yield return Start(BossPattern.Slam);
        hp=session.Player.Health;yield return new WaitForSeconds(.45f);Check(session.Player.Health==hp && boss.AttackArea.HalfAngle==180,"Slam displays a full circle with a long windup");
        yield return Wait(()=>session.Player.Health<hp);Check(session.Player.Health==hp-boss.Settings(BossPattern.Slam).damage,"Slam damage occurs only after warning");
        Check(Object.FindFirstObjectByType<CircusBossVfx>(),"Slam creates a visible shockwave");
        yield return Create(1,new Vector3(0,.06f,-4));yield return Start(BossPattern.SingleFlame);
        Check(boss.Charging,"Single flame exposes a charge before release");count=boss.RangedAttacks;
        yield return new WaitForSeconds(.3f);Check(boss.RangedAttacks==count,"Single flame cannot fire immediately");
        yield return new WaitForSeconds(.45f);Check(boss.LastVolleySize==1 && boss.RangedAttacks>count,"Single flame reuses the existing visible projectile");
        Check(!boss.TryStartPattern(BossPattern.Charge),"No special attack can interrupt an active shot");
        yield return Create(1,new Vector3(0,.06f,-4));owner.TakeDamage(owner.MaxHealth*.2f);yield return null;yield return Start(BossPattern.FanFlame);
        yield return new WaitForSeconds(.8f);
        var shots=Object.FindObjectsByType<CircusProjectile>(FindObjectsSortMode.None).Where(p=>p.Owner==owner).ToArray();
        Check(boss.Phase==1 && boss.LastVolleySize==3 && shots.Length==3,"Late Phase 1 unlocks three-way flame fan");
        Check(shots.All(p=>!p.Homing) && shots.SelectMany((p,i)=>shots.Skip(i+1).Select(q=>Vector3.Angle(p.Direction,q.Direction))).DefaultIfEmpty(0).Max()>30,"Fan projectiles leave gaps and do not home");
        yield return Create(2,new Vector3(0,.06f,-4));yield return Start(BossPattern.HomingFlames);
        yield return new WaitForSeconds(.82f);shots=Object.FindObjectsByType<CircusProjectile>(FindObjectsSortMode.None).Where(p=>p.Owner==owner).ToArray();
        Check(shots.Length==1,"Tracking flames release one at a time");var first=shots.FirstOrDefault();
        Move(session.Player.transform,new Vector3(5,.06f,-4));var old=first?first.Direction:Vector3.forward;
        yield return new WaitForSeconds(.2f);Check(first && first.Homing && Vector3.Angle(old,first.Direction)<=8,"Homing is weak and turn-rate limited");
        yield return new WaitForSeconds(.55f);Check(boss.RangedAttacks>=3,"Tracking pattern releases all three sequential flames");
        yield return new WaitForSeconds(.4f);Check(first && !first.Homing,"Tracking expires after about one second");
        if(first){old=first.Direction;Move(session.Player.transform,new Vector3(-5,.06f,-4));yield return new WaitForSeconds(.2f);Check(first && Vector3.Angle(old,first.Direction)<.1f,"Expired homing continues straight despite target movement");}
        yield return Create(2,new Vector3(0,.06f,-1));yield return Start(BossPattern.Charge);
        var dashOrigin=owner.transform.position;var forward=boss.AttackArea.transform.forward;
        Check(boss.AttackArea.Visible && boss.AttackArea.Contains(dashOrigin+forward*4+Vector3.right*1.19f) && !boss.AttackArea.Contains(dashOrigin+forward*4+Vector3.right*1.21f),"Dash lane warning and hit width agree");
        hp=session.Player.Health;Move(session.Player.transform,session.Player.transform.position+Vector3.right*3);
        yield return new WaitForSeconds(.4f);Check(Vector3.Distance(owner.transform.position,dashOrigin)<.05f,"Rush remains still during direction warning");
        yield return new WaitForSeconds(1.15f);
        Check(session.Player.Health==hp && Vector3.Distance(owner.transform.position,dashOrigin)>2,"Side-step avoids locked-direction rush");
        Check(Mathf.Abs(owner.transform.position.x-dashOrigin.x)<.1f && boss.Action==BossAction.Recovery,"Rush does not steer and ends with long recovery");
        yield return Create(2,new Vector3(0,.06f,-1));yield return Start(BossPattern.Charge);hp=session.Player.Health;
        yield return new WaitForSeconds(.4f);Check(session.Player.Health==hp,"Rush cannot damage during lane warning");
        yield return new WaitForSeconds(1);Check(session.Player.Health==hp-boss.Settings(BossPattern.Charge).damage,"Remaining in the traversed dash lane takes one real hit");
        hp=session.Player.Health;yield return new WaitForSeconds(.25f);Check(session.Player.Health==hp && boss.Action==BossAction.Recovery,"Rush collision stops and never applies continuous contact damage");
        yield return Create(2,new Vector3(0,.06f,2.4f));yield return Start(BossPattern.DoubleSwing);
        count=boss.MeleeAttacks;yield return Wait(()=>boss.MeleeAttacks>count);hp=session.Player.Health;
        yield return Wait(()=>boss.Action==BossAction.MeleeWindup);Check(boss.AttackArea.Visible,"Second combo swing has a fresh directional warning");
        hp=session.Player.Health;
        Move(session.Player.transform,owner.transform.position+Vector3.back*5);
        yield return new WaitForSeconds(.7f);Check(boss.MeleeAttacks==count+2 && session.Player.Health==hp,"Second swing can also be dodged rather than forcing a hit");
        yield return Create(3,new Vector3(0,.06f,-4));yield return Start(BossPattern.RingFlames);
        yield return Wait(()=>boss.Action==BossAction.SpecialWindup);
        Check(Vector3.ProjectOnPlane(owner.transform.position,Vector3.up).magnitude<=1.3f,"Circular attack repositions toward arena center before charging");
        Check(boss.Charging && boss.AttackArea.Visible,"Final-phase ring has visible orbs, aura and floor warning");
        hp=session.Player.Health;Move(session.Player.transform,owner.transform.position+Vector3.forward*6);
        yield return new WaitForSeconds(.7f);Check(boss.RangedAttacks==0,"Ring charges for a full reaction window");
        yield return new WaitForSeconds(.5f);shots=Object.FindObjectsByType<CircusProjectile>(FindObjectsSortMode.None).Where(p=>p.Owner==owner).ToArray();
        Check(shots.Length==10 && boss.LastVolleySize==10,"Final ring releases ten existing flame projectiles");
        Check(shots.All(p=>Vector3.Angle(p.Direction,Vector3.forward)>=29),"Circular volley leaves a broad escape gap");
        Check(!boss.CanUse(BossPattern.RingFlames),"Ring cannot repeat immediately or ignore cooldown");
        yield return new WaitForSeconds(.2f);Check(boss.Action==BossAction.Recovery && !boss.TryStartPattern(BossPattern.Charge),"Live ring projectiles prevent an unavoidable rush overlap");
        yield return new WaitForSeconds(.9f);Check(session.Player.Health==hp,"Player can survive inside the broad ring escape gap");
        yield return Create(1,new Vector3(0,.06f,-4));owner.TakeDamage(owner.MaxHealth*.3f);yield return null;yield return null;
        Check(boss.Phase==2 && boss.PhaseTransitions==1 && boss.IsInvulnerable,"70 percent triggers one protected Phase 2 transition");
        hp=owner.Health;owner.TakeDamage(25);Check(owner.Health==hp,"Transition rejects free damage");
        session.Pause();var action=boss.Action;yield return new WaitForSecondsRealtime(.25f);Check(boss.Action==action,"Pause freezes phase transition");session.Resume();
        yield return Wait(()=>!boss.IsInvulnerable);owner.TakeDamage(owner.Health-owner.MaxHealth*.35f);yield return null;yield return null;
        Check(boss.Phase==3 && boss.PhaseTransitions==2,"35 percent triggers exactly one further phase transition");
        yield return Wait(()=>!boss.IsInvulnerable);yield return new WaitForSeconds(1);Check(boss.PhaseTransitions==2,"No duplicate phase transitions after threshold crossing");
        // Observe natural selection rather than forcing a fixed sequence.
        float end=Time.realtimeSinceStartup+25;
        while(boss.PatternHistory.Count<4 && Time.realtimeSinceStartup<end)
        {session.Player.RestoreHealth(100);var d=boss.PatternHistory.Count%2==0?Vector3.back*6:Vector3.back*2.6f;Move(session.Player.transform,owner.transform.position+d);yield return null;}
        var history=boss.PatternHistory;
        Check(history.Count>=4 && !history.Zip(history.Skip(1),(a,b)=>a==b).Any(x=>x),"Natural selection varies eligible patterns without consecutive repeats");
        owner.TakeDamage(10000);yield return null;
        Check(boss.Action==BossAction.Dead && !boss.Charging && !Object.FindObjectsByType<CircusProjectile>(FindObjectsSortMode.None).Any(p=>p.Owner==owner),"Death cancels all attacks, charge objects and owned projectiles immediately");
        yield return new WaitForSeconds(.2f);Check(owner,"Death animation remains visible before cleanup");
        yield return new WaitForSeconds(1.5f);Check(!owner,"Death animation cleans up boss on schedule");
        session.ReturnToMenu();yield return null;
        Check(!Object.FindFirstObjectByType<CircusCharge>() && !Object.FindFirstObjectByType<CircusGroundFlame>(),"Menu/retry cleanup leaves no attack objects");
        report.Add(report.Any(x=>x.StartsWith("FAIL"))?"RESULT: FAILED":"RESULT: ALL PASSED");File.WriteAllLines("Temp/finale-verification.txt",report);
        running=false;
    }
}
