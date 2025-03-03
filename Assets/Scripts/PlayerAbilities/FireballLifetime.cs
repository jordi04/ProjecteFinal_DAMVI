using UnityEngine;

// Simple component to handle the fireball's lifetime
public class FireballLifetime : MonoBehaviour
{
    private float _lifetime = 3f;
    private float _timer = 0f;

    public void SetLifetime(float lifetime)
    {
        _lifetime = lifetime;
        _timer = 0f; // Reset timer when reusing from pool
    }
    public float GetLifetime()
    {
        return _lifetime;
    }
    private void Update()
    {
        _timer += Time.deltaTime;
        if (_timer >= _lifetime)
        {
            // Return to pool instead of destroying
            gameObject.SetActive(false);
        }
    }

    private void OnDisable()
    {
        // Reset state when returned to pool
        _timer = 0f;
    }
}