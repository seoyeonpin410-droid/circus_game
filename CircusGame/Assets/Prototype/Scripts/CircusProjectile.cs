using UnityEngine;

namespace CircusPrototype
{
    public sealed class CircusProjectile : MonoBehaviour
    {
        [SerializeField] float speed = 7;
        [SerializeField] float life = 6;
        CircusSession session;
        Vector3 velocity;
        float age;
        bool playerShot;
        float damage;
        public bool IsPlayerShot => playerShot;
        public void Launch(Vector3 direction, CircusSession value) { velocity = direction * speed; session = value; }
        public void LaunchPlayer(Vector3 direction, CircusSession value, float amount)
        {
            playerShot = true;
            damage = amount;
            velocity = direction.normalized * 18;
            session = value;
            life = 2;
        }
        void Update()
        {
            if (!session || session.State != ShowState.Playing) return;
            var step = velocity * Time.deltaTime;
            if (Physics.SphereCast(transform.position, .18f, step.normalized, out var hit, step.magnitude,
                (1 << 0) | (1 << (playerShot ? 9 : 8)), QueryTriggerInteraction.Ignore))
            {
                if (playerShot) hit.collider.GetComponentInParent<CircusEnemy>()?.TakeDamage(damage);
                else hit.collider.GetComponentInParent<CircusPlayer>()?.TakeDamage(10);
                Destroy(gameObject);
                return;
            }
            transform.position += step;
            age += Time.deltaTime;
            if (age >= life) Destroy(gameObject);
        }
    }
}
