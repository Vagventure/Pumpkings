using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(RewardManager))]
public class RewardManagerEditor : Editor
{
    private SerializedProperty rewardSelectionUI;
    private SerializedProperty eventResolver;
    private SerializedProperty shopController;
    private SerializedProperty shopItemRewardPresenter;
    private SerializedProperty passiveRewardPresenter;
    private SerializedProperty rewardCatalog;
    private SerializedProperty levelController;
    private SerializedProperty activeBonuses;

    private void OnEnable()
    {
        rewardSelectionUI = serializedObject.FindProperty("rewardSelectionUI");
        eventResolver = serializedObject.FindProperty("eventResolver");
        shopController = serializedObject.FindProperty("shopController");
        shopItemRewardPresenter = serializedObject.FindProperty("shopItemRewardPresenter");
        passiveRewardPresenter = serializedObject.FindProperty("passiveRewardPresenter");
        rewardCatalog = serializedObject.FindProperty("rewardCatalog");
        levelController = serializedObject.FindProperty("levelController");
        activeBonuses = serializedObject.FindProperty("activeBonuses");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawScriptField();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("References", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(rewardSelectionUI);
        EditorGUILayout.PropertyField(eventResolver, new GUIContent("Event Resolver"));
        EditorGUILayout.PropertyField(shopController);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Feedback", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(shopItemRewardPresenter, new GUIContent("Shop Item Reward Presenter"));
        EditorGUILayout.PropertyField(passiveRewardPresenter, new GUIContent("Passive Reward Presenter"));

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(rewardCatalog);

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(levelController);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Runtime State", EditorStyles.boldLabel);
        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.PropertyField(activeBonuses);
        }

        DrawTimers();
        DrawDebugActivationButtons();

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawScriptField()
    {
        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.ObjectField(
                "Script",
                MonoScript.FromMonoBehaviour((RewardManager)target),
                typeof(RewardManager),
                false);
        }
    }

    private void DrawTimers()
    {
        RewardManager rewardManager = (RewardManager)target;

        if (rewardManager.ActiveBonuses.Count == 0)
        {
            return;
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Timers", EditorStyles.boldLabel);

        foreach (BonusDefinition bonus in rewardManager.ActiveBonuses)
        {
            if (bonus == null || !bonus.UsesTimer())
            {
                continue;
            }

            rewardManager.TryGetTimer(bonus, out float timer);
            EditorGUILayout.LabelField(bonus.Title, $"{timer:0.00}s / {bonus.IntervalSeconds:0.00}s");
        }
    }

    private void DrawDebugActivationButtons()
    {
        RewardManager rewardManager = (RewardManager)target;
        RewardCatalog catalog = rewardManager.Catalog;

        if (catalog == null)
        {
            return;
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Debug Activate", EditorStyles.boldLabel);

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Activation buttons are available in Play Mode.", MessageType.Info);
            return;
        }

        DrawCatalogSection("Rewards", catalog.Rewards, rewardManager);
    }

    private static void DrawCatalogSection(string label, System.Collections.Generic.IReadOnlyList<RewardItem> rewards, RewardManager rewardManager)
    {
        if (rewards == null || rewards.Count == 0)
        {
            return;
        }

        EditorGUILayout.LabelField(label, EditorStyles.miniBoldLabel);

        foreach (RewardItem reward in rewards)
        {
            if (reward == null)
            {
                continue;
            }

            bool isOwned = reward switch
            {
                BonusDefinition bonus => rewardManager.IsBonusActive(bonus),
                ShopItemDefinition shopItem => rewardManager.ShopController != null
                    && rewardManager.ShopController.IsShopItemUnlocked(shopItem),
                _ => false
            };

            using (new EditorGUI.DisabledScope(isOwned))
            {
                if (GUILayout.Button($"{reward.Title} ({reward.Path} {reward.Level})"))
                {
                    rewardManager.ApplyReward(reward);
                    EditorUtility.SetDirty(rewardManager);
                }
            }
        }
    }
}
