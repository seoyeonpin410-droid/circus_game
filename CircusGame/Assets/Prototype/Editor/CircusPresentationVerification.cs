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
public static class CircusPresentationVerification
{
    static readonly BindingFlags Flags=BindingFlags.NonPublic|BindingFlags.Instance;
    static readonly List<string> results=new List<string>();
    static CircusSession session;static bool running;
    static void Set(object obj,string field,object value){obj.GetType().GetField(field,Flags).SetValue(obj,value);}
    static void Check(bool ok,string message){results.Add((ok?"PASS ":"FAIL ")+message);File.WriteAllLines("Temp/presentation-verification.txt",results);}
    static void Move(Transform t,Vector3 p){var c=t.GetComponent<CharacterController>();if(c)c.enabled=false;t.position=p;if(c)c.enabled=true;Physics.SyncTransforms();}
    static IEnumerator Wait(Func<bool> done,float seconds=6){float end=Time.realtimeSinceStartup+seconds;while(!done() && Time.realtimeSinceStartup<end)yield return null;}
    static CircusEnemy Enemy(string name,Vector3 p){var e=Object.Instantiate(AssetDatabase.LoadAssetAtPath<CircusEnemy>("Assets/Prototype/Prefabs/"+name+".prefab"),p,Quaternion.identity);e.Bind(session);return e;}
    public static string Run(){if(running)return "Already running";running=true;results.Clear();File.WriteAllText("Temp/presentation-verification.txt","RUNNING");session=Object.FindFirstObjectByType<CircusSession>();session.StartCoroutine(Verify());return "Presentation verification started";}
    static IEnumerator Verify()
    {
        Application.runInBackground=true;Time.timeScale=1;session.BeginShow();yield return new WaitForSeconds(3.7f);
        session.StopCoroutine((Coroutine)typeof(CircusSession).GetField("roundRoutine",Flags).GetValue(session));
        foreach(var e in session.Enemies.ToArray())if(e){session.EnemyDefeated(e);Object.Destroy(e.gameObject);}yield return null;
        var player=session.Player;var rig=Object.FindFirstObjectByType<CircusCamera>();rig.SetFirstPerson(false);Set(rig,"yaw",0f);Set(rig,"pitch",12f);
        Move(player.transform,new Vector3(0,.06f,0));yield return null;
        int stops=session.HitStopCount;Check(player.Attack() && !player.MeleeArea.Visible,"Player yellow sector is never rendered");
        yield return new WaitForSeconds(.06f);
        var visual=(Transform)typeof(CircusPlayer).GetField("visual",Flags).GetValue(player);
        Check(Quaternion.Angle(visual.rotation,Quaternion.LookRotation(Vector3.forward))>2,"Melee has a small counter-rotation before impact");
        yield return new WaitForSeconds(.075f);Check(Object.FindFirstObjectByType<CircusSlash>(),"A short moving slash replaces the floor sector");
        yield return new WaitForSeconds(.35f);Check(session.HitStopCount==stops && !session.HitStopActive,"Miss produces no hit stop");
        Check(Quaternion.Angle(visual.rotation,Quaternion.LookRotation(rig.AimForward))<.5f,"Player returns to the resting orientation");
        var close=Enemy("CloseRang",new Vector3(0,.06f,2));Set(close,"moveSpeed",0f);Set(close,"nextAttack",float.MaxValue);yield return new WaitForSeconds(.2f);
        float hp=close.Health;Check(player.Attack(),"Next melee accepted after cooldown");
        yield return Wait(()=>session.HitStopActive,2);
        Check(close.Health==hp-25 && session.HitStopActive && Time.timeScale==0,"Real hit deals existing normal damage and starts 40 ms hit stop");
        Check(close.Flashing && Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None).Any(p=>p.particleCount>0),"Real hit flashes the artist model and emits particles");
        session.Pause();yield return new WaitForSecondsRealtime(.08f);Check(Time.timeScale==0 && session.State==ShowState.Paused,"Pause during hit stop stays paused");session.Resume();yield return null;
        Check(Time.timeScale==1 && !session.HitStopActive,"Resume restores normal simulation");Object.Destroy(close.gameObject);yield return null;
        var ranged=Enemy("RangedRang",new Vector3(0,.06f,7));var rest=ranged.Model.localPosition;Set(ranged,"nextAttack",Time.time);yield return Wait(()=>ranged.Winding);
        yield return new WaitForSeconds(.18f);Check(ranged.Winding && Vector3.Distance(rest,ranged.Model.localPosition)>.025f,"Ranged enemy pulls back during preparation");
        Check(Object.FindObjectsByType<CircusProjectile>(FindObjectsSortMode.None).Length==0,"No projectile before ranged windup finishes");
        yield return Wait(()=>Object.FindObjectsByType<CircusProjectile>(FindObjectsSortMode.None).Length>0);
        yield return null;var shot=Object.FindFirstObjectByType<CircusProjectile>();
        Check(shot && shot.GetComponent<CircusProjectileVfx>() && !shot.GetComponent<MeshRenderer>().enabled && !shot.GetComponent<TrailRenderer>().enabled,"Enemy projectile hides sphere and triangular trail");
        Check(shot && shot.GetComponentsInChildren<ParticleSystem>().Length==2,"Enemy projectile uses a soft particle core and short wake");
        yield return new WaitForSeconds(.07f);Check(Vector3.Distance(rest,ranged.Model.localPosition)>.015f,"Release has a visible small recoil");
        yield return new WaitForSeconds(.24f);Check(Vector3.Distance(rest,ranged.Model.localPosition)<.005f,"Ranged enemy settles after release");
        Object.Destroy(ranged.gameObject);foreach(var p in Object.FindObjectsByType<CircusProjectile>(FindObjectsSortMode.None))Object.Destroy(p.gameObject);yield return null;
        Move(player.transform,new Vector3(0,.06f,2.5f));player.RestoreHealth(100);var owner=Enemy("DokkaebiBoss",new Vector3(0,.06f,5));var boss=owner.GetComponent<CircusBoss>();
        Set(boss,"nextGround",float.MaxValue);yield return Wait(()=>!boss.IsInvulnerable);
        bool selected=false;yield return Wait(()=>selected=boss.TryStartPattern(BossPattern.Slam));Check(selected,"Existing Slam pattern selected without adding patterns");
        rest=(Vector3)typeof(CircusBoss).GetField("modelRest",Flags).GetValue(boss);hp=player.Health;
        yield return new WaitForSeconds(.55f);Check(owner.Model.localPosition.y>rest.y+.2f && player.Health==hp && boss.AttackArea.Visible,"Slam raises body and shows danger circle without early damage");
        yield return Wait(()=>boss.Action==BossAction.SpecialAttack,3);Check(player.Health==hp && !boss.SlamContact,"Windup ending does not apply damage before the drop");
        yield return Wait(()=>boss.SlamContact,2);
        Check(player.Health==hp-boss.Settings(BossPattern.Slam).damage && Mathf.Abs(owner.Model.localPosition.y-rest.y)<.001f,"Slam damage occurs exactly at ground contact");
        Check(!boss.AttackArea.Visible && Object.FindFirstObjectByType<CircusBossVfx>() && Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None).Any(p=>p.name=="Dust"),"Ground contact removes warning and creates shockwave and dust");
        Check(boss.Action==BossAction.Recovery && !boss.TryStartPattern(BossPattern.Swing),"Ground contact opens a long recovery instead of chaining an attack");
        yield return new WaitForSeconds(.8f);Check(boss.Action==BossAction.Recovery,"Slam leaves a visible counterattack window");
        Check(owner.MaxHealth==1000 && boss.Settings(BossPattern.Slam).damage==22 && player.BossDamageScale==.4f,"Serialized balance protects both sides from a few lethal hits");
        var audio=new SerializedObject(session.GetComponent<CircusAudio>());
        Check(!audio.FindProperty("effectsEnabled").boolValue && audio.FindProperty("combatAccentsEnabled").boolValue && audio.FindProperty("meleeImpactClip").objectReferenceValue && audio.FindProperty("slamImpactClip").objectReferenceValue,"Only two quiet combat accents enabled; previous effects remain disabled");
        Object.Destroy(owner.gameObject);session.ReturnToMenu();yield return null;
        Check(Time.timeScale==1,"Returning to menu leaves no frozen time scale");
        results.Add(results.Any(s=>s.StartsWith("FAIL"))?"RESULT: FAILED":"RESULT: ALL PASSED");File.WriteAllLines("Temp/presentation-verification.txt",results);running=false;
    }
}
