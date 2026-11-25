using UnityEngine;

namespace Luvsic.Graybox
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class DemoEnemy : MonoBehaviour
    {
        [SerializeField] private GrayboxFlow flow;
        [SerializeField] private GrayboxPlayer player;
        [SerializeField] private Transform model;
        [SerializeField] private MeshRenderer body;
        [SerializeField] private Material warningMaterial;
        [SerializeField] private Material hitMaterial;

        private const float AttackRange = 1.75f;
        private const float AttackWindup = 0.6f;

        private CharacterController controller;
        private Vector3 home;
        private Quaternion modelRest;
        private Material normalMaterial;
        private int health = 45;
        private float windup;
        private float recovery;
        private float hurt;
        private bool charging;
        private bool alerted;
        private bool returning;
        private int patrolDirection = 1;

        public int Health => health;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            home = transform.position;
            modelRest = model.localRotation;
            normalMaterial = body.sharedMaterial;
        }

        private void Update()
        {
            if (health <= 0 || !flow.Simulating)
                return;

            float deltaTime = Time.deltaTime;
            recovery = Mathf.Max(0f, recovery - deltaTime);
            hurt = Mathf.Max(0f, hurt - deltaTime);
            Vector3 offset = player.transform.position - transform.position;
            offset.y = 0f;
            float distance = offset.magnitude;
            bool lineOfSight = !Physics.Linecast(transform.position + Vector3.up * 1.2f,
                player.transform.position + Vector3.up * 1.3f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            Vector3 move = Vector3.zero;

            if (charging)
            {
                windup -= deltaTime;
                Face(offset);
                if (windup <= 0f)
                {
                    charging = false;
                    recovery = 1.0f;
                    if (distance <= AttackRange && lineOfSight)
                        flow.TakeHit(18, transform.position);
                }
            }
            else if (returning || Vector3.Distance(home, transform.position) > 9f)
            {
                alerted = false;
                Vector3 toHome = home - transform.position;
                toHome.y = 0f;
                returning = toHome.sqrMagnitude > 0.25f;
                if (returning)
                    move = toHome.normalized * 1.6f;
            }
            else if ((lineOfSight && distance <= 7f) || (alerted && distance <= 8f))
            {
                alerted = true;
                Face(offset);
                if (distance > AttackRange - 0.2f)
                    move = offset.normalized * 2.45f;
                else if (recovery <= 0f && lineOfSight)
                {
                    charging = true;
                    windup = AttackWindup;
                }
            }
            else if (alerted)
            {
                returning = true;
            }

            else
            {
                Vector3 patrolTarget = home + Vector3.right * (patrolDirection * 1.1f);
                Vector3 direction = patrolTarget - transform.position;
                direction.y = 0f;
                if (direction.sqrMagnitude < 0.11f)
                    patrolDirection = -patrolDirection;
                else
                    move = direction.normalized * 0.95f;
            }

            Face(move);
            controller.Move((move + Vector3.down * 2f) * deltaTime);
            model.localRotation = charging ? modelRest * Quaternion.Euler(-17f, 0f, -11f) : modelRest;
            body.sharedMaterial = hurt > 0f ? hitMaterial : charging ? warningMaterial : normalMaterial;
        }

        public void TakeHit(int damage)
        {
            if (health <= 0)
                return;

            health = Mathf.Max(0, health - damage);
            hurt = 0.22f;
            alerted = true;
            if (health > 0)
                return;

            controller.enabled = false;
            body.sharedMaterial = normalMaterial;
            model.localRotation = modelRest * Quaternion.Euler(0f, 0f, 89f);
            model.localPosition += new Vector3(0.06f, 0.17f, 0f);
        }

        private void Face(Vector3 direction)
        {
            if (direction.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(direction);
        }
    }
}
