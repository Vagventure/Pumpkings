using System.Globalization;

public static class RewardEffectFormatter
{
    public static string FormatEffectValue(RewardItem rewardItem)
    {
        return rewardItem switch
        {
            RecyclingPatrolDefinition patrol => $"{FormatCompactFloat(patrol.WorkDuration)}s",
            ShopItemDefinition shopItem => FormatNumber(shopItem.AwarenessValue),
            BonusDefinition bonus => FormatBonusValue(bonus),
            _ => string.Empty
        };
    }

    public static string FormatCost(ShopItemDefinition shopItem)
    {
        return shopItem == null ? string.Empty : shopItem.Cost.ToString(CultureInfo.InvariantCulture);
    }

    public static string FormatShopAwarenessValue(int awarenessValue)
    {
        return FormatNumber(awarenessValue);
    }

    public static string FormatBonusValue(BonusDefinition bonus)
    {
        if (bonus == null)
        {
            return string.Empty;
        }

        return bonus.EffectType switch
        {
            BonusEffectType.TrashLessPollution => FormatPercent(bonus.PercentValue),
            BonusEffectType.TrashAutoCollect => FormatRatePerSecond(1f, bonus.IntervalSeconds),
            BonusEffectType.TrashMoreGold => FormatPercent(bonus.PercentValue),
            BonusEffectType.ShopCheaper => FormatShopCheaper(bonus),
            BonusEffectType.ShopMoreAwareness => FormatShopMoreAwareness(bonus),
            BonusEffectType.ShopPassiveAwareness => FormatPassiveAwareness(bonus),
            _ => string.Empty
        };
    }

    private static string FormatShopCheaper(BonusDefinition bonus)
    {
        if (bonus.PercentValue > 0f)
        {
            return FormatPercent(bonus.PercentValue);
        }

        return FormatNumber(bonus.FlatValue);
    }

    private static string FormatShopMoreAwareness(BonusDefinition bonus)
    {
        if (bonus.PercentValue > 0f)
        {
            return FormatPercent(bonus.PercentValue);
        }

        return FormatNumber(bonus.FlatValue);
    }

    private static string FormatPassiveAwareness(BonusDefinition bonus)
    {
        return FormatRatePerSecond(UnityEngine.Mathf.Max(0, bonus.FlatValue), bonus.IntervalSeconds);
    }

    private static string FormatRatePerSecond(float amount, float seconds)
    {
        return $"{FormatRate(amount, seconds)}/s";
    }

    private static string FormatRate(float amount, float seconds)
    {
        float safeSeconds = UnityEngine.Mathf.Max(0.01f, seconds);
        return FormatCompactFloat(amount / safeSeconds);
    }

    private static string FormatPercent(float value)
    {
        return $"{FormatCompactFloat(System.Math.Abs(value))}%";
    }

    private static string FormatNumber(int value)
    {
        return System.Math.Abs(value).ToString(CultureInfo.InvariantCulture);
    }

    private static string FormatCompactFloat(float value)
    {
        return value.ToString(value % 1f == 0f ? "0" : "0.##", CultureInfo.InvariantCulture);
    }
}
