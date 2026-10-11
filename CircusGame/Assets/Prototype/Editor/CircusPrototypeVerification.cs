using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CircusPrototype;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public static class CircusPrototypeVerification
{
    static readonly List<string> results = new List<string>();
    static Keyboard keyboard;
    static Mouse mouse;
    static InputSettings.BackgroundBehavior savedBackground;
    static InputSettings.EditorInputBehaviorInPlayMode savedEditorInput;
    const string Report = "Temp/circus-verification.txt";
    public static string Run()
    {
        if (!EditorApplication.isPlaying) throw new InvalidOperationException("Play Mode required");
        results.Clear();
        savedBackground=InputSystem.settings.backgroundBehavior;
        savedEditorInput=InputSystem.settings.editorInputBehaviorInPlayMode;
        InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        var session = UnityEngine.Object.FindFirstObjectByType<CircusSession>();
        session.StartCoroutine(Verify(session));
        return "Started runtime verification; report: " + Report;
    }
    static void Check(bool success,string name)
    {
        results.Add((success ? "PASS " : "FAIL ") + name);
        File.WriteAllLines(Report,results);
    }
    static void Click(string path)
    {
        var buttons=UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsInactive.Include,FindObjectsSortMode.None);
        var button=buttons.First(b=>b.name==path); button.onClick.Invoke();
    }
    static IEnumerator Verify(CircusSession session)
    {
        session.ReturnToMenu(); yield return null;
        Check(session.State==ShowState.Menu && session.Player.Health==100,"Menu and initial health");
        Click("설정"); yield return null;
        var settings=UnityEngine.Object.FindObjectsByType<RectTransform>(FindObjectsInactive.Include,FindObjectsSortMode.None).First(t=>t.name=="Settings");
        Check(settings.gameObject.activeSelf,"Settings button opens panel");
        var slider=settings.GetComponentInChildren<UnityEngine.UI.Slider>(); slider.value=.35f;
        Check(Mathf.Abs(AudioListener.volume-.35f)<.001f,"Volume slider changes actual audio volume");
        slider.value=.7f; Click("돌아가기"); Click("공연 시작"); yield return new WaitForSeconds(.25f);
        Check(session.State==ShowState.Playing,"Start button begins show");
        keyboard=InputSystem.AddDevice<Keyboard>(); mouse=InputSystem.AddDevice<Mouse>();
        var initial=session.Player.transform.position;
        InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.W));
        yield return new WaitForSeconds(.6f);
        InputSystem.QueueStateEvent(keyboard,new KeyboardState()); yield return null;
        Check(Vector3.Distance(initial,session.Player.transform.position)>2,"WASD movement through Input System");
        yield return new WaitForSeconds(.2f);
        var groundedY=session.Player.transform.position.y;
        InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Space)); yield return new WaitForSeconds(.18f);
        Check(session.Player.transform.position.y>groundedY+.5f,"Space jump through Input System");
        InputSystem.QueueStateEvent(keyboard,new KeyboardState()); yield return new WaitForSeconds(.9f);
        Check(Mathf.Abs(session.Player.transform.position.y-groundedY)<.2f,"Jump lands on arena collider");
        // Place one live target in front, then click through the real input path.
        while(session.Alive==0)yield return null;
        var target=session.Enemies[0]; var targetPos=session.Player.transform.position+Vector3.forward*2.6f;
        target.transform.position=new Vector3(targetPos.x,.06f,targetPos.z); Physics.SyncTransforms();
        float previousHp=target.Health;
        var screen=Camera.main.WorldToScreenPoint(target.transform.position);
        InputSystem.QueueStateEvent(mouse,new MouseState{position=new Vector2(screen.x,screen.y)}.WithButton(MouseButton.Left));
        yield return new WaitForSeconds(.3f);
        Check(session.Player.AttackCount>0 && (!target || target.Health<previousHp),"Left mouse fires bell projectile and damages enemy");
        InputSystem.QueueStateEvent(mouse,new MouseState{position=new Vector2(screen.x,screen.y)});
        yield return null;
        InputSystem.RemoveDevice(keyboard); InputSystem.RemoveDevice(mouse); keyboard=null; mouse=null;
        Click("Ⅱ"); yield return null;
        Check(session.State==ShowState.Paused && Time.timeScale==0,"HUD pause button freezes show");
        Click("계속하기"); yield return null;
        Check(session.State==ShowState.Playing && Time.timeScale==1,"Resume restores show");
        session.Player.TakeDamage(92); yield return null;
        Check(session.Infected && session.Player.Health==8,"Health below 10% enters infection");
        var fill=UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Image>(FindObjectsInactive.Include,FindObjectsSortMode.None).First(t=>t.name=="HP Fill");
        Check(fill.color.g>fill.color.r && Mathf.Abs(fill.fillAmount-.08f)<.01f,"Infected HP bar is green and proportional");
        yield return new WaitForSeconds(.7f); session.Player.TakeDamage(100); yield return null;
        Check(session.State==ShowState.Defeat,"Zero HP opens defeat result");
        Click("다시 공연"); yield return null;
        Check(session.State==ShowState.Playing && session.Player.Health==100 && !session.Infected && session.Score==0,"Retry clears previous show state");
        yield return new WaitForSeconds(10);
        Check(session.Alive<=6 && session.Spawned==6,"Spawner enforces simultaneous enemy cap");
        session.Player.RestoreHealth(100);
        var positions=session.Enemies.Select(e=>e.transform.position).ToArray();
        var controller=session.Player.GetComponent<CharacterController>();
        controller.enabled=false; session.Player.transform.position=new Vector3(0,.06f,-6); controller.enabled=true;
        var distances=positions.Select(p=>Vector3.Distance(p,session.Player.transform.position)).ToArray();
        yield return new WaitForSeconds(.5f);
        Check(session.Enemies.Select((e,i)=>Vector3.Distance(e.transform.position,session.Player.transform.position)<distances[i]-.2f).Any(v=>v),"Melee enemies pursue relocated player");
        session.Pause(); var pausedPositions=session.Enemies.Select(e=>e.transform.position).ToArray();
        yield return new WaitForSecondsRealtime(.3f);
        Check(session.Enemies.Select((e,i)=>Vector3.Distance(e.transform.position,pausedPositions[i])).All(d=>d<.001f),"Pause freezes enemy pursuit");
        session.Resume();
        while(session.State==ShowState.Playing && session.Round==1)
        {
            foreach(var enemy in session.Enemies.ToArray())enemy.TakeDamage(1000);
            yield return new WaitForSeconds(.2f);
        }
        Check(session.State==ShowState.Intermission && session.Score==800,"Round 1 ends only after all eight enemies defeated");
        session.Pause(); yield return null; session.Resume();
        Check(session.State==ShowState.Intermission,"Pause resumes the correct intermission state");
        yield return new WaitForSeconds(7);
        Check(session.Round==2 && session.Enemies.Any(e=>e.IsRanged),"Round 2 adds ranged enemies");
        var rangedEnemy=session.Enemies.First(e=>e.IsRanged); var rangedPosition=rangedEnemy.transform.position;
        yield return new WaitForSeconds(.5f);
        Check(Vector3.Distance(rangedEnemy.transform.position,rangedPosition)<.001f,"Ranged enemy remains stationary");
        float deadline=Time.time+4;
        while(Time.time<deadline && UnityEngine.Object.FindObjectsByType<CircusProjectile>(FindObjectsSortMode.None).Length==0)yield return null;
        Check(UnityEngine.Object.FindObjectsByType<CircusProjectile>(FindObjectsSortMode.None).Length>0,"Ranged enemies fire projectiles");
        // Exercise projectile physics separately with a target straight ahead.
        session.Player.RestoreHealth(100); yield return new WaitForSeconds(.7f);
        var projectilePrefab=AssetDatabase.LoadAssetAtPath<CircusProjectile>("Assets/Prototype/Prefabs/RangProjectile.prefab");
        var shot=UnityEngine.Object.Instantiate(projectilePrefab,session.Player.transform.position+Vector3.up+Vector3.forward*2,Quaternion.identity);
        shot.Launch(Vector3.back,session); float hpBefore=session.Player.Health;
        yield return new WaitForSeconds(.45f);
        Check(!shot && session.Player.Health<hpBefore,"Enemy projectile hits player and damages HP");
        while(session.State==ShowState.Playing)
        {
            session.Player.RestoreHealth(100);
            foreach(var enemy in session.Enemies.ToArray())enemy.TakeDamage(1000);
            yield return new WaitForSeconds(.2f);
        }
        Check(session.State==ShowState.Victory && session.Remaining==0 && session.Score==2200,"Two rounds complete and victory score is correct");
        Click("다시 공연"); yield return null;
        Check(session.Alive==0 && session.Spawned==0 && session.Round==1,"Restart resets enemies and round counters");
        session.ReturnToMenu(); yield return null;
        Check(UnityEngine.Object.FindObjectsByType<CircusProjectile>(FindObjectsSortMode.None).Length==0,"Menu return cleans projectiles");
        results.Add(results.Any(s=>s.StartsWith("FAIL"))?"RESULT: FAILED":"RESULT: ALL PASSED");
        File.WriteAllLines(Report,results);
        InputSystem.settings.backgroundBehavior=savedBackground;
        InputSystem.settings.editorInputBehaviorInPlayMode=savedEditorInput;
    }
}
