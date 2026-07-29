using System;
using UnityEngine;

public abstract class RewardPresentationPresenter : MonoBehaviour
{
    public void Present(RewardItem reward, Action completed)
    {
        if (reward == null)
        {
            Debug.LogWarning($"{GetType().Name}: Cannot present a null reward.");
            completed?.Invoke();
            return;
        }

        OnPresent(reward, completed);
    }

    protected abstract void OnPresent(RewardItem reward, Action completed);
}
