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
using Object=UnityEngine.Object;
public static class CircusThreeActVerification
{
    static readonly List<string> report=new List<string>();
    const string Report="Temp/three-act-verification.txt";
    static void Check(bool value,string title) { report.Add((value ? "PASS " : "FAIL ")+title); File.WriteAllLines(Report,report); }
    public static string Run()
    {
        report.Clear(); Object.FindFirstObjectByType<CircusSession>().StartCoroutine(Verify()); return "Three act runtime verification started";
    }
    static void Move(CircusPlayer player,Vector3 position)
    { var controller=player.GetComponent<CharacterController>(); controller.enabled=false; player.transform.position=position; controller.enabled=true; Physics.SyncTransforms(); }
    static IEnumerator Verify()
    {
        var session=Object.FindFirstObjectByType<CircusSession>(); var player=session.Player;
        var rig=Object.FindFirstObjectByType<CircusCamera>();
        var oldBackground=InputSystem.settings.backgroundBehavior; var oldEditor=InputSystem.settings.editorInputBehaviorInPlayMode;
        InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        var keyboard=InputSystem.AddDevice<Keyboard>(); var mouse=InputSystem.AddDevice<Mouse>();
        var pitch=typeof(CircusCamera).GetField("pitch",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
        var oldPitch=pitch.GetValue(rig); pitch.SetValue(rig,0f);
        session.BeginShow(); yield return new WaitForSeconds(.1f);
        var start=player.transform.position;
        InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.W,Key.Space));
        Check(session.State==ShowState.Countdown && session.CountdownText=="3","Countdown begins with 3");
        Check(!player.Fire() && !player.Attack(),"Countdown blocks both attacks");
        yield return new WaitForSeconds(1);
        Check(session.CountdownText=="2","Countdown advances to 2");
        Check(Vector3.Distance(start,player.transform.position)<.01f && session.Spawned==0 && session.GameplaySeconds==0,"Countdown freezes player, spawns and gameplay timer");
        yield return new WaitForSeconds(1);
        Check(session.CountdownText=="1","Countdown advances to 1");
        session.Pause(); float remaining=session.CountdownRemaining;
        yield return new WaitForSecondsRealtime(.25f);
        Check(Mathf.Abs(session.CountdownRemaining-remaining)<.001f,"Pause freezes countdown");
        session.Resume(); InputSystem.QueueStateEvent(keyboard,new KeyboardState());
        yield return new WaitForSeconds(1);
        Check(session.IsRunning && session.CountdownText=="START!","Three seconds unlock gameplay with START");
        yield return new WaitForSeconds(.6f);
        Check(session.GameplaySeconds>0 && session.Round==1,"Act 1 timer begins after countdown");
        start=player.transform.position; InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.W));
        yield return new WaitForSeconds(.35f); InputSystem.QueueStateEvent(keyboard,new KeyboardState()); yield return null;
        Check(Vector3.Distance(start,player.transform.position)>1,"Existing camera-relative movement works");
        float grounded=player.transform.position.y; InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Space));
        yield return new WaitForSeconds(.18f); InputSystem.QueueStateEvent(keyboard,new KeyboardState());
        Check(player.transform.position.y>grounded+.4f,"Existing jump works"); yield return new WaitForSeconds(.9f);
        var closePrefab=AssetDatabase.LoadAssetAtPath<CircusEnemy>("Assets/Prototype/Prefabs/CloseRang.prefab");
        var target=Object.Instantiate(closePrefab,player.transform.position+rig.AimForward*6,Quaternion.identity); target.Bind(session);
        typeof(CircusEnemy).GetField("moveSpeed",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).SetValue(target,0f);
        Physics.SyncTransforms(); float hp=target.Health;
        var mousePosition=new Vector2(Screen.width*.5f,Screen.height*.25f);
        InputSystem.QueueStateEvent(mouse,new MouseState{position=mousePosition}); yield return null; yield return null;
        InputSystem.QueueStateEvent(mouse,new MouseState{position=mousePosition}.WithButton(MouseButton.Left)); yield return new WaitForSeconds(.1f);
        InputSystem.QueueStateEvent(mouse,new MouseState{position=mousePosition}); yield return new WaitForSeconds(.45f);
        Check(target && target.Health<hp,"Left click projectile still damages enemy");
        if(target)
        {
            target.transform.position=player.transform.position+rig.AimForward*2; Physics.SyncTransforms(); hp=target.Health;
            Check(player.Attack() && player.MeleeArea.Visible,"Melee exposes gold sector before damage");
            yield return new WaitForSeconds(.08f); Check(target.Health==hp,"Melee respects preparation delay");
            yield return new WaitForSeconds(.2f); Check(!target || target.Health<hp,"Melee hits after preparation");
            if(target) Object.Destroy(target.gameObject);
        }
        var testArea=CircusAttackArea.Create("Verification sector",null);
        testArea.Show(Vector3.zero,Vector3.forward,3.6f,Mathf.Acos(.25f)*Mathf.Rad2Deg,Color.yellow);
        Check(testArea.Contains(Vector3.forward*3.59f) && !testArea.Contains(Vector3.forward*3.61f) && !testArea.Contains(Vector3.back),"Displayed melee radius and direction match damage predicate");
        Object.Destroy(testArea.gameObject);
        rig.SetFirstPerson(true); yield return null; Check(rig.FirstPerson && player.Fire(),"First person camera and projectile preserved");
        rig.SetFirstPerson(false);
        int firstCount=0; float deadline=Time.realtimeSinceStartup+30;
        while(session.Round==1 && session.State!=ShowState.Intermission && Time.realtimeSinceStartup<deadline)
        {
            foreach(var enemy in session.Enemies.ToArray()) { if(enemy.Spawning)continue; Check(enemy.MaxHealth==50,"Act 1 base enemy health"); firstCount++; enemy.TakeDamage(10000); }
            player.RestoreHealth(100); yield return new WaitForSeconds(.1f);
        }
        Check(firstCount==8 && session.State==ShowState.Intermission,"Eight Act 1 enemies lead to intermission");
        float elapsed=session.GameplaySeconds; yield return new WaitForSeconds(.2f);
        Check(session.GameplaySeconds==elapsed && !player.Attack(),"Intermission freezes combat timer and attacks");
        deadline=Time.realtimeSinceStartup+10;
        while(session.Round<2 && Time.realtimeSinceStartup<deadline) yield return null;
        bool sawRanged=false; int secondCount=0; deadline=Time.realtimeSinceStartup+30;
        while(session.Round==2 && session.State!=ShowState.Intermission && Time.realtimeSinceStartup<deadline)
        {
            foreach(var enemy in session.Enemies.ToArray())
            { if(enemy.Spawning)continue; sawRanged|=enemy.IsRanged; Check(enemy.MaxHealth==70,"Act 2 increases enemy health"); secondCount++; enemy.TakeDamage(10000); }
            player.RestoreHealth(100); yield return new WaitForSeconds(.1f);
        }
        Check(secondCount==12 && sawRanged,"Act 2 has twelve enemies including ranged type");
        deadline=Time.realtimeSinceStartup+10;
        while(!session.Boss && Time.realtimeSinceStartup<deadline) yield return null;
        var boss=session.Boss;
        Check(session.Round==3 && boss && session.Enemies.Count==1 && boss.MaxHealth==650,"Act 3 spawns exactly one high-health boss");
        if(boss)
        {
            Check(session.State==ShowState.BossIntro && !player.Fire() && !player.Attack(),"Boss entrance blocks both player attacks");
            float introHp=boss.Health;boss.TakeDamage(25);float introTime=session.GameplaySeconds;
            var introPosition=boss.transform.position;
            yield return new WaitForSeconds(1);
            Check(boss.Health==introHp && boss.transform.position==introPosition && session.GameplaySeconds==introTime,"Boss entrance freezes combat and gameplay timer");
            session.Pause();yield return new WaitForSecondsRealtime(.2f);session.Resume();
            Check(session.State==ShowState.BossIntro,"Pause resumes boss entrance state");
            while(!session.IsRunning)yield return null;
            var ai=boss.GetComponent<CircusBoss>();
            Move(player,new Vector3(0,.06f,0)); player.RestoreHealth(100);
            deadline=Time.realtimeSinceStartup+8; bool warning=false;
            while(ai.RangedAttacks==0 && Time.realtimeSinceStartup<deadline)
            { warning|=ai.Action==BossAction.RangedWindup; yield return null; }
            Check(warning && ai.RangedAttacks>0,"Boss warns then selects ranged attack at distance");
            Check(Object.FindObjectsByType<CircusProjectile>(FindObjectsSortMode.None).Any(p=>!p.IsPlayerShot),"Boss creates visible enemy projectile");
            var hud=Object.FindFirstObjectByType<CircusHud>(); var hudSo=new SerializedObject(hud);
            var bossPanel=hudSo.FindProperty("bossPanel").objectReferenceValue as GameObject;
            Check(bossPanel.activeInHierarchy,"Boss health panel appears in Act 3");
            Move(player,boss.transform.position+Vector3.back*2.6f); player.RestoreHealth(100);
            deadline=Time.realtimeSinceStartup+8;
            while(ai.Action!=BossAction.MeleeWindup && Time.realtimeSinceStartup<deadline) yield return null;
            Check(ai.AttackArea.Visible,"Boss melee displays danger sector before hit");
            float before=player.Health;
            Move(player,boss.transform.position+ai.AttackArea.transform.forward*4.4f);
            int previous=ai.MeleeAttacks; yield return new WaitForSeconds(.8f);
            Check(ai.MeleeAttacks>previous && player.Health==before,"Leaving boss sector avoids melee damage");
            Move(player,boss.transform.position+Vector3.back*2.6f);
            deadline=Time.realtimeSinceStartup+8;
            while(ai.Action!=BossAction.MeleeWindup && Time.realtimeSinceStartup<deadline) yield return null;
            session.Pause(); var action=ai.Action; yield return new WaitForSecondsRealtime(.25f);
            Check(ai.Action==action,"Pause freezes boss windup"); session.Resume();
            player.RestoreHealth(100); yield return new WaitForSeconds(.8f);
            Check(player.Health<100,"Standing in boss sector receives melee damage");
            boss.TakeDamage(25); yield return null;
            var fill=hudSo.FindProperty("bossHealthFill").objectReferenceValue as UnityEngine.UI.Image;
            Check(Mathf.Abs(fill.fillAmount-boss.Health/boss.MaxHealth)<.01f,"Boss health UI tracks actual damage");
            boss.TakeDamage(10000); yield return new WaitForSeconds(.2f);
            Check(session.State==ShowState.Victory && session.Score==3400,"Boss death completes Act 3 and clear score");
        }
        session.BeginShow(); yield return null;
        Check(session.State==ShowState.Countdown && session.Alive==0 && !session.Boss && player.Health==100 && session.Round==1,"Retry resets boss and begins new countdown");
        session.ReturnToMenu(); yield return null;
        Check(Object.FindObjectsByType<CircusProjectile>(FindObjectsSortMode.None).Length==0 && Time.timeScale==1,"Menu clears projectiles and restores time");
        pitch.SetValue(rig,oldPitch); rig.SetFirstPerson(false);
        InputSystem.RemoveDevice(keyboard); InputSystem.RemoveDevice(mouse);
        InputSystem.settings.backgroundBehavior=oldBackground; InputSystem.settings.editorInputBehaviorInPlayMode=oldEditor;
        report.Add(report.Any(s=>s.StartsWith("FAIL")) ? "RESULT: FAILED" : "RESULT: ALL PASSED"); File.WriteAllLines(Report,report);
    }
}
