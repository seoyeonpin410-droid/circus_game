using System;
using System.Linq;
using System.Reflection;
using CircusPrototype;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;
public static class CircusShowSetup
{
    static readonly BindingFlags F=BindingFlags.NonPublic|BindingFlags.Static;
    static Transform root;static Material red,gold,pale;
    static void Field(string name,object value)=>typeof(CircusStageSetup).GetField(name,F).SetValue(null,value);
    static GameObject Block(string name,Transform parent,Vector3 p,Vector3 size,Material m,Quaternion q)=> (GameObject)typeof(CircusStageSetup).GetMethod("Block",F).Invoke(null,new object[]{name,parent,p,size,m,q});
    static CircusAreaWave Wave(CircusCombatArea area,string label,int melee,int ranged,int[] directions=null)
    {
        var close=AssetDatabase.LoadAssetAtPath<CircusEnemy>("Assets/Prototype/Prefabs/CloseRang.prefab");var shot=AssetDatabase.LoadAssetAtPath<CircusEnemy>("Assets/Prototype/Prefabs/RangedRang.prefab");
        var points=directions==null?area.spawnPoints:directions.Select(i=>area.spawnPoints[i]).ToArray();var groups=new System.Collections.Generic.List<CircusWaveGroup>();
        if(melee>0)groups.Add(new CircusWaveGroup{prefab=close,count=melee,spawnPoints=points,spawnDelay=.65f});
        if(ranged>0)groups.Add(new CircusWaveGroup{prefab=shot,count=ranged,spawnPoints=points.Reverse().ToArray(),spawnDelay=.7f});
        return new CircusAreaWave{label=label,startDelay=.4f,groups=groups.ToArray()};
    }
    public static string Configure()
    {
        if(Application.isPlaying)throw new InvalidOperationException("Edit mode only");
        if(GameObject.Find("Dokkaebi Grotto Show"))throw new InvalidOperationException("Show exists; edit saved data in place");
        var session=Object.FindFirstObjectByType<CircusSession>();var old=GameObject.Find("Connected Circus Stage");var oldBoss=session.Areas.Last();
        root=new GameObject("Dokkaebi Grotto Show").transform;
        var cube=GameObject.Find("Stage Dressing").transform.Find("Footlight 0").gameObject;var floor=GameObject.Find("Circus Ring");
        red=AssetDatabase.LoadAssetAtPath<Material>("Assets/Prototype/Materials/Velvet.mat");gold=AssetDatabase.LoadAssetAtPath<Material>("Assets/Prototype/Materials/Brass.mat");pale=AssetDatabase.LoadAssetAtPath<Material>("Assets/Prototype/Materials/Ivory.mat");
        Field("root",root);Field("cubeSource",cube);Field("floorSource",floor);Field("red",red);Field("gold",gold);Field("pale",pale);Field("dark",AssetDatabase.LoadAssetAtPath<Material>("Assets/Prototype/Materials/Arena.mat"));
        Vector3 center=new Vector3(0,0,-66);float radius=19.6f;
        var a1=new GameObject("ACT 1 same main stage").AddComponent<CircusCombatArea>();a1.transform.SetParent(root);a1.transform.position=center;
        var a2=new GameObject("ACT 2 same main stage").AddComponent<CircusCombatArea>();a2.transform.SetParent(root);a2.transform.position=center;
        foreach(var a in new[]{a1,a2}){a.halfSize=Vector2.one*radius;a.ellipse=true;a.areaId="도깨비 굴";a.minimumSpawnDistance=5;a.spawnWarning=.75f;a.nextWaveDelay=1;a.healthMultiplier=1;a.clearDelay=.3f;a.clearPresentation=.7f;}
        a1.act=1;a1.maxAlive=6;a2.act=2;a2.maxAlive=7;a2.attackIntervalMultiplier=.9f;
        var mainFloor=Object.Instantiate(floor,root);mainFloor.name="Expanded single circus stage floor";mainFloor.transform.position=center+Vector3.down*.12f;mainFloor.transform.localScale=new Vector3(radius*2,.12f,radius*2);
        var exit=center+Vector3.forward*radius;
        typeof(CircusStageSetup).GetMethod("WallRing",F).Invoke(null,new object[]{a1,new Vector3(10000,0,10000),exit});
        var gate=(GameObject)typeof(CircusStageSetup).GetMethod("Gate",F).Invoke(null,new object[]{"Boss Room Open after ACT 2",exit,Vector3.forward,root});a1.exitBarrier=a2.exitBarrier=gate;
        foreach(int sign in new[]{-1,1})Block("Main gate fixed jamb "+sign,root,exit+Vector3.right*sign*5.1f+Vector3.up*2.6f,new Vector3(2.2f,5.2f,3.5f),pale,Quaternion.identity);
        var motif=Object.Instantiate(GameObject.Find("Stage Dressing").transform.Find("Inner Ring").gameObject,root);motif.name="Existing brass stage motif";motif.transform.position=center+Vector3.up*.045f;motif.transform.localScale=Vector3.one*1.9f;
        var roofSource=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/모델링/서커스장/뚜껑.obj");
        if(roofSource){var canopy=(GameObject)PrefabUtility.InstantiatePrefab(roofSource,root);canopy.name="Existing circus dome canopy";var rs=canopy.GetComponentsInChildren<Renderer>();var bounds=rs[0].bounds;foreach(var r in rs)bounds.Encapsulate(r.bounds);canopy.transform.localScale*=43/Mathf.Max(bounds.size.x,bounds.size.z);bounds=rs[0].bounds;foreach(var r in rs)bounds.Encapsulate(r.bounds);canopy.transform.position+=center+new Vector3(-bounds.center.x,9.5f-bounds.min.y,-bounds.center.z);}
        var points=new Transform[6];var lights=new Light[6];
        for(int i=0;i<6;i++){
            var d=Quaternion.Euler(0,30+i*60,0)*Vector3.forward;var point=new GameObject("Rang stage entry "+i).transform;point.SetParent(root);point.position=center+d*16.5f+Vector3.up*.06f;points[i]=point;
            var side=Vector3.Cross(Vector3.up,d);foreach(int sign in new[]{-1,1})Block("Circus entry column",root,center+d*19.1f+side*sign*1.7f+Vector3.up*4.4f,new Vector3(.6f,8.8f,.6f),i%2==0?red:pale,Quaternion.identity);
            var lamp=new GameObject("Main stage light "+i);lamp.transform.SetParent(root);lamp.transform.position=center+d*18.5f+Vector3.up*4;var l=lamp.AddComponent<Light>();l.type=LightType.Point;l.range=14;l.intensity=2;l.color=new Color(1,.68f,.35f);l.shadows=LightShadows.None;lights[i]=l;
        }
        a1.spawnPoints=a2.spawnPoints=points;
        a1.waves=new[]{Wave(a1,"기본",3,0,new[]{0,1}),Wave(a1,"양방향",4,0,new[]{0,3}),Wave(a1,"원거리 합류",3,2,new[]{0,2,4}),Wave(a1,"1막 마지막",4,2)};
        a2.waves=new[]{Wave(a2,"여러 방향",4,2),Wave(a2,"순차 포위",4,2,new[]{0,2,4}),Wave(a2,"무대의 위험",3,3),Wave(a2,"외곽 포위",4,2,new[]{1,3,5}),Wave(a2,"마지막 랑",4,3)};
        // Wave 2 deliberately separates front melee, left melee and right ranged groups.
        var close=a2.waves[1].groups[0].prefab;var shot=a2.waves[1].groups[1].prefab;
        a2.waves[1].groups=new[]{new CircusWaveGroup{prefab=close,count=2,spawnPoints=new[]{points[0]},spawnDelay=.65f},new CircusWaveGroup{prefab=close,count=2,spawnPoints=new[]{points[2]},spawnDelay=.7f},new CircusWaveGroup{prefab=shot,count=2,spawnPoints=new[]{points[4]},spawnDelay=.7f}};
        var boss=Object.Instantiate(oldBoss,root);boss.name="Original Boss Arena";boss.transform.position=Vector3.zero;boss.act=3;boss.bossArena=true;
        typeof(CircusStageSetup).GetMethod("Corridor",F).Invoke(null,new object[]{exit,boss.entranceBarrier.transform.position,5});
        var start=new GameObject("Show player start").transform;start.SetParent(root);start.position=center+Vector3.back*8+Vector3.up*.06f;
        var hazard=root.gameObject.AddComponent<CircusArenaHazard>();hazard.arena=a2;hazard.stageLights=lights;
        var so=new SerializedObject(session);so.FindProperty("combatAreas").arraySize=3;var areas=new[]{a1,a2,boss};for(int i=0;i<3;i++)so.FindProperty("combatAreas").GetArrayElementAtIndex(i).objectReferenceValue=areas[i];
        so.FindProperty("sameStageShow").boolValue=true;so.FindProperty("stageStart").objectReferenceValue=start;so.FindProperty("actClearDelay").floatValue=2.5f;so.ApplyModifiedPropertiesWithoutUndo();
        for(int i=0;i<3;i++){areas[i].ResetArea(i==0);EditorUtility.SetDirty(areas[i]);}
        old.SetActive(false);session.Player.transform.position=start.position;
        foreach(var text in Resources.FindObjectsOfTypeAll<TMPro.TextMeshProUGUI>().Where(t=>t.gameObject.scene.IsValid() && t.text.Contains("WASD 이동"))){text.text=text.text.Replace("SHIFT 달리기 · SPACE","SHIFT 달리기 · Q 대시 · SPACE");EditorUtility.SetDirty(text);}
        var prefab=PrefabUtility.LoadPrefabContents("Assets/Prototype/Prefabs/DokkaebiBoss.prefab");try{var b=new SerializedObject(prefab.GetComponent<CircusBoss>());b.FindProperty("phaseTwoGroundCount").intValue=7;b.FindProperty("phaseThreeGroundCount").intValue=9;b.FindProperty("groundInterval").floatValue=.15f;b.FindProperty("groundBatchSize").intValue=3;b.FindProperty("groundBatchGap").floatValue=.55f;b.ApplyModifiedPropertiesWithoutUndo();PrefabUtility.SaveAsPrefabAsset(prefab,"Assets/Prototype/Prefabs/DokkaebiBoss.prefab");}finally{PrefabUtility.UnloadPrefabContents(prefab);}
        EditorSceneManager.MarkSceneDirty(session.gameObject.scene);EditorSceneManager.SaveOpenScenes();AssetDatabase.SaveAssets();return "Single 39.2m stage, ACT 1 four waves (18), ACT 2 five waves (31), six entries, short boss path; dungeon disabled";
    }
}
