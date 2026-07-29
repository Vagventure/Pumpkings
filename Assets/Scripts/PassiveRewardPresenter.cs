using System;

public class PassiveRewardPresenter : RewardPresentationPresenter
{
    protected override void OnPresent(RewardItem reward, Action completed)
    {
        completed?.Invoke();
    }
}
