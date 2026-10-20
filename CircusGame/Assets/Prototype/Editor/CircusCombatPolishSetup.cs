using System;
using CircusPrototype;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
public static class CircusCombatPolishSetup
{
    public static string Configure()
    {
        var audio=UnityEngine.Object.FindFirstObjectByType<CircusAudio>();
        var so=new SerializedObject(audio); var clips=so.FindProperty("clips");var volumes=so.FindProperty("cueVolumes");
        var names=Enum.GetNames(typeof(CircusCue));clips.arraySize=volumes.arraySize=names.Length;
        float[] gains={.25f,.35f,.3f,.45f,.6f,.45f,.85f,.65f,.3f,.35f,.55f,.85f,.8f,.4f,.85f,0,.4f,.15f,.5f,.2f,.5f,.3f,.55f};
        for(int i=0;i<names.Length;i++)
        {
            string path="Assets/Prototype/Audio/Combat/"+names[i]+".wav";
            if(names[i]=="PlayerMelee")path="Assets/Prototype/Audio/Free/KenneyRPG/knifeSlice.ogg";
            if(names[i]=="EnemyMelee")path="Assets/Prototype/Audio/Free/KenneyRPG/knifeSlice2.ogg";
            if(names[i]=="UIClick")path="Assets/Prototype/Audio/Free/KenneyInterface/click_001.ogg";
            clips.GetArrayElementAtIndex(i).objectReferenceValue=names[i]=="FlameFlight" ? null : AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if(names[i]!="FlameFlight" && !clips.GetArrayElementAtIndex(i).objectReferenceValue)throw new Exception("Missing sound: "+path);
            volumes.GetArrayElementAtIndex(i).floatValue=gains[i];
        }
        so.ApplyModifiedPropertiesWithoutUndo();
        var root=PrefabUtility.LoadPrefabContents("Assets/Prototype/Prefabs/DokkaebiBoss.prefab");
        try
        {
            var boss=new SerializedObject(root.GetComponent<CircusBoss>());
            boss.FindProperty("windupSeconds").floatValue=.65f;
            boss.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root,"Assets/Prototype/Prefabs/DokkaebiBoss.prefab");
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        EditorSceneManager.MarkSceneDirty(audio.gameObject.scene);EditorSceneManager.SaveOpenScenes();AssetDatabase.SaveAssets();
        return "23 cue slots: 22 role-specific clips, flight loop intentionally silent; boss telegraph 0.65 seconds";
    }
}
