using UnityEngine;

public class AudioSFXController : MonoBehaviour
{
    public static AudioSFXController Instance { get; private set; }

    [System.Serializable]
    private class AudioTriggerClipConfig
    {
        public AudioTrigger trigger;
        public AudioClip clip;
        public bool loop;
    }

    [Header("Progress Event")]
    [SerializeField] private AudioClip eventSpawnSFXClip;
    [SerializeField] private AudioClip eventDurationSFXClip;
    [SerializeField] private AudioClip eventButtonClickSFXClip;
    [SerializeField] private AudioClip eventTypingSFXClip;
    [SerializeField] private AudioSource eventDurationSource;
    [SerializeField] private AudioSource eventTypingSource;

    [Header("Shop")]
    [SerializeField] private AudioClip shopItemPurchasedSFXClip;
    [SerializeField] private AudioClip shopItemPurchaseFailedSFXClip;
    [SerializeField] private AudioClip shopItemUnlockedSFXClip;

    [Header("Rewards")]
    [SerializeField] private AudioClip bonusUnlockedSFXClip;
    [SerializeField] private AudioClip rewardChoiceShownSFXClip;
    [SerializeField] private AudioClip rewardChoiceSelectedSFXClip;

    [Header("Audio Triggers")]
    [SerializeField] private AudioTriggerClipConfig[] audioTriggerClips;

    [Header("Player")]
    [SerializeField] private AudioClip playerWalkOnSound;

    private void Awake()
    {
        if (!SetupSingleton())
        {
            return;
        }

        EnsureEventDurationSource();
    }

    private void OnEnable()
    {
        RewardManager.OnEventSpawnSFX += HandleEventSpawnSFX;
        RewardManager.OnEventDurationSFX += HandleEventDurationSFX;
        RewardManager.OnRewardChoiceShownSFX += HandleRewardChoiceShownSFX;
        RewardManager.OnRewardChoiceSelectedSFX += HandleRewardChoiceSelectedSFX;
        RewardManager.OnBonusUnlockedSFX += HandleBonusUnlockedSFX;
        RewardManager.OnShopItemUnlockedSFX += HandleShopItemUnlockedSFX;
        EventPresentationEvents.OnEventButtonClickSFX += HandleEventButtonClickSFX;
        EventPresentationEvents.OnEventDurationStopSFX += HandleEventDurationStopSFX;
        UIVfxController.OnEventTextRevealStartedSFX += HandleEventTextRevealStartedSFX;
        UIVfxController.OnEventTextRevealStoppedSFX += HandleEventTextRevealStoppedSFX;
        ScoringService.OnShopItemPurchasedSFX += HandleShopItemPurchasedSFX;
        ScoringService.OnShopItemPurchaseFailedSFX += HandleShopItemPurchaseFailedSFX;
        PointAndClickPlayerController.OnPlayerWalkStartedSFX += HandlePlayerWalkStartedSFX;
        PointAndClickPlayerController.OnPlayerWalkStoppedSFX += HandlePlayerWalkStoppedSFX;
        AudioTriggerEvents.Triggered += HandleAudioTrigger;
    }

    private void OnDisable()
    {
        RewardManager.OnEventSpawnSFX -= HandleEventSpawnSFX;
        RewardManager.OnEventDurationSFX -= HandleEventDurationSFX;
        RewardManager.OnRewardChoiceShownSFX -= HandleRewardChoiceShownSFX;
        RewardManager.OnRewardChoiceSelectedSFX -= HandleRewardChoiceSelectedSFX;
        RewardManager.OnBonusUnlockedSFX -= HandleBonusUnlockedSFX;
        RewardManager.OnShopItemUnlockedSFX -= HandleShopItemUnlockedSFX;
        EventPresentationEvents.OnEventButtonClickSFX -= HandleEventButtonClickSFX;
        EventPresentationEvents.OnEventDurationStopSFX -= HandleEventDurationStopSFX;
        UIVfxController.OnEventTextRevealStartedSFX -= HandleEventTextRevealStartedSFX;
        UIVfxController.OnEventTextRevealStoppedSFX -= HandleEventTextRevealStoppedSFX;
        ScoringService.OnShopItemPurchasedSFX -= HandleShopItemPurchasedSFX;
        ScoringService.OnShopItemPurchaseFailedSFX -= HandleShopItemPurchaseFailedSFX;
        PointAndClickPlayerController.OnPlayerWalkStartedSFX -= HandlePlayerWalkStartedSFX;
        PointAndClickPlayerController.OnPlayerWalkStoppedSFX -= HandlePlayerWalkStoppedSFX;
        AudioTriggerEvents.Triggered -= HandleAudioTrigger;

        StopEventDurationSFX();
        StopEventTypingSFX();
        StopPlayerWalkSFX();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private bool SetupSingleton()
    {
        if (Instance != null && Instance != this)
        {
            enabled = false;
            Destroy(gameObject);
            return false;
        }

        Instance = this;
        return true;
    }

    private void EnsureEventDurationSource()
    {
        if (eventDurationSource == null)
        {
            eventDurationSource = GetComponent<AudioSource>();
        }

        if (eventDurationSource == null)
        {
            eventDurationSource = gameObject.AddComponent<AudioSource>();
        }

        eventDurationSource.playOnAwake = false;
        eventDurationSource.loop = true;
    }

    private void EnsureEventTypingSource()
    {
        if (eventTypingSource == null)
        {
            eventTypingSource = gameObject.AddComponent<AudioSource>();
        }

        eventTypingSource.playOnAwake = false;
        eventTypingSource.loop = true;
    }

    private void HandleEventSpawnSFX()
    {
        PlaySFX(eventSpawnSFXClip);
    }

    private void HandleEventDurationSFX()
    {
        if (eventDurationSFXClip == null)
        {
            return;
        }

        EnsureEventDurationSource();
        eventDurationSource.clip = eventDurationSFXClip;
        eventDurationSource.loop = true;
        eventDurationSource.Play();
    }

    private void HandleEventDurationStopSFX()
    {
        StopEventDurationSFX();
    }

    private void HandleEventButtonClickSFX()
    {
        PlaySFX(eventButtonClickSFXClip);
    }

    private void HandleEventTextRevealStartedSFX()
    {
        if (eventTypingSFXClip == null)
        {
            return;
        }

        EnsureEventTypingSource();
        eventTypingSource.clip = eventTypingSFXClip;
        eventTypingSource.loop = true;
        eventTypingSource.Play();
    }

    private void HandleEventTextRevealStoppedSFX()
    {
        StopEventTypingSFX();
    }

    private void HandleShopItemPurchasedSFX()
    {
        PlaySFX(shopItemPurchasedSFXClip);
    }

    private void HandleShopItemPurchaseFailedSFX()
    {
        PlaySFX(shopItemPurchaseFailedSFXClip);
    }

    private void HandleShopItemUnlockedSFX()
    {
        PlaySFX(shopItemUnlockedSFXClip);
    }

    private void HandleBonusUnlockedSFX()
    {
        PlaySFX(bonusUnlockedSFXClip);
    }

    private void HandleRewardChoiceShownSFX()
    {
        PlaySFX(rewardChoiceShownSFXClip);
    }

    private void HandleRewardChoiceSelectedSFX()
    {
        PlaySFX(rewardChoiceSelectedSFXClip);
    }

    private void HandleAudioTrigger(AudioTrigger trigger)
    {
        AudioTriggerClipConfig config = GetAudioTriggerConfig(trigger);

        if (config == null)
        {
            return;
        }

        if (config.loop)
        {
            PlayEnvironmentLoopSFX(config.clip);
            return;
        }

        PlayEnvironmentSFX(config.clip);
    }

    private AudioTriggerClipConfig GetAudioTriggerConfig(AudioTrigger trigger)
    {
        if (audioTriggerClips == null)
        {
            return null;
        }

        for (int i = 0; i < audioTriggerClips.Length; i++)
        {
            AudioTriggerClipConfig config = audioTriggerClips[i];

            if (config != null && config.trigger == trigger)
            {
                return config;
            }
        }

        return null;
    }

    private void HandlePlayerWalkStartedSFX()
    {
        if (playerWalkOnSound == null)
        {
            return;
        }

        AudioManager audioManager = AudioManager.Instance;

        if (audioManager != null)
        {
            audioManager.PlayPlayerLoopSound(playerWalkOnSound);
        }
    }

    private void HandlePlayerWalkStoppedSFX()
    {
        StopPlayerWalkSFX();
    }

    private void StopEventDurationSFX()
    {
        if (eventDurationSource == null)
        {
            return;
        }

        eventDurationSource.Stop();
        eventDurationSource.clip = null;
    }

    private void StopEventTypingSFX()
    {
        if (eventTypingSource == null)
        {
            return;
        }

        eventTypingSource.Stop();
        eventTypingSource.clip = null;
    }

    private void StopPlayerWalkSFX()
    {
        AudioManager audioManager = AudioManager.Instance;

        if (audioManager != null)
        {
            audioManager.StopPlayerLoopSound(playerWalkOnSound);
        }
    }

    private static void PlaySFX(AudioClip clip)
    {
        if (clip == null)
        {
            return;
        }

        AudioManager audioManager = AudioManager.Instance;

        if (audioManager != null)
        {
            audioManager.PlaySfx(clip);
        }
    }

    private static void PlayEnvironmentSFX(AudioClip clip)
    {
        if (clip == null)
        {
            return;
        }

        AudioManager audioManager = AudioManager.Instance;

        if (audioManager != null)
        {
            audioManager.PlayEnvironmentSfx(clip);
        }
    }

    private static void PlayEnvironmentLoopSFX(AudioClip clip)
    {
        if (clip == null)
        {
            return;
        }

        AudioManager audioManager = AudioManager.Instance;

        if (audioManager != null)
        {
            audioManager.PlayEnvironmentLoopSfx(clip);
        }
    }
}
