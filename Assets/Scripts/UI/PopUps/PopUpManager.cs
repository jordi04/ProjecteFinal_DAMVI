using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.InputSystem; // Add this for the new Input System

public class PopUpManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject popupPanel;
    [SerializeField] private CanvasGroup panelCanvasGroup;
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private Button continueButton;

    [Header("Animation Settings")]
    [SerializeField] private float fadeInDuration = 0.5f;
    [SerializeField] private float fadeOutDuration = 0.3f;

    [Header("Input Settings")]
    [SerializeField] private InputAction skipAction; // New input action for skipping

    // FMOD event reference
    public FMODUnity.EventReference defaultPopupSound;

    private bool canBeSkipped = false;
    private bool isActive = false;
    private float previousTimeScale;
    private Coroutine autoHideCoroutine;
    private FMOD.Studio.EventInstance soundInstance;

    // Singleton pattern
    public static PopUpManager Instance { get; private set; }

    private void Awake()
    {
        // Ensure only one instance exists
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Set up the skip action if not already configured in the Inspector
        if (skipAction == null)
        {
            skipAction = new InputAction("Skip", InputActionType.Button);
            skipAction.AddBinding("<Keyboard>/space");
            skipAction.AddBinding("<Gamepad>/buttonSouth");

        }
    }

    private void OnEnable()
    {
        // Enable the input action and register the callback
        skipAction.Enable();
        skipAction.performed += OnSkipActionPerformed;
    }

    private void OnDisable()
    {
        // Disable the input action and unregister the callback
        skipAction.Disable();
        skipAction.performed -= OnSkipActionPerformed;
    }

    private void OnSkipActionPerformed(InputAction.CallbackContext context)
    {
        // This is called when the skip action is performed
        if (isActive && canBeSkipped)
        {
            HidePopUp();
        }
    }

    public void ShowPopUp(PopUpSettings settings)
    {
        if (isActive) return;

        isActive = true;

        Debug.Log("ShowPopUp called with message: " + settings.message);

        // Apply settings
        if (messageText != null)
            messageText.text = settings.message;

        // Play sound using FMOD with error handling
        try
        {
            soundInstance = FMODUnity.RuntimeManager.CreateInstance(defaultPopupSound);
            if (soundInstance.isValid())
            {
                soundInstance.start();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("FMOD error: " + e.Message + " - Continuing without sound");
        }

        // Handle game pausing
        if (settings.pauseGameOnShow)
        {
            // Store current time scale before pausing
            previousTimeScale = Time.timeScale;

            // Pause the game
            Time.timeScale = 0f;

            // Show continue button if game is paused
            if (continueButton != null)
                continueButton.gameObject.SetActive(true);
        }
        else
        {
            if (continueButton != null)
                continueButton.gameObject.SetActive(false);
        }

        // Activate the popup panel (not the entire canvas)
        popupPanel.SetActive(true);

        // Reset and start fade in
        panelCanvasGroup.alpha = 0f;
        StartCoroutine(FadeIn());
        StartCoroutine(EnableSkipping(settings.minTimeBeforeSkip));

        if (settings.autoHide && !settings.pauseGameOnShow)
        {
            if (autoHideCoroutine != null)
                StopCoroutine(autoHideCoroutine);

            autoHideCoroutine = StartCoroutine(AutoHideAfterDelay(settings.autoHideDuration));
        }
    }

    public void HidePopUp()
    {
        if (!isActive || !canBeSkipped) return;

        if (autoHideCoroutine != null)
        {
            StopCoroutine(autoHideCoroutine);
            autoHideCoroutine = null;
        }

        StartCoroutine(FadeOut());
    }

    private IEnumerator FadeIn()
    {
        Debug.Log("Starting fade in, initial alpha: " + panelCanvasGroup.alpha);
        float elapsedTime = 0f;

        while (elapsedTime < fadeInDuration)
        {
            panelCanvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsedTime / fadeInDuration);
            elapsedTime += Time.unscaledDeltaTime;
            yield return null;
        }

        panelCanvasGroup.alpha = 1f;
        Debug.Log("Fade in complete, final alpha: " + panelCanvasGroup.alpha);
    }

    private IEnumerator FadeOut()
    {
        Debug.Log("Starting fade out, initial alpha: " + panelCanvasGroup.alpha);
        float elapsedTime = 0f;

        while (elapsedTime < fadeOutDuration)
        {
            panelCanvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsedTime / fadeOutDuration);
            elapsedTime += Time.unscaledDeltaTime;
            yield return null;
        }

        panelCanvasGroup.alpha = 0f;
        popupPanel.SetActive(false);
        isActive = false;
        Debug.Log("Fade out complete, panel deactivated");

        // Release FMOD sound instance safely
        try
        {
            if (soundInstance.isValid())
            {
                soundInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
                soundInstance.release();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("FMOD error during cleanup: " + e.Message);
        }

        // Resume the game if it was paused
        if (Time.timeScale == 0f)
        {
            Time.timeScale = previousTimeScale;
        }
    }

    private IEnumerator EnableSkipping(float delay)
    {
        canBeSkipped = false;
        yield return new WaitForSecondsRealtime(delay);
        canBeSkipped = true;
    }

    private IEnumerator AutoHideAfterDelay(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        HidePopUp();
    }

    private void OnDestroy()
    {
        if (continueButton != null)
        {
            continueButton.onClick.RemoveListener(HidePopUp);
        }

        // Clean up FMOD instance if needed
        try
        {
            if (soundInstance.isValid())
            {
                soundInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
                soundInstance.release();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("FMOD error during cleanup: " + e.Message);
        }
    }
}
