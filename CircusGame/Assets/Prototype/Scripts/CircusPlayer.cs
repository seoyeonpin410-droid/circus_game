using UnityEngine;
using UnityEngine.InputSystem;

namespace CircusPrototype
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class CircusPlayer : MonoBehaviour
    {
        [SerializeField] Transform visual;
        [SerializeField] Transform bell;
        [SerializeField] Camera aimCamera;
        [SerializeField] float moveSpeed = 6;
        [SerializeField] float jumpHeight = 1.8f;
        [SerializeField] float attackRange = 3.6f;
        [SerializeField] float attackCooldown = .45f;
        [SerializeField] LayerMask enemyMask;
        [SerializeField] LineRenderer bellWave;
        [SerializeField] AudioSource bellAudio;
        [SerializeField] CircusProjectile soundProjectile;
        CharacterController controller;
        CircusSession session;
        Vector3 startPosition;
        float verticalSpeed;
        float nextAttack;
        float nextShot;
        float lastDamage = -100;
        float attackTime = -100;
        Quaternion bellRest;
        CircusCamera cameraRig;
        Renderer[] bodyRenderers;
        readonly Collider[] attackHits = new Collider[64];
        readonly System.Collections.Generic.HashSet<CircusEnemy> hitEnemies = new System.Collections.Generic.HashSet<CircusEnemy>();
        CircusAttackArea meleeArea;
        bool meleePending;
        float meleeReady;
        Vector3 meleeOrigin, meleeForward;
        public bool MeleePending => meleePending;
        public CircusAttackArea MeleeArea => meleeArea;
        public float Health { get; private set; } = 100;
        public int AttackCount { get; private set; }
        public Vector3 AimDirection { get; private set; } = Vector3.forward;
        public float AttackSwing => Mathf.Sin(Mathf.Clamp01((Time.time - attackTime) / .28f) * Mathf.PI);
        public void SetFirstPersonBodyHidden(bool hidden)
        {
            if (bodyRenderers == null) bodyRenderers = visual.GetComponentsInChildren<Renderer>(true);
            foreach (var renderer in bodyRenderers) renderer.enabled = !hidden;
        }
        public void SetPresentationVisible(bool value) { if (visual.gameObject.activeSelf != value) visual.gameObject.SetActive(value); }
        public void Bind(CircusSession value)
        {
            session = value;
            cameraRig = aimCamera.GetComponent<CircusCamera>();
            controller = GetComponent<CharacterController>();
            startPosition = transform.position;
            bellRest = bell.localRotation;
            meleeArea = CircusAttackArea.Create("Player melee reach", transform);
        }
        void Update()
        {
            if (!session || !session.IsRunning) { if (meleeArea) meleeArea.Hide(); return; }
            var keyboard = Keyboard.current;
            Vector2 input = Vector2.zero;
            if (keyboard != null)
            {
                input.x = (keyboard.dKey.isPressed ? 1 : 0) - (keyboard.aKey.isPressed ? 1 : 0);
                input.y = (keyboard.wKey.isPressed ? 1 : 0) - (keyboard.sKey.isPressed ? 1 : 0);
            }
            input = Vector2.ClampMagnitude(input, 1);
            var forward = Vector3.ProjectOnPlane(aimCamera.transform.forward, Vector3.up).normalized;
            var right = Vector3.ProjectOnPlane(aimCamera.transform.right, Vector3.up).normalized;
            var movement = (forward * input.y + right * input.x) * moveSpeed;
            if (controller.isGrounded && verticalSpeed < 0) verticalSpeed = -2;
            if (controller.isGrounded && keyboard?.spaceKey.wasPressedThisFrame == true)
                verticalSpeed = Mathf.Sqrt(jumpHeight * 40);
            verticalSpeed -= 20 * Time.deltaTime;
            controller.Move((movement + Vector3.up * verticalSpeed) * Time.deltaTime);
            if (meleePending)
            {
                meleeArea.Show(meleeOrigin, meleeForward, attackRange, Mathf.Acos(.25f) * Mathf.Rad2Deg,
                    new Color(1,.75f,.15f,.25f));
                if (Time.time >= meleeReady) FinishMelee();
            }
            var mouse = Mouse.current;
            if (mouse != null)
            {
                AimDirection = cameraRig.AimForward;
                if (mouse.leftButton.isPressed && session.State == ShowState.Playing
                    && (Cursor.lockState == CursorLockMode.Locked
                        || !(UnityEngine.EventSystems.EventSystem.current?.IsPointerOverGameObject() ?? false)))
                    Fire();
                if (mouse.rightButton.isPressed && session.State == ShowState.Playing
                    && (Cursor.lockState == CursorLockMode.Locked
                        || !(UnityEngine.EventSystems.EventSystem.current?.IsPointerOverGameObject() ?? false)))
                    Attack();
            }
            visual.rotation = Quaternion.LookRotation(AimDirection);
            float swing = Mathf.Clamp01((Time.time - attackTime) / .28f);
            bell.localRotation = bellRest * Quaternion.Euler(0, 0, Mathf.Sin(swing * Mathf.PI) * -65);
            if (bellWave) bellWave.enabled = Time.time - attackTime < .18f;
        }
        public bool Fire()
        {
            if (!session || session.State != ShowState.Playing || !soundProjectile || Time.time < nextShot) return false;
            nextShot = Time.time + attackCooldown;
            attackTime = Time.time;
            AttackCount++;
            CircusAudio.Play(CircusCue.PlayerShot, transform.position);
            var ray = aimCamera.ViewportPointToRay(new Vector3(.5f, .5f));
            var aimPoint = Physics.Raycast(ray, out var hit, 40, (1 << 0) | enemyMask.value,
                QueryTriggerInteraction.Ignore) ? hit.point : ray.GetPoint(40);
            var origin = transform.position + Vector3.up * 1.3f + AimDirection * .65f;
            var shot = Instantiate(soundProjectile, origin, Quaternion.identity);
            shot.LaunchPlayer(aimPoint - origin, session, session.Infected ? 40 : 25);
            return true;
        }
        public bool Attack()
        {
            if (!session || !session.IsRunning || meleePending || Time.time < nextAttack) return false;
            nextAttack = Time.time + attackCooldown;
            meleePending = true; meleeReady = Time.time + .18f;
            meleeOrigin = transform.position; meleeForward = cameraRig.AimForward;
            meleeArea.Show(meleeOrigin, meleeForward, attackRange, Mathf.Acos(.25f)*Mathf.Rad2Deg, new Color(1,.75f,.15f,.25f));
            return true;
        }
        void FinishMelee()
        {
            meleePending = false; attackTime = Time.time; AttackCount++;
            CircusAudio.Play(CircusCue.PlayerMelee, transform.position);
            int count = Physics.OverlapSphereNonAlloc(meleeOrigin + Vector3.up, attackRange + 1,
                attackHits, enemyMask, QueryTriggerInteraction.Collide);
            hitEnemies.Clear();
            for (int i = 0; i < count; i++)
            {
                var enemy = attackHits[i].GetComponentInParent<CircusEnemy>();
                if (enemy && hitEnemies.Add(enemy) && meleeArea.Contains(enemy.transform.position))
                {
                    enemy.TakeDamage(session.Infected ? 40 : 25);
                    if(session.Infected && !enemy.Spawning) cameraRig.Shake(.025f,.09f);
                }
            }
            meleeArea.Hide();
            if (bellWave)
            {
                for (int i = 0; i < bellWave.positionCount; i++)
                {
                    var direction = Quaternion.AngleAxis(Mathf.Lerp(-Mathf.Acos(.25f)*Mathf.Rad2Deg, Mathf.Acos(.25f)*Mathf.Rad2Deg,
                        i / (float)(bellWave.positionCount - 1)), Vector3.up) * meleeForward;
                    bellWave.SetPosition(i, meleeOrigin + Vector3.up * .12f + direction * attackRange);
                }
                bellWave.enabled = true;
            }
        }
        public void TakeDamage(float amount)
        {
            if (!session || !session.IsRunning || Time.time - lastDamage < .65f) return;
            lastDamage = Time.time;
            CircusAudio.Play(CircusCue.PlayerHit, transform.position);
            Health = Mathf.Max(0, Health - amount);
            if(amount>=24) cameraRig.Shake(.04f,.12f);
            session.HealthChanged();
        }
        public void RestoreHealth(float amount) { Health = Mathf.Min(100, Health + amount); }
        public void ResetPerformer()
        {
            controller.enabled = false;
            transform.position = startPosition;
            controller.enabled = true;
            Health = 100;
            verticalSpeed = nextAttack = nextShot = 0;
            lastDamage = attackTime = -100;
            AttackCount = 0; meleePending = false;
            if (meleeArea) meleeArea.Hide();
            if (bellWave) bellWave.enabled = false;
        }
    }
}
