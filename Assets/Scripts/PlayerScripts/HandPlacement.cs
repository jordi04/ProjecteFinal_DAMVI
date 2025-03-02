using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HandPlacement : MonoBehaviour
{
    [SerializeField] private Transform targetObject;
    [SerializeField] private Vector3 positionOffset;
    [SerializeField] private Vector3 rotationOffset;

    [Header("Smoothing Settings")]
    [SerializeField] private float positionSmoothTime = 0.1f;
    [SerializeField] private float rotationSmoothTime = 0.1f;

    private Vector3 currentVelocity;
    private Vector3 currentRotationVelocity;
    private Quaternion currentRotation;

    private void Start()
    {
        // Validate that target object is assigned
        if (targetObject == null)
        {
            Debug.LogError("Target object is not assigned to HandPlacement script on " + gameObject.name);
            return;
        }

        // Initialize current rotation to match target's rotation with offset
        currentRotation = targetObject.rotation * Quaternion.Euler(rotationOffset);
    }

    private void Update()
    {
        if (targetObject != null)
        {
            // Calculate target position with offset in the target's local space
            Vector3 targetPosition = targetObject.position + targetObject.TransformDirection(positionOffset);

            // Smoothly interpolate position
            transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref currentVelocity, positionSmoothTime);

            // Calculate target rotation with offset
            Quaternion targetRotation = targetObject.rotation * Quaternion.Euler(rotationOffset);

            // Smoothly interpolate rotation
            transform.rotation = SmoothDamp(transform.rotation, targetRotation, ref currentRotationVelocity, rotationSmoothTime);
        }
    }

    // Custom quaternion smooth damping function
    private Quaternion SmoothDamp(Quaternion current, Quaternion target, ref Vector3 currentVelocity, float smoothTime)
    {
        // Convert the quaternion to euler angles for smoother interpolation
        Vector3 currentEuler = current.eulerAngles;
        Vector3 targetEuler = target.eulerAngles;

        // Fix the angles to prevent issues with 0/360 degree wrapping
        for (int i = 0; i < 3; i++)
        {
            if (Mathf.Abs(currentEuler[i] - targetEuler[i]) > 180f)
            {
                if (currentEuler[i] > targetEuler[i])
                {
                    targetEuler[i] += 360f;
                }
                else
                {
                    targetEuler[i] -= 360f;
                }
            }
        }

        // Apply smooth damp to each euler angle component
        Vector3 smoothedEuler = new Vector3(
            Mathf.SmoothDamp(currentEuler.x, targetEuler.x, ref currentVelocity.x, smoothTime),
            Mathf.SmoothDamp(currentEuler.y, targetEuler.y, ref currentVelocity.y, smoothTime),
            Mathf.SmoothDamp(currentEuler.z, targetEuler.z, ref currentVelocity.z, smoothTime)
        );

        // Convert back to quaternion
        return Quaternion.Euler(smoothedEuler);
    }
}