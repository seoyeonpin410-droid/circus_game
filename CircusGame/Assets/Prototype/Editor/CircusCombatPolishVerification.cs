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
public static class CircusCombatPolishVerification
{
    static readonly List<string> report=new List<string>();
    static void Check(bool ok,string label) { report.Add((ok?"PASS ":"FAIL ")+label);File.WriteAllLines("Temp/combat-polish-verification.txt",report); }
    static void Set(object owner,string field,object value) { owner.GetType().GetField(field,BindingFlags.NonPublic|BindingFlags.Instance).SetValue(owner,value); }
    static void Move(Transform t,Vector3 position) { var cc=t.GetComponent<CharacterController>();bool enabled=cc && cc.enabled;if(enabled)cc.enabled=false;t.position=position;if(enabled)cc.enabled=true;Physics.SyncTransforms(); }
    static IEnumerator Until(Func<bool> condition,float seconds=8)
    { float end=Time.realtimeSinceStartup+seconds;while(!condition() && Time.realtimeSinceStartup<end)yield return null; }
    public static string Run() { report.Clear();Object.FindFirstObjectByType<CircusSession>().StartCoroutine(Verify());return "Combat polish verification started"; }
    static IEnumerator Verify()
    {
        var s=Object.FindFirstObjectByType<CircusSession>();s.BeginShow();yield return new WaitForSeconds(3.6f);
        var routine=typeof(CircusSession).GetField("roundRoutine",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(s) as Coroutine;
        s.StopCoroutine(routine);
        var spawned=s.SpawnNext();Check(spawned && spawned.Spawning,"Enemy reserves spawn with a visible warning before activation");
        if(spawned)
        {
            float hp=spawned.Health;spawned.TakeDamage(20);
            Check(spawned.Health==hp && !spawned.GetComponent<CharacterController>().enabled && spawned.Model.GetComponentsInChildren<Renderer>().All(r=>!r.enabled),"Spawn warning has no visible enemy, collision, or damage");
            s.Pause();yield return new WaitForSecondsRealtime(.25f);Check(spawned.Spawning,"Pause freezes spawn warning");s.Resume();
            yield return new WaitForSeconds(.6f);
            Check(!spawned.Spawning && spawned.GetComponent<CharacterController>().enabled,"Enemy activates after half-second warning");
            var hpFill=new SerializedObject(spawned).FindProperty("healthFill").objectReferenceValue as UnityEngine.UI.Image;
            Check(hpFill && hpFill.gameObject.activeInHierarchy,"Enemy health bar reappears after spawn warning");
            spawned.TakeDamage(1);Check(spawned.Flashing,"Real damage triggers hit flash");
            Check(spawned.Model.GetComponentsInChildren<Renderer>().All(r=>r.sharedMaterials.All(m=>!m.HasProperty("_EmissionColor") || m.IsKeywordEnabled("_EMISSION"))),"Imported artist material supports visible emission flash at runtime");
            Check(Object.FindFirstObjectByType<CircusImpact>(),"Real damage creates a small hit effect");
            yield return new WaitForSeconds(.15f);Check(!spawned.Flashing,"Hit flash ends quickly");
        }
        yield return new WaitForSeconds(.6f);
        foreach(var e in s.Enemies.ToArray())e.TakeDamage(10000);
        Move(s.Player.transform,new Vector3(0,.06f,-3));
        var enemy=Object.Instantiate(AssetDatabase.LoadAssetAtPath<CircusEnemy>("Assets/Prototype/Prefabs/DokkaebiBoss.prefab"),new Vector3(0,.06f,5),Quaternion.identity);
        enemy.Bind(s);var ai=enemy.GetComponent<CircusBoss>();Set(ai,"nextGround",float.MaxValue);
        yield return Until(()=>ai.Action==BossAction.RangedWindup);
        Check(ai.Action==BossAction.RangedWindup && ai.Charging,"Far distance selects ranged windup with a visible charge orb");
        int shots=ai.RangedAttacks;yield return new WaitForSeconds(.3f);
        Check(ai.RangedAttacks==shots,"Ranged windup gives reaction time before spawning projectiles");
        Move(s.Player.transform,s.Player.transform.position+Vector3.right*3);
        yield return Until(()=>ai.RangedAttacks>shots);
        Check(ai.LastVolleySize==1 && !ai.Charging,"Phase 1 fires one projectile and clears charge visual");
        yield return new WaitForSeconds(.16f);
        Check(ai.Action==BossAction.Recovery,"Ranged attack enters recovery instead of chaining immediately");
        Move(s.Player.transform,enemy.transform.position+Vector3.back*2.6f);s.Player.RestoreHealth(100);
        yield return Until(()=>ai.Action==BossAction.MeleeWindup);
        Check(ai.AttackArea.Visible && ai.AttackArea.Contains(s.Player.transform.position),"Close distance selects melee with matching visible danger sector");
        float health=s.Player.Health;int melee=ai.MeleeAttacks;
        yield return new WaitForSeconds(.4f);Check(ai.MeleeAttacks==melee && s.Player.Health==health,"Boss melee gives at least 0.5 seconds to react");
        Move(s.Player.transform,enemy.transform.position+ai.AttackArea.transform.forward*4.5f);
        yield return Until(()=>ai.MeleeAttacks>melee);
        Check(s.Player.Health==health,"Exiting actual telegraph boundary avoids melee damage");
        yield return new WaitForSeconds(.3f);Check(ai.Action==BossAction.Recovery,"Melee attack exposes a punishable recovery window");
        enemy.TakeDamage(enemy.Health-enemy.MaxHealth*.5f);yield return null;yield return null;
        Check(ai.Phase==2 && ai.Action==BossAction.PhaseTransition,"Crossing 50 percent health triggers phase transition once");
        int before=ai.MeleeAttacks+ai.RangedAttacks;yield return new WaitForSeconds(.5f);
        Check(ai.MeleeAttacks+ai.RangedAttacks==before,"Phase transition does not attack");
        s.Pause();yield return new WaitForSecondsRealtime(.25f);Check(ai.Action==BossAction.PhaseTransition,"Pause freezes phase transition");s.Resume();
        Move(s.Player.transform,enemy.transform.position+Vector3.back*8);s.Player.RestoreHealth(100);
        shots=ai.RangedAttacks;yield return Until(()=>ai.RangedAttacks>shots);
        Check(ai.LastVolleySize==3,"Phase 2 upgrades the existing shot to a three-way volley");
        var volley=Object.FindObjectsByType<CircusProjectile>(FindObjectsSortMode.None).Where(p=>!p.IsPlayerShot).ToArray();
        Check(volley.Length>=3,"Three-way volley creates three real, visible projectiles");
        Move(s.Player.transform,enemy.transform.position+Vector3.back*2.6f);s.Player.RestoreHealth(100);
        yield return Until(()=>ai.Action==BossAction.MeleeWindup);melee=ai.MeleeAttacks;
        yield return Until(()=>ai.MeleeAttacks>melee);
        yield return Until(()=>ai.Action==BossAction.MeleeWindup,3);
        Check(ai.Action==BossAction.MeleeWindup && ai.AttackArea.Visible,"Second combo swing has its own telegraph after a short recovery");
        float comboStart=Time.time;yield return Until(()=>ai.MeleeAttacks>=melee+2,3);
        Check(ai.MeleeAttacks==melee+2 && Time.time-comboStart>=.5f,"Phase 2 second melee hit respects its preparation delay");
        yield return new WaitForSeconds(.4f);Check(ai.Action==BossAction.Recovery,"Two-hit combo ends in recovery");
        Check(ai.Phase==2,"Phase two remains active without repeating transition");
        var audio=new SerializedObject(Object.FindFirstObjectByType<CircusAudio>());var clips=audio.FindProperty("clips");
        Check(clips.arraySize==23 && !clips.GetArrayElementAtIndex((int)CircusCue.FlameFlight).objectReferenceValue,"Audio bank has explicit roles and no constant flight loop");
        Check(AssetDatabase.GetAssetPath(clips.GetArrayElementAtIndex((int)CircusCue.PlayerMelee).objectReferenceValue).EndsWith("knifeSlice.ogg"),"Player melee uses a plain CC0 swing without bell layering");
        Check(audio.FindProperty("cueVolumes").GetArrayElementAtIndex((int)CircusCue.PlayerHit).floatValue>audio.FindProperty("cueVolumes").GetArrayElementAtIndex((int)CircusCue.EnemyHit).floatValue,"Player damage sound has priority over crowd hit sounds");
        enemy.TakeDamage(10000);yield return null;s.ReturnToMenu();yield return null;
        Check(!Object.FindFirstObjectByType<CircusCharge>() && !Object.FindFirstObjectByType<CircusGroundFlame>(),"Death/menu clean up charge and hazard objects");
        report.Add(report.Any(x=>x.StartsWith("FAIL"))?"RESULT: FAILED":"RESULT: ALL PASSED");File.WriteAllLines("Temp/combat-polish-verification.txt",report);
    }
}
