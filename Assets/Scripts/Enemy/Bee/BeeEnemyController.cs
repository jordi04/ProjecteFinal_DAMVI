using UnityEngine;
using UnityEngine.AI;

public class BeeEnemyController : EnemyController
{
    [Header("Bee Specific Settings")]
    [Tooltip("The speed at which the bee flies.")]
    [SerializeField] private float flightSpeed = 7f;

    [Tooltip("The height at which the bee hovers.")]
    [SerializeField] private float hoverHeight = 3f;

    [Tooltip("Enable or disable hovering behavior.")]
    [SerializeField] private bool enableHover = true;

    protected override void Awake()
    {
        base.Awake();

        // Customize the movement speed for the bee enemy.
        if (navAgent != null)
        {
            navAgent.speed = flightSpeed;
        }
    }

    protected override void Start()
    {
        base.Start();
        // Additional bee-specific initialization can be added here.
    }

    protected override void Update()
    {
        // Run the base update to handle core enemy behavior.
        base.Update();

        // Add bee-specific hovering behavior.
        if (enableHover && !isDead)
        {
            Hover();
        }
    }

    /// <summary>
    /// Makes the bee enemy smoothly hover to a specified height.
    /// </summary>
    private void Hover()
    {
        Vector3 currentPos = transform.position;
        float newY = Mathf.Lerp(currentPos.y, hoverHeight, Time.deltaTime);
        transform.position = new Vector3(currentPos.x, newY, currentPos.z);
    }
}
