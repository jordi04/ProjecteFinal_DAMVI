using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class MushroomAI : EnemyController
{
    [Header("Jump Settings")]
    [SerializeField] float jumpRange = 27f;         // Distancia para activar salto
    [SerializeField] float jumpForce = 20f;        // Fuerza total del salto
    [SerializeField] float jumpCooldown = 2f;      // Tiempo entre saltos
    [SerializeField] float jumpHeight = 0.5f;      // Altura del salto
    [SerializeField] float horizontalMultiplier = 3f; // Control distancia horizontal

    [Header("Contact Damage")]
    [SerializeField] float contactDamage = 10f;    // Daño por contacto
    [SerializeField] float contactCooldown = 0.5f; // Tiempo entre daños

    [Header("Visual Settings")]
    [SerializeField] Renderer mushroomRenderer;    // Referencia visual

    private float lastContactTime;
    private bool isJumping;

    protected override void Awake()
    {
        movementType = MovementType.NavMesh;
        attackType = AttackType.Melee;

        base.Awake();
        ConfigureComponents();
    }

    void ConfigureComponents()
    {
        // Configuración física
        if (enemyRigidbody != null)
        {
            enemyRigidbody.isKinematic = false;
            enemyRigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            enemyRigidbody.mass = 1.5f;
            enemyRigidbody.drag = 1f;
        }

        // Configuración visual
        if (enemyRenderer == null && mushroomRenderer != null)
            enemyRenderer = mushroomRenderer;

        // Configuración del NavMeshAgent
        NavMeshAgent agent = GetComponent<NavMeshAgent>();
        agent.stoppingDistance = 0.1f;
        agent.angularSpeed = 720f;
        agent.acceleration = 50f;
    }

    protected override void InitializeStrategies()
    {
        movementStrategy = new JumpingMovement(
            moveSpeed,
            jumpRange,
            faceTarget,
            avoidObstacles,
            jumpRange,
            jumpForce,
            jumpCooldown,
            jumpHeight,
            horizontalMultiplier,
            attackableLayerMask,
            this
        );
    }

    // Daño por contacto continuo
    void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player") &&
            Time.time > lastContactTime + contactCooldown)
        {
            collision.gameObject.GetComponent<IDamageable>()?.TakeDamage(contactDamage);
            lastContactTime = Time.time;
        }
    }

    protected override void Update()
    {
        base.Update();
        if (isDead) return;

        ((JumpingMovement)movementStrategy).UpdateRotation(target);
        ((JumpingMovement)movementStrategy).CheckJump(enemyRigidbody, target);
    }

    protected class JumpingMovement : NavMeshMovement
    {
        private MushroomAI mushroom;
        private float jumpRange;
        private float jumpForce;
        private float jumpCooldown;
        private float jumpHeight;
        private float horizontalMultiplier;
        private bool canJump = true;

        public JumpingMovement(
            float speed,
            float stopDistance,
            bool faceTarget,
            bool avoidObstacles,
            float jumpRange,
            float jumpForce,
            float jumpCooldown,
            float jumpHeight,
            float horizontalMultiplier,
            LayerMask attackMask,
            MushroomAI mushroom) : base(speed, stopDistance, faceTarget, avoidObstacles)
        {
            this.mushroom = mushroom;
            this.jumpRange = jumpRange;
            this.jumpForce = jumpForce;
            this.jumpCooldown = jumpCooldown;
            this.jumpHeight = jumpHeight;
            this.horizontalMultiplier = horizontalMultiplier;
        }

        public void UpdateRotation(Transform target)
        {
            if (target != null && Agent != null)
            {
                Vector3 direction = (target.position - Agent.transform.position).normalized;
                direction.y = 0;

                if (direction != Vector3.zero)
                {
                    Quaternion targetRot = Quaternion.LookRotation(direction);
                    Agent.transform.rotation = Quaternion.RotateTowards(
                        Agent.transform.rotation,
                        targetRot,
                        720f * Time.deltaTime
                    );
                }
            }
        }

        public void CheckJump(Rigidbody rb, Transform target)
        {
            if (canJump && target != null &&
                Vector3.Distance(rb.position, target.position) <= jumpRange)
            {
                mushroom.StartCoroutine(PerformJump(rb, target));
            }
        }

        private IEnumerator PerformJump(Rigidbody rb, Transform target)
        {
            canJump = false;
            mushroom.isJumping = true;

            // Desactivar control del agente
            Agent.isStopped = true;
            Agent.updatePosition = false;

            // Calcular dirección del salto
            Vector3 jumpDirection = (target.position - rb.position).normalized;
            jumpDirection = new Vector3(
                jumpDirection.x * horizontalMultiplier,
                jumpHeight,
                jumpDirection.z * horizontalMultiplier
            );

            // Rotación y fuerza
            rb.rotation = Quaternion.LookRotation(jumpDirection);
            rb.AddForce(jumpDirection * jumpForce, ForceMode.VelocityChange);

            // Tiempo en aire dinámico
            float airTime = Mathf.Clamp(jumpForce * 0.03f, 0.5f, 1f);
            yield return new WaitForSeconds(airTime);

            // Reactivar agente
            Agent.Warp(rb.position);
            Agent.stoppingDistance = 0.1f;
            Agent.updatePosition = true;
            Agent.isStopped = false;
            Agent.SetDestination(target.position);

            yield return new WaitForSeconds(jumpCooldown);
            canJump = true;
            mushroom.isJumping = false;
        }
    }

    void OnDrawGizmosSelected()
    {
        // Gizmo de rango de salto
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, jumpRange);

        // Gizmo de daño por contacto
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
    }
}