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
public static class CircusStageVerification
{
    static readonly List<string> report=new List<string>();static CircusSession session;static bool running;static int peak;
    static readonly BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
    static void Check(bool ok,string message){report.Add((ok?"PASS ":"FAIL ")+message);File.WriteAllLines("Temp/stage-verification.txt",report);}
    static void Set(object o,string name,object value)=>o.GetType().GetField(name,F).SetValue(o,value);
    static void Move(Vector3 p){var c=session.Player.GetComponent<CharacterController>();c.enabled=false;session.Player.transform.position=p;c.enabled=true;Physics.SyncTransforms();}
    static void Audit(){peak=Mathf.Max(peak,session.Alive);foreach(var e in session.Enemies)if(e && !e.IsBoss && e.CombatArea && !e.CombatArea.Contains(e.transform.position,.1f))Check(false,"Enemy escaped "+e.CombatArea.areaId);}
    static IEnumerator Wait(Func<bool> predicate,float seconds=12){float end=Time.realtimeSinceStartup+seconds;while(!predicate() && Time.realtimeSinceStartup<end){Audit();yield return null;}}
    static IEnumerator Walk(Vector3 target,float seconds=18)
    {
        var cc=session.Player.GetComponent<CharacterController>();float end=Time.realtimeSinceStartup+seconds;
        while(Vector3.Distance(Vector3.ProjectOnPlane(target-session.Player.transform.position,Vector3.up),Vector3.zero)>.35f && Time.realtimeSinceStartup<end){
            var d=target-session.Player.transform.position;d.y=0;cc.Move(d.normalized*Mathf.Min(d.magnitude,session.Player.MoveSpeed*Time.deltaTime));Audit();yield return null;
        }
        Check(Vector3.ProjectOnPlane(target-session.Player.transform.position,Vector3.up).magnitude<.6f,"Physical walking route is open: "+target.ToString("F0"));
    }
    public static string Run(){if(running)return "Already running";running=true;peak=0;report.Clear();session=Object.FindFirstObjectByType<CircusSession>();session.StartCoroutine(Flow());return "Connected-stage full Play Mode verification started";}
    static IEnumerator Flow()
    {
        Application.runInBackground=true;Time.timeScale=1;session.BeginShow();
        Check(session.ConnectedStage && session.Areas.Length==6 && session.Alive==0 && !session.Boss,"New run has five empty combat areas and no active boss");
        Check(!session.TryEnterArea(session.Areas[5]),"Boss trigger cannot bypass Act 1/2");
        yield return new WaitForSeconds(.15f);Check(session.Spawned==0 && session.GameplaySeconds==0,"Countdown does not advance combat or spawn enemies");
        yield return Wait(()=>session.IsRunning);yield return new WaitForSeconds(.6f);
        Check(session.Alive==0 && session.Areas.All(a=>a.State==CircusAreaState.NotStarted),"Areas stay dormant until their actual entrance trigger");
        Time.timeScale=3;int killed=0,waves=0,score=0;
        for(int i=0;i<5;i++){
            var a=session.Areas[i];Check(!session.TryEnterArea(session.Areas[Mathf.Min(5,i+1)]),"Cannot start later area before "+a.areaId);
            yield return Walk(a.entranceTrigger.transform.position);yield return Wait(()=>a.State==CircusAreaState.Battle);
            Check(a.Starts==1 && a.entranceBarrier.activeSelf && a.exitBarrier.activeSelf,"Trigger locks both portals exactly once: "+a.areaId);
            Check(session.Round==a.act && session.Score==score,"Act and cumulative score persist on entry: "+a.areaId);
            Check(!session.TryEnterArea(a),"Repeated trigger cannot restart active area: "+a.areaId);
            yield return Wait(()=>session.Alive>0);var first=session.Enemies.FirstOrDefault();
            Check(first && first.Spawning && a.Contains(first.transform.position,1) && Vector3.Distance(first.transform.position,session.Player.transform.position)>=a.minimumSpawnDistance-.1f,"Spawn warns inside this arena away from the player: "+a.areaId);
            if(i==0){session.Pause();int count=session.Spawned;var pos=session.Enemies[0].transform.position;yield return new WaitForSecondsRealtime(.2f);Check(session.Spawned==count && session.Enemies[0].transform.position==pos,"Pause freezes pending spawns and enemy movement");session.Resume();Time.timeScale=3;
                // A real capsule movement against the closed exit must stop before crossing it.
                var from=a.exitBarrier.transform.position;var outward=(from-a.Center).normalized;Move(from-outward*1.5f+Vector3.up*.06f);var cc=session.Player.GetComponent<CharacterController>();
                for(int n=0;n<20;n++){cc.Move(outward*.2f);yield return null;}
                Check(Vector3.Dot(session.Player.transform.position-from,outward)<0,"Player capsule cannot walk through the locked exit");Move(a.Center+Vector3.up*.06f);
            }
            int lastWave=0;bool healSet=false;float deadline=Time.realtimeSinceStartup+35;
            while(a.State!=CircusAreaState.Cleared && Time.realtimeSinceStartup<deadline){
                Audit();if(session.WaveNumber>lastWave){waves++;lastWave=session.WaveNumber;}
                if(!healSet && (i==1 || i==4) && session.WaveNumber==a.waves.Length && session.Remaining<=session.Alive && session.Alive>0){Set(session.Player,"<Health>k__BackingField",40f);healSet=true;}
                foreach(var e in session.Enemies.ToArray())if(e && !e.Spawning){
                    int before=session.Score;e.TakeDamage(10000);if(e.Health<=0){int after=session.Score;session.EnemyDefeated(e);Check(session.Score==after && after>before,"Death counted once in "+a.areaId);killed++;}
                }
                yield return new WaitForSeconds(.12f);
            }
            Check(a.State==CircusAreaState.Cleared && a.Clears==1 && session.Remaining==0 && !a.entranceBarrier.activeSelf && !a.exitBarrier.activeSelf,"All waves clear and both portals open: "+a.areaId);
            if(i==1 || i==4)Check(session.Player.Health>= (i==1?60:65)-.1f,"Act clear restores only configured health: "+a.act);
            score=session.Score;Check(!session.TryEnterArea(a) && a.Starts==1 && a.Clears==1,"Cleared arena cannot replay or reset progress: "+a.areaId);
            Check(!session.Boss,"Boss stays unspawned through regular area "+a.areaId);
            session.Player.RestoreHealth(100);yield return Walk(a.Center+Vector3.up*.06f);yield return Walk(a.exitBarrier.transform.position+(a.exitBarrier.transform.position-a.Center).normalized*2);
        }
        Check(killed==35 && waves==8 && score==4050 && peak<=6,"Eight configured waves, 35 enemies, 4050 score, max six alive");
        Check(session.BossPath && session.Round==2 && !session.Boss,"Act 2 clear enters a quiet boss path, not immediate boss combat");
        yield return new WaitForSeconds(.6f);var music=session.GetComponent<CircusMusic>();Check(music.BossVolume==0 && music.CombatVolume<.12f,"Ordinary BGM fades on the boss approach and boss BGM stays silent");
        yield return Walk(session.Areas[5].entranceTrigger.transform.position);yield return Wait(()=>session.Boss);
        var owner=session.Boss;var boss=owner?owner.GetComponent<CircusBoss>():null;
        Check(owner && session.State==ShowState.BossIntro && session.Round==3 && session.Areas[5].entranceBarrier.activeSelf,"Boss trigger closes entrance and invokes original introduction");
        Check(!session.Player.Fire() && !session.TryEnterArea(session.Areas[5]),"Intro locks attack and cannot be restarted");
        yield return Wait(()=>session.State==ShowState.Playing);
        Check(boss && boss.Entrance.Complete && boss.PatternHistory.Count==0,"Original landing, name, roar and camera complete before first attack");
        Time.timeScale=1;yield return Wait(()=>!boss.IsInvulnerable);owner.TakeDamage(310);yield return null;yield return Wait(()=>!boss.IsInvulnerable);owner.TakeDamage(owner.Health-340);yield return null;yield return Wait(()=>!boss.IsInvulnerable);
        Check(boss.Phase==3 && boss.PhaseTransitions==2,"Existing three-phase boss still transitions correctly on expanded stage");
        owner.TakeDamage(10000);yield return Wait(()=>session.State==ShowState.Victory);
        Check(session.State==ShowState.Victory && session.Score==5250 && session.Areas.All(a=>a.State==CircusAreaState.Cleared),"Boss death reaches final clear with score from all areas");
        session.BeginShow();yield return null;Check(session.Score==0 && session.Alive==0 && !session.Boss && session.Areas.All(a=>a.State==CircusAreaState.NotStarted) && session.Areas.All(a=>a.Starts==0),"Retry resets area flags, scores, enemies and boss exactly once");
        session.ReturnToMenu();yield return null;Check(Time.timeScale==1 && !Object.FindFirstObjectByType<CircusProjectile>() && !Object.FindFirstObjectByType<CircusSafeField>(),"Menu cleanup removes projectiles and arena hazards");
        report.Add(report.Any(s=>s.StartsWith("FAIL"))?"RESULT: FAILED":"RESULT: ALL PASSED");File.WriteAllLines("Temp/stage-verification.txt",report);running=false;
    }
    public static string Retry(){session=Object.FindFirstObjectByType<CircusSession>();session.StartCoroutine(DeathRetry());return "Death and retry verification started";}
    static IEnumerator DeathRetry()
    {
        var checks=new List<string>();Action<bool,string> log=(ok,text)=>{checks.Add((ok?"PASS ":"FAIL ")+text);File.WriteAllLines("Temp/stage-retry.txt",checks);};
        Time.timeScale=1;session.BeginShow();yield return Wait(()=>session.IsRunning);yield return new WaitForSeconds(.6f);
        yield return Walk(session.Areas[0].entranceTrigger.transform.position);yield return Wait(()=>session.Alive>0);session.Player.TakeDamage(10000);yield return null;
        log(session.State==ShowState.Defeat,"Death stops the active arena and stage routine");int spawned=session.Spawned;float timer=session.GameplaySeconds;
        yield return new WaitForSeconds(.5f);log(spawned==session.Spawned && timer==session.GameplaySeconds,"No new wave or gameplay time advances after death");
        session.BeginShow();yield return null;log(session.State==ShowState.Countdown && session.Score==0 && session.Spawned==0 && session.Alive==0 && !session.Boss && session.Areas.All(a=>a.State==CircusAreaState.NotStarted),"Retry cleans every arena and restarts the countdown");
        log(!session.Areas[0].entranceBarrier.activeSelf && session.Areas.Skip(1).All(a=>a.entranceBarrier.activeSelf),"Only the first arena unlocks after retry");
        session.ReturnToMenu();yield return null;checks.Add(checks.Any(s=>s.StartsWith("FAIL"))?"RESULT: FAILED":"RESULT: ALL PASSED");File.WriteAllLines("Temp/stage-retry.txt",checks);
    }
}
