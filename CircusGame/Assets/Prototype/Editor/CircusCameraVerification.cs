using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CircusPrototype;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public static class CircusCameraVerification
{
    static readonly List<string> report = new List<string>();
    static void Check(bool value,string description)
    {
        report.Add((value ? "PASS " : "FAIL ")+description);
        File.WriteAllLines("Temp/camera-verification.txt",report);
    }
    public static string Run()
    {
        report.Clear();
        var session=Object.FindFirstObjectByType<CircusSession>();
        session.StartCoroutine(Verify(session));
        return "Camera verification started";
    }
    static IEnumerator Verify(CircusSession session)
    {
        var rig=Object.FindFirstObjectByType<CircusCamera>(); var cam=rig.GetComponent<Camera>();
        var oldBackground=InputSystem.settings.backgroundBehavior;
        var oldEditor=InputSystem.settings.editorInputBehaviorInPlayMode;
        InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        var keyboard=InputSystem.AddDevice<Keyboard>();
        session.BeginShow(); yield return new WaitForSeconds(.2f);
        Check(!rig.FirstPerson && !cam.orthographic,"Default perspective third person");
        Check(Vector3.Distance(cam.transform.position,session.Player.transform.position)>4,"Camera follows behind character");
        var position=session.Player.transform.position;
        InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.W)); yield return new WaitForSeconds(.4f);
        InputSystem.QueueStateEvent(keyboard,new KeyboardState()); yield return null;
        Check(Vector3.Distance(position,session.Player.transform.position)>1.5f,"Third person camera-relative movement");
        InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.V)); yield return null; yield return null;
        Check(rig.FirstPerson,"V key switches to first person");
        InputSystem.QueueStateEvent(keyboard,new KeyboardState()); yield return null;
        Check(Vector3.Distance(cam.transform.position,session.Player.transform.position+Vector3.up*1.55f)<.05f,"First person at eye position");
        var visual=session.Player.transform.Find("Artist Character");
        Check(visual.GetComponentsInChildren<Renderer>().All(r=>!r.enabled),"Body hidden in first person");
        Check(rig.transform.Find("First Person Bell").gameObject.activeSelf,"First person bell visible");
        position=session.Player.transform.position;
        InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.D)); yield return new WaitForSeconds(.4f);
        InputSystem.QueueStateEvent(keyboard,new KeyboardState()); yield return null;
        Check(Vector3.Distance(position,session.Player.transform.position)>1.5f,"First person movement");
        Check(session.Player.Attack(),"Attack still available in first person");
        session.Pause(); yield return null;
        Check(Cursor.lockState==CursorLockMode.None && Cursor.visible,"Pause releases cursor for UI");
        session.Resume(); yield return null;
        InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.V)); yield return null; yield return null;
        InputSystem.QueueStateEvent(keyboard,new KeyboardState()); yield return null;
        Check(!rig.FirstPerson && visual.GetComponentsInChildren<Renderer>().All(r=>r.enabled),"V restores third person and body");
        session.ReturnToMenu(); yield return null;
        Check(cam.orthographic && Cursor.lockState==CursorLockMode.None,"Menu camera and cursor restored");
        InputSystem.RemoveDevice(keyboard);
        InputSystem.settings.backgroundBehavior=oldBackground;
        InputSystem.settings.editorInputBehaviorInPlayMode=oldEditor;
        report.Add(report.Any(l=>l.StartsWith("FAIL"))?"RESULT: FAILED":"RESULT: ALL PASSED");
        File.WriteAllLines("Temp/camera-verification.txt",report);
    }
}
