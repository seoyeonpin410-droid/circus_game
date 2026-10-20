using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace CircusPrototype
{
    public enum ShowState { Menu, Countdown, Playing, Intermission, Paused, Victory, Defeat }
    public sealed class CircusSession : MonoBehaviour
    {
        [SerializeField] CircusPlayer player;
        [SerializeField] CircusEnemy closeEnemy, rangedEnemy, bossPrefab;
        [SerializeField] Transform[] spawnPoints;
        [SerializeField] CircusHud hud;
        [SerializeField] int[] roundCounts = { 8, 12 };
        [SerializeField] float spawnInterval = 1.3f;
        [SerializeField] int maxAlive = 6;
        [SerializeField] float intermissionSeconds = 2.5f;
        readonly List<CircusEnemy> enemies = new List<CircusEnemy>();
        Coroutine roundRoutine;
        int spawned, defeated, round, score;
        bool infected;
        public ShowState State { get; private set; } = ShowState.Menu;
        public CircusPlayer Player => player;
        public IReadOnlyList<CircusEnemy> Enemies => enemies;
        public CircusEnemy Boss { get; private set; }
        public int Round => round + 1;
        public int Remaining => round == 2 ? (Boss && Boss.Health > 0 ? 1 : 0) : Mathf.Max(0, roundCounts[Mathf.Min(round, 1)] - defeated);
        public int Alive => enemies.Count;
        public int Spawned => spawned;
        public int Score => score;
        public bool Infected => infected;
        public bool IsRunning => State == ShowState.Playing;
        public bool CanPause => IsRunning || State == ShowState.Countdown || State == ShowState.Intermission;
        public bool HasGameplayView => CanPause || State == ShowState.Paused;
        public float GameplaySeconds { get; private set; }
        public int CountdownNumber => Mathf.CeilToInt(CountdownRemaining);
        public string CountdownText { get; private set; } = "";
        public float CountdownRemaining { get; private set; }
        void Awake()
        {
            Time.timeScale = 1; player.Bind(this); hud.Bind(this);
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
        }
        void Update()
        {
            if (IsRunning) GameplaySeconds += Time.deltaTime;
            if (UnityEngine.InputSystem.Keyboard.current?.escapeKey.wasPressedThisFrame == true) TogglePause();
        }
        public void BeginShow()
        {
            ClearShow(); round = score = spawned = defeated = 0;
            GameplaySeconds = 0; infected = false; player.ResetPerformer();
            State = ShowState.Countdown; hud.CloseSettings(); roundRoutine = StartCoroutine(RunShow());
        }
        IEnumerator RunShow()
        {
            CountdownRemaining = 3; int lastNumber = 0;
            while (CountdownRemaining > 0)
            {
                int number = Mathf.CeilToInt(CountdownRemaining); CountdownText = number.ToString();
                if (number != lastNumber) { CircusAudio.Play(CircusCue.Countdown); lastNumber = number; }
                yield return null; CountdownRemaining = Mathf.Max(0, CountdownRemaining - Time.deltaTime);
            }
            CountdownText = "START!"; CircusAudio.Play(CircusCue.Start); State = ShowState.Playing;
            hud.Announce("1막 · 무대 위의 랑", 2);
            yield return new WaitForSeconds(.5f); CountdownText = "";
            for (round = 0; round < 3; round++)
            {
                spawned = defeated = 0;
                if (round < 2)
                {
                    while (spawned < roundCounts[round] || enemies.Count > 0)
                    {
                        int cap = round == 0 ? Mathf.Min(4, maxAlive) : maxAlive;
                        if (spawned < roundCounts[round] && enemies.Count < cap)
                        { SpawnNext(); yield return new WaitForSeconds(spawnInterval * (round == 0 ? 1 : .75f)); }
                        else yield return null;
                    }
                }
                else
                {
                    Boss = Instantiate(bossPrefab, FarthestSpawn().position, Quaternion.identity);
                    Boss.Bind(this); enemies.Add(Boss); spawned = 1; CircusAudio.Play(CircusCue.BossArrival);
                    while (Boss && Boss.Health > 0) yield return null;
                    break;
                }
                State = ShowState.Intermission; ClearProjectiles();
                hud.Announce(round == 0 ? "1막 완료 · 잠시 후 2막" : "2막 완료 · 잠시 후 3막 - Boss", intermissionSeconds);
                player.RestoreHealth(20); yield return new WaitForSeconds(intermissionSeconds);
                hud.Announce(round == 0 ? "2막 · 거세지는 방울 소리" : "3막 - Boss · 도깨비 대장", 2.5f);
                CircusAudio.Play(CircusCue.ActChange); State = ShowState.Playing;
            }
            State = ShowState.Victory; ClearProjectiles(); hud.Announce("3막 완료 · 게임 클리어", 0); CircusAudio.Play(CircusCue.Clear);
        }
        Transform FarthestSpawn()
        {
            var chosen = spawnPoints[0]; float distance = -1;
            foreach (var point in spawnPoints)
            {
                float d = (player.transform.position - point.position).sqrMagnitude;
                if (d > distance) { distance = d; chosen = point; }
            }
            return chosen;
        }
        public CircusEnemy SpawnNext()
        {
            if (!IsRunning || round > 1 || spawned >= roundCounts[round]) return null;
            var candidates = new List<Transform>();
            foreach (var point in spawnPoints)
            {
                bool occupied = false;
                foreach (var enemy in enemies) if (enemy && (enemy.transform.position - point.position).sqrMagnitude < 3) occupied = true;
                if (!occupied && (player.transform.position - point.position).sqrMagnitude > 9) candidates.Add(point);
            }
            if (candidates.Count == 0) return null;
            var prefab = round > 0 && spawned % 3 == 1 ? rangedEnemy : closeEnemy;
            var instance = Instantiate(prefab, candidates[Random.Range(0, candidates.Count)].position, Quaternion.identity);
            instance.Bind(this, round == 0 ? 1 : 1.4f, round == 0 ? 1 : .8f);
            enemies.Add(instance); spawned++; return instance;
        }
        public void EnemyDefeated(CircusEnemy enemy)
        {
            if (!enemies.Remove(enemy)) return;
            defeated++; score += enemy.IsBoss ? 1200 : enemy.IsRanged ? 150 : 100;
        }
        public void HealthChanged()
        {
            if (player.Health > 0 && player.Health < 10 && !infected)
            { infected = true; hud.Announce("감염 · 방울 소리가 변한다", 3); }
            if (player.Health <= 0 && IsRunning)
            {
                if (roundRoutine != null) StopCoroutine(roundRoutine);
                State = ShowState.Defeat; ClearProjectiles(); CountdownText = "";
                hud.Announce("무인이 쓰러졌다", 0); CircusAudio.Play(CircusCue.PlayerDeath);
            }
        }
        ShowState pausedFrom;
        public void Pause() { if (!CanPause) return; pausedFrom = State; State = ShowState.Paused; Time.timeScale = 0; }
        public void TogglePause() { if (State == ShowState.Paused) Resume(); else Pause(); }
        public void Resume() { if (State != ShowState.Paused) return; State = pausedFrom; Time.timeScale = 1; }
        public void ReturnToMenu() { ClearShow(); State = ShowState.Menu; hud.CloseSettings(); player.ResetPerformer(); }
        public void Quit()
        {
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
        }
        void ClearShow()
        {
            Time.timeScale = 1; if (roundRoutine != null) StopCoroutine(roundRoutine);
            foreach (var enemy in enemies) if (enemy) Destroy(enemy.gameObject);
            enemies.Clear(); Boss = null; CountdownText = ""; CountdownRemaining = 0;
            ClearProjectiles(); CircusAudio.StopFlights();
        }
        void ClearProjectiles()
        {
            foreach (var shot in FindObjectsByType<CircusProjectile>(FindObjectsSortMode.None)) Destroy(shot.gameObject);
        }
        void OnDestroy() { Time.timeScale = 1; }
    }
}
