using System.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "Swap Ability", menuName = "Player Ability/Swap")]
public class PlayerAbility_Swap : PlayerAbility
{
    [SerializeField] private float raycastDistance = 100f;
    [SerializeField] private uint manaCost = 20;
    [SerializeField] private float requiredChargeTime = 1.5f;
    [SerializeField] private Color highlightColor = Color.red;
    [SerializeField] private float effectIntensityMultiplier = 1.0f;
    [SerializeField] private bool autoCastWhenCharged = true; // New serialized field to toggle auto-cast

    private float chargeTime = 0f;
    private GameObject lastHitObject;
    private bool isHolding = false;
    private float currentEffectIntensity = 0f;
    private bool isEffectActive = false;
    private bool wasTargetingValidObject = false;
    private bool hasAutoTriggered = false; // Track if we've already auto-triggered this charge cycle

    // Default shader parameter values with updated values
    private const float DEFAULT_SPEED_LINES_RADIAL_SCALE = 10f;
    private const float DEFAULT_SPEED_LINES_REMAP = 1f;
    private const float DEFAULT_MASK_SCALE = 2.0f;
    private const float DEFAULT_SPEED_LINES_POWER = 1.0f;

    public override void Use(AbilityUseType useType)
    {
        switch (useType)
        {
            case AbilityUseType.Pressed:
                if (CanUse() && ManaSystem.instance.HasManaAmount(manaCost))
                {
                    isHolding = true;
                    chargeTime = 0f;
                    wasTargetingValidObject = false;
                    hasAutoTriggered = false; // Reset auto-trigger flag when starting a new charge

                    // Initialize effect parameters to defaults when starting the ability
                    if (CustomRenderPassFeature.runtimeMaterial != null)
                    {
                        CustomRenderPassFeature.runtimeMaterial.SetFloat("_SpeedLinesRadialScale", DEFAULT_SPEED_LINES_RADIAL_SCALE);
                        CustomRenderPassFeature.runtimeMaterial.SetFloat("_SpeedLinesRemap", DEFAULT_SPEED_LINES_REMAP);
                        CustomRenderPassFeature.runtimeMaterial.SetFloat("_MaskScale", DEFAULT_MASK_SCALE);
                        CustomRenderPassFeature.runtimeMaterial.SetFloat("_SpeedLinesPower", DEFAULT_SPEED_LINES_POWER);
                    }
                }
                break;

            case AbilityUseType.Held:
                if (isHolding)
                {
                    // Continue charging the ability regardless of where the player is aiming
                    chargeTime += Time.deltaTime;

                    // First perform raycast to detect swappable objects
                    GameObject previousTarget = lastHitObject;
                    PerformRaycast();

                    // Key logic difference: Track if we're targeting a valid object
                    bool isTargetingValidObject = (lastHitObject != null);

                    // Track transitions between targeting states
                    if (!wasTargetingValidObject && isTargetingValidObject)
                    {
                        // Just acquired a target - start visual effect
                        isEffectActive = true;
                        CustomRenderPassFeature.isActive = true;
                        UpdateEffectIntensity();
                    }
                    else if (wasTargetingValidObject && !isTargetingValidObject)
                    {
                        // Just lost target - cancel visual effect but keep charging
                        if (isEffectActive)
                        {
                            AnimateCancelEffect();
                            isEffectActive = false;
                        }
                    }
                    else if (isTargetingValidObject)
                    {
                        // Still targeting valid object - update effect
                        isEffectActive = true;
                        UpdateEffectIntensity();
                    }

                    // Save current targeting state for next frame
                    wasTargetingValidObject = isTargetingValidObject;

                    // NEW: Auto-cast when fully charged
                    if (autoCastWhenCharged && !hasAutoTriggered && chargeTime >= requiredChargeTime && lastHitObject != null)
                    {
                        // We have a valid target and sufficient charge time
                        if (ManaSystem.instance.TryConsumeMana(manaCost))
                        {
                            // Set flag to prevent multiple triggers in the same charge cycle
                            hasAutoTriggered = true;

                            // Successful swap - play swap animation and perform the swap
                            AnimateSwapEffect();
                            SwapWithObject();
                            AbilityManager.instance.StartCooldown(this, cooldownTime, AbilityType.Swap);

                            // Release the ability after auto-casting
                            isHolding = false;
                            chargeTime = 0f;
                            ResetLastHitObject();
                            wasTargetingValidObject = false;
                        }
                    }
                }
                break;

            case AbilityUseType.Released:
                if (isHolding)
                {
                    // Only process release if we haven't already auto-triggered
                    if (!hasAutoTriggered)
                    {
                        if (lastHitObject != null && chargeTime >= requiredChargeTime)
                        {
                            // We have a valid target and sufficient charge time
                            if (ManaSystem.instance.TryConsumeMana(manaCost))
                            {
                                // Successful swap - play swap animation and perform the swap
                                AnimateSwapEffect();
                                SwapWithObject();
                                AbilityManager.instance.StartCooldown(this, cooldownTime, AbilityType.Swap);
                            }
                            else
                            {
                                // Not enough mana - cancel effect
                                if (isEffectActive)
                                {
                                    AnimateCancelEffect();
                                    isEffectActive = false;
                                }
                            }
                        }
                        else if (isEffectActive)
                        {
                            // Not fully charged or no valid target but effect is active - cancel effect
                            AnimateCancelEffect();
                            isEffectActive = false;
                        }
                    }

                    // Reset state variables
                    isHolding = false;
                    chargeTime = 0f;
                    ResetLastHitObject();
                    wasTargetingValidObject = false;
                    hasAutoTriggered = false;
                }
                break;
        }
    }

    private void PerformRaycast()
    {
        Ray ray = GameManager.instance.mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, raycastDistance))
        {
            if (hit.collider.CompareTag("Swapable"))
            {
                GameObject hitObject = hit.collider.gameObject;

                if (lastHitObject != null && lastHitObject != hitObject)
                    ResetLastHitObject();

                Renderer renderer = hitObject.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.color = highlightColor;
                    lastHitObject = hitObject;
                    return;
                }
            }
        }

        // No valid target found
        ResetLastHitObject();
    }

    private void ResetLastHitObject()
    {
        if (lastHitObject != null)
        {
            Renderer renderer = lastHitObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = Color.white; // Reset to default color
            }
            lastHitObject = null;
        }
    }

    private void SwapWithObject()
    {
        if (lastHitObject == null)
            return;

        Vector3 playerPos = GameManager.instance.player.transform.position;
        Vector3 objectPos = lastHitObject.transform.position;
        Rigidbody playerRb = GameManager.instance.player.GetComponent<Rigidbody>();
        CharacterController playerCC = GameManager.instance.player.GetComponent<CharacterController>();
        bool wasKinematic = false;
        if (playerRb != null)
        {
            wasKinematic = playerRb.isKinematic;
            playerRb.isKinematic = true;
        }
        if (playerCC != null)
        {
            playerCC.enabled = false;
        }
        // Perform the swap
        GameManager.instance.player.transform.position = objectPos;
        lastHitObject.transform.position = playerPos;
        // Re-enable physics components
        if (playerRb != null)
        {
            playerRb.isKinematic = wasKinematic;
        }
        if (playerCC != null)
        {
            playerCC.enabled = true;
        }
    }

    private void UpdateEffectIntensity()
    {
        if (CustomRenderPassFeature.runtimeMaterial == null)
            return;

        CustomRenderPassFeature.isActive = true;

        // Calculate intensity based on charge time (0 to 1)
        float normalizedCharge = Mathf.Clamp01(chargeTime / requiredChargeTime);
        currentEffectIntensity = normalizedCharge * effectIntensityMultiplier;

        // --- ANIMATION STATE 1: CHARGING THE ABILITY ---
        // Only apply charging effect when aiming at a valid swappable object
        if (lastHitObject != null)
        {
            // Animation effect: energy pulling inward

            // _MaskScale decreases over time (focused energy)
            float maskScale = Mathf.Lerp(DEFAULT_MASK_SCALE, 0.7f, normalizedCharge);

            // _SpeedLinesRadialScale decreases (sharper lines)
            float speedLinesRadialScale = Mathf.Lerp(DEFAULT_SPEED_LINES_RADIAL_SCALE, 2.5f, normalizedCharge);

            // _SpeedLinesRemap decreases (lines appear larger or more spaced)
            float speedLinesRemap = Mathf.Lerp(DEFAULT_SPEED_LINES_REMAP, 0.1f, normalizedCharge);

            // Other parameters to enhance the effect
            float speedLinesAnimation = 3f + 3f * normalizedCharge; // Speed up animation
            float speedLinesTiling = 200f + 100f * normalizedCharge; // Increase line density
            float speedLinesPower = Mathf.Lerp(DEFAULT_SPEED_LINES_POWER, 2.5f, normalizedCharge); // Increase line strength

            // Apply all parameters to the shader
            CustomRenderPassFeature.runtimeMaterial.SetFloat("_SpeedLinesAnimation", speedLinesAnimation);
            CustomRenderPassFeature.runtimeMaterial.SetFloat("_SpeedLinesTiling", speedLinesTiling);
            CustomRenderPassFeature.runtimeMaterial.SetFloat("_SpeedLinesRadialScale", speedLinesRadialScale);
            CustomRenderPassFeature.runtimeMaterial.SetFloat("_SpeedLinesRemap", speedLinesRemap);
            CustomRenderPassFeature.runtimeMaterial.SetFloat("_MaskScale", maskScale);
            CustomRenderPassFeature.runtimeMaterial.SetFloat("_SpeedLinesPower", speedLinesPower);

            // Color intensifies with charge
            Color effectColor = highlightColor;
            // Start more transparent and become more opaque
            effectColor.a = Mathf.Lerp(0.4f, 0.9f, normalizedCharge);
            CustomRenderPassFeature.runtimeMaterial.SetColor("_Colour", effectColor);
        }
    }

    private void AnimateCancelEffect()
    {
        // --- ANIMATION STATE 2: CANCELING THE CHARGE ---
        // Smoothly return all parameters to default values
        GameManager.instance.StartCoroutine(AnimateCancelEffectCoroutine());
    }

    private void AnimateSwapEffect()
    {
        // --- ANIMATION STATE 3: PERFORMING THE SWAP ---
        // Energetic release effect in stages
        GameManager.instance.StartCoroutine(AnimateSwapEffectCoroutine());
    }

    private IEnumerator AnimateCancelEffectCoroutine()
    {
        if (CustomRenderPassFeature.runtimeMaterial == null)
            yield break;

        // Make sure the effect is active when starting the cancel animation
        CustomRenderPassFeature.isActive = true;

        // Get current values to animate from
        float currentRadialScale = CustomRenderPassFeature.runtimeMaterial.GetFloat("_SpeedLinesRadialScale");
        float currentRemap = CustomRenderPassFeature.runtimeMaterial.GetFloat("_SpeedLinesRemap");
        float currentMaskScale = CustomRenderPassFeature.runtimeMaterial.GetFloat("_MaskScale");
        float currentPower = CustomRenderPassFeature.runtimeMaterial.GetFloat("_SpeedLinesPower");
        Color currentColor = CustomRenderPassFeature.runtimeMaterial.GetColor("_Colour");

        // Duration of animation - increased to make animation more visible
        float cancelDuration = 0.8f;
        float startTime = Time.time;

        // Make sure we're showing something visible by adding a slight flash
        Color flashColor = currentColor;
        flashColor.a = 0.7f;
        CustomRenderPassFeature.runtimeMaterial.SetColor("_Colour", flashColor);

        // Immediate visual feedback to show the cancel is happening
        CustomRenderPassFeature.runtimeMaterial.SetFloat("_SpeedLinesAnimation", 5f);

        while (Time.time < startTime + cancelDuration)
        {
            // Calculate progress (0 to 1)
            float t = (Time.time - startTime) / cancelDuration;

            // Ease out function for smoother animation
            float easeOut = 1 - (1 - t) * (1 - t);

            // Add a slight "bounce" effect to make the cancellation more visible
            float bounceEffect = 1.0f;
            if (t < 0.4f)
            {
                // During first 40% of animation time, expand slightly
                bounceEffect = 1.0f + (0.3f * (t / 0.4f));
            }
            else
            {
                // Then contract back to normal and beyond
                bounceEffect = 1.3f - (1.3f * ((t - 0.4f) / 0.6f));
            }

            // Apply bounce effect to mask scale
            float targetMaskScale = DEFAULT_MASK_SCALE * bounceEffect;

            // Smoothly transition all parameters back to default values (with bounce effect)
            CustomRenderPassFeature.runtimeMaterial.SetFloat("_SpeedLinesRadialScale",
                Mathf.Lerp(currentRadialScale, DEFAULT_SPEED_LINES_RADIAL_SCALE * bounceEffect, easeOut));

            CustomRenderPassFeature.runtimeMaterial.SetFloat("_SpeedLinesRemap",
                Mathf.Lerp(currentRemap, DEFAULT_SPEED_LINES_REMAP, easeOut));

            CustomRenderPassFeature.runtimeMaterial.SetFloat("_MaskScale",
                Mathf.Lerp(currentMaskScale, targetMaskScale, easeOut));

            CustomRenderPassFeature.runtimeMaterial.SetFloat("_SpeedLinesPower",
                Mathf.Lerp(currentPower, DEFAULT_SPEED_LINES_POWER, easeOut));

            // Handle color changes and fade out
            Color fadeColor = currentColor;
            if (t < 0.3f)
            {
                // First 30% - slightly increase opacity for visual feedback
                fadeColor.a = Mathf.Lerp(currentColor.a, 0.8f, t / 0.3f);
            }
            else
            {
                // Then fade out
                fadeColor.a = Mathf.Lerp(0.8f, 0f, (t - 0.3f) / 0.7f);
            }
            CustomRenderPassFeature.runtimeMaterial.SetColor("_Colour", fadeColor);

            // Add a slight color shift to red to indicate cancellation
            fadeColor.r = Mathf.Lerp(currentColor.r, 1.0f, easeOut * 0.5f);
            CustomRenderPassFeature.runtimeMaterial.SetColor("_Colour", fadeColor);

            yield return null;
        }

        // Reset all parameters to defaults
        CustomRenderPassFeature.runtimeMaterial.SetFloat("_SpeedLinesRadialScale", DEFAULT_SPEED_LINES_RADIAL_SCALE);
        CustomRenderPassFeature.runtimeMaterial.SetFloat("_SpeedLinesRemap", DEFAULT_SPEED_LINES_REMAP);
        CustomRenderPassFeature.runtimeMaterial.SetFloat("_MaskScale", DEFAULT_MASK_SCALE);
        CustomRenderPassFeature.runtimeMaterial.SetFloat("_SpeedLinesPower", DEFAULT_SPEED_LINES_POWER);

        // Now we can safely turn off the effect after the animation completes
        CustomRenderPassFeature.isActive = false;
    }

    private IEnumerator AnimateSwapEffectCoroutine()
    {
        if (CustomRenderPassFeature.runtimeMaterial == null)
            yield break;

        // Make sure the effect is active
        CustomRenderPassFeature.isActive = true;

        // Get current values to start animation from
        float currentRadialScale = CustomRenderPassFeature.runtimeMaterial.GetFloat("_SpeedLinesRadialScale");
        float currentRemap = CustomRenderPassFeature.runtimeMaterial.GetFloat("_SpeedLinesRemap");
        float currentMaskScale = CustomRenderPassFeature.runtimeMaterial.GetFloat("_MaskScale");
        Color currentColor = CustomRenderPassFeature.runtimeMaterial.GetColor("_Colour");

        // Durations for each effect
        float radialScaleDuration = 1.0f;     // Step 1: 1 second
        float remapDuration = 3.0f;           // Step 2: 3 seconds
        float maskScaleDuration = 4.5f;       // Step 3: 4.5 seconds

        // The total animation time is the longest of the three durations
        float totalDuration = Mathf.Max(radialScaleDuration, remapDuration, maskScaleDuration);

        // Target values for each parameter
        float targetRadialScale = 8.0f;
        float targetRemap = 1.0f;
        float targetMaskScale = 2.0f;

        // Flash the effect at the beginning to indicate the swap is happening
        Color flashColor = highlightColor;
        flashColor.a = 1.0f;
        CustomRenderPassFeature.runtimeMaterial.SetColor("_Colour", flashColor);

        // Start time for all animations
        float startTime = Time.time;

        // Run all animations in parallel with their own durations
        while (Time.time < startTime + totalDuration)
        {
            // Calculate elapsed time since start
            float elapsedTime = Time.time - startTime;

            // --- RADIAL SCALE ANIMATION ---
            // Only animate if we're within the duration for this effect
            if (elapsedTime <= radialScaleDuration)
            {
                float t = elapsedTime / radialScaleDuration;
                float ease = t * t; // Ease in
                float radialValue = Mathf.Lerp(currentRadialScale, targetRadialScale, ease);
                CustomRenderPassFeature.runtimeMaterial.SetFloat("_SpeedLinesRadialScale", radialValue);
            }
            else
            {
                // Keep at target value after duration is complete
                CustomRenderPassFeature.runtimeMaterial.SetFloat("_SpeedLinesRadialScale", targetRadialScale);
            }

            // --- REMAP ANIMATION ---
            if (elapsedTime <= remapDuration)
            {
                float t = elapsedTime / remapDuration;
                float ease = t * (2 - t); // Ease in-out
                float remapValue = Mathf.Lerp(currentRemap, targetRemap, ease);
                CustomRenderPassFeature.runtimeMaterial.SetFloat("_SpeedLinesRemap", remapValue);
            }
            else
            {
                CustomRenderPassFeature.runtimeMaterial.SetFloat("_SpeedLinesRemap", targetRemap);
            }

            // --- MASK SCALE ANIMATION ---
            if (elapsedTime <= maskScaleDuration)
            {
                float t = elapsedTime / maskScaleDuration;
                // Use a slower ease out function for mask scale
                float ease = t < 0.5f ? 2 * t * t : 1 - Mathf.Pow(-2 * t + 2, 2) / 2;
                float maskValue = Mathf.Lerp(currentMaskScale, targetMaskScale, ease);
                CustomRenderPassFeature.runtimeMaterial.SetFloat("_MaskScale", maskValue);
            }
            else
            {
                CustomRenderPassFeature.runtimeMaterial.SetFloat("_MaskScale", targetMaskScale);
            }

            // --- COLOR/OPACITY ANIMATION ---
            // Keep full opacity for the first half of the total duration
            if (elapsedTime < totalDuration * 0.5f)
            {
                Color color = highlightColor;
                color.a = 1.0f;
                CustomRenderPassFeature.runtimeMaterial.SetColor("_Colour", color);
            }
            // Then fade out over the second half
            else
            {
                float fadeProgress = (elapsedTime - (totalDuration * 0.5f)) / (totalDuration * 0.5f);
                Color fadeColor = highlightColor;
                fadeColor.a = Mathf.Lerp(1.0f, 0f, fadeProgress);
                CustomRenderPassFeature.runtimeMaterial.SetColor("_Colour", fadeColor);
            }

            yield return null;
        }

        // Add a slight delay before turning off the effect
        yield return new WaitForSeconds(0.1f);

        // Reset all values to defaults
        CustomRenderPassFeature.runtimeMaterial.SetFloat("_SpeedLinesRadialScale", DEFAULT_SPEED_LINES_RADIAL_SCALE);
        CustomRenderPassFeature.runtimeMaterial.SetFloat("_SpeedLinesRemap", DEFAULT_SPEED_LINES_REMAP);
        CustomRenderPassFeature.runtimeMaterial.SetFloat("_MaskScale", DEFAULT_MASK_SCALE);
        CustomRenderPassFeature.runtimeMaterial.SetFloat("_SpeedLinesPower", DEFAULT_SPEED_LINES_POWER);

        // Now we can safely turn off the effect
        CustomRenderPassFeature.isActive = false;
    }
}