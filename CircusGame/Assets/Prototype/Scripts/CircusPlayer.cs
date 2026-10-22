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
        [SerializeField, Min(1)] float sprintMultiplier = 1.6f;
        [SerializeField] float jumpHeight = 1.8f;
        [SerializeField] float attackRange = 3.6f;
        [SerializeField] float attackCooldown = .56f;
        [SerializeField] float rangedCooldown=.65f, meleeWindup=.11f, meleeSwing=.09f, meleeRecovery=.18f;
        [SerializeField] float meleeLunge=.08f, meleeCounterYaw=14, meleeSwingYaw=24;
        [SerializeField,Range(.1f,1)] float bossDamageScale=.5f;
        [SerializeField,Range(.03f,.05f)] float hitStopSeconds=.04f;
        Vector3 visualRest; float meleeStarted=-100; bool slashShown;
        public float BossDamageScale=>bossDamageScale;
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
        bool sprinting;
        [Header("Q dash: Shift remains sprint")]
        [SerializeField,Min(.1f)] float dashDuration=.2f,dashDistance=3.5f,dashCooldown=1.5f;
        [SerializeField,Range(0,.15f)] float dashInvulnerability=.12f;
        float dashUntil=-100,nextDash,dashImmuneUntil=-100,dashRemaining;
        Vector3 dashDirection;TrailRenderer dashTrail;
        public bool IsDashing=>session && session.IsRunning && Time.time<dashUntil;
        public bool DashInvulnerable=>IsDashing && Time.time<dashImmuneUntil;
        public float DashCooldownRemaining=>Mathf.Max(0,nextDash-Time.time);
        public bool Dash(Vector3 direction){
            if(!session || !session.IsRunning || Time.time<nextDash || IsDashing)return false;
            direction.y=0;dashDirection=direction.sqrMagnitude>.01f?direction.normalized:cameraRig.AimForward;
            dashUntil=Time.time+Mathf.Max(.1f,dashDuration);nextDash=Time.time+Mathf.Max(.1f,dashCooldown);dashImmuneUntil=Time.time+Mathf.Min(dashInvulnerability,dashDuration);dashRemaining=dashDistance;
            if(!dashTrail){var go=new GameObject("Short player dash trail");go.transform.SetParent(transform,false);go.transform.localPosition=Vector3.up*.45f;dashTrail=go.AddComponent<TrailRenderer>();dashTrail.sharedMaterial=CircusCombatFx.Glow;dashTrail.time=.12f;dashTrail.startWidth=.45f;dashTrail.endWidth=0;dashTrail.startColor=new Color(1,.8f,.4f,.6f);dashTrail.endColor=Color.clear;}
            dashTrail.Clear();dashTrail.emitting=true;CircusCombatFx.Burst(transform.position+Vector3.up*.1f,new Color(.8f,.7f,.45f,.5f),8,.15f,1);return true;
        }
        public bool MeleePending => meleePending;
        public CircusAttackArea MeleeArea => meleeArea;
        public float MoveSpeed=>moveSpeed;
        public bool IsSprinting=>session && session.IsRunning && sprinting && !IsDashing;
        public float CurrentMoveSpeed=>moveSpeed*(IsSprinting?Mathf.Max(1,sprintMultiplier):1);
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
            bellRest = bell.localRotation; visualRest=visual.localPosition;
            meleeArea = CircusAttackArea.Create("Player melee reach", transform);
        }
        void Update()
        {
            if (!session || !session.IsRunning) { sprinting=false;if(dashTrail)dashTrail.emitting=false; if (meleeArea) meleeArea.Hide(); return; }
            var keyboard = Keyboard.current;
            Vector2 input = Vector2.zero;
            if (keyboard != null)
            {
                input.x = (keyboard.dKey.isPressed ? 1 : 0) - (keyboard.aKey.isPressed ? 1 : 0);
                input.y = (keyboard.wKey.isPressed ? 1 : 0) - (keyboard.sKey.isPressed ? 1 : 0);
            }
            input = Vector2.ClampMagnitude(input, 1);
            sprinting=input.sqrMagnitude>.001f && keyboard!=null && (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed);
            var forward = Vector3.ProjectOnPlane(aimCamera.transform.forward, Vector3.up).normalized;
            var right = Vector3.ProjectOnPlane(aimCamera.transform.right, Vector3.up).normalized;
            var movement = (forward * input.y + right * input.x) * CurrentMoveSpeed;
            if(keyboard?.qKey.wasPressedThisFrame==true)Dash(movement);
            if(IsDashing){float step=Mathf.Min(dashRemaining,dashDistance/Mathf.Max(.1f,dashDuration)*Time.deltaTime);movement=dashDirection*(step/Mathf.Max(.0001f,Time.deltaTime));dashRemaining-=step;}
            if(dashTrail)dashTrail.emitting=IsDashing;
            if (controller.isGrounded && verticalSpeed < 0) verticalSpeed = -2;
            if (controller.isGrounded && keyboard?.spaceKey.wasPressedThisFrame == true)
                verticalSpeed = Mathf.Sqrt(jumpHeight * 40);
            verticalSpeed -= 20 * Time.deltaTime;
            controller.Move((movement + Vector3.up * verticalSpeed) * Time.deltaTime);
            if (meleePending)
            {
                if(!slashShown && Time.time>=meleeStarted+meleeWindup) { slashShown=true; CircusCombatFx.Slash(meleeOrigin,meleeForward,attackRange*.78f,.16f); }
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
            float elapsed=Time.time-meleeStarted, yaw=0, lunge=0;
            if(elapsed<meleeWindup) yaw=-meleeCounterYaw*Mathf.SmoothStep(0,1,elapsed/Mathf.Max(.01f,meleeWindup));
            else if(elapsed<meleeWindup+meleeSwing) {
                float t=(elapsed-meleeWindup)/Mathf.Max(.01f,meleeSwing);
                yaw=Mathf.Lerp(-meleeCounterYaw,meleeSwingYaw,t);lunge=meleeLunge*t;
            } else if(elapsed<meleeWindup+meleeSwing+meleeRecovery) {
                float t=1-Mathf.SmoothStep(0,1,(elapsed-meleeWindup-meleeSwing)/Mathf.Max(.01f,meleeRecovery));yaw=meleeSwingYaw*t;lunge=meleeLunge*t;
            }
            visual.rotation=Quaternion.LookRotation(elapsed<meleeWindup+meleeSwing+meleeRecovery?meleeForward:AimDirection)*Quaternion.Euler(0,yaw,0);
            visual.localPosition=visualRest+transform.InverseTransformDirection(meleeForward)*lunge;
            float swing = Mathf.Clamp01((Time.time - attackTime) / .28f);
            bell.localRotation = bellRest * Quaternion.Euler(0, 0, Mathf.Sin(swing * Mathf.PI) * -65);
            if (bellWave) bellWave.enabled = false;
        }
        public bool Fire()
        {
            if (!session || session.State != ShowState.Playing || !soundProjectile || Time.time < nextShot) return false;
            nextShot = Time.time + rangedCooldown;
            attackTime = Time.time;
            AttackCount++;
            CircusAudio.Play(CircusCue.PlayerShot, transform.position);
            var ray = aimCamera.ViewportPointToRay(new Vector3(.5f, .5f));
            var aimPoint = Physics.Raycast(ray, out var hit, 40, (1 << 0) | enemyMask.value,
                QueryTriggerInteraction.Ignore) ? hit.point : ray.GetPoint(40);
            var origin = transform.position + Vector3.up * 1.3f + AimDirection * .65f;
            var shot = Instantiate(soundProjectile, origin, Quaternion.identity);
            shot.LaunchPlayer(aimPoint - origin, session, session.Infected ? 40 : 25, bossDamageScale);
            return true;
        }
        public bool Attack()
        {
            if (!session || !session.IsRunning || meleePending || Time.time < nextAttack) return false;
            nextAttack = Time.time + attackCooldown;
            meleePending = true; meleeStarted=Time.time;slashShown=false; meleeReady = Time.time + meleeWindup+meleeSwing*.65f;
            meleeOrigin = transform.position; meleeForward = cameraRig.AimForward;
            meleeArea.Show(meleeOrigin, meleeForward, attackRange, Mathf.Acos(.25f)*Mathf.Rad2Deg, Color.clear);
            meleeArea.Hide();
            return true;
        }
        void FinishMelee()
        {
            meleePending = false; attackTime = Time.time; AttackCount++;
            CircusAudio.Play(CircusCue.PlayerMelee, transform.position);
            int count = Physics.OverlapSphereNonAlloc(meleeOrigin + Vector3.up, attackRange + 1,
                attackHits, enemyMask, QueryTriggerInteraction.Collide);
            hitEnemies.Clear(); bool landed=false;
            for (int i = 0; i < count; i++)
            {
                var enemy = attackHits[i].GetComponentInParent<CircusEnemy>();
                if (enemy && hitEnemies.Add(enemy) && meleeArea.Contains(enemy.transform.position))
                {
                    float before=enemy.Health;
                    enemy.TakeDamage((session.Infected ? 40 : 25)*(enemy.IsBoss?bossDamageScale:1));
                    if(enemy.Health<before)landed=true;
                }
            }
            meleeArea.Hide();
            if(landed) { session.RequestHitStop(hitStopSeconds);cameraRig.Shake(.018f,.08f);CircusAudio.PlayAccent(false,meleeOrigin); }
        }

        public void TakeDamage(float amount,bool groundShockwave=false)
        {
            if (!session || !session.IsRunning || Time.time - lastDamage < .65f) return;
            if(!groundShockwave && DashInvulnerable)return;
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
            AttackCount = 0; meleePending = false;meleeStarted=-100;visual.localPosition=visualRest;
            sprinting=false;dashUntil=dashImmuneUntil=-100;nextDash=0;dashRemaining=0;if(dashTrail){dashTrail.emitting=false;dashTrail.Clear();}
            if (meleeArea) meleeArea.Hide();
            if (bellWave) bellWave.enabled = false;
        }
    }
}
