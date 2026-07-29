#if UNITY_EDITOR
using NUnit.Framework;

public class RecyclingPatrolCooldownTests
{
    [Test]
    public void Tick_AdvancesLinearlyOnlyWhileGameplayIsActive()
    {
        RecyclingPatrolCooldown cooldown = new RecyclingPatrolCooldown();

        cooldown.Start(20f);
        cooldown.Tick(5f, true);

        Assert.That(cooldown.RemainingSeconds, Is.EqualTo(15f).Within(0.001f));
        Assert.That(cooldown.FillAmount, Is.EqualTo(0.75f).Within(0.001f));

        cooldown.Tick(10f, false);

        Assert.That(cooldown.RemainingSeconds, Is.EqualTo(15f).Within(0.001f));

        cooldown.Tick(15f, true);

        Assert.That(cooldown.IsActive, Is.False);
        Assert.That(cooldown.FillAmount, Is.Zero);
    }
}
#endif
