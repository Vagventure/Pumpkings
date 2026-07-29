using System;

public class ShopItemRewardPresenter : RewardPresentationPresenter
{
    protected override void OnPresent(RewardItem reward, Action completed)
    {
        completed?.Invoke();
    }
}
