using CircusPrototype;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
public static class CircusPresentationSetup
{
    static void Float(SerializedObject so,string name,float value){so.FindProperty(name).floatValue=value;}
    public static string Configure()
    {
        if(Application.isPlaying)return "Stop Play Mode before configuring";
        const string folder="Assets/Prototype/Materials";
        if(!AssetDatabase.IsValidFolder(folder))AssetDatabase.CreateFolder("Assets/Prototype","Materials");
        var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(folder+"/SoftFlame.png");
        if(!texture){
            texture=new Texture2D(64,64,TextureFormat.RGBA32,false);
            for(int y=0;y<64;y++)for(int x=0;x<64;x++){
                float d=Vector2.Distance(new Vector2(x,y),new Vector2(31.5f,31.5f))/31.5f;
                texture.SetPixel(x,y,new Color(1,1,1,Mathf.Pow(Mathf.Clamp01(1-d*d),2)));
            }
            texture.Apply();System.IO.File.WriteAllBytes(folder+"/SoftFlame.png",texture.EncodeToPNG());Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(folder+"/SoftFlame.png");texture=AssetDatabase.LoadAssetAtPath<Texture2D>(folder+"/SoftFlame.png");
        }
        var mat=AssetDatabase.LoadAssetAtPath<Material>(folder+"/SoftCombatGlow.mat");
        if(!mat){mat=new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));AssetDatabase.CreateAsset(mat,folder+"/SoftCombatGlow.mat");}
        mat.SetTexture("_BaseMap",texture);mat.SetColor("_BaseColor",Color.white);mat.SetFloat("_Surface",1);mat.SetFloat("_Blend",2);
        mat.SetFloat("_SrcBlend",(float)UnityEngine.Rendering.BlendMode.SrcAlpha);mat.SetFloat("_DstBlend",(float)UnityEngine.Rendering.BlendMode.One);
        mat.SetFloat("_ZWrite",0);mat.SetFloat("_Cull",0);mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");mat.renderQueue=3000;EditorUtility.SetDirty(mat);
        var session=Object.FindFirstObjectByType<CircusSession>();
        var fx=session.GetComponent<CircusCombatFx>();if(!fx)fx=session.gameObject.AddComponent<CircusCombatFx>();
        var so=new SerializedObject(fx);so.FindProperty("glowMaterial").objectReferenceValue=mat;so.ApplyModifiedPropertiesWithoutUndo();
        so=new SerializedObject(session.Player);Float(so,"attackCooldown",.56f);Float(so,"rangedCooldown",.65f);Float(so,"bossDamageScale",.5f);
        Float(so,"meleeWindup",.11f);Float(so,"meleeSwing",.09f);Float(so,"meleeRecovery",.18f);Float(so,"hitStopSeconds",.04f);so.ApplyModifiedPropertiesWithoutUndo();
        so=new SerializedObject(session.GetComponent<CircusAudio>());so.FindProperty("effectsEnabled").boolValue=false;so.FindProperty("combatAccentsEnabled").boolValue=true;
        Float(so,"meleeImpactVolume",.12f);Float(so,"slamImpactVolume",.2f);
        so.FindProperty("meleeImpactClip").objectReferenceValue=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Prototype/Audio/Combat/MeleeAccent.wav");
        so.FindProperty("slamImpactClip").objectReferenceValue=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Prototype/Audio/Combat/SlamAccent.wav");so.ApplyModifiedPropertiesWithoutUndo();
        string path="Assets/Prototype/Prefabs/RangProjectile.prefab";var root=PrefabUtility.LoadPrefabContents(path);
        try{if(!root.GetComponent<CircusProjectileVfx>())root.AddComponent<CircusProjectileVfx>();var vfx=new SerializedObject(root.GetComponent<CircusProjectileVfx>());Float(vfx,"coreSize",.42f);vfx.ApplyModifiedPropertiesWithoutUndo();root.GetComponent<MeshRenderer>().enabled=false;
            var trail=root.GetComponent<TrailRenderer>();if(trail)trail.enabled=false;PrefabUtility.SaveAsPrefabAsset(root,path);}
        finally{PrefabUtility.UnloadPrefabContents(root);}
        path="Assets/Prototype/Prefabs/RangedRang.prefab";root=PrefabUtility.LoadPrefabContents(path);
        try{so=new SerializedObject(root.GetComponent<CircusEnemy>());Float(so,"windupSeconds",.35f);so.ApplyModifiedPropertiesWithoutUndo();PrefabUtility.SaveAsPrefabAsset(root,path);}
        finally{PrefabUtility.UnloadPrefabContents(root);}
        path="Assets/Prototype/Prefabs/DokkaebiBoss.prefab";root=PrefabUtility.LoadPrefabContents(path);
        try{
            so=new SerializedObject(root.GetComponent<CircusEnemy>());Float(so,"health",900);so.ApplyModifiedPropertiesWithoutUndo();
            so=new SerializedObject(root.GetComponent<CircusBoss>());
            string[] patterns={"swing","slam","singleFlame","fanFlame","homingFlames","dash","doubleSwing","ringFlames","groundSeals"};
            float[] damage={16,22,10,10,8,18,14,10,12};
            for(int i=0;i<patterns.Length;i++)so.FindProperty(patterns[i]).FindPropertyRelative("damage").floatValue=damage[i];
            so.FindProperty("slam").FindPropertyRelative("windup").floatValue=.85f;so.FindProperty("slam").FindPropertyRelative("recovery").floatValue=1.6f;
            Float(so,"slamLift",.45f);Float(so,"slamDropSeconds",.1f);Float(so,"slamPoseRecovery",.3f);so.ApplyModifiedPropertiesWithoutUndo();PrefabUtility.SaveAsPrefabAsset(root,path);
        }finally{PrefabUtility.UnloadPrefabContents(root);}
        EditorSceneManager.MarkSceneDirty(session.gameObject.scene);EditorSceneManager.SaveOpenScenes();AssetDatabase.SaveAssets();
        return "Presentation materials, existing prefabs, scene references, two quiet accents and combat balance saved";
    }
}
