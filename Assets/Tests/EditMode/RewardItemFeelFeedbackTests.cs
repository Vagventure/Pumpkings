#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;

public class RewardItemFeelFeedbackTests
{
    [Test]
    public void PlayAccepted_WithoutConfiguredPlayer_CompletesImmediately()
    {
        GameObject gameObject = new GameObject("Reward Item Feel");

        try
        {
            RewardItemFeelFeedback feedback = gameObject.AddComponent<RewardItemFeelFeedback>();
            bool completed = false;

            feedback.PlayAccepted(() => completed = true);

            Assert.That(completed, Is.True);
        }
        finally
        {
            Object.DestroyImmediate(gameObject);
        }
    }
}
#endif
