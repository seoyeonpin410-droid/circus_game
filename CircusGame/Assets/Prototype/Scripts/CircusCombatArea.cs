using System;
using UnityEngine;
namespace CircusPrototype
{
    public enum CircusAreaState { NotStarted, Battle, Cleared }
    [Serializable] public sealed class CircusWaveGroup
    {
        public CircusEnemy prefab;
        [Min(1)] public int count=3;
        public Transform[] spawnPoints;
        [Min(.1f)] public float spawnDelay=.45f;
    }
    [Serializable] public sealed class CircusAreaWave
    {
        public string label="Wave";
        [Min(0)] public float startDelay=.35f;
        public CircusWaveGroup[] groups;
        public int Total {get {int n=0;if(groups!=null)foreach(var g in groups)if(g!=null && g.prefab)n+=Mathf.Max(0,g.count);return n;}}
    }
    public sealed class CircusCombatArea : MonoBehaviour
    {
        public string areaId="A";
        [Range(1,3)] public int act=1;
        public bool bossArena,ellipse=true;
        public Vector2 halfSize=new Vector2(10,10);
        public BoxCollider entranceTrigger;
        public GameObject entranceBarrier,exitBarrier;
        public Transform[] spawnPoints;
        public CircusAreaWave[] waves;
        [Min(0)] public float clearDelay=.3f,clearPresentation=.7f,nextWaveDelay=1.2f;
        [Range(1,7)] public int maxAlive=6;
        [Min(3)] public float minimumSpawnDistance=5;
        [Min(.3f)] public float spawnWarning=.65f;
        [Range(.8f,1.5f)] public float healthMultiplier=1,attackIntervalMultiplier=1;
        public CircusAreaState State {get;private set;}
        public int Starts {get;private set;}
        public int Clears {get;private set;}
        public Vector3 Center=>transform.position;
        public bool Contains(Vector3 point,float margin=0)
        {
            var p=transform.InverseTransformPoint(point);float x=Mathf.Max(.5f,halfSize.x-margin),z=Mathf.Max(.5f,halfSize.y-margin);
            return ellipse?p.x*p.x/(x*x)+p.z*p.z/(z*z)<=1:Mathf.Abs(p.x)<=x && Mathf.Abs(p.z)<=z;
        }
        public Vector3 Clamp(Vector3 point,float margin=.8f)
        {
            var p=transform.InverseTransformPoint(point);float x=Mathf.Max(.5f,halfSize.x-margin),z=Mathf.Max(.5f,halfSize.y-margin);
            if(ellipse){float n=Mathf.Sqrt(p.x*p.x/(x*x)+p.z*p.z/(z*z));if(n>1){p.x/=n;p.z/=n;}}
            else {p.x=Mathf.Clamp(p.x,-x,x);p.z=Mathf.Clamp(p.z,-z,z);}
            return transform.TransformPoint(p);
        }
        public void ResetArea(bool unlocked)
        {State=CircusAreaState.NotStarted;Starts=Clears=0;Set(entranceBarrier,!unlocked);Set(exitBarrier,true);}
        public void Unlock(){if(State==CircusAreaState.NotStarted)Set(entranceBarrier,false);}
        public bool StartBattle()
        {if(State!=CircusAreaState.NotStarted)return false;State=CircusAreaState.Battle;Starts++;Set(entranceBarrier,true);Set(exitBarrier,true);return true;}
        public void Complete(bool openExit=true)
        {if(State!=CircusAreaState.Battle)return;State=CircusAreaState.Cleared;Clears++;Set(entranceBarrier,false);Set(exitBarrier,!openExit);}
        static void Set(GameObject go,bool active){if(go)go.SetActive(active);}
    }
}
