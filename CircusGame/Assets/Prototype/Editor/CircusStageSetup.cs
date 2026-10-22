using System;
using System.Linq;
using CircusPrototype;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;
public static class CircusStageSetup
{
    static Transform root;static GameObject cubeSource,floorSource;static Material red,gold,dark,pale;
    static GameObject Block(string name,Transform parent,Vector3 center,Vector3 size,Material material,Quaternion rotation=default)
    {
        var go=new GameObject(name);go.transform.SetParent(parent,true);go.transform.SetPositionAndRotation(center,rotation==default?Quaternion.identity:rotation);go.transform.localScale=size;
        go.AddComponent<MeshFilter>().sharedMesh=cubeSource.GetComponent<MeshFilter>().sharedMesh;go.AddComponent<MeshRenderer>().sharedMaterial=material;go.AddComponent<BoxCollider>();return go;
    }
    static Vector3 Dir(Vector3 a,Vector3 b){var d=b-a;d.y=0;return d.normalized;}
    static float Edge(CircusCombatArea a,Vector3 direction)=>a.ellipse?1/Mathf.Sqrt(direction.x*direction.x/(a.halfSize.x*a.halfSize.x)+direction.z*direction.z/(a.halfSize.y*a.halfSize.y)):Mathf.Min(Mathf.Abs(direction.x)>.001f?a.halfSize.x/Mathf.Abs(direction.x):999,Mathf.Abs(direction.z)>.001f?a.halfSize.y/Mathf.Abs(direction.z):999);
    static GameObject Gate(string name,Vector3 center,Vector3 forward,Transform parent)
    {
        var frame=new GameObject(name);frame.transform.SetParent(parent,true);frame.transform.position=center;
        var yaw=Quaternion.LookRotation(forward);var right=Vector3.Cross(Vector3.up,forward);
        // Existing velvet/ivory pedestal mesh and materials form a temporary circus shutter.
        var barrier=Block("Velvet barrier",frame.transform,center+Vector3.up*2.6f,new Vector3(8,5.2f,.45f),red,yaw);
        foreach(int sign in new[]{-1,1})Block("Brass edge",frame.transform,center+right*sign*3.8f+Vector3.up*2.6f,new Vector3(.16f,5.1f,.6f),gold,yaw);
        return frame;
    }
    static void WallRing(CircusCombatArea area,Vector3 entry,Vector3 exit)
    {
        int count=area.ellipse?40:4;Vector3[] points;
        if(area.ellipse)points=Enumerable.Range(0,count).Select(i=>area.Center+new Vector3(Mathf.Sin(i*2*Mathf.PI/count)*area.halfSize.x,0,Mathf.Cos(i*2*Mathf.PI/count)*area.halfSize.y)).ToArray();
        else points=new[]{area.Center+new Vector3(-area.halfSize.x,0,-area.halfSize.y),area.Center+new Vector3(area.halfSize.x,0,-area.halfSize.y),area.Center+new Vector3(area.halfSize.x,0,area.halfSize.y),area.Center+new Vector3(-area.halfSize.x,0,area.halfSize.y)};
        int index=0;
        for(int i=0;i<count;i++){
            var a=points[i];var b=points[(i+1)%count];int pieces=area.ellipse?1:Mathf.CeilToInt(Vector3.Distance(a,b)/1.4f);
            for(int n=0;n<pieces;n++){
                var from=Vector3.Lerp(a,b,(float)n/pieces);var to=Vector3.Lerp(a,b,(float)(n+1)/pieces);var p=(from+to)*.5f;
                if(Vector3.Distance(p,entry)<4.1f || Vector3.Distance(p,exit)<4.1f)continue;
                var rotation=Quaternion.LookRotation(Dir(from,to));
                Block("Circus wall "+index,area.transform,p+Vector3.up*2.6f,new Vector3(.6f,5.2f,Vector3.Distance(from,to)+.15f),index++%3==0?red:pale,rotation);
                Block("Brass wall crown",area.transform,p+Vector3.up*5.1f,new Vector3(.7f,.18f,Vector3.Distance(from,to)+.15f),gold,rotation);
            }
        }
    }
    static void Corridor(Vector3 from,Vector3 to,int index)
    {
        var holder=new GameObject(index==5?"Boss Entrance Path":"Recovery Path "+index).transform;holder.SetParent(root,true);
        var d=Dir(from,to);var side=Vector3.Cross(Vector3.up,d);float length=Vector3.Distance(from,to);var rotation=Quaternion.LookRotation(d);
        Block("Existing arena floor",holder,(from+to)*.5f+Vector3.down*.1f,new Vector3(7,.2f,length+2),dark,rotation);
        foreach(int sign in new[]{-1,1}){
            Block("Circus corridor wall",holder,(from+to)*.5f+side*sign*3.7f+Vector3.up*2.6f,new Vector3(.5f,5.2f,length+2),index>=4?red:pale,rotation);
            int repeats=Mathf.Max(2,Mathf.FloorToInt(length/6));
            for(int k=0;k<=repeats;k++){
                var p=Vector3.Lerp(from,to,(float)k/repeats)+side*sign*3.45f;
                Block("Existing footlight pillar",holder,p+Vector3.up*1.7f,new Vector3(.5f,3.4f,.6f),k%2==0?red:gold,rotation);
                if(k%2==0){var go=new GameObject("Warm path lamp");go.transform.SetParent(holder);go.transform.position=p+Vector3.up*2.7f;var light=go.AddComponent<Light>();light.type=LightType.Point;light.color=index==5?new Color(.3f,.7f,.6f):new Color(1,.6f,.25f);light.range=7;light.intensity=index==5?.5f:1;light.shadows=LightShadows.None;}
            }
        }
        for(float t=4;t<length-2;t+=6){var p=from+d*t+Vector3.up*.025f;Block("Brass path direction",holder,p,new Vector3(.14f,.035f,1.6f),gold,rotation);}
    }
    static CircusAreaWave Wave(string label,CircusCombatArea area,int close,int ranged,CircusEnemy melee,CircusEnemy shot)
    {
        var groups=new System.Collections.Generic.List<CircusWaveGroup>();
        if(close>0)groups.Add(new CircusWaveGroup{prefab=melee,count=close,spawnDelay=.45f,spawnPoints=area.spawnPoints});
        if(ranged>0)groups.Add(new CircusWaveGroup{prefab=shot,count=ranged,spawnDelay=.55f,spawnPoints=area.spawnPoints.Where((p,i)=>i%2==1).ToArray()});
        return new CircusAreaWave{label=label,startDelay=.35f,groups=groups.ToArray()};
    }
    public static string Configure()
    {
        if(Application.isPlaying)throw new InvalidOperationException("Stop Play Mode first");
        if(GameObject.Find("Connected Circus Stage"))throw new InvalidOperationException("Stage exists; edit its saved area data in place");
        var session=Object.FindFirstObjectByType<CircusSession>();cubeSource=GameObject.Find("Stage Dressing").transform.Find("Footlight 0").gameObject;floorSource=GameObject.Find("Circus Ring");
        red=AssetDatabase.LoadAssetAtPath<Material>("Assets/Prototype/Materials/Velvet.mat");gold=AssetDatabase.LoadAssetAtPath<Material>("Assets/Prototype/Materials/Brass.mat");dark=AssetDatabase.LoadAssetAtPath<Material>("Assets/Prototype/Materials/Arena.mat");pale=AssetDatabase.LoadAssetAtPath<Material>("Assets/Prototype/Materials/Ivory.mat");
        root=new GameObject("Connected Circus Stage").transform;
        var centers=new[]{new Vector3(0,0,-340),new Vector3(-16,0,-272),new Vector3(16,0,-204),new Vector3(-25,0,-134),new Vector3(0,0,-66),Vector3.zero};
        var sizes=new[]{new Vector2(10,10),new Vector2(8,10),new Vector2(13,8),new Vector2(12,12),new Vector2(13,11),new Vector2(14,14)};
        var areas=new CircusCombatArea[6];
        for(int i=0;i<6;i++){
            var go=new GameObject(i==5?"Boss Arena":"Combat Area "+"ABCDF"[i]);go.transform.SetParent(root);go.transform.position=centers[i];var area=go.AddComponent<CircusCombatArea>();areas[i]=area;
            area.areaId=i==5?"BOSS":i==4?"FINAL":((char)('A'+i)).ToString();area.act=i<2?1:i<5?2:3;area.bossArena=i==5;area.ellipse=i!=1;area.halfSize=sizes[i];area.maxAlive=6;area.healthMultiplier=1;area.attackIntervalMultiplier=i<2?1:.9f;
            if(i<5){
                if(area.ellipse){var floor=Object.Instantiate(floorSource,go.transform);floor.name="Existing circus floor";floor.transform.position=centers[i]+Vector3.down*.12f;floor.transform.localScale=new Vector3(sizes[i].x*2,.12f,sizes[i].y*2);}
                else Block("Existing stage floor",go.transform,centers[i]+Vector3.down*.1f,new Vector3(sizes[i].x*2,.2f,sizes[i].y*2),dark);
                var motif=Object.Instantiate(GameObject.Find("Stage Dressing").transform.Find("Inner Ring").gameObject,go.transform);motif.name="Existing brass floor motif";motif.transform.position=centers[i]+Vector3.up*.045f;motif.transform.localScale=Vector3.one*(i==2?1.3f:1);
            }
        }
        var start=new GameObject("Player Stage Start").transform;start.SetParent(root);start.position=centers[0]+Vector3.back*22+Vector3.up*.06f;
        var entries=new Vector3[6];var exits=new Vector3[6];
        for(int i=0;i<6;i++){
            var a=areas[i];var incoming=Dir(i==0?start.position:centers[i-1],centers[i]);var outgoing=i==5?Vector3.forward:Dir(centers[i],centers[i+1]);
            entries[i]=centers[i]-incoming*Edge(a,incoming);exits[i]=centers[i]+outgoing*Edge(a,outgoing);
            a.entranceBarrier=Gate("Entrance Shutter",entries[i],incoming,a.transform);if(i<5)a.exitBarrier=Gate("Exit Shutter",exits[i],outgoing,a.transform);
            if(i<5)WallRing(a,entries[i],exits[i]);
            var trigger=new GameObject("Area Entrance Trigger");trigger.transform.SetParent(a.transform,true);trigger.transform.SetPositionAndRotation(entries[i]+incoming*3+Vector3.up*1.5f,Quaternion.LookRotation(incoming));
            var box=trigger.AddComponent<BoxCollider>();box.isTrigger=true;box.size=new Vector3(6,3,3);a.entranceTrigger=box;trigger.AddComponent<CircusAreaTrigger>().area=a;
            if(i<5){a.spawnPoints=new Transform[8];for(int n=0;n<8;n++){var point=new GameObject("Spawn "+n).transform;point.SetParent(a.transform);float angle=n*Mathf.PI/4;point.position=centers[i]+new Vector3(Mathf.Sin(angle)*sizes[i].x*.7f,.06f,Mathf.Cos(angle)*sizes[i].y*.7f);a.spawnPoints[n]=point;}}
        }
        // Keep the original boss arena size and all boss-local pattern coordinates.
        foreach(var c in GameObject.Find("Stage Dressing").GetComponentsInChildren<BoxCollider>())if(c.name.StartsWith("Stage Boundary") && c.transform.position.z< -12.5f && Mathf.Abs(c.transform.position.x)<5)Object.DestroyImmediate(c.gameObject);
        Block("Boss entrance side guard L",areas[5].transform,new Vector3(-4.5f,2.6f,-12.8f),new Vector3(1.2f,5.2f,4),pale);
        Block("Boss entrance side guard R",areas[5].transform,new Vector3(4.5f,2.6f,-12.8f),new Vector3(1.2f,5.2f,4),pale);
        Block("Start landing",root,start.position+Vector3.down*.16f,new Vector3(12,.2f,12),dark);
        Block("Start rear wall",root,start.position+Vector3.back*6+Vector3.up*2.6f,new Vector3(12,5.2f,.5f),red);
        foreach(int sign in new[]{-1,1})Block("Start side wall",root,start.position+Vector3.right*sign*6+Vector3.up*2.6f,new Vector3(.5f,5.2f,12),pale);
        Corridor(start.position,entries[0],0);
        for(int i=0;i<5;i++)Corridor(exits[i],entries[i+1],i+1);
        // Small existing pedestal groups create lanes, with wide ways around both sides.
        foreach(int index in new[]{1,2,3}){
            var a=areas[index];foreach(int sign in new[]{-1,1}){
                var p=a.Center+new Vector3(index==2?sign*5:sign*3,0,index==1?1:-2);
                Block("Stage prop pedestal",a.transform,p+Vector3.up*1.1f,new Vector3(1.2f,2.2f,1.2f),red);Block("Brass prop cap",a.transform,p+Vector3.up*2.3f,new Vector3(1.5f,.2f,1.5f),gold);
            }
        }
        var melee=AssetDatabase.LoadAssetAtPath<CircusEnemy>("Assets/Prototype/Prefabs/CloseRang.prefab");var shot=AssetDatabase.LoadAssetAtPath<CircusEnemy>("Assets/Prototype/Prefabs/RangedRang.prefab");
        areas[0].waves=new[]{Wave("기본 전투",areas[0],3,0,melee,shot)};
        areas[1].waves=new[]{Wave("근거리",areas[1],3,0,melee,shot),Wave("첫 혼합",areas[1],2,2,melee,shot)};
        areas[2].waves=new[]{Wave("측면 혼합",areas[2],3,2,melee,shot)};
        areas[3].waves=new[]{Wave("포위",areas[3],4,0,melee,shot),Wave("원거리 압박",areas[3],2,3,melee,shot)};
        areas[4].waves=new[]{Wave("최종 혼합",areas[4],3,2,melee,shot),Wave("마지막 랑",areas[4],4,2,melee,shot)};
        areas[5].waves=Array.Empty<CircusAreaWave>();
        for(int i=0;i<6;i++){areas[i].ResetArea(i==0);EditorUtility.SetDirty(areas[i]);}
        var so=new SerializedObject(session);so.FindProperty("combatAreas").arraySize=6;for(int i=0;i<6;i++)so.FindProperty("combatAreas").GetArrayElementAtIndex(i).objectReferenceValue=areas[i];
        so.FindProperty("stageStart").objectReferenceValue=start;so.FindProperty("bossBackdrop").objectReferenceValue=GameObject.Find("건물");so.FindProperty("connectedStage").boolValue=true;so.ApplyModifiedPropertiesWithoutUndo();
        session.Player.transform.position=start.position;
        GameObject.Find("Rang Spawners").SetActive(false);
        RenderSettings.fog=true;RenderSettings.fogMode=FogMode.Linear;RenderSettings.fogStartDistance=26;RenderSettings.fogEndDistance=60;RenderSettings.fogColor=new Color(.08f,.055f,.07f);
        var camera=Camera.main;camera.farClipPlane=80;camera.backgroundColor=RenderSettings.fogColor;
        // Existing HUD, frame, sprites and fonts stay in place; only make room for the wave line.
        var hud=Object.FindFirstObjectByType<CircusHud>();var hs=new SerializedObject(hud);var label=hs.FindProperty("remainingText").objectReferenceValue as TMPro.TextMeshProUGUI;
        if(label){label.fontSize=Mathf.Min(label.fontSize,21);label.rectTransform.sizeDelta=new Vector2(Mathf.Max(label.rectTransform.sizeDelta.x,700),90);EditorUtility.SetDirty(label);}
        EditorSceneManager.MarkSceneDirty(session.gameObject.scene);EditorSceneManager.SaveOpenScenes();AssetDatabase.SaveAssets();
        return "Connected A/B/C/D/FINAL/BOSS: 5 combat areas, 8 waves, 35 ordinary enemies, max 6 alive; original boss arena retained";
    }
    public static string SealPortals()
    {
        if(Application.isPlaying)throw new InvalidOperationException("Edit Mode only");
        cubeSource=GameObject.Find("Stage Dressing").transform.Find("Footlight 0").gameObject;pale=AssetDatabase.LoadAssetAtPath<Material>("Assets/Prototype/Materials/Ivory.mat");
        var oldLamp=GameObject.Find("Stage Dressing").transform.Find("Footlight 6");if(oldLamp)oldLamp.position=new Vector3(-7,oldLamp.position.y,-14.4f);
        root=GameObject.Find("Connected Circus Stage").transform;var start=root.Find("Player Stage Start");
        if(start && !root.Find("Start front shoulder -1"))foreach(int sign in new[]{-1,1})Block("Start front shoulder "+sign,root,start.position+Vector3.forward*6+Vector3.right*sign*4.75f+Vector3.up*2.6f,new Vector3(2.5f,5.2f,.5f),pale);
        foreach(var area in Object.FindFirstObjectByType<CircusSession>().Areas)foreach(var gate in new[]{area.entranceBarrier,area.exitBarrier}){
            if(!gate || area.transform.Find(gate.name+" Fixed jamb -1"))continue;
            var forward=gate.transform.GetChild(0).forward;var side=Vector3.Cross(Vector3.up,forward);
            foreach(int sign in new[]{-1,1})Block(gate.name+" Fixed jamb "+sign,area.transform,gate.transform.position+side*sign*5.1f+Vector3.up*2.6f,new Vector3(2.2f,5.2f,3.5f),pale,Quaternion.LookRotation(forward));
        }
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());EditorSceneManager.SaveOpenScenes();return "Portal side gaps sealed with permanent existing pedestal walls";
    }
}
