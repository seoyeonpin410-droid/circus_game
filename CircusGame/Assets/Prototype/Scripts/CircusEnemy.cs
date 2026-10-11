using UnityEngine;

namespace CircusPrototype
{
    public sealed class CircusEnemy : MonoBehaviour
    {
        [SerializeField] bool ranged;
        [SerializeField] float health = 50;
        [SerializeField] float attackInterval = 2.3f;
        [SerializeField] float meleeRange = 2.2f;
        [SerializeField] float moveSpeed = 2.6f;
        [SerializeField] CircusProjectile projectile;
        [SerializeField] Transform model;
        [SerializeField] UnityEngine.UI.Image healthFill;
        [SerializeField] LineRenderer warning;
        CircusSession session;
        float nextAttack;
        float maxHealth;
        bool dead;
        CharacterController controller;
        float verticalSpeed;
        public bool IsRanged => ranged;
        public float Health => health;
        public void Bind(CircusSession value)
        {
            session = value;
            controller = GetComponent<CharacterController>();
            maxHealth = health;
            nextAttack = Time.time + Random.Range(1.5f, 2.5f);
        }
        void Update()
        {
            if (!session || session.State != ShowState.Playing || dead) return;
            var direction = session.Player.transform.position - transform.position;
            direction.y = 0;
            if (!ranged && controller)
            {
                var movement = direction.magnitude > meleeRange * .72f ? direction.normalized : Vector3.zero;
                // Keep the crowd spread out rather than stacking on one point.
                foreach (var other in session.Enemies)
                {
                    if (!other || other == this) continue;
                    var away = transform.position - other.transform.position;
                    away.y = 0;
                    if (away.sqrMagnitude > .001f && away.sqrMagnitude < 1.44f)
                        movement += away.normalized * (1.2f - away.magnitude);
                }
                if (controller.isGrounded && verticalSpeed < 0) verticalSpeed = -2;
                verticalSpeed -= 20 * Time.deltaTime;
                controller.Move((Vector3.ClampMagnitude(movement, 1) * moveSpeed
                    + Vector3.up * verticalSpeed) * Time.deltaTime);
                direction = session.Player.transform.position - transform.position;
                direction.y = 0;
            }
            if (direction.sqrMagnitude > .001f) model.rotation = Quaternion.LookRotation(direction);
            bool canAttack = ranged || (direction.magnitude < meleeRange
                && Mathf.Abs(session.Player.transform.position.y - transform.position.y) < 1.4f);
            if (warning)
            {
                warning.enabled = canAttack && nextAttack - Time.time < .7f;
                warning.SetPosition(0, transform.position + Vector3.up * .15f);
                warning.SetPosition(1, session.Player.transform.position + Vector3.up * .15f);
            }
            if (!canAttack || Time.time < nextAttack) return;
            nextAttack = Time.time + attackInterval;
            if (ranged)
            {
                var origin = transform.position + Vector3.up * 1.1f;
                var shot = Instantiate(projectile, origin, Quaternion.identity);
                shot.Launch((session.Player.transform.position + Vector3.up - origin).normalized, session);
            }
            else session.Player.TakeDamage(12);
        }
        public void TakeDamage(float damage)
        {
            if (dead) return;
            health = Mathf.Max(0, health - damage);
            if (healthFill) healthFill.fillAmount = health / maxHealth;
            if (health > 0) return;
            dead = true;
            session.EnemyDefeated(this);
            Destroy(gameObject);
        }
        void LateUpdate()
        {
            if (healthFill && Camera.main) healthFill.canvas.transform.rotation = Camera.main.transform.rotation;
        }
    }
}
