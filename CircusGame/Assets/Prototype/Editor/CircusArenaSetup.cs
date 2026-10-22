using System;
using CircusPrototype;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;
public static class CircusArenaSetup
{
    static void Float(SerializedObject so,string field,float value){so.FindProperty(field).floatValue=value;}
    public static string Configure()
    {
        if(Application.isPlaying)return "Stop Play Mode first";
        const string path="Assets/Prototype/Prefabs/DokkaebiBoss.prefab";
        const string matPath="Assets/Prototype/Materials/ArenaWarning.mat";
        var material=AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if(!material){material=new Material(Shader.Find("Circus/ArenaWarning"));AssetDatabase.CreateAsset(material,matPath);}
        var root=PrefabUtility.LoadPrefabContents(path);
        try{
            var so=new SerializedObject(root.GetComponent<CircusEnemy>());Float(so,"health",1000);so.ApplyModifiedPropertiesWithoutUndo();
            so=new SerializedObject(root.GetComponent<CircusBoss>());
            so.FindProperty("safeFieldMaterial").objectReferenceValue=material;
            string[] names={"swing","slam","singleFlame","fanFlame","homingFlames","dash","doubleSwing","ringFlames","groundSeals","sequentialFlames","multiDash","safeZones","orbitVolley","finale"};
            float[] damage={16,22,10,10,8,16,14,10,10,9,16,18,9,20};
            float[] windup={.55f,.9f,.6f,.7f,.75f,.7f,.55f,1.1f,.85f,.7f,.7f,1.8f,.8f,1.8f};
            float[] recovery={.45f,1,.5f,.65f,.9f,.85f,.8f,1.15f,.7f,.8f,1.35f,1.2f,1,1.9f};
            float[] cooldown={2,7,3.5f,6,10,8,7,20,13,8,13,20,12,26};
            for(int i=0;i<names.Length;i++){
                var setting=so.FindProperty(names[i]);setting.FindPropertyRelative("damage").floatValue=damage[i];setting.FindPropertyRelative("windup").floatValue=windup[i];
                setting.FindPropertyRelative("recovery").floatValue=recovery[i];setting.FindPropertyRelative("cooldown").floatValue=cooldown[i];
            }
            so.FindProperty("orbitVolley").FindPropertyRelative("spread").floatValue=150;
            Float(so,"phaseTwoSpeed",1.05f);Float(so,"phaseThreeSpeed",1.15f);Float(so,"phaseTwoCooldown",.98f);Float(so,"phaseThreeCooldown",.92f);
            Float(so,"phaseTwoAttackSpeed",1);Float(so,"phaseThreeAttackSpeed",1.03f);Float(so,"transitionSeconds",1.1f);
            Float(so,"groundDuration",2.2f);Float(so,"groundInterval",.22f);Float(so,"hazardWarningSeconds",1.1f);so.FindProperty("hazardAreaCount").intValue=4;
            Float(so,"arenaRadius",14);Float(so,"safeRadius",3);so.FindProperty("safeAreaCount").intValue=2;
            Float(so,"sequentialInterval",.34f);Float(so,"orbitInterval",.32f);Float(so,"multiDashGap",.4f);
            so.FindProperty("phaseTwoDashCount").intValue=2;so.FindProperty("phaseThreeDashCount").intValue=3;
            so.ApplyModifiedPropertiesWithoutUndo();PrefabUtility.SaveAsPrefabAsset(root,path);
        }finally{PrefabUtility.UnloadPrefabContents(root);}
        var session=Object.FindFirstObjectByType<CircusSession>();var player=new SerializedObject(session.Player);Float(player,"bossDamageScale",.4f);player.ApplyModifiedPropertiesWithoutUndo();
        var audio=new SerializedObject(session.GetComponent<CircusAudio>());var clips=audio.FindProperty("bossPatternClips");var volumes=audio.FindProperty("bossPatternVolumes");
        clips.arraySize=volumes.arraySize=Enum.GetValues(typeof(CircusBossSound)).Length;
        for(int i=0;i<clips.arraySize;i++){
            clips.GetArrayElementAtIndex(i).objectReferenceValue=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Prototype/Audio/Arena/"+(CircusBossSound)i+".wav");
            volumes.GetArrayElementAtIndex(i).floatValue=i==(int)CircusBossSound.UltimateImpact?.24f:i==(int)CircusBossSound.UltimateCharge?.18f:.13f;
        }
        audio.FindProperty("bossPatternSoundsEnabled").boolValue=true;Float(audio,"bossPatternMaster",.65f);audio.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.MarkSceneDirty(session.gameObject.scene);EditorSceneManager.SaveOpenScenes();AssetDatabase.SaveAssets();
        return "Existing boss expanded to 14 patterns; arena warning material, safe combos, balance and quiet role-specific cues saved";
    }
}
