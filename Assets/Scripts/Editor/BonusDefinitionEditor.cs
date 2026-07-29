using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(BonusDefinition))]
public class BonusDefinitionEditor : Editor
{
    private static readonly BonusEffectType[] TrashEffects =
    {
        BonusEffectType.TrashLessPollution,
        BonusEffectType.TrashAutoCollect,
        BonusEffectType.TrashMoreGold
    };

    private static readonly string[] TrashEffectLabels =
    {
        "Less Pollution",
        "Auto Collect",
        "More Gold"
    };

    private static readonly BonusEffectType[] ShopEffects =
    {
        BonusEffectType.ShopCheaper,
        BonusEffectType.ShopMoreAwareness,
        BonusEffectType.ShopPassiveAwareness
    };

    private static readonly string[] ShopEffectLabels =
    {
        "Cheaper",
        "More Awareness",
        "Passive Awareness"
    };

    private SerializedProperty icon;
    private SerializedProperty effectIcon;
    private SerializedProperty title;
    private SerializedProperty subtitle;
    private SerializedProperty description;
    private SerializedProperty path;
    private SerializedProperty level;
    private SerializedProperty category;
    private SerializedProperty effectType;
    private SerializedProperty targetValue;
    private SerializedProperty shopTargetPath;
    private SerializedProperty flatValue;
    private SerializedProperty percentValue;
    private SerializedProperty intervalSeconds;

    private void OnEnable()
    {
        icon = serializedObject.FindProperty("icon");
        effectIcon = serializedObject.FindProperty("effectIcon");
        title = serializedObject.FindProperty("title");
        subtitle = serializedObject.FindProperty("subtitle");
        description = serializedObject.FindProperty("description");
        path = serializedObject.FindProperty("path");
        level = serializedObject.FindProperty("level");
        category = serializedObject.FindProperty("category");
        effectType = serializedObject.FindProperty("effectType");
        targetValue = serializedObject.FindProperty("targetValue");
        shopTargetPath = serializedObject.FindProperty("shopTargetPath");
        flatValue = serializedObject.FindProperty("flatValue");
        percentValue = serializedObject.FindProperty("percentValue");
        intervalSeconds = serializedObject.FindProperty("intervalSeconds");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawScriptField();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Progression", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(path);
        EditorGUILayout.PropertyField(level);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Display", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(icon);
        EditorGUILayout.PropertyField(effectIcon, new GUIContent("Effect Icon"));
        EditorGUILayout.PropertyField(title, new GUIContent("Title"));
        EditorGUILayout.PropertyField(subtitle, new GUIContent("Subtitle"));
        EditorGUILayout.PropertyField(description);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Effect", EditorStyles.boldLabel);
        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(category);
        if (EditorGUI.EndChangeCheck())
        {
            CoerceEffectToCategory();
        }

        DrawEffectType();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Target", EditorStyles.boldLabel);
        DrawTarget();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Values", EditorStyles.boldLabel);
        DrawValues();

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawScriptField()
    {
        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.ObjectField(
                "Script",
                MonoScript.FromScriptableObject((BonusDefinition)target),
                typeof(BonusDefinition),
                false);
        }
    }

    private void DrawEffectType()
    {
        BonusEffectType[] effects = GetAllowedEffects();
        string[] labels = GetAllowedEffectLabels();
        BonusEffectType currentEffect = (BonusEffectType)effectType.enumValueIndex;
        int selectedIndex = GetEffectIndex(effects, currentEffect);

        if (selectedIndex < 0)
        {
            selectedIndex = 0;
            effectType.enumValueIndex = (int)effects[selectedIndex];
        }

        selectedIndex = EditorGUILayout.Popup("Effect Type", selectedIndex, labels);
        effectType.enumValueIndex = (int)effects[selectedIndex];
    }

    private void DrawTarget()
    {
        BonusCategory currentCategory = (BonusCategory)category.enumValueIndex;

        if (currentCategory == BonusCategory.Trash)
        {
            if (!System.Enum.IsDefined(typeof(TrashType), targetValue.intValue))
            {
                targetValue.intValue = (int)TrashType.Bottle;
            }

            TrashType targetTrashType = (TrashType)targetValue.intValue;
            targetTrashType = (TrashType)EditorGUILayout.EnumPopup("Target", targetTrashType);
            targetValue.intValue = (int)targetTrashType;
            return;
        }

        BonusEffectType currentEffect = (BonusEffectType)effectType.enumValueIndex;

        if (currentEffect != BonusEffectType.ShopCheaper)
        {
            shopTargetPath.enumValueIndex = (int)RewardPath.None;
            EditorGUILayout.HelpBox("This shop bonus applies to all shop items.", MessageType.Info);
            return;
        }

        if (!System.Enum.IsDefined(typeof(RewardPath), shopTargetPath.enumValueIndex))
        {
            shopTargetPath.enumValueIndex = (int)RewardPath.None;
        }

        RewardPath targetPath = (RewardPath)shopTargetPath.enumValueIndex;
        targetPath = (RewardPath)EditorGUILayout.EnumPopup("Reward Path (None = All)", targetPath);
        shopTargetPath.enumValueIndex = (int)targetPath;
    }

    private void DrawValues()
    {
        BonusEffectType currentEffect = (BonusEffectType)effectType.enumValueIndex;

        switch (currentEffect)
        {
            case BonusEffectType.TrashLessPollution:
            case BonusEffectType.TrashMoreGold:
                EditorGUILayout.PropertyField(percentValue);
                break;

            case BonusEffectType.TrashAutoCollect:
                EditorGUILayout.PropertyField(intervalSeconds);
                break;

            case BonusEffectType.ShopCheaper:
                EditorGUILayout.PropertyField(flatValue);
                EditorGUILayout.PropertyField(percentValue);
                break;

            case BonusEffectType.ShopMoreAwareness:
                EditorGUILayout.PropertyField(flatValue);
                EditorGUILayout.PropertyField(percentValue);
                break;

            case BonusEffectType.ShopPassiveAwareness:
                EditorGUILayout.PropertyField(flatValue);
                EditorGUILayout.PropertyField(intervalSeconds);
                break;
        }
    }

    private void CoerceEffectToCategory()
    {
        BonusEffectType[] effects = GetAllowedEffects();
        BonusEffectType currentEffect = (BonusEffectType)effectType.enumValueIndex;

        if (GetEffectIndex(effects, currentEffect) < 0)
        {
            effectType.enumValueIndex = (int)effects[0];
            if ((BonusCategory)category.enumValueIndex == BonusCategory.Trash)
            {
                targetValue.intValue = (int)TrashType.Bottle;
            }
            else
            {
                shopTargetPath.enumValueIndex = (int)RewardPath.None;
            }
        }
    }

    private BonusEffectType[] GetAllowedEffects()
    {
        return (BonusCategory)category.enumValueIndex == BonusCategory.Trash
            ? TrashEffects
            : ShopEffects;
    }

    private string[] GetAllowedEffectLabels()
    {
        return (BonusCategory)category.enumValueIndex == BonusCategory.Trash
            ? TrashEffectLabels
            : ShopEffectLabels;
    }

    private static int GetEffectIndex(BonusEffectType[] effects, BonusEffectType currentEffect)
    {
        for (int i = 0; i < effects.Length; i++)
        {
            if (effects[i] == currentEffect)
            {
                return i;
            }
        }

        return -1;
    }
}
