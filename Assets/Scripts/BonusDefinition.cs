using UnityEngine;

public enum BonusCategory
{
    Trash,
    Shop
}

public enum BonusEffectType
{
    TrashLessPollution,
    TrashAutoCollect,
    TrashMoreGold,
    ShopCheaper,
    ShopMoreAwareness,
    ShopPassiveAwareness
}

[CreateAssetMenu(fileName = "BonusDefinition", menuName = "Pumpkins/Bonuses/Bonus Definition")]
public class BonusDefinition : RewardItem
{
    [Header("Effect")]
    [SerializeField] private BonusCategory category;
    [SerializeField] private BonusEffectType effectType;

    [Header("Target")]
    [SerializeField] private int targetValue;
    [SerializeField] private RewardPath shopTargetPath;

    [Header("Values")]
    [SerializeField] private int flatValue;
    [SerializeField] private float percentValue;
    [SerializeField] private float intervalSeconds;

    public BonusCategory Category => category;
    public BonusEffectType EffectType => effectType;
    public TrashType TrashType => (TrashType)targetValue;
    public int FlatValue => flatValue;
    public float PercentValue => percentValue;
    public float IntervalSeconds => intervalSeconds;
    public RewardPath ShopTargetPath => shopTargetPath;

    public bool MatchesTrash(Trash trash)
    {
        if (trash == null)
        {
            return false;
        }

        TrashType targetTrashType = TrashType;

        if (targetTrashType == TrashType.All)
        {
            return true;
        }

        return trash.TrashType == targetTrashType;
    }

    public bool MatchesShopItem(ShopItemDefinition shopItem)
    {
        if (shopItem == null)
        {
            return false;
        }

        RewardPath targetPath = ShopTargetPath;
        return targetPath == RewardPath.None || shopItem.Path == targetPath;
    }

    public bool UsesTimer()
    {
        return effectType == BonusEffectType.TrashAutoCollect
            || effectType == BonusEffectType.ShopPassiveAwareness;
    }

    private void OnValidate()
    {
        if (category == BonusCategory.Trash && !IsTrashEffect(effectType))
        {
            effectType = BonusEffectType.TrashLessPollution;
            targetValue = 0;
        }

        if (category == BonusCategory.Shop && !IsShopEffect(effectType))
        {
            effectType = BonusEffectType.ShopCheaper;
        }

        ClampTargets();
    }

    private void ClampTargets()
    {
        if (category == BonusCategory.Trash)
        {
            if (!System.Enum.IsDefined(typeof(TrashType), targetValue))
            {
                targetValue = (int)TrashType.Bottle;
            }

            return;
        }

        if (category == BonusCategory.Shop)
        {
            if (!System.Enum.IsDefined(typeof(RewardPath), shopTargetPath))
            {
                shopTargetPath = RewardPath.None;
            }
        }
    }

    private static bool IsTrashEffect(BonusEffectType bonusEffectType)
    {
        return bonusEffectType == BonusEffectType.TrashLessPollution
            || bonusEffectType == BonusEffectType.TrashAutoCollect
            || bonusEffectType == BonusEffectType.TrashMoreGold;
    }

    private static bool IsShopEffect(BonusEffectType bonusEffectType)
    {
        return bonusEffectType == BonusEffectType.ShopCheaper
            || bonusEffectType == BonusEffectType.ShopMoreAwareness
            || bonusEffectType == BonusEffectType.ShopPassiveAwareness;
    }
}
