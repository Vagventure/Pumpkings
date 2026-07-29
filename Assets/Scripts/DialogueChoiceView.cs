using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DialogueChoiceView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private enum ChoiceVisualState
    {
        Selection,
        Selected,
        Discarded
    }

    [SerializeField] private TMP_Text hotkeyText;
    [SerializeField] private TMP_Text buttonText;
    [SerializeField] private Button selectButton;
    [SerializeField] private Color selectionColor = Color.white;
    [SerializeField] private Color selectedColor = Color.white;
    [SerializeField] private Color discardedColor = Color.gray;

    private Action selected;
    private ChoiceVisualState currentState = ChoiceVisualState.Selection;
    private bool isHovered;

    private void Awake()
    {
        if (selectButton == null)
        {
            selectButton = GetComponentInChildren<Button>(true);
        }
    }

    private void OnDestroy()
    {
        if (selectButton != null)
        {
            selectButton.onClick.RemoveListener(HandleClicked);
        }
    }

    public void Configure(int hotkeyIndex, string text, Action onSelected)
    {
        selected = onSelected;
        isHovered = false;

        if (hotkeyText != null)
        {
            hotkeyText.text = $"{hotkeyIndex}.";
        }

        if (buttonText != null)
        {
            buttonText.text = text ?? string.Empty;
        }

        if (selectButton != null)
        {
            selectButton.onClick.RemoveListener(HandleClicked);
            selectButton.onClick.AddListener(HandleClicked);
            selectButton.interactable = selected != null;
        }

        ApplyVisualState(ChoiceVisualState.Selection);
        gameObject.SetActive(true);
        RuntimeUILayoutRefresher.RefreshNowAndNextFrame(this, this);
    }

    public void MarkSelected()
    {
        isHovered = false;
        SetInteractable(false);
        ApplyVisualState(ChoiceVisualState.Selected);
    }

    public void MarkDiscarded()
    {
        isHovered = false;
        SetInteractable(false);
        ApplyVisualState(ChoiceVisualState.Discarded);
    }

    public void Clear()
    {
        selected = null;
        isHovered = false;

        if (hotkeyText != null)
        {
            hotkeyText.text = string.Empty;
        }

        if (buttonText != null)
        {
            buttonText.text = string.Empty;
        }

        if (selectButton != null)
        {
            selectButton.onClick.RemoveListener(HandleClicked);
            selectButton.interactable = true;
        }

        gameObject.SetActive(false);
        RuntimeUILayoutRefresher.RefreshNowAndNextFrame(this, this);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!CanUseHoverColor())
        {
            return;
        }

        isHovered = true;
        ApplyTextColor(selectedColor);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!isHovered)
        {
            return;
        }

        isHovered = false;
        ApplyVisualState(currentState);
    }

    public void Trigger()
    {
        if (selected == null)
        {
            return;
        }

        HandleClicked();
    }

    private void HandleClicked()
    {
        selected?.Invoke();
    }

    private void SetInteractable(bool interactable)
    {
        if (selectButton != null)
        {
            selectButton.interactable = interactable;
        }
    }

    private void ApplyVisualState(ChoiceVisualState state)
    {
        currentState = state;
        Color color = state switch
        {
            ChoiceVisualState.Selected => selectedColor,
            ChoiceVisualState.Discarded => discardedColor,
            _ => selectionColor
        };

        ApplyTextColor(color);

        if (selectButton != null)
        {
            ApplyColor(selectButton.targetGraphic, color);
        }
    }

    private bool CanUseHoverColor()
    {
        return currentState == ChoiceVisualState.Selection
            && selected != null
            && (selectButton == null || selectButton.interactable);
    }

    private void ApplyTextColor(Color color)
    {
        ApplyColor(hotkeyText, color);
        ApplyColor(buttonText, color);
    }

    private static void ApplyColor(Graphic graphic, Color color)
    {
        if (graphic != null)
        {
            graphic.color = color;
        }
    }
}
