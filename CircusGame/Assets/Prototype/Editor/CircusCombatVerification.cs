using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CircusPrototype;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public static class CircusCombatVerification
{
    static readonly List<string> report = new List<string>();
    static void Check(bool value, string text)
    {
        report.Add((value ? "PASS " : "FAIL ") + text);
        File.WriteAllLines("Temp/combat-verification.txt", report);
    }
    public static string Run()
    {
        report.Clear();
        var session = Object.FindFirstObjectByType<CircusSession>();
        session.StartCoroutine(Verify(session));
        return "Combat verification started";
    }
    static IEnumerator Verify(CircusSession session)
    {
        var savedBackground = InputSystem.settings.backgroundBehavior;
        var savedEditor = InputSystem.settings.editorInputBehaviorInPlayMode;
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        var mouse = InputSystem.AddDevice<Mouse>();
        var rig = Object.FindFirstObjectByType<CircusCamera>();
        var pitch = typeof(CircusCamera).GetField("pitch", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var savedPitch = pitch.GetValue(rig);
        pitch.SetValue(rig, 0f);
        var prefab = AssetDatabase.LoadAssetAtPath<CircusEnemy>("Assets/Prototype/Prefabs/CloseRang.prefab");
        foreach (bool first in new[] {false, true})
        {
            session.BeginShow();
            rig.SetFirstPerson(first);
            yield return new WaitForSeconds(3.3f);
            var target = Object.Instantiate(prefab, session.Player.transform.position + rig.AimForward * 8, Quaternion.identity);
            target.Bind(session);
            typeof(CircusEnemy).GetField("moveSpeed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(target, 0f);
            Physics.SyncTransforms();
            var mousePosition = new Vector2(Screen.width * .5f, Screen.height * .25f);
            InputSystem.QueueStateEvent(mouse, new MouseState {position = mousePosition});
            yield return null; yield return null;
            var before = target.Health;
            InputSystem.QueueStateEvent(mouse, new MouseState {position = mousePosition}.WithButton(MouseButton.Left));
            yield return new WaitForSeconds(.15f);
            InputSystem.QueueStateEvent(mouse, new MouseState {position = mousePosition});
            Check(Object.FindObjectsByType<CircusProjectile>(FindObjectsSortMode.None).Any(p => p.IsPlayerShot), (first ? "First" : "Third") + " person left click creates projectile");
            yield return new WaitForSeconds(.5f);
            Check(target && target.Health < before, (first ? "First" : "Third") + " person crosshair projectile hits distant enemy");
            var fill = new SerializedObject(target).FindProperty("healthFill").objectReferenceValue as UnityEngine.UI.Image;
            Check(fill && Mathf.Abs(fill.fillAmount - target.Health / 50f) < .01f && fill.fillAmount < 1, "Enemy health UI reacts to projectile damage");
            target.transform.position = session.Player.transform.position + rig.AimForward * 2;
            Physics.SyncTransforms(); before = target.Health;
            InputSystem.QueueStateEvent(mouse, new MouseState {position = mousePosition}.WithButton(MouseButton.Right));
            yield return new WaitForSeconds(.32f);
            InputSystem.QueueStateEvent(mouse, new MouseState {position = mousePosition});
            Check(!target || target.Health < before, (first ? "First" : "Third") + " person right click retains melee attack");
            if (target) Object.Destroy(target.gameObject);
        }
        session.BeginShow(); yield return new WaitForSeconds(3.3f);
        var chaser = Object.Instantiate(prefab, session.Player.transform.position + Vector3.right * 4, Quaternion.identity);
        chaser.Bind(session);
        yield return new WaitForSeconds(2.8f);
        Check(Vector3.Distance(chaser.transform.position, session.Player.transform.position) < 2.2f
            && session.Player.Health < 100, "Melee zombie closes distance and damages player");
        Object.Destroy(chaser.gameObject);
        session.ReturnToMenu(); yield return null;
        Check(Object.FindObjectsByType<CircusProjectile>(FindObjectsSortMode.None).Length == 0, "Menu cleans both projectile types");
        pitch.SetValue(rig, savedPitch); rig.SetFirstPerson(false);
        InputSystem.RemoveDevice(mouse);
        InputSystem.settings.backgroundBehavior = savedBackground;
        InputSystem.settings.editorInputBehaviorInPlayMode = savedEditor;
        report.Add(report.Any(l => l.StartsWith("FAIL")) ? "RESULT: FAILED" : "RESULT: ALL PASSED");
        File.WriteAllLines("Temp/combat-verification.txt", report);
    }
}
