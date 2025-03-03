using UnityEngine;

public class BobbingUI : MonoBehaviour
{
    [Tooltip("The amplitude of the vertical bobbing motion.")]
    public float verticalAmplitude = 10f;

    [Tooltip("The amplitude of the horizontal shift.")]
    public float horizontalAmplitude = 5f;

    [Tooltip("The speed at which the bobbing cycles occur. (Higher = faster)")]
    public float speed = 1f;

    private RectTransform rectTransform;
    private Vector2 originalPosition;

    void Start()
    {
        // Get the RectTransform component attached to this UI element.
        rectTransform = GetComponent<RectTransform>();
        // Remember the original position so we can bob around it.
        originalPosition = rectTransform.anchoredPosition;
    }

    void Update()
    {
        // Scale time by speed to control the frequency.
        float cycleTime = Time.time * speed;
        // tCycle represents the fraction (0 to 1) of the current cycle.
        float tCycle = cycleTime % 1f;

        // Determine the current cycle index (integer part of cycleTime).
        int cycleIndex = (int)Mathf.Floor(cycleTime);
        // Alternate horizontal direction: even cycles move right, odd cycles move left.
        float horizontalDirection = (cycleIndex % 2 == 0) ? 1f : -1f;

        // Vertical bobbing uses a full sine wave cycle (up and then down twice per cycle).
        // At t=0: y=0, at t=0.25: y=+max, at t=0.5: y=0, at t=0.75: y=-max, at t=1: y=0.
        float verticalOffset = verticalAmplitude * Mathf.Sin(tCycle * 2f * Mathf.PI);

        // Horizontal motion uses a half sine wave: it starts at 0, peaks at mid-cycle, and returns to 0.
        // This gives a smooth left/right shift per cycle.
        float horizontalOffset = horizontalAmplitude * horizontalDirection * Mathf.Sin(tCycle * Mathf.PI);

        // Update the UI element's anchored position.
        rectTransform.anchoredPosition = originalPosition + new Vector2(horizontalOffset, verticalOffset);
    }
}
