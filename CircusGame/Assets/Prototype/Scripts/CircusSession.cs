using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CircusPrototype
{
    public enum ShowState { Menu, Countdown, Playing, Intermission, Paused, Victory, Defeat }

    public sealed class CircusSession : MonoBehaviour
    {
        [SerializeField] CircusPlayer player;
        [SerializeField] CircusEnemy closeEnemy;
        [SerializeField] CircusEnemy rangedEnemy;
        [SerializeField] Transform[] spawnPoints;
        [SerializeField] CircusHud hud;
        [SerializeField] int[] roundCounts = { 8, 12 };
        [SerializeField] float spawnInterval = 1.3f;
        [SerializeField] int maxAlive = 6;
        readonly List<CircusEnemy> enemies = new List<CircusEnemy>();
        Coroutine roundRoutine;
        int spawned;
        int defeated;
        int round;
        int score;
        bool infected;
        public ShowState State { get; private set; } = ShowState.Menu;
        public int CountdownNumber { get; private set; }
        public CircusPlayer Player => player;
        public IReadOnlyList<CircusEnemy> Enemies => enemies;
        public int Round => round + 1;
        public int Remaining => State == ShowState.Menu ? roundCounts[0] : roundCounts[round] - defeated;
        public int Alive => enemies.Count;
        public int Spawned => spawned;
        public int Score => score;
        public bool Infected => infected;
        public bool IsRunning => State == ShowState.Playing || State == ShowState.Intermission;

        void Awake()
        {
            Time.timeScale = 1;
            player.Bind(this);
            hud.Bind(this);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        void Update()
        {
            if (UnityEngine.InputSystem.Keyboard.current?.escapeKey.wasPressedThisFrame == true)
            {
                if (IsRunning) Pause();
                else if (State == ShowState.Paused) Resume();
            }
        }

        public void BeginShow()
        {
            ClearShow();

            round = score = spawned = defeated = 0;
            infected = false;

            player.ResetPerformer();
            hud.CloseSettings();

            State = ShowState.Countdown;
            CountdownNumber = 3;

            roundRoutine = StartCoroutine(StartCountdown());
        }

        IEnumerator StartCountdown()
        {
            for (int i = 3; i >= 1; i--)
            {
                CountdownNumber = i;
                yield return new WaitForSeconds(1f);
            }

            CountdownNumber = 0;
            State = ShowState.Playing;

            hud.Announce("제1막 · 무대 위의 랑", 3);

            roundRoutine = StartCoroutine(RunRound());
        }

        IEnumerator RunRound()
        {
            yield return new WaitForSeconds(1.5f);
            while (spawned < roundCounts[round])
            {
                if (enemies.Count < maxAlive)
                {
                    SpawnNext();
                    yield return new WaitForSeconds(spawnInterval);
                }
                else yield return null;
            }
            while (enemies.Count > 0) yield return null;
            if (round + 1 < roundCounts.Length)
            {
                State = ShowState.Intermission;
                hud.Announce("제1막 종료 · 잠시 후 제2막", 4);
                player.RestoreHealth(20);
                yield return new WaitForSeconds(4);
                round++;
                spawned = defeated = 0;
                State = ShowState.Playing;
                hud.Announce("제2막 · 방울을 울려라", 3);
                roundRoutine = StartCoroutine(RunRound());
            }
            else
            {
                State = ShowState.Victory;
                ClearProjectiles();
                hud.Announce(infected ? "감염된 무인 · 공연은 끝났다" : "살아남은 무인 · 공연은 끝났다", 0);
            }
        }

        public CircusEnemy SpawnNext()
        {
            if (spawned >= roundCounts[round]) return null;
            var candidates = new List<Transform>();
            foreach (var point in spawnPoints)
            {
                bool occupied = false;
                foreach (var enemy in enemies)
                    if ((enemy.transform.position - point.position).sqrMagnitude < 3) occupied = true;
                if (!occupied && (player.transform.position - point.position).sqrMagnitude > 9)
                    candidates.Add(point);
            }
            if (candidates.Count == 0) return null;
            var prefab = round > 0 && spawned % 3 == 1 ? rangedEnemy : closeEnemy;
            var pointToUse = candidates[Random.Range(0, candidates.Count)];
            var instance = Instantiate(prefab, pointToUse.position, Quaternion.identity);
            instance.Bind(this);
            enemies.Add(instance);
            spawned++;
            return instance;
        }

        public void EnemyDefeated(CircusEnemy enemy)
        {
            if (!enemies.Remove(enemy)) return;
            defeated++;
            score += enemy.IsRanged ? 150 : 100;
        }

        public void HealthChanged()
        {
            if (player.Health > 0 && player.Health < 10 && !infected)
            {
                infected = true;
                hud.Announce("감염 · 방울 소리가 변한다", 3);
            }
            if (player.Health <= 0 && IsRunning)
            {
                if (roundRoutine != null) StopCoroutine(roundRoutine);
                State = ShowState.Defeat;
                ClearProjectiles();
                hud.Announce("무인이 쓰러졌다", 0);
            }
        }

        public void Pause() { pausedFrom = State; State = ShowState.Paused; Time.timeScale = 0; }
        ShowState pausedFrom = ShowState.Playing;
        public void TogglePause()
        {
            if (State == ShowState.Paused) Resume();
            else if (IsRunning) { pausedFrom = State; Pause(); }
        }
        public void Resume() { State = pausedFrom; Time.timeScale = 1; }
        public void ReturnToMenu()
        {
            ClearShow();
            State = ShowState.Menu;
            hud.CloseSettings();
            player.ResetPerformer();
        }
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
            Time.timeScale = 1;
            if (roundRoutine != null) StopCoroutine(roundRoutine);
            foreach (var enemy in enemies) if (enemy) Destroy(enemy.gameObject);
            enemies.Clear();
            ClearProjectiles();
        }
        void ClearProjectiles()
        {
            foreach (var shot in FindObjectsByType<CircusProjectile>(FindObjectsSortMode.None))
                Destroy(shot.gameObject);
        }
        void OnDestroy() { Time.timeScale = 1; }
    }
}
