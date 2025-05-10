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

    private float chargeTime = 0f;
    private GameObject lastHitObject;
    private bool isHolding = false;
    private float currentEffectIntensity = 0f;

    public override void Use(AbilityUseType useType)
    {
        switch (useType)
        {
            case AbilityUseType.Pressed:
                if (CanUse() && ManaSystem.instance.HasManaAmount(manaCost))
                {
                    isHolding = true;
                    chargeTime = 0f;

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
                    chargeTime += Time.deltaTime;

                    // First perform raycast to detect swappable objects
                    GameObject previousTarget = lastHitObject;
                    PerformRaycast();

                    // Update effect based on whether we're looking at a valid target
                    UpdateEffectIntensity();

                    // If we just lost or acquired a target, make appropriate transitions
                    if (previousTarget != null && lastHitObject == null)
                    {
                        // We just lost our target - cancel effect
                        AnimateCancelEffect();
                    }
                    else if (previousTarget == null && lastHitObject != null)
                    {
                        // We just acquired a new target - ensure effect is active
                        CustomRenderPassFeature.isActive = true;
                    }
                }
                break;

            case AbilityUseType.Released:
                if (isHolding && lastHitObject != null && chargeTime >= requiredChargeTime)
                {
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
                        AnimateCancelEffect();
                    }
                }
                else if (isHolding)
                {
                    // Not fully charged or no valid target - cancel effect
                    AnimateCancelEffect();
                }

                isHolding = false;
                chargeTime = 0f;
                ResetLastHitObject();
                // Note: Don't disable the effect here, let the animations handle it
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

                    CustomRenderPassFeature.isActive = true;
                }
                return;
            }
        }

        CustomRenderPassFeature.isActive = false;
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

    // Default shader parameter values with updated values
    private const float DEFAULT_SPEED_LINES_RADIAL_SCALE = 10f;
    private const float DEFAULT_SPEED_LINES_REMAP = 1f;
    private const float DEFAULT_MASK_SCALE = 2.0f;
    private const float DEFAULT_SPEED_LINES_POWER = 1.0f;

    private void UpdateEffectIntensity()
    {
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
            // Updated to target 2.5 instead of 0.03
            float speedLinesRadialScale = Mathf.Lerp(DEFAULT_SPEED_LINES_RADIAL_SCALE, 2.5f, normalizedCharge);

            // _SpeedLinesRemap decreases (lines appear larger or more spaced)
            // Updated to target 0.1 instead of 0.4
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
        else
        {
            // Not targeting valid object - start cancel animation
            if (isHolding && chargeTime > 0)
            {
                AnimateCancelEffect();
            }
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

        // Debug log for troubleshooting (remove in production)
        Debug.Log("Cancel animation completed, effect disabled");
    }

    private IEnumerator AnimateSwapEffectCoroutine()
    {
        if (CustomRenderPassFeature.runtimeMaterial == null)
            yield break;

        // Get current values to start animation from
        float currentRadialScale = CustomRenderPassFeature.runtimeMaterial.GetFloat("_SpeedLinesRadialScale");
        float currentRemap = CustomRenderPassFeature.runtimeMaterial.GetFloat("_SpeedLinesRemap");
        float currentMaskScale = CustomRenderPassFeature.runtimeMaterial.GetFloat("_MaskScale");

        // Total duration for the entire swap animation
        float totalDuration = 0.4f;
        float startTime = Time.time;

        // Divide animation into three sequential stages
        float stage1Duration = totalDuration * 0.3f; // First 30% of time
        float stage2Duration = totalDuration * 0.3f; // Next 30% of time
        float stage3Duration = totalDuration * 0.4f; // Final 40% of time

        // First stage: Increase SpeedLinesRadialScale (lines become less sharp)
        while (Time.time < startTime + stage1Duration)
        {
            float t = (Time.time - startTime) / stage1Duration;
            float ease = t * t; // Ease in

            // Rapidly increase radial scale from current to 0.3 (much less focused)
            CustomRenderPassFeature.runtimeMaterial.SetFloat("_SpeedLinesRadialScale",
                Mathf.Lerp(currentRadialScale, 0.3f, ease));

            // Brighten color
            Color burstColor = highlightColor;
            burstColor.a = Mathf.Lerp(0.9f, 1.0f, ease);
            CustomRenderPassFeature.runtimeMaterial.SetColor("_Colour", burstColor);

            yield return null;
        }

        // Second stage: Increase SpeedLinesRemap (lines become smaller/tighter)
        float stage2StartTime = startTime + stage1Duration;
        while (Time.time < stage2StartTime + stage2Duration)
        {
            float t = (Time.time - stage2StartTime) / stage2Duration;
            float ease = t * (2 - t); // Ease in-out

            // Increase remap value (lines become smaller)
            CustomRenderPassFeature.runtimeMaterial.SetFloat("_SpeedLinesRemap",
                Mathf.Lerp(currentRemap, 0.95f, ease));

            yield return null;
        }

        // Third stage: Increase MaskScale (lines move outward from center)
        float stage3StartTime = stage2StartTime + stage2Duration;
        while (Time.time < stage3StartTime + stage3Duration)
        {
            float t = (Time.time - stage3StartTime) / stage3Duration;
            float ease = 1 - (1 - t) * (1 - t); // Ease out

            // Increase mask scale (expand outward)
            CustomRenderPassFeature.runtimeMaterial.SetFloat("_MaskScale",
                Mathf.Lerp(currentMaskScale, 1.6f, ease));

            // Fade out the effect
            Color fadeColor = highlightColor;
            fadeColor.a = Mathf.Lerp(1.0f, 0f, ease);
            CustomRenderPassFeature.runtimeMaterial.SetColor("_Colour", fadeColor);

            yield return null;
        }

        // Reset all values to defaults and turn off effect
        CustomRenderPassFeature.runtimeMaterial.SetFloat("_SpeedLinesRadialScale", DEFAULT_SPEED_LINES_RADIAL_SCALE);
        CustomRenderPassFeature.runtimeMaterial.SetFloat("_SpeedLinesRemap", DEFAULT_SPEED_LINES_REMAP);
        CustomRenderPassFeature.runtimeMaterial.SetFloat("_MaskScale", DEFAULT_MASK_SCALE);
        CustomRenderPassFeature.runtimeMaterial.SetFloat("_SpeedLinesPower", DEFAULT_SPEED_LINES_POWER);
        CustomRenderPassFeature.isActive = false;
    }

    // This method is replaced by more specific coroutines for each animation state
    private IEnumerator FadeEffectIntensity(float startMultiplier, float endMultiplier, float duration)
    {
        // Legacy method - keeping for reference but implementation moved to specific animation coroutines
        yield return null;
    }
}