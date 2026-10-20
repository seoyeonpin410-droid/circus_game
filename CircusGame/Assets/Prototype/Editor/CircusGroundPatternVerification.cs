using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CircusPrototype;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;
public static class CircusGroundPatternVerification
{
    static readonly List<string> report=new List<string>();
    static void Check(bool ok,string message) { report.Add((ok?"PASS ":"FAIL ")+message);File.WriteAllLines("Temp/ground-pattern-verification.txt",report); }
    static void Move(CircusPlayer p,Vector3 position) { var c=p.GetComponent<CharacterController>();c.enabled=false;p.transform.position=position;c.enabled=true;Physics.SyncTransforms(); }
    public static string Run() { report.Clear();var s=Object.FindFirstObjectByType<CircusSession>();s.StartCoroutine(Verify(s));return "Ground pattern verification started"; }
    static IEnumerator Verify(CircusSession session)
    {
        var audio=Object.FindFirstObjectByType<CircusAudio>();var clips=new SerializedObject(audio).FindProperty("clips");
        bool configured=clips.arraySize==System.Enum.GetValues(typeof(CircusCue)).Length;
        for(int i=0;i<clips.arraySize;i++) if(i!=(int)CircusCue.FlameFlight) configured &= clips.GetArrayElementAtIndex(i).objectReferenceValue!=null;
        Check(configured,"Role-specific sound cues assigned; flight loop intentionally silent");
        session.BeginShow();yield return new WaitForSeconds(3.2f);
        var roundRoutine=typeof(CircusSession).GetField("roundRoutine",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(session) as Coroutine;
        if(roundRoutine!=null)session.StopCoroutine(roundRoutine);
        Move(session.Player,new Vector3(0,.06f,-2));
        var prefab=AssetDatabase.LoadAssetAtPath<CircusEnemy>("Assets/Prototype/Prefabs/DokkaebiBoss.prefab");
        var enemy=Object.Instantiate(prefab,new Vector3(0,.06f,6),Quaternion.identity);enemy.Bind(session);
        var boss=enemy.GetComponent<CircusBoss>();
        typeof(CircusBoss).GetField("nextGround",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).SetValue(boss,0f);
        float deadline=Time.realtimeSinceStartup+10;
        while(Object.FindObjectsByType<CircusGroundFlame>(FindObjectsSortMode.None).Length==0 && Time.realtimeSinceStartup<deadline)yield return null;
        var first=Object.FindFirstObjectByType<CircusGroundFlame>();
        Check(first && boss.Action==BossAction.GroundCast,"Boss selects ground-cast state and spawns first seal");
        if(!first)yield break;
        var center=first.Center;float before=session.Player.Health;
        yield return new WaitForSeconds(.25f);
        Check(first.Phase==GroundFlamePhase.Warning && session.Player.Health==before,"Purple warning does not damage player");
        Move(session.Player,center+Vector3.right*3);
        yield return new WaitForSeconds(.95f);
        Check(first && first.Phase==GroundFlamePhase.Burning,"Warning becomes burning floor after telegraph");
        Check(first.Center==center,"Marked location stays fixed after player moves");
        Check(session.Player.Health==before,"Leaving first circle avoids explosion damage");
        Check(Object.FindObjectsByType<CircusGroundFlame>(FindObjectsSortMode.None).Length==3,"Ground cast creates three delayed circles");
        Check(first.Area.Contains(center+Vector3.forward*1.79f) && !first.Area.Contains(center+Vector3.forward*1.81f),"Circle radius matches damage boundary");
        Move(session.Player,center);yield return new WaitForSeconds(.9f);
        Check(session.Player.Health<before,"Standing in burning circle receives periodic damage");
        session.Pause();var phase=first.Phase;float health=session.Player.Health;
        yield return new WaitForSecondsRealtime(.35f);
        Check(first && first.Phase==phase && session.Player.Health==health,"Pause freezes hazard lifetime and damage");
        session.Resume();yield return new WaitForSeconds(.6f);
        Check(!first,"Burning circle expires on schedule");
        enemy.TakeDamage(10000);yield return new WaitForSeconds(.1f);
        Check(Object.FindObjectsByType<CircusGroundFlame>(FindObjectsSortMode.None).Length==0,"Boss death clears remaining circles");
        var owner=Object.Instantiate(prefab,new Vector3(0,.06f,6),Quaternion.identity);owner.Bind(session);
        var hazard=CircusGroundFlame.Create(session,owner,session.Player.transform.position,1.8f,1,1.4f,18);
        session.ReturnToMenu();yield return null;yield return null;
        Check(!hazard,"Menu return removes hazards");Object.Destroy(owner.gameObject);
        report.Add(report.Any(s=>s.StartsWith("FAIL"))?"RESULT: FAILED":"RESULT: ALL PASSED");File.WriteAllLines("Temp/ground-pattern-verification.txt",report);
    }
}
