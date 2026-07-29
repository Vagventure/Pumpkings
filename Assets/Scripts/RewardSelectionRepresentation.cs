using System;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class RewardSelectionRepresentation : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image iconDisplay;
    [FormerlySerializedAs("displayNameText")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text subtitleText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private Button selectButton;

    private RewardItem rewardItem;
    private Action<RewardItem> selected;

    private void Awake()
    {
        if (selectButton == null)
        {
            selectButton = GetComponentInChildren<Button>();
        }
    }

    private void OnDestroy()
    {
        if (selectButton != null)
        {
            selectButton.onClick.RemoveListener(HandleClicked);
        }
    }

    public void Configure(RewardItem reward, Action<RewardItem> onSelected)
    {
        rewardItem = reward;
        selected = onSelected;

        if (titleText != null)
        {
            titleText.text = rewardItem == null ? "" : rewardItem.Title;
        }

        if (subtitleText != null)
        {
            subtitleText.text = rewardItem == null ? "" : rewardItem.Subtitle;
        }

        if (descriptionText != null)
        {
            descriptionText.text = rewardItem == null ? "" : rewardItem.Description;
        }

        SetIcon(rewardItem == null ? null : rewardItem.Icon);

        if (selectButton != null)
        {
            selectButton.onClick.RemoveListener(HandleClicked);
            selectButton.onClick.AddListener(HandleClicked);
            selectButton.interactable = selected != null;
        }
    }

    public void ConfigureDisplayOnly(RewardItem reward)
    {
        rewardItem = null;
        selected = null;

        if (titleText != null)
        {
            titleText.text = reward == null ? "" : reward.Title;
        }

        if (subtitleText != null)
        {
            subtitleText.text = reward == null ? "" : reward.Subtitle;
        }

        if (descriptionText != null)
        {
            descriptionText.text = reward == null ? "" : reward.Description;
        }

        SetIcon(reward == null ? null : reward.Icon);

        if (selectButton != null)
        {
            selectButton.onClick.RemoveListener(HandleClicked);
            selectButton.interactable = false;
        }
    }

    public void Clear()
    {
        rewardItem = null;
        selected = null;

        if (titleText != null)
        {
            titleText.text = "";
        }

        if (subtitleText != null)
        {
            subtitleText.text = "";
        }

        if (descriptionText != null)
        {
            descriptionText.text = "";
        }

        SetIcon(null);

        if (selectButton != null)
        {
            selectButton.onClick.RemoveListener(HandleClicked);
            selectButton.interactable = true;
        }
    }

    private void HandleClicked()
    {
        if (rewardItem == null)
        {
            return;
        }

        selected?.Invoke(rewardItem);
    }

    private void SetIcon(Sprite icon)
    {
        if (iconDisplay == null)
        {
            return;
        }

        iconDisplay.sprite = icon;
        iconDisplay.enabled = icon != null;
        iconDisplay.preserveAspect = true;
    }
}
