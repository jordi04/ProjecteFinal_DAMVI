using FMODUnity;
using UnityEngine;
using UnityEngine.UI;

public class SoundManager : MonoBehaviour
{
    const float defaultMusicVolume = 1f;
    const float defaultSfxVolume = 1f;
    const float defaultMasterVolume = 1f;


    private FMOD.Studio.Bus masterBus;
    private FMOD.Studio.Bus musicBus;
    private FMOD.Studio.Bus sfxBus;

    [SerializeField] Slider masterSlider;
    [SerializeField] TMPro.TextMeshProUGUI textMasterVolume;
    [SerializeField] Slider musicSlider;
    [SerializeField] TMPro.TextMeshProUGUI textMusicVolume;
    [SerializeField] Slider sfxSlider;
    [SerializeField] TMPro.TextMeshProUGUI textSfxVolume;
    [SerializeField] Button restoreValuesButton;

    private float currentMasterVolume;
    private float currentMusicVolume;
    private float currentSfxVolume;


    private void Start()
    {
        masterBus = RuntimeManager.GetBus("bus:/");
        musicBus = RuntimeManager.GetBus("bus:/Music");
        sfxBus = RuntimeManager.GetBus("bus:/SFX");
        
        LoadSavedValues();
    }

    private void OnEnable()
    {
        restoreValuesButton.onClick.AddListener(restoreDefaultValues);

        // Carregar els PlayerPrefs desats
        float savedMasterVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
        float savedMusicVolume = PlayerPrefs.GetFloat("MusicVolume", 1f);
        float savedSFXVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);

        LoadSavedValues();
    }

    private void OnDisable()
    {
        restoreValuesButton.onClick.RemoveListener(restoreDefaultValues);
    }

    public void SetMasterVolume(float volume)
    {
        currentMasterVolume = volume;
        int displayVolume = (int)(volume * 100);
        textMasterVolume.text = "Master volume: " + displayVolume.ToString();
        applyChanges();
    }

    // Establir un volum temporal per la musica
    public void SetMusicVolume(float volume)
    {
        currentMusicVolume = volume;
        int displayVolume = (int)(volume * 100);
        textMusicVolume.text = "Music volume: " + displayVolume.ToString();
        applyChanges();
    }

    // Establir un volum temporal perls SFX
    public void SetSFXVolume(float volume)
    {
        currentSfxVolume = volume;
        int displayVolume = (int)(volume * 100);
        textSfxVolume.text = "SFX volume: " + displayVolume.ToString();
        applyChanges();
    }

    // Restablir els valors a predeterminats
    public void restoreDefaultValues()
    {
        SetMasterVolume(defaultMasterVolume);
        masterSlider.value = defaultMasterVolume;
        SetMusicVolume(defaultMusicVolume);
        musicSlider.value = defaultMusicVolume;
        SetSFXVolume(defaultSfxVolume);
        sfxSlider.value = defaultSfxVolume;
    }

    // Aplicar els canvis
    public void applyChanges()
    {
        masterBus.setVolume(currentMasterVolume);
        musicBus.setVolume(currentMusicVolume);
        sfxBus.setVolume(currentSfxVolume);
        PlayerPrefs.SetFloat("MasterVolume", currentMasterVolume);
        PlayerPrefs.SetFloat("MusicVolume", currentMusicVolume);
        PlayerPrefs.SetFloat("SFXVolume", currentSfxVolume);
        PlayerPrefs.Save();
    }

    public void LoadSavedValues()
    {
        float savedMasterVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
        float savedMusicVolume = PlayerPrefs.GetFloat("MusicVolume", 1f);
        float savedSFXVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);

        SetMasterVolume(savedMasterVolume);
        masterSlider.value = savedMasterVolume;
        SetMusicVolume(savedMusicVolume);
        musicSlider.value = savedMusicVolume;
        SetSFXVolume(savedSFXVolume);
        sfxSlider.value = savedSFXVolume;
        applyChanges();
    }
}
