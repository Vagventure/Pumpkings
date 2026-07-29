using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class RewardSelectionUIPrefabBootstrap
{
    private const string UiFolder = "Assets/UI";
    private const string PanelPrefabPath = UiFolder + "/RewardSelectionPanel.prefab";

    static RewardSelectionUIPrefabBootstrap()
    {
        EditorApplication.delayCall += CreatePrefabIfMissing;
    }

    private static void CreatePrefabIfMissing()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }

        if (AssetDatabase.LoadAssetAtPath<GameObject>(PanelPrefabPath) != null)
        {
            return;
        }

        EnsureFolder();

        GameObject panel = BuildPanel();
        PrefabUtility.SaveAsPrefabAsset(panel, PanelPrefabPath);
        Object.DestroyImmediate(panel);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static GameObject BuildPanel()
    {
        GameObject panel = CreateUiObject("RewardSelectionPanel", null, new Vector2(760f, 340f));
        Image panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0.08f, 0.08f, 0.08f, 0.92f);

        RewardSelectionUI selectionUI = panel.AddComponent<RewardSelectionUI>();

        GameObject title = CreateText("Title", panel.transform, "Choose Reward", 34, TextAlignmentOptions.Center);
        RectTransform titleRect = title.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -24f);
        titleRect.sizeDelta = new Vector2(-48f, 60f);

        GameObject firstCard = CreateRewardCard("RewardChoice_1", panel.transform, new Vector2(-190f, -35f));
        GameObject secondCard = CreateRewardCard("RewardChoice_2", panel.transform, new Vector2(190f, -35f));

        RewardSelectionRepresentation firstRepresentation = firstCard.GetComponent<RewardSelectionRepresentation>();
        RewardSelectionRepresentation secondRepresentation = secondCard.GetComponent<RewardSelectionRepresentation>();

        SerializedObject serializedSelectionUI = new SerializedObject(selectionUI);
        serializedSelectionUI.FindProperty("rewardTitleText").objectReferenceValue = title.GetComponent<TMP_Text>();
        SerializedProperty slots = serializedSelectionUI.FindProperty("rewardChoiceSlots");
        slots.arraySize = 2;
        slots.GetArrayElementAtIndex(0).objectReferenceValue = firstRepresentation;
        slots.GetArrayElementAtIndex(1).objectReferenceValue = secondRepresentation;
        serializedSelectionUI.ApplyModifiedPropertiesWithoutUndo();

        panel.SetActive(false);
        return panel;
    }

    private static GameObject CreateRewardCard(string name, Transform parent, Vector2 anchoredPosition)
    {
        GameObject card = CreateUiObject(name, parent, new Vector2(300f, 210f));
        RectTransform rectTransform = card.GetComponent<RectTransform>();
        rectTransform.anchoredPosition = anchoredPosition;

        Image image = card.AddComponent<Image>();
        image.color = new Color(0.18f, 0.18f, 0.18f, 1f);
        image.raycastTarget = true;

        Button button = card.AddComponent<Button>();
        button.targetGraphic = image;

        RewardSelectionRepresentation representation = card.AddComponent<RewardSelectionRepresentation>();

        GameObject iconObject = CreateUiObject("RewardIcon", card.transform, new Vector2(56f, 56f));
        Image iconDisplay = iconObject.AddComponent<Image>();
        iconDisplay.preserveAspect = true;
        iconDisplay.raycastTarget = false;
        iconDisplay.enabled = false;
        RectTransform iconRect = iconObject.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.5f, 1f);
        iconRect.anchorMax = new Vector2(0.5f, 1f);
        iconRect.pivot = new Vector2(0.5f, 1f);
        iconRect.anchoredPosition = new Vector2(0f, -14f);

        GameObject nameText = CreateText("RewardName", card.transform, "Reward", 22, TextAlignmentOptions.Center);
        RectTransform nameRect = nameText.GetComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0f, 1f);
        nameRect.anchorMax = new Vector2(1f, 1f);
        nameRect.pivot = new Vector2(0.5f, 1f);
        nameRect.anchoredPosition = new Vector2(0f, -72f);
        nameRect.sizeDelta = new Vector2(-32f, 46f);

        GameObject descriptionText = CreateText("RewardDescription", card.transform, "Reward description", 18, TextAlignmentOptions.Center);
        RectTransform descriptionRect = descriptionText.GetComponent<RectTransform>();
        descriptionRect.anchorMin = new Vector2(0f, 0f);
        descriptionRect.anchorMax = new Vector2(1f, 1f);
        descriptionRect.pivot = new Vector2(0.5f, 0.5f);
        descriptionRect.anchoredPosition = new Vector2(0f, -28f);
        descriptionRect.sizeDelta = new Vector2(-32f, -100f);

        SerializedObject serializedRepresentation = new SerializedObject(representation);
        serializedRepresentation.FindProperty("iconDisplay").objectReferenceValue = iconDisplay;
        serializedRepresentation.FindProperty("titleText").objectReferenceValue = nameText.GetComponent<TMP_Text>();
        serializedRepresentation.FindProperty("descriptionText").objectReferenceValue = descriptionText.GetComponent<TMP_Text>();
        serializedRepresentation.FindProperty("selectButton").objectReferenceValue = button;
        serializedRepresentation.ApplyModifiedPropertiesWithoutUndo();

        return card;
    }

    private static GameObject CreateText(string name, Transform parent, string text, int fontSize, TextAlignmentOptions alignment)
    {
        GameObject textObject = CreateUiObject(name, parent, new Vector2(260f, 60f));
        TextMeshProUGUI label = textObject.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = fontSize;
        label.alignment = alignment;
        label.color = Color.white;
        label.raycastTarget = false;
        return textObject;
    }

    private static GameObject CreateUiObject(string name, Transform parent, Vector2 size)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform));
        RectTransform rectTransform = gameObject.GetComponent<RectTransform>();
        rectTransform.SetParent(parent, false);
        rectTransform.sizeDelta = size;
        return gameObject;
    }

    private static void EnsureFolder()
    {
        if (!AssetDatabase.IsValidFolder(UiFolder))
        {
            AssetDatabase.CreateFolder("Assets", "UI");
        }
    }
}
