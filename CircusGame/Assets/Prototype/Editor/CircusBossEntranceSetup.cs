using CircusPrototype;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
public static class CircusBossEntranceSetup
{
    static void Float(SerializedObject so,string field,float value){so.FindProperty(field).floatValue=value;}
    public static string Configure()
    {
        if(Application.isPlaying)return "Stop Play Mode first";
        var session=Object.FindFirstObjectByType<CircusSession>();var so=new SerializedObject(session);
        so.FindProperty("bossLandingPosition").vector3Value=new Vector3(0,.06f,0);Float(so,"bossIntroPlayerDistance",7);so.ApplyModifiedPropertiesWithoutUndo();
        var audio=new SerializedObject(session.GetComponent<CircusAudio>());
        audio.FindProperty("bossIntroSoundsEnabled").boolValue=true;
        audio.FindProperty("bossLandingClip").objectReferenceValue=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Prototype/Audio/Combat/BossLandingAccent.wav");
        audio.FindProperty("bossRoarClip").objectReferenceValue=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Prototype/Audio/Combat/BossRoarAccent.wav");
        Float(audio,"bossLandingVolume",.24f);Float(audio,"bossRoarVolume",.15f);audio.ApplyModifiedPropertiesWithoutUndo();
        var music=new SerializedObject(session.GetComponent<CircusMusic>());Float(music,"introFadeOutSeconds",.4f);Float(music,"introBossFadeSeconds",.85f);music.ApplyModifiedPropertiesWithoutUndo();
        var hud=Object.FindFirstObjectByType<CircusHud>();so=new SerializedObject(hud);Float(so,"bossNamePopupSeconds",.32f);Float(so,"bossNameYOffset",-230);so.ApplyModifiedPropertiesWithoutUndo();
        var panel=so.FindProperty("bossPanel").objectReferenceValue as GameObject;if(panel && !panel.GetComponent<CanvasGroup>())panel.AddComponent<CanvasGroup>();
        const string path="Assets/Prototype/Prefabs/DokkaebiBoss.prefab";var root=PrefabUtility.LoadPrefabContents(path);
        try{
            var entry=root.GetComponent<CircusBossEntrance>();if(!entry)entry=root.AddComponent<CircusBossEntrance>();
            so=new SerializedObject(entry);string[] fields={"quietSeconds","fallSeconds","landingHold","closeupSeconds","roarSeconds","returnSeconds","fallHeight","landingSquash","roarScale","roarLean","wideDistance","wideHeight","closeDistance","closeHeight","lookHeight","cinematicFov"};
            float[] values={.65f,.5f,.5f,.55f,1.05f,.85f,9,.94f,.025f,12,10,3.5f,6.2f,2.4f,1.9f,52};
            for(int i=0;i<fields.Length;i++)Float(so,fields[i],values[i]);so.ApplyModifiedPropertiesWithoutUndo();
            so=new SerializedObject(root.GetComponent<CircusBoss>());Float(so,"entranceSeconds",4.1f);
            string[] patterns={"swing","slam","singleFlame","fanFlame","homingFlames","dash","doubleSwing","ringFlames","groundSeals"};
            float[] windup={.55f,.9f,.6f,.65f,.7f,.65f,.55f,1.1f,.9f};
            for(int i=0;i<patterns.Length;i++)so.FindProperty(patterns[i]).FindPropertyRelative("windup").floatValue=windup[i];
            Float(so,"meleeStrikeSeconds",.1f);Float(so,"meleeTwist",26);Float(so,"meleeFollowThrough",48);Float(so,"meleeVisualStep",.12f);
            Float(so,"rangedPullback",.18f);Float(so,"rangedKick",.12f);Float(so,"rangedReleaseHold",.16f);Float(so,"poseRecoverySeconds",.25f);
            Float(so,"dashCrouch",.16f);Float(so,"dashPullback",.16f);so.ApplyModifiedPropertiesWithoutUndo();PrefabUtility.SaveAsPrefabAsset(root,path);
        }finally{PrefabUtility.UnloadPrefabContents(root);}
        EditorSceneManager.MarkSceneDirty(session.gameObject.scene);EditorSceneManager.SaveOpenScenes();AssetDatabase.SaveAssets();
        return "4.1-second entrance, existing artist frame/font, intro-only audio and nine existing pattern windups configured";
    }
}
