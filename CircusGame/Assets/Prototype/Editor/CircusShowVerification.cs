using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CircusPrototype;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Object=UnityEngine.Object;
public static class CircusShowVerification
{
    static readonly BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
    static readonly List<string> report=new List<string>();static CircusSession s;static CircusBoss boss;static CircusEnemy owner;static Keyboard kb;static bool running;static int peak;
    static void Set(object o,string n,object v)=>o.GetType().GetField(n,F).SetValue(o,v);
    static T Get<T>(object o,string n)=>(T)o.GetType().GetField(n,F).GetValue(o);
    static void Check(bool ok,string text){report.Add((ok?"PASS ":"FAIL ")+text);File.WriteAllLines("Temp/show-verification.txt",report);}
    static void Keys(params Key[] keys)=>InputSystem.QueueStateEvent(kb,new KeyboardState(keys));
    static void Move(Vector3 p){var cc=s.Player.GetComponent<CharacterController>();cc.enabled=false;s.Player.transform.position=p;cc.enabled=true;Set(s.Player,"verticalSpeed",0f);Physics.SyncTransforms();}
    static IEnumerator Wait(Func<bool> predicate,float seconds=20){float end=Time.realtimeSinceStartup+seconds;while(!predicate() && Time.realtimeSinceStartup<end){peak=Mathf.Max(peak,s.Alive);yield return null;}}
    static IEnumerator Walk(Vector3 p){float end=Time.realtimeSinceStartup+15;var cc=s.Player.GetComponent<CharacterController>();while(Vector3.ProjectOnPlane(p-s.Player.transform.position,Vector3.up).magnitude>.3f && Time.realtimeSinceStartup<end){var d=p-s.Player.transform.position;d.y=0;cc.Move(d.normalized*Mathf.Min(d.magnitude,s.Player.MoveSpeed*Time.deltaTime));yield return null;}Check(Vector3.ProjectOnPlane(p-s.Player.transform.position,Vector3.up).magnitude<.6f,"Physical boss path is open");}
    static IEnumerator Pattern(BossPattern p)
    {
        boss.ClearHazards();boss.SendMessage("CancelExpanded");boss.SendMessage("HideWarnings");yield return null;
        Set(boss,"betweenDashes",false);Set(boss,"centering",false);Set(boss,"ultimateQueued",false);Set(boss,"jumpPatternPending",false);Set(boss,"followup",null);Set(boss,"lastPattern",null);Set(boss,"heavyBlockedUntil",0f);Set(boss,"nextGround",p==BossPattern.GroundSeals?0f:float.MaxValue);Set(boss,"until",0f);Set(boss,"<Action>k__BackingField",BossAction.PatternSelect);Get<Dictionary<BossPattern,float>>(boss,"ready").Clear();
        var cc=owner.GetComponent<CharacterController>();cc.enabled=false;owner.transform.position=new Vector3(0,.06f,0);cc.enabled=true;Move(new Vector3(0,.06f,-6));s.Player.RestoreHealth(100);Set(s.Player,"lastDamage",-100f);Check(boss.TryStartPattern(p),"Pattern starts: "+p);
    }
    public static string Run(){if(running)return "Already running";running=true;report.Clear();s=Object.FindFirstObjectByType<CircusSession>();s.StartCoroutine(Verify());return "Single-stage, dash, jump ring and sequential-floor verification started";}
    static IEnumerator Verify()
    {
        Application.runInBackground=true;Time.timeScale=1;var bg=InputSystem.settings.backgroundBehavior;var ed=InputSystem.settings.editorInputBehaviorInPlayMode;
        InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;kb=InputSystem.AddDevice<Keyboard>();kb.MakeCurrent();
        s.BeginShow();Check(s.SameStageShow && s.Areas.Length==3 && !Object.FindObjectsByType<CircusAreaTrigger>(FindObjectsSortMode.None).Any(t=>t.area && !t.area.bossArena),"Dungeon rooms are inactive; one stage and one boss trigger remain");
        Check(s.Areas[0].Center==s.Areas[1].Center && s.Areas[0].halfSize.x==19.6f && s.Areas[0].spawnPoints.Length==6,"Both acts share the expanded 39.2m stage and six entries");
        Check(!s.Player.Dash(Vector3.forward) && !s.TryEnterArea(s.Areas[2]) && s.Alive==0,"Countdown blocks dash, enemies and premature boss entry");
        yield return Wait(()=>s.ActiveArea==s.Areas[0] && s.WaveNumber==1);
        Keys(Key.Q);yield return null;yield return null;var start=s.Player.transform.position;Check(s.Player.IsDashing && s.Player.DashInvulnerable && !s.Player.Dash(Vector3.right),"Q starts a short dash and cooldown rejects another dash");
        float hp=s.Player.Health;s.Player.TakeDamage(10);Check(s.Player.Health==hp,"Dash start has brief ordinary-hit immunity");
        Keys();yield return new WaitForSeconds(.14f);Set(s.Player,"lastDamage",-100f);s.Player.TakeDamage(10);Check(s.Player.Health==hp-10,"Dash is vulnerable again after the short immunity window");
        yield return new WaitForSeconds(.15f);Check(!s.Player.IsDashing && s.Player.DashCooldownRemaining>0,"Dash ends before cooldown does");
        yield return Wait(()=>s.Player.DashCooldownRemaining<=0);Move(s.Areas[0].Center+Vector3.back*18.5f+Vector3.up*.06f);s.Player.Dash(Vector3.back);yield return new WaitForSeconds(.3f);Check(s.Areas[0].Contains(s.Player.transform.position,.1f),"Dash cannot cross the stage wall");
        Move(s.Areas[0].Center+Vector3.up*.06f);Keys(Key.W,Key.LeftShift);yield return null;yield return null;Check(s.Player.IsSprinting && Mathf.Abs(s.Player.CurrentMoveSpeed-9.6f)<.01f,"Shift sprint remains separate from Q dash");Keys();s.Player.RestoreHealth(100);
        Time.timeScale=3;var used=new HashSet<int>();int killed=0,waves=0;var hazard=Object.FindFirstObjectByType<CircusArenaHazard>();
        for(int act=0;act<2;act++){
            var area=s.Areas[act];yield return Wait(()=>s.ActiveArea==area);Check(!s.Areas[2].entranceBarrier.activeSelf==false && area.exitBarrier.activeSelf,"Boss route remains locked in act "+(act+1));
            int previousWave=0;bool gimmickChecked=false;float deadline=Time.realtimeSinceStartup+80;
            while(area.State!=CircusAreaState.Cleared && Time.realtimeSinceStartup<deadline){
                if(s.WaveNumber>previousWave){previousWave=s.WaveNumber;waves++;if(act==1 && previousWave==2)Move(area.spawnPoints[0].position);}
                foreach(var e in s.Enemies.ToArray())if(e && !e.IsBoss){
                    used.Add(Array.FindIndex(area.spawnPoints,p=>Vector3.Distance(p.position,e.transform.position)<2));
                    Check(area.Contains(e.transform.position,.1f),"Spawn/AI remains on the active stage");
                    if(act==1 && !gimmickChecked){Set(e,"nextAttack",float.MaxValue);Set(e,"moveSpeed",0f);}
                    else if(!e.Spawning){int before=s.Score;e.TakeDamage(10000);if(e.Health<=0){killed++;int score=s.Score;s.EnemyDefeated(e);Check(score==s.Score && score>before,"Enemy death and score counted once");}}
                }
                if(act==0)Check(!hazard.WarningVisible,"Act 1 has no arena hazard");
                if(act==1 && !gimmickChecked){
                    s.Player.RestoreHealth(100);if(hazard.WarningVisible){
                        hp=s.Player.Health;Check(hazard.Active && Vector3.Distance(hazard.WarningCenter,s.Player.transform.position)<1,"Act 2 creates a readable local floor warning");
                        s.Pause();var p=s.Player.transform.position;yield return new WaitForSecondsRealtime(.15f);Check(hazard.WarningVisible && p==s.Player.transform.position,"Pause freezes the stage warning");s.Resume();Time.timeScale=3;
                        yield return Walk(hazard.WarningCenter+Vector3.right*5);yield return new WaitForSeconds(1.2f);Check(hazard.Blasts>0 && s.Player.Health==hp,"Leaving the warning avoids damage and ample safe floor remains");gimmickChecked=true;
                    }
                }
                s.Player.RestoreHealth(100);peak=Mathf.Max(peak,s.Alive);yield return new WaitForSeconds(.3f);
            }
            Check(area.State==CircusAreaState.Cleared && area.Clears==1,"Act "+(act+1)+" finishes exactly once");
            if(act==0){Check(s.Areas[0].exitBarrier.activeSelf && !s.Boss,"Act 1 clear keeps the boss gate locked and needs no travel");Move(area.Center+Vector3.up*.06f);}
        }
        Check(killed==49 && waves==9 && used.Count(i=>i>=0)==6 && peak<=7 && s.Score==5700,"Nine waves, 49 enemies, six entry directions, max seven and cumulative score");
        Check(s.BossPath && !hazard.Active && !hazard.WarningVisible && !s.Areas[1].exitBarrier.activeSelf && !s.Boss,"Act 2 clear stops the gimmick and opens the boss path without spawning boss early");
        yield return Walk(s.Areas[1].Center+Vector3.up*.06f);yield return Walk(s.Areas[1].exitBarrier.transform.position+Vector3.forward*2);yield return Walk(s.Areas[2].entranceTrigger.transform.position);
        yield return Wait(()=>s.Boss);owner=s.Boss;boss=owner.GetComponent<CircusBoss>();Check(s.State==ShowState.BossIntro && !s.Player.Dash(Vector3.forward) && !s.Player.Fire(),"Boss door closes and original intro locks input");
        yield return Wait(()=>s.IsRunning && !boss.IsInvulnerable);Time.timeScale=1;owner.TakeDamage(310);yield return null;yield return Wait(()=>!boss.IsInvulnerable);
        yield return Pattern(BossPattern.GroundSeals);var plan=boss.GroundPlan.ToArray();Check(plan.Length==7 && plan.SelectMany((p,i)=>plan.Skip(i+1).Select(q=>Vector3.Distance(p,q))).Min()>3.6f,"Phase 2 plans seven nonoverlapping floor circles");
        Move(new Vector3(-5,.06f,-6));yield return Wait(()=>Object.FindObjectsByType<CircusGroundFlame>(FindObjectsSortMode.None).Length>=7);Check(Object.FindObjectsByType<CircusGroundFlame>(FindObjectsSortMode.None).Length==7,"Seven circles appear sequentially in small batches");
        yield return Pattern(BossPattern.GroundShockwave);yield return new WaitForSeconds(.35f);Check(!Object.FindFirstObjectByType<CircusGroundShockwave>() && owner.Model.localPosition.y>Get<Vector3>(boss,"modelRest").y+.05f,"Jump ring has a distinct long lift before the slam");
        yield return Wait(()=>Object.FindFirstObjectByType<CircusGroundShockwave>());Set(boss,"until",float.MaxValue);var ring=Object.FindFirstObjectByType<CircusGroundShockwave>();hp=s.Player.Health;yield return Wait(()=>ring.Resolved);
        Check(s.Player.Health==hp-14 && !ring.JumpAvoided,"Grounded player receives one travelling ring hit");yield return new WaitForSeconds(.2f);Check(s.Player.Health==hp-14,"Expanding ring does not damage repeatedly as a filled disc");boss.ClearHazards();yield return null;
        Set(boss,"<Action>k__BackingField",BossAction.Recovery);Set(boss,"until",float.MaxValue);Move(new Vector3(0,.06f,-6));s.Player.RestoreHealth(100);Set(s.Player,"lastDamage",-100f);yield return null;
        ring=CircusGroundShockwave.Create(s,owner,owner.transform.position,8,16.2f,14,.55f,.35f);yield return Wait(()=>ring.Radius>=3.5f);Keys(Key.Space);yield return new WaitForSeconds(.12f);Keys();yield return Wait(()=>ring.Resolved);
        Check(ring.JumpAvoided && s.Player.Health==100,"Actual Space jump clears the moving floor ring");Set(s.Player,"lastDamage",-100f);s.Player.TakeDamage(5);Check(s.Player.Health==95 && s.Player.transform.position.y>.35f,"Airborne player remains vulnerable to ordinary attacks");boss.ClearHazards();yield return new WaitForSeconds(.9f);
        Move(new Vector3(0,.06f,-6));s.Player.RestoreHealth(100);Set(s.Player,"lastDamage",-100f);yield return null;ring=CircusGroundShockwave.Create(s,owner,owner.transform.position,8,16.2f,14,.55f,.35f);yield return Wait(()=>ring.Radius>=4);s.Player.Dash(Vector3.forward);yield return Wait(()=>ring.Resolved);
        Check(!ring.JumpAvoided && s.Player.Health==86,"Ground ring requires jumping and bypasses dash immunity");boss.ClearHazards();yield return null;
        owner.TakeDamage(owner.Health-340);yield return null;yield return Wait(()=>!boss.IsInvulnerable);yield return Pattern(BossPattern.GroundSeals);Check(boss.GroundPlan.Count==9,"Phase 3 plans nine safe floor positions");Move(new Vector3(-5,.06f,-6));yield return Wait(()=>Object.FindObjectsByType<CircusGroundFlame>(FindObjectsSortMode.None).Length>=9);Check(Object.FindObjectsByType<CircusGroundFlame>(FindObjectsSortMode.None).Length==9,"Nine circles complete without a simultaneous arena-wide blast");
        yield return Pattern(BossPattern.GroundShockwave);yield return Wait(()=>Object.FindFirstObjectByType<CircusGroundShockwave>());var firstRing=Object.FindFirstObjectByType<CircusGroundShockwave>();yield return Wait(()=>firstRing.Radius>=3.5f);Keys(Key.Space);yield return new WaitForSeconds(.12f);Keys();yield return Wait(()=>Object.FindObjectsByType<CircusGroundShockwave>(FindObjectsSortMode.None).Length>=2);Check(Object.FindObjectsByType<CircusGroundShockwave>(FindObjectsSortMode.None).Length==2,"Phase 3 emits two rings with a fresh jump interval");var secondRing=Object.FindObjectsByType<CircusGroundShockwave>(FindObjectsSortMode.None).First(w=>w!=firstRing);yield return Wait(()=>secondRing.Radius>=3.5f);Keys(Key.Space);yield return new WaitForSeconds(.12f);Keys();yield return Wait(()=>secondRing.Resolved);Check(firstRing.JumpAvoided && secondRing.JumpAvoided && s.Player.Health==100,"Both Phase 3 rings can be cleared with two actual Space jumps");
        owner.TakeDamage(10000);yield return Wait(()=>s.State==ShowState.Victory);Check(s.Score==6900 && !Object.FindFirstObjectByType<CircusGroundShockwave>() && !Object.FindFirstObjectByType<CircusGroundFlame>(),"Boss death cancels ring/floor attacks and ends at 6900 score");
        s.BeginShow();yield return null;Check(s.Score==0 && s.Alive==0 && !s.Player.IsDashing && s.Player.DashCooldownRemaining==0 && !hazard.WarningVisible,"Retry resets waves, dash and hazard state");s.ReturnToMenu();Keys();InputSystem.RemoveDevice(kb);InputSystem.settings.backgroundBehavior=bg;InputSystem.settings.editorInputBehaviorInPlayMode=ed;Time.timeScale=1;
        report.Add(report.Any(t=>t.StartsWith("FAIL"))?"RESULT: FAILED":"RESULT: ALL PASSED");File.WriteAllLines("Temp/show-verification.txt",report);running=false;
    }
}
