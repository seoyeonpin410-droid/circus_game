using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CircusPrototype;
using UnityEngine;
using Object=UnityEngine.Object;
public static class CircusStagePlayProbe
{
    static readonly BindingFlags F=BindingFlags.NonPublic|BindingFlags.Instance;
    static bool running;
    public static string Run(bool heal=true,float timeScale=2)
    {
        if(running)return "Already running";running=true;var s=Object.FindFirstObjectByType<CircusSession>();s.StartCoroutine(Play(s,heal,timeScale));return "Actual moving/aiming/attacking stage play probe started";
    }
    static void Set(object o,string n,object v)=>o.GetType().GetField(n,F).SetValue(o,v);
    static IEnumerator Play(CircusSession s,bool heal,float scale)
    {
        Application.runInBackground=true;s.BeginShow();Time.timeScale=scale;
        var rig=Object.FindFirstObjectByType<CircusCamera>();var camera=Camera.main;var cc=s.Player.GetComponent<CharacterController>();var path=new Queue<Vector3>();
        int previous=-1,shots=0,swings=0,peak=0;float start=Time.time,areaStart=Time.time;var notes=new List<string>();var captured=new HashSet<string>();
        Directory.CreateDirectory("Temp/StagePlay");File.WriteAllText("Temp/stage-play.txt","RUNNING");float deadline=Time.realtimeSinceStartup+600;
        Vector3 lastPosition=s.Player.transform.position;float stalled=0;int sidestep=1;
        while(s.State!=ShowState.Victory && s.State!=ShowState.Defeat && Time.realtimeSinceStartup<deadline){
            if(s.State==ShowState.BossIntro){rig.enabled=true;yield return null;continue;}
            if(!s.IsRunning){yield return null;continue;}
            if(heal)s.Player.RestoreHealth(100);peak=Mathf.Max(peak,s.Alive);
            var area=s.ActiveArea;var enemies=s.Enemies.Where(e=>e && !e.Spawning && e.Health>0).ToArray();
            Vector3 movement=Vector3.zero,aim=s.Player.transform.position+Vector3.forward*6+Vector3.up;
            if(area && area.State==CircusAreaState.Battle && enemies.Length>0){
                var target=enemies.OrderBy(e=>Vector3.Distance(e.transform.position,s.Player.transform.position)).First();var d=target.transform.position-s.Player.transform.position;d.y=0;aim=target.transform.position+Vector3.up;
                if(area.bossArena){
                    var field=Object.FindFirstObjectByType<CircusSafeField>();var boss=target.GetComponent<CircusBoss>();
                    if(field && !field.Fired){var goal=Vector3.Distance(field.SafeA,s.Player.transform.position)<Vector3.Distance(field.SafeB,s.Player.transform.position)?field.SafeA:field.SafeB;movement=goal-s.Player.transform.position;}
                    else movement=d.normalized*Mathf.Clamp(d.magnitude-7,-2,2)+Vector3.Cross(Vector3.up,d.normalized)*.9f;
                }else movement=d.normalized*Mathf.Clamp(d.magnitude-5,-1,1)+Vector3.Cross(Vector3.up,d.normalized)*.65f;
                movement.y=0;if(movement.magnitude>1)movement.Normalize();
                var desired=s.Player.transform.position+movement*s.Player.MoveSpeed*Time.deltaTime;var clamped=area.Clamp(desired,.9f);movement=(clamped-s.Player.transform.position)/Mathf.Max(.001f,s.Player.MoveSpeed*Time.deltaTime);movement.y=0;
                if(Physics.Raycast(s.Player.transform.position+Vector3.up*1.3f,(aim-s.Player.transform.position-Vector3.up*1.3f).normalized,out var hit,Mathf.Max(.1f,d.magnitude-.5f),(1<<0)|(1<<9),QueryTriggerInteraction.Ignore) && !hit.collider.GetComponentInParent<CircusEnemy>())movement=Quaternion.Euler(0,70*sidestep,0)*d.normalized;
                var flat=d.sqrMagnitude>.001f?d.normalized:Vector3.forward;Set(rig,"yaw",Mathf.Atan2(flat.x,flat.z)*Mathf.Rad2Deg);
                rig.SetFirstPerson(true);rig.enabled=false;camera.transform.position=s.Player.transform.position+Vector3.up*1.6f;camera.transform.LookAt(aim);
                if(d.magnitude<2.7f){if(s.Player.Attack())swings++;}else if(s.Player.Fire())shots++;
            }else if(!area || area.State==CircusAreaState.Cleared){
                int index=s.StageIndex;
                if(index!=previous && index<s.Areas.Length){
                    notes.Add($"Reached travel to {s.Areas[index].areaId}: {Time.time-start:F1}s, prior segment {Time.time-areaStart:F1}s");areaStart=Time.time;File.WriteAllLines("Temp/stage-play.txt",notes);
                    path.Clear();if(index>0){var old=s.Areas[index-1];path.Enqueue(old.Center);path.Enqueue(old.exitBarrier.transform.position+(old.exitBarrier.transform.position-old.Center).normalized*2);}path.Enqueue(s.Areas[index].entranceTrigger.transform.position);previous=index;
                }
                while(path.Count>0 && Vector3.ProjectOnPlane(path.Peek()-s.Player.transform.position,Vector3.up).magnitude<.6f)path.Dequeue();
                if(path.Count>0){movement=path.Peek()-s.Player.transform.position;movement.y=0;aim=s.Player.transform.position+movement.normalized*5+Vector3.up;movement.Normalize();}
                rig.SetFirstPerson(true);rig.enabled=false;camera.transform.position=s.Player.transform.position+Vector3.up*1.6f;if(movement.sqrMagnitude>.01f)camera.transform.LookAt(aim);
            }
            if(Vector3.Distance(s.Player.transform.position,lastPosition)<.015f && movement.magnitude>.1f)stalled+=Time.deltaTime;else stalled=0;lastPosition=s.Player.transform.position;
            if(stalled>1){sidestep=-sidestep;movement=Quaternion.Euler(0,75*sidestep,0)*movement;stalled=0;}
            cc.Move(Vector3.ClampMagnitude(movement,1)*s.Player.MoveSpeed*Time.deltaTime);
            if(area && area.State==CircusAreaState.Battle && captured.Add(area.areaId)){
                yield return new WaitForEndOfFrame();var image=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes("Temp/StagePlay/"+area.areaId+".png",image.EncodeToPNG());Object.Destroy(image);
            }
            yield return null;
        }
        notes.Add($"RESULT: {s.State}; combat-time {s.GameplaySeconds:F2}s; elapsed scaled time {Time.time-start:F2}s; shots {shots}; melee {swings}; score {s.Score}; peak {peak}; HP {s.Player.Health}; automatic healing {heal}. Real camera/projectile/melee damage, no injected enemy damage.");
        File.WriteAllLines("Temp/stage-play.txt",notes);Time.timeScale=1;rig.enabled=true;running=false;
    }
}
