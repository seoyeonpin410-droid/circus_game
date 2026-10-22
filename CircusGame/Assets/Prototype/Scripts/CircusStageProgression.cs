using System.Collections;
using UnityEngine;
namespace CircusPrototype
{
    public enum CircusStageFlow { Intro,Act1_A,Act1_B,Act1Clear,Act2_C,Act2_D,Act2_Final,Act2Clear,BossPath,BossIntro,BossBattle,GameClear,Act1Stage,Act2Stage }
    public sealed partial class CircusSession
    {
        [Header("Connected circus stage")]
        [SerializeField] bool connectedStage=true;
        [SerializeField] bool sameStageShow;
        public bool SameStageShow=>sameStageShow;
        [SerializeField] CircusCombatArea[] combatAreas;
        [SerializeField] Transform stageStart;
        [SerializeField] GameObject bossBackdrop;
        [SerializeField,Min(.2f)] float battleStartDelay=.35f,actClearDelay=1;
        [SerializeField,Range(0,30)] float actOneHeal=20,actTwoHeal=25;
        int nextArea,waveDeaths,waveTotal;
        bool requestedArea;
        public bool ConnectedStage=>connectedStage && combatAreas!=null && combatAreas.Length>0;
        public CircusCombatArea ActiveArea {get;private set;}
        public CircusStageFlow StageFlow {get;private set;}
        public int WaveNumber {get;private set;}
        public int WaveTotal=>waveTotal;
        public int StageIndex=>nextArea;
        public CircusCombatArea[] Areas=>combatAreas;
        public bool BossPath=>ConnectedStage && StageFlow==CircusStageFlow.BossPath;
        public string StageHint=>!ConnectedStage?"":BossPath?"마지막 공연장으로 이동하세요":ActiveArea && ActiveArea.State==CircusAreaState.Battle && !ActiveArea.bossArena?(WaveNumber>0?$"{(sameStageShow?"도깨비 굴":"구역 "+ActiveArea.areaId)} · WAVE {WaveNumber}/{ActiveArea.waves.Length}":$"{(sameStageShow?"도깨비 굴":"구역 "+ActiveArea.areaId)} · 전투 준비"):nextArea<combatAreas.Length?$"출구를 따라 {combatAreas[nextArea].areaId} 구역으로 이동":"공연 완료";
        void ResetStage()
        {
            nextArea=waveDeaths=waveTotal=0;WaveNumber=0;requestedArea=false;ActiveArea=null;StageFlow=CircusStageFlow.Intro;
            if(combatAreas!=null)for(int i=0;i<combatAreas.Length;i++)if(combatAreas[i])combatAreas[i].ResetArea(i==0);
            if(bossBackdrop)bossBackdrop.SetActive(false);
        }
        void PlaceAtStageStart()
        {
            if(!ConnectedStage || !stageStart)return;
            var cc=player.GetComponent<CharacterController>();cc.enabled=false;player.transform.SetPositionAndRotation(stageStart.position,stageStart.rotation);cc.enabled=true;
            FindFirstObjectByType<CircusCamera>()?.ResetStageView(stageStart.eulerAngles.y);
        }
        public bool TryEnterArea(CircusCombatArea area)
        {
            if(!ConnectedStage || !IsRunning || requestedArea || nextArea>=combatAreas.Length || combatAreas[nextArea]!=area || area.State!=CircusAreaState.NotStarted)return false;
            if(!area.Contains(player.transform.position,.5f))return false;
            if(area.bossArena && (nextArea<1 || combatAreas[nextArea-1].State!=CircusAreaState.Cleared))return false;
            requestedArea=true;return true;
        }
        IEnumerator RunConnectedStage()
        {
            for(nextArea=0;nextArea<combatAreas.Length;nextArea++){
                var area=combatAreas[nextArea];ActiveArea=null;requestedArea=false;waveTotal=waveDeaths=0;WaveNumber=0;
                if(!sameStageShow || area.bossArena)area.Unlock();else requestedArea=true;
                if(area.bossArena)StageFlow=CircusStageFlow.BossPath;
                while(!requestedArea)yield return null;
                ActiveArea=area;if(!area.StartBattle())yield break;
                spawned=defeated=0;round=area.act-1;
                if(area.bossArena){
                    StageFlow=CircusStageFlow.BossIntro;if(bossBackdrop)bossBackdrop.SetActive(true);
                    yield return RunBossEncounter();area.Complete();StageFlow=CircusStageFlow.GameClear;break;
                }
                StageFlow=nextArea==0?CircusStageFlow.Act1_A:nextArea==1?CircusStageFlow.Act1_B:nextArea==2?CircusStageFlow.Act2_C:nextArea==3?CircusStageFlow.Act2_D:CircusStageFlow.Act2_Final;
                if(sameStageShow)StageFlow=area.act==1?CircusStageFlow.Act1Stage:CircusStageFlow.Act2Stage;
                hud.Announce(sameStageShow?$"{area.act}막 · 도깨비 굴의 랑을 소탕하세요":$"{area.act}막 · {area.areaId} 구역",1.2f);
                yield return new WaitForSeconds(battleStartDelay);
                for(int w=0;w<area.waves.Length;w++){
                    var wave=area.waves[w];WaveNumber=w+1;waveDeaths=0;waveTotal=0;
                    if(w>0){hud.Announce($"WAVE {WaveNumber} · 다음 랑이 다가옵니다",area.nextWaveDelay);yield return new WaitForSeconds(area.nextWaveDelay);}
                    waveTotal=wave.Total;yield return new WaitForSeconds(wave.startDelay);
                    if(wave.groups!=null)foreach(var group in wave.groups){
                        if(group==null || !group.prefab)continue;
                        for(int n=0;n<group.count;n++){
                            CircusEnemy instance=null;
                            while(!instance){
                                if(enemies.Count<area.maxAlive)instance=SpawnAreaEnemy(area,group,n);
                                if(!instance)yield return null;
                            }
                            yield return new WaitForSeconds(group.spawnDelay);
                        }
                    }
                    while(waveDeaths<waveTotal || enemies.Count>0)yield return null;
                    ClearProjectiles();
                }
                yield return new WaitForSeconds(area.clearDelay);
                if(!sameStageShow){hud.Announce("AREA CLEAR",area.clearPresentation+.15f);yield return new WaitForSeconds(area.clearPresentation);}
                bool actEnd=nextArea+1<combatAreas.Length && combatAreas[nextArea+1].act!=area.act;
                if(actEnd){
                    State=ShowState.Intermission;StageFlow=area.act==1?CircusStageFlow.Act1Clear:CircusStageFlow.Act2Clear;
                    hud.Announce(sameStageShow?(area.act==1?"1막 완료 · 다음 공연을 준비하세요":"2막 완료 · 보스방이 열립니다"):$"{area.act}막 완료 · 다음 통로가 열립니다",actClearDelay+.5f);player.RestoreHealth(area.act==1?actOneHeal:actTwoHeal);
                    yield return new WaitForSeconds(actClearDelay);State=ShowState.Playing;
                }
                area.Complete(!sameStageShow || area.act==2);
                CircusCombatFx.Burst(area.exitBarrier?area.exitBarrier.transform.position:area.Center,new Color(.9f,.65f,.2f),8,.12f,1);
            }
            CompleteShow();
        }
        CircusEnemy SpawnAreaEnemy(CircusCombatArea area,CircusWaveGroup group,int index)
        {
            var points=group.spawnPoints!=null && group.spawnPoints.Length>0?group.spawnPoints:area.spawnPoints;
            if(points==null || points.Length==0)return null;
            Vector3 chosen=Vector3.zero;bool found=false;
            for(int pass=0;pass<2 && !found;pass++){
            var candidates=pass==0?points:area.spawnPoints;if(candidates==null || candidates.Length==0)continue;
            for(int n=0;n<candidates.Length;n++){
                var point=candidates[(index+n+spawned)%candidates.Length];if(!point || !area.Contains(point.position,1))continue;
                if(Vector3.Distance(point.position,player.transform.position)<area.minimumSpawnDistance)continue;
                bool occupied=false;foreach(var e in enemies)if(e && Vector3.Distance(e.transform.position,point.position)<1.8f)occupied=true;
                if(!occupied){chosen=point.position;found=true;break;}
            }
            }
            if(!found)return null;
            var instance=Instantiate(group.prefab,chosen,Quaternion.identity);instance.Bind(this,area.healthMultiplier,area.attackIntervalMultiplier);instance.BindArea(area);instance.PrepareSpawn(area.spawnWarning);
            enemies.Add(instance);spawned++;return instance;
        }
        void CompleteShow(){State=ShowState.Victory;StageFlow=CircusStageFlow.GameClear;ClearProjectiles();hud.Announce("3막 완료 · 게임 클리어",0);CircusAudio.Play(CircusCue.Clear);}
    }
}
