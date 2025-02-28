using UnityEngine;
using StarterAssets;

public class HorseCameraHeadbob : MonoBehaviour
{
    [Header("Headbob Parameters")]
    [Tooltip("Enable/disable the headbob effect")]
    public bool enableHeadbob = true;

    [Tooltip("The target to apply headbob to (usually the camera)")]
    public Transform bobTarget;

    [Header("Amplitude")]
    [Tooltip("How much the camera moves up and down")]
    public float verticalBobAmount = 0.05f;
    [Tooltip("How much the camera moves side to side")]
    public float horizontalBobAmount = 0.05f;

    [Header("Frequency")]
    [Tooltip("Speed of the up/down motion")]
    public float verticalBobFrequency = 5.0f;
    [Tooltip("Speed of the side to side motion")]
    public float horizontalBobFrequency = 2.5f;

    [Header("Movement")]
    [Tooltip("Minimum movement speed required to trigger headbob")]
    public float bobStartMovementSpeed = 0.1f;
    [Tooltip("How fast the headbob effect fades in/out")]
    public float bobFadeSpeed = 5.0f;

    [Header("Gait Settings")]
    [Tooltip("Amplifies headbob when galloping (sprinting)")]
    public float sprintBobMultiplier = 1.5f;
    [Tooltip("Whether the horse is currently galloping (sprinting)")]
    public bool isGalloping = false;

    // References
    private CharacterController _characterController;

    // Internal tracking variables
    private Vector3 _bobTargetOriginalPosition;
    private float _timer = 0;
    private float _currentBobAmount = 0;
    private float _targetBobAmount = 0;

    // Cached component references
    private FirstPersonController _fpsController;

    private void Start()
    {
        // Get required components
        _characterController = GetComponentInParent<CharacterController>();
        _fpsController = GetComponentInParent<FirstPersonController>();

        // Store original position
        if (bobTarget == null)
        {
            bobTarget = transform;
        }
        _bobTargetOriginalPosition = bobTarget.localPosition;
    }

    private void Update()
    {
        if (!enableHeadbob)
            return;

        // Get current movement speed
        float speed = _characterController ? new Vector3(_characterController.velocity.x, 0, _characterController.velocity.z).magnitude : 0;

        // Check if we should be headbobbing
        _targetBobAmount = (speed > bobStartMovementSpeed) ? 1 : 0;

        // Check if galloping/sprinting
        if (_fpsController != null)
        {
            StarterAssetsInputs inputs = _fpsController.GetComponent<StarterAssetsInputs>();
            isGalloping = inputs != null && inputs.sprint;
        }

        // Smooth the bob amount for gradual transitions
        _currentBobAmount = Mathf.Lerp(_currentBobAmount, _targetBobAmount, Time.deltaTime * bobFadeSpeed);

        // Apply headbob if moving
        if (_currentBobAmount > 0)
        {
            // Increment the timer
            _timer += Time.deltaTime;

            // Calculate bob offsets with natural horse gait (different frequencies)
            float verticalOffset = Mathf.Sin(_timer * verticalBobFrequency) * verticalBobAmount;
            float horizontalOffset = Mathf.Cos(_timer * horizontalBobFrequency) * horizontalBobAmount;

            // Apply sprint multiplier if galloping
            if (isGalloping)
            {
                verticalOffset *= sprintBobMultiplier;
                horizontalOffset *= sprintBobMultiplier;
            }

            // Apply the offsets scaled by the current bob amount
            Vector3 bobPosition = new Vector3(
                _bobTargetOriginalPosition.x + horizontalOffset * _currentBobAmount,
                _bobTargetOriginalPosition.y + verticalOffset * _currentBobAmount,
                _bobTargetOriginalPosition.z
            );

            // Update camera position
            bobTarget.localPosition = bobPosition;
        }
        else
        {
            // Return to original position when not moving
            bobTarget.localPosition = Vector3.Lerp(
                bobTarget.localPosition,
                _bobTargetOriginalPosition,
                Time.deltaTime * bobFadeSpeed
            );

            // Reset timer when stopped
            if (Vector3.Distance(bobTarget.localPosition, _bobTargetOriginalPosition) < 0.001f)
            {
                _timer = 0;
            }
        }
    }
}