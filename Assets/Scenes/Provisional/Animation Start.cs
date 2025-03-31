using Cinemachine;
using System.Collections;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UI;

public class AnimationStart : MonoBehaviour
{
    public PlayableDirector playableDirector;
    public CinemachineVirtualCamera playerCamera;
    public CinemachineVirtualCamera cinematicCamera;
    public Image fadePanel; 
    public Transform player;

    private bool hasTriggered = false;

    private void Awake()
    {
        playableDirector = GetComponent<PlayableDirector>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!hasTriggered && other.CompareTag("Player"))
        {
            hasTriggered = true;
            StartCoroutine(StartCinematic());
        }
    }

    private IEnumerator StartCinematic()
    {
        yield return StartCoroutine(FadeToBlack());

        // Move Player to this object's position and rotation
        player.position = transform.position;
        player.rotation = transform.rotation;

        playerCamera.Priority = 0;
        cinematicCamera.Priority = 1;
        playableDirector.Play();

        yield return StartCoroutine(FadeFromBlack());
    }

    private IEnumerator FadeToBlack()
    {
        float duration = 1f; // Fade duration
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Clamp01(elapsed / duration);
            fadePanel.color = new Color(0, 0, 0, alpha);
            yield return null;
        }

        // Ensure the fade is fully black before moving the player
        fadePanel.color = new Color(0, 0, 0, 1);
    }

    private IEnumerator FadeFromBlack()
    {
        float duration = 1f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Clamp01(1 - (elapsed / duration));
            fadePanel.color = new Color(0, 0, 0, alpha);
            yield return null;
        }

        // Ensure fade is fully transparent at the end
        fadePanel.color = new Color(0, 0, 0, 0);
    }
}
