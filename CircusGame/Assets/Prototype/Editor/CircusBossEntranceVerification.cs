using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CircusPrototype;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Object=UnityEngine.Object;
public static class CircusBossEntranceVerification
{
    static readonly BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
    static readonly List<string> report=new List<string>();
    static CircusSession session;static CircusCamera camera;static bool running;
    static CircusProjectile leftover;
    static void Set(object o,string field,object value){o.GetType().GetField(field,F).SetValue(o,value);}
    static T Get<T>(object o,string field)=>(T)o.GetType().GetField(field,F).GetValue(o);
    static void Check(bool ok,string text){report.Add((ok?"PASS ":"FAIL ")+text);File.WriteAllLines("Temp/boss-entrance-verification.txt",report);}
    static IEnumerator Wait(Func<bool> condition,float seconds=12){float end=Time.realtimeSinceStartup+seconds;while(!condition() && Time.realtimeSinceStartup<end)yield return null;}
    static void Move(Transform t,Vector3 position){var cc=t.GetComponent<CharacterController>();if(cc)cc.enabled=false;t.position=position;if(cc)cc.enabled=true;Physics.SyncTransforms();}
    static IEnumerator Advance(bool firstPerson)
    {
        camera.SetFirstPerson(firstPerson);session.BeginShow();Time.timeScale=5;bool sentinel=false;
        float end=Time.realtimeSinceStartup+20;
        while(!session.Boss && Time.realtimeSinceStartup<end){
            foreach(var e in session.Enemies.ToArray())if(e && !e.Spawning)e.TakeDamage(10000);
            if(!sentinel && session.Round==2 && session.State==ShowState.Intermission){
                sentinel=true;leftover=Object.Instantiate(AssetDatabase.LoadAssetAtPath<CircusProjectile>("Assets/Prototype/Prefabs/RangProjectile.prefab"),Vector3.up*10,Quaternion.identity);leftover.Launch(Vector3.forward,session);
            }
            session.Player.RestoreHealth(100);yield return null;
        }
        Time.timeScale=1;yield return null;
    }
    public static string Run(){if(running)return "Already running";running=true;report.Clear();File.WriteAllText("Temp/boss-entrance-verification.txt","RUNNING");session=Object.FindFirstObjectByType<CircusSession>();camera=Object.FindFirstObjectByType<CircusCamera>();session.StartCoroutine(Verify());return "Boss entrance and existing attack windup verification started";}
    static IEnumerator Verify()
    {
        Application.runInBackground=true;var oldBackground=InputSystem.settings.backgroundBehavior;var oldEditor=InputSystem.settings.editorInputBehaviorInPlayMode;
        InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        var keyboard=InputSystem.AddDevice<Keyboard>();var mouse=InputSystem.AddDevice<Mouse>();
        yield return Advance(false);var owner=session.Boss;Check(owner && session.State==ShowState.BossIntro,"Real Act 2 to Act 3 flow enters the introduction");
        if(!owner)yield break;var boss=owner.GetComponent<CircusBoss>();var entry=boss.Entrance;var hud=Object.FindFirstObjectByType<CircusHud>();var music=session.GetComponent<CircusMusic>();
        Check(entry.Duration>=3 && entry.Duration<=5 && Mathf.Abs(entry.Duration-4.1f)<.01f,"Complete entrance timeline is 4.1 seconds");
        Check(!leftover && session.Enemies.Count==1,"Intro clears remaining projectiles and has exactly one boss");
        Check(Vector3.Distance(owner.transform.position,new Vector3(0,.06f,0))<.01f && Vector3.Distance(owner.transform.position,session.Player.transform.position)>=6.9f,"Boss lands at arena center with safe player spacing");
        var position=session.Player.transform.position;float gameplay=session.GameplaySeconds,hp=owner.Health;
        owner.TakeDamage(25);InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.W,Key.Space,Key.V));yield return new WaitForSeconds(.2f);
        Check(!session.Player.Attack() && !session.Player.Fire() && owner.Health==hp && session.GameplaySeconds==gameplay && Vector3.Distance(position,session.Player.transform.position)<.001f && !camera.FirstPerson,"Intro blocks movement, jump, view toggle, attacks, incoming boss damage and combat timer");
        InputSystem.QueueStateEvent(keyboard,new KeyboardState());
        Check(music.CombatVolume<.001f && music.BossVolume==0 && !hud.BossNameVisible && !hud.BossHealthVisible,"Prelude is quiet and neither name nor HP bar appears early");
        yield return Wait(()=>entry.Elapsed>=entry.QuietSeconds+.07f);
        var rest=Get<Vector3>(entry,"restPosition");float y1=owner.Model.localPosition.y;yield return new WaitForSeconds(.09f);float y2=owner.Model.localPosition.y;yield return new WaitForSeconds(.09f);float y3=owner.Model.localPosition.y;
        Check(owner.Model.gameObject.activeSelf && y1>y2 && y2>y3 && y2-y3>y1-y2,"Visible falling model accelerates rather than teleporting");
        Check(!entry.Landed && session.Player.Health==100,"Fall cannot deal combat damage");yield return Wait(()=>entry.Landed);
        Check(Mathf.Abs(owner.Model.localPosition.y-rest.y)<.001f && Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None).Any(p=>p.name=="Dust") && Object.FindFirstObjectByType<CircusBossVfx>(),"Ground contact synchronizes body position, dust and shockwave");
        var audio=session.GetComponent<CircusAudio>();var voice=Get<AudioSource>(audio,"introVoice");
        Check(voice.clip==Get<AudioClip>(audio,"bossLandingClip") && voice.isPlaying,"Landing accent starts on the impact frame");
        var baseScale=Get<Vector3>(entry,"restScale");Check(owner.Model.localScale.y<baseScale.y && owner.Model.localScale.y>=baseScale.y*.93f,"Landing squash is visible and restrained");
        yield return new WaitForSeconds(.08f);session.Pause();
        // Sample after LateUpdate has rendered the first paused frame.
        yield return null;float elapsed=entry.Elapsed;var camPos=camera.transform.position;var pose=owner.Model.localPosition;
        yield return new WaitForSecondsRealtime(.2f);Check(entry.Elapsed==elapsed && camera.transform.position==camPos && owner.Model.localPosition==pose && !voice.isPlaying,"Pause freezes cinematic timeline, camera, model and entrance sound");session.Resume();
        yield return Wait(()=>entry.NameAge>=.08f);yield return null;
        var so=new SerializedObject(hud);var name=so.FindProperty("countdownText").objectReferenceValue as TMPro.TextMeshProUGUI;var frame=so.FindProperty("countdownPanel").objectReferenceValue as GameObject;
        Check(hud.BossNameVisible && name.text=="도깨비 대장" && frame.transform.localScale.x>.8f && frame.transform.localScale.x<=1.05f,"Boss name pops in during the closeup");
        var frameRect=(RectTransform)frame.transform;
        Check(Vector3.Distance(name.rectTransform.TransformPoint(name.rectTransform.rect.center),frameRect.TransformPoint(frameRect.rect.center))<.01f,"Name stays centered inside the existing art frame");
        Check(AssetDatabase.GetAssetPath(frame.GetComponent<UnityEngine.UI.Image>().sprite)=="Assets/이미지/자산 8.png" && AssetDatabase.GetAssetPath(name.font).Contains("GowunBatang"),"Name reuses the artist frame and existing Korean font");
        var bounds=new Bounds();bool first=true;foreach(var r in owner.Model.GetComponentsInChildren<Renderer>()){if(first){bounds=r.bounds;first=false;}else bounds.Encapsulate(r.bounds);}
        var view=camera.GetComponent<Camera>();bool fits=true;
        for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2)for(int z=-1;z<=1;z+=2){var point=view.WorldToViewportPoint(bounds.center+Vector3.Scale(bounds.extents,new Vector3(x,y,z)));fits&=point.z>0 && point.x>.025f && point.x<.975f && point.y>.025f && point.y<.975f;}
        Check(fits && view.fieldOfView<60 && camera.IntroCameraActive,"Smooth closeup keeps the full boss inside the frame");
        yield return Wait(()=>entry.NameAge>=.35f);yield return null;
        Check(hud.BossHealthVisible && entry.Roared && voice.clip==Get<AudioClip>(audio,"bossRoarClip") && owner.Model.localScale.y>baseScale.y,"Name precedes HP reveal and a whole-model roar");
        Check(music.BossVolume==0,"Boss BGM remains silent through the roar preparation");
        yield return Wait(()=>entry.MusicAllowed);yield return new WaitForSeconds(.15f);Check(music.BossVolume>0 && music.CombatVolume==0,"Boss BGM fades in after the roar while the camera returns");
        yield return Wait(()=>session.IsRunning);yield return null;
        Check(entry.Complete && !camera.IntroCameraActive && owner.GetComponent<CharacterController>().enabled && !camera.FirstPerson,"Cinematic finishes before ordinary third-person combat resumes");
        Check(session.Player.Fire(),"Player attacks unlock after camera return");
        foreach(var p in Object.FindObjectsByType<CircusProjectile>(FindObjectsSortMode.None))Object.Destroy(p.gameObject);
        owner.TakeDamage(owner.Health-owner.MaxHealth*.3f);yield return null;yield return Wait(()=>boss.Phase==3 && !boss.IsInvulnerable);
        Check(Enum.GetValues(typeof(BossPattern)).Length==9,"No new boss pattern was added");
        foreach(BossPattern pattern in Enum.GetValues(typeof(BossPattern))){
            boss.ClearHazards();boss.SendMessage("HideWarnings",SendMessageOptions.DontRequireReceiver);
            Get<Dictionary<BossPattern,float>>(boss,"ready").Clear();Set(boss,"lastPattern",null);Set(boss,"followup",null);Set(boss,"comboStep",0);Set(boss,"centering",false);Set(boss,"until",0f);Set(boss,"nextGround",0f);Set(boss,"<Action>k__BackingField",BossAction.PatternSelect);
            bool close=pattern==BossPattern.Swing || pattern==BossPattern.DoubleSwing || pattern==BossPattern.Slam;
            Move(session.Player.transform,owner.transform.position+Vector3.back*(close?2.6f:6.5f));session.Player.RestoreHealth(100);Set(session.Player,"lastDamage",-100f);yield return null;
            // Own the start of this test; ordinary selection remains tested by the existing suite.
            Set(boss,"until",0f);Set(boss,"<Action>k__BackingField",BossAction.PatternSelect);Set(boss,"lastPattern",null);Get<Dictionary<BossPattern,float>>(boss,"ready").Clear();
            int attacks=boss.MeleeAttacks+boss.RangedAttacks+boss.GroundAttacks;var root=owner.transform.position;
            bool started=boss.TryStartPattern(pattern);Check(started,"Existing pattern begins with a distinct windup: "+pattern);
            if(!started)continue;
            if(pattern==BossPattern.RingFlames){yield return Wait(()=>boss.Action==BossAction.SpecialWindup);root=owner.transform.position;}
            yield return new WaitForSeconds(.2f);
            Check(owner.transform.position==root && boss.MeleeAttacks+boss.RangedAttacks+boss.GroundAttacks==attacks && (boss.Action==BossAction.MeleeWindup || boss.Action==BossAction.RangedWindup || boss.Action==BossAction.SpecialWindup || (pattern==BossPattern.RingFlames && boss.Action==BossAction.Chase)),"No attack or root movement during preparation: "+pattern);
            var offset=owner.Model.localPosition-rest;
            if(pattern==BossPattern.Swing || pattern==BossPattern.DoubleSwing)Check(Quaternion.Angle(owner.Model.rotation,Quaternion.LookRotation(boss.AttackArea.transform.forward))>5,"Melee visibly counter-twists before release");
            if(pattern==BossPattern.Slam)Check(offset.y>0 && boss.AttackArea.Visible,"Slam lifts its body above the danger circle");
            if(pattern==BossPattern.Charge)Check(offset.y<-.02f && offset.z>.02f && boss.AttackArea.Visible,"Rush crouches and pulls back behind a locked lane");
            if(pattern==BossPattern.SingleFlame || pattern==BossPattern.FanFlame || pattern==BossPattern.HomingFlames || pattern==BossPattern.RingFlames)Check(offset.z>0 && boss.Charging,"Ranged preparation pulls the body back and gathers soft flame particles");
            yield return Wait(()=>boss.Action==BossAction.Recovery,12);
            Check(boss.Action==BossAction.Recovery && !boss.TryStartPattern(BossPattern.Swing),"Attack finishes in a protected recovery interval: "+pattern);
            yield return new WaitForSeconds(.3f);
        }
        session.ReturnToMenu();yield return null;Check(Time.timeScale==1 && !camera.IntroCameraActive && name.rectTransform.anchoredPosition==Vector2.zero && name.rectTransform.localScale==Vector3.one,"Menu restores camera, countdown layout and time scale");
        yield return Advance(true);entry=session.Boss.GetComponent<CircusBoss>().Entrance;yield return Wait(()=>entry.NameAge>=.35f);
        Check(camera.FirstPerson && camera.IntroCameraActive && !Get<Transform>(camera,"firstPersonBell").gameObject.activeSelf,"First-person preference is preserved while its view-model is hidden during the cut");
        yield return Wait(()=>session.IsRunning);yield return null;Check(camera.FirstPerson && !camera.IntroCameraActive && Get<Transform>(camera,"firstPersonBell").gameObject.activeSelf,"Intro returns smoothly to the player's original first-person mode");
        session.ReturnToMenu();camera.SetFirstPerson(false);yield return null;
        Check(!Get<AudioSource>(audio,"introVoice").isPlaying,"Menu stops remaining entrance audio");
        InputSystem.RemoveDevice(keyboard);InputSystem.RemoveDevice(mouse);InputSystem.settings.backgroundBehavior=oldBackground;InputSystem.settings.editorInputBehaviorInPlayMode=oldEditor;
        report.Add(report.Any(s=>s.StartsWith("FAIL"))?"RESULT: FAILED":"RESULT: ALL PASSED");File.WriteAllLines("Temp/boss-entrance-verification.txt",report);running=false;
    }
    public static string Record(){if(running)return "Verification still running";running=true;session=Object.FindFirstObjectByType<CircusSession>();camera=Object.FindFirstObjectByType<CircusCamera>();session.StartCoroutine(RecordIntro());return "Recording real Act 3 entrance";}
    static IEnumerator RecordIntro()
    {
        EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor")).Focus();yield return Advance(false);
        string path="Temp/BossEntranceFrames";Directory.CreateDirectory(path);Time.captureFramerate=30;int index=0;
        var small=new Texture2D(960,540,TextureFormat.RGB24,false);var rt=new RenderTexture(960,540,0);
        while(session.State==ShowState.BossIntro || index<132){
            yield return new WaitForEndOfFrame();var image=ScreenCapture.CaptureScreenshotAsTexture();var previous=RenderTexture.active;
            Graphics.Blit(image,rt);RenderTexture.active=rt;small.ReadPixels(new Rect(0,0,960,540),0,0);small.Apply();RenderTexture.active=previous;
            File.WriteAllBytes(path+"/"+(index++).ToString("D3")+".png",small.EncodeToPNG());Object.Destroy(image);
            if(index>160)break;
        }
        Time.captureFramerate=0;rt.Release();Object.Destroy(rt);Object.Destroy(small);session.ReturnToMenu();File.WriteAllText(path+"/recording.txt","COMPLETE "+index+" real game frames at 30 fps");running=false;
    }
}
