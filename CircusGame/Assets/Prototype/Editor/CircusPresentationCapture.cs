using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using CircusPrototype;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;
public static class CircusPresentationCapture
{
    static readonly BindingFlags F=BindingFlags.NonPublic|BindingFlags.Instance;
    static CircusSession session;static CircusCamera rig;static int frame;
    static void Set(object o,string field,object value){o.GetType().GetField(field,F).SetValue(o,value);}
    static void Move(Transform t,Vector3 p){var c=t.GetComponent<CharacterController>();if(c)c.enabled=false;t.position=p;if(c)c.enabled=true;Physics.SyncTransforms();}
    static CircusEnemy Enemy(string name,Vector3 p){var e=Object.Instantiate(AssetDatabase.LoadAssetAtPath<CircusEnemy>("Assets/Prototype/Prefabs/"+name+".prefab"),p,Quaternion.identity);e.Bind(session);return e;}
    public static string Record(){session=Object.FindFirstObjectByType<CircusSession>();rig=Object.FindFirstObjectByType<CircusCamera>();session.StartCoroutine(Capture());return "Recording three real Play Mode attack sequences";}
    static IEnumerator Frames(string name,int count,Action<int> action=null)
    {
        string path="Temp/Presentation/"+name;Directory.CreateDirectory(path);
        var small=new Texture2D(960,540,TextureFormat.RGB24,false);var rt=new RenderTexture(960,540,0);
        for(int i=0;i<count;i++){
            action?.Invoke(i);yield return new WaitForEndOfFrame();
            var original=ScreenCapture.CaptureScreenshotAsTexture();var previous=RenderTexture.active;
            Graphics.Blit(original,rt);RenderTexture.active=rt;small.ReadPixels(new Rect(0,0,960,540),0,0);small.Apply();RenderTexture.active=previous;
            File.WriteAllBytes(path+"/"+i.ToString("D3")+".png",small.EncodeToPNG());Object.Destroy(original);frame++;
        }
        rt.Release();Object.Destroy(rt);Object.Destroy(small);
    }
    static IEnumerator Capture()
    {
        Application.runInBackground=true;EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor")).Focus();
        session.BeginShow();yield return new WaitForSeconds(3.7f);session.StopCoroutine((Coroutine)typeof(CircusSession).GetField("roundRoutine",F).GetValue(session));
        foreach(var e in session.Enemies.ToArray())if(e){session.EnemyDefeated(e);Object.Destroy(e.gameObject);}yield return null;
        rig.SetFirstPerson(false);Set(rig,"yaw",0f);Set(rig,"pitch",12f);Move(session.Player.transform,new Vector3(0,.06f,0));
        var close=Enemy("CloseRang",new Vector3(.5f,.06f,2));Set(close,"moveSpeed",0f);Set(close,"nextAttack",float.MaxValue);
        Time.captureFramerate=30;yield return Frames("player-melee",30,i=>{if(i==4)session.Player.Attack();});Object.Destroy(close.gameObject);yield return null;
        var ranged=Enemy("RangedRang",new Vector3(1.6f,.06f,5));Set(ranged,"nextAttack",Time.time+.15f);
        yield return Frames("enemy-ranged",42);Object.Destroy(ranged.gameObject);
        foreach(var p in Object.FindObjectsByType<CircusProjectile>(FindObjectsSortMode.None))Object.Destroy(p.gameObject);yield return null;
        Move(session.Player.transform,new Vector3(0,.06f,1.7f));session.Player.RestoreHealth(100);var owner=Enemy("DokkaebiBoss",new Vector3(0,.06f,5));var boss=owner.GetComponent<CircusBoss>();Set(boss,"nextGround",float.MaxValue);
        while(boss.IsInvulnerable)yield return null;
        bool began=false;while(!began){began=boss.TryStartPattern(BossPattern.Slam);yield return null;}
        yield return Frames("boss-slam",84);Time.captureFramerate=0;Object.Destroy(owner.gameObject);session.ReturnToMenu();
        File.WriteAllText("Temp/Presentation/recording.txt","COMPLETE "+frame+" actual game frames at 30 fps");
    }
    public static string Balance(bool moving=false){session=Object.FindFirstObjectByType<CircusSession>();rig=Object.FindFirstObjectByType<CircusCamera>();session.StartCoroutine(Probe(moving));return "Real projectile-only boss balance probe started";}
    static IEnumerator Probe(bool moving)
    {
        session.BeginShow();yield return new WaitForSeconds(3.7f);session.StopCoroutine((Coroutine)typeof(CircusSession).GetField("roundRoutine",F).GetValue(session));
        foreach(var e in session.Enemies.ToArray())if(e){session.EnemyDefeated(e);Object.Destroy(e.gameObject);}yield return null;
        Move(session.Player.transform,new Vector3(0,.06f,-2));var owner=Enemy("DokkaebiBoss",new Vector3(0,.06f,5));var boss=owner.GetComponent<CircusBoss>();
        while(boss.IsInvulnerable)yield return null;
        float start=Time.time,end=start+125;int shots=0;rig.SetFirstPerson(true);rig.enabled=false;var camera=Camera.main;var cc=session.Player.GetComponent<CharacterController>();Directory.CreateDirectory("Temp/Presentation");
        while(owner && owner.Health>0 && Time.time<end){
            session.Player.RestoreHealth(100);
            var d=owner.transform.position-session.Player.transform.position;d.y=0;
            Set(rig,"yaw",Mathf.Atan2(d.x,d.z)*Mathf.Rad2Deg);Set(rig,"pitch",0f);
            if(moving){var travel=d.normalized*Mathf.Clamp(d.magnitude-6,-1.5f,1.5f)+Vector3.Cross(Vector3.up,d.normalized)*1.2f;cc.Move(travel*Time.deltaTime);}
            camera.transform.position=session.Player.transform.position+Vector3.up*1.6f;camera.transform.LookAt(owner.transform.position+Vector3.up);
            // Aim at the visible boss collider through the actual camera/raycast/projectile path.
            if(session.Player.Fire())shots++;yield return null;
        }
        File.WriteAllText("Temp/Presentation/balance"+(moving?"-moving":"-aimed")+".txt",$"Boss HP: {owner.MaxHealth}\nActual elapsed combat seconds: {Time.time-start:F2}\nPlayer projectiles fired: {shots}\nBoss HP remaining: {owner.Health}\nPhase transitions: {boss.PhaseTransitions}\nPattern history: {string.Join(",",boss.PatternHistory)}\nImmediate repeats: {boss.PatternHistory.Zip(boss.PatternHistory.Skip(1),(a,b)=>a==b).Count(v=>v)}\nUltimate executions: {boss.UltimateExecutions}\nMoving: {moving}. Actual aimed projectiles; player healed solely to prevent test interruption. No melee input.\n");
        rig.enabled=true;if(owner)Object.Destroy(owner.gameObject);session.ReturnToMenu();
    }
}
