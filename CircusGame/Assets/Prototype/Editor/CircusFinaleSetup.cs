using CircusPrototype;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
public static class CircusFinaleSetup
{
    public static string Configure()
    {
        var session=Object.FindFirstObjectByType<CircusSession>();
        var music=session.GetComponent<CircusMusic>();if(!music)music=session.gameObject.AddComponent<CircusMusic>();
        var so=new SerializedObject(music);
        string[] fields={"combatLoop","bossLoop","finalPercussion"},names={"OrdinaryPerformance","DokkaebiFinale","FinalPercussion"};
        for(int i=0;i<fields.Length;i++)so.FindProperty(fields[i]).objectReferenceValue=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Prototype/Audio/Music/"+names[i]+".wav");
        so.ApplyModifiedPropertiesWithoutUndo();
        var audio=new SerializedObject(session.GetComponent<CircusAudio>());var clips=audio.FindProperty("clips");var volumes=audio.FindProperty("cueVolumes");
        int old=clips.arraySize;clips.arraySize=volumes.arraySize=System.Enum.GetValues(typeof(CircusCue)).Length;
        for(int i=old;i<clips.arraySize;i++)
        {
            clips.GetArrayElementAtIndex(i).objectReferenceValue=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Prototype/Audio/Combat/"+(CircusCue)i+".wav");
            volumes.GetArrayElementAtIndex(i).floatValue=i==(int)CircusCue.BossPrepare?.18f:.65f;
        }
        audio.ApplyModifiedPropertiesWithoutUndo();
        foreach(string name in names)
        {
            var importer=AssetImporter.GetAtPath("Assets/Prototype/Audio/Music/"+name+".wav") as AudioImporter;
            if(!importer)continue;var settings=importer.defaultSampleSettings;settings.loadType=AudioClipLoadType.Streaming;settings.compressionFormat=AudioCompressionFormat.Vorbis;settings.quality=.8f;
            importer.defaultSampleSettings=settings;importer.SaveAndReimport();
        }
        var root=PrefabUtility.LoadPrefabContents("Assets/Prototype/Prefabs/DokkaebiBoss.prefab");
        try
        {
            var boss=new SerializedObject(root.GetComponent<CircusBoss>());boss.FindProperty("phaseTwoHealth").floatValue=.7f;boss.FindProperty("phaseThreeHealth").floatValue=.35f;
            boss.FindProperty("phaseTwoCooldown").floatValue=.9f;boss.FindProperty("phaseThreeCooldown").floatValue=.85f;
            boss.ApplyModifiedPropertiesWithoutUndo();PrefabUtility.SaveAsPrefabAsset(root,"Assets/Prototype/Prefabs/DokkaebiBoss.prefab");
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
        EditorSceneManager.MarkSceneDirty(session.gameObject.scene);EditorSceneManager.SaveOpenScenes();AssetDatabase.SaveAssets();
        return "Existing scene and artist UI preserved; finale pattern data, music and 5 new sound roles configured";
    }
}
