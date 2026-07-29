#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class EventDialogueDefinitionsTests
{
    [Test]
    public void SpeakerDefinition_FallsBackToNeutralPortrait()
    {
        SpeakerDefinition speaker = ScriptableObject.CreateInstance<SpeakerDefinition>();
        Sprite neutral = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), Vector2.zero);

        try
        {
            SetField(speaker, "neutralPortrait", neutral);
            SetField(speaker, "happyPortrait", null);

            Assert.That(speaker.GetPortrait(SpeakerExpression.Happy), Is.SameAs(neutral));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(neutral);
            UnityEngine.Object.DestroyImmediate(speaker);
        }
    }

    [Test]
    public void EventDialogueLine_CreateRuntimeStoresSpeakerAndExpression()
    {
        SpeakerDefinition speaker = ScriptableObject.CreateInstance<SpeakerDefinition>();

        try
        {
            EventDialogueLine line = EventDialogueLine.CreateRuntime(
                DialogueSpeakerSide.Right,
                speaker,
                SpeakerExpression.Sad,
                "We need a better plan.",
                null);

            Assert.That(line.SpeakerSide, Is.EqualTo(DialogueSpeakerSide.Right));
            Assert.That(line.Speaker, Is.SameAs(speaker));
            Assert.That(line.Expression, Is.EqualTo(SpeakerExpression.Sad));
            Assert.That(line.Text, Is.EqualTo("We need a better plan."));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(speaker);
        }
    }

    [Test]
    public void DialogueChoiceDefinition_FallsBackToRewardTitleAndButtonText()
    {
        RewardTitleTestItem reward = ScriptableObject.CreateInstance<RewardTitleTestItem>();

        try
        {
            SetField(reward, "title", "School Classes");
            DialogueChoiceDefinition choice = DialogueChoiceDefinition.CreateRuntime(reward);
            SetField(choice, "buttonText", string.Empty);
            SetField(choice, "playerLine", string.Empty);

            Assert.That(choice.GetButtonText(), Is.EqualTo("School Classes"));
            Assert.That(choice.GetPlayerLine(), Is.EqualTo("School Classes"));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(reward);
        }
    }

    [Test]
    public void EventPresentationResolver_PlayerChoiceLineUsesEventDefinitionPresentation()
    {
        EventDefinition definition = ScriptableObject.CreateInstance<EventDefinition>();
        SpeakerDefinition speaker = ScriptableObject.CreateInstance<SpeakerDefinition>();
        TestEventPresentationResolver resolver = new GameObject("Resolver").AddComponent<TestEventPresentationResolver>();

        try
        {
            SetField(definition, "playerChoiceSpeaker", speaker);
            SetField(definition, "playerChoiceSpeakerSide", DialogueSpeakerSide.Right);
            SetField(definition, "playerChoiceExpression", SpeakerExpression.Happy);
            SetField(resolver, "currentDefinition", definition);

            EventDialogueLine line = resolver.CreatePlayerChoiceLineForTest();

            Assert.That(line.SpeakerSide, Is.EqualTo(DialogueSpeakerSide.Right));
            Assert.That(line.Speaker, Is.SameAs(speaker));
            Assert.That(line.Expression, Is.EqualTo(SpeakerExpression.Happy));
            Assert.That(line.Text, Is.EqualTo(string.Empty));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(resolver.gameObject);
            UnityEngine.Object.DestroyImmediate(speaker);
            UnityEngine.Object.DestroyImmediate(definition);
        }
    }

    [Test]
    public void RewardManager_KeepsDialogueChoiceWithoutRewardWhenItHasText()
    {
        RewardManager rewardManager = new GameObject("RewardManager").AddComponent<RewardManager>();
        ProgressEventDefinition progressEvent = new ProgressEventDefinition();
        DialogueChoiceDefinition choice = new DialogueChoiceDefinition();
        List<DialogueChoiceDefinition> configuredChoices = new() { choice };
        List<DialogueChoiceDefinition> results = new();

        try
        {
            SetField(progressEvent, "dialogueChoices", configuredChoices);
            SetField(choice, "buttonText", "Kids could draw posters.");
            SetField(choice, "playerLine", "That plan can work.");

            InvokePrivate(
                rewardManager,
                "BuildDialogueChoices",
                progressEvent,
                results);

            Assert.That(results, Has.Count.EqualTo(1));
            Assert.That(results[0].Reward, Is.Null);
            Assert.That(results[0].GetButtonText(), Is.EqualTo("Kids could draw posters."));
            Assert.That(results[0].GetPlayerLine(), Is.EqualTo("That plan can work."));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(rewardManager.gameObject);
        }
    }

    [Test]
    public void EventPresentationResolver_UsesConfiguredChoicesSide()
    {
        ChoiceSideTestResolver resolver = new GameObject("Resolver").AddComponent<ChoiceSideTestResolver>();
        EventDefinition definition = ScriptableObject.CreateInstance<EventDefinition>();
        DialogueChoiceDefinition choice = new DialogueChoiceDefinition();

        try
        {
            SetField(choice, "buttonText", "Choose lectures.");

            resolver.StartEvent(
                definition,
                new List<DialogueChoiceDefinition> { choice },
                DialogueSpeakerSide.Left,
                null,
                null);

            Assert.That(resolver.CapturedChoicesSide, Is.EqualTo(DialogueSpeakerSide.Left));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(definition);
            UnityEngine.Object.DestroyImmediate(resolver.gameObject);
        }
    }

    [Test]
    public void MockDialogueTimestampProvider_ReturnsDeterministicSequence()
    {
        MockDialogueTimestampProvider provider = new MockDialogueTimestampProvider(
            new DateTime(2026, 5, 31, 7, 3, 0),
            TimeSpan.FromMinutes(17d));

        Assert.That(provider.GetNextTimestamp(), Is.EqualTo("Sunday, 31 May 2026, 7:03 AM"));
        Assert.That(provider.GetNextTimestamp(), Is.EqualTo("Sunday, 31 May 2026, 7:20 AM"));
    }

    private static void SetField(object target, string fieldName, object value)
    {
        Type currentType = target.GetType();

        while (currentType != null)
        {
            FieldInfo field = currentType.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);

            if (field != null)
            {
                field.SetValue(target, value);
                return;
            }

            currentType = currentType.BaseType;
        }

        throw new MissingFieldException(target.GetType().FullName, fieldName);
    }

    private static void InvokePrivate(object target, string methodName, params object[] arguments)
    {
        MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);

        if (method == null)
        {
            throw new MissingMethodException(target.GetType().FullName, methodName);
        }

        method.Invoke(target, arguments);
    }

    private sealed class RewardTitleTestItem : RewardItem
    {
    }

    private sealed class TestEventPresentationResolver : EventPresentationResolver
    {
        public EventDialogueLine CreatePlayerChoiceLineForTest()
        {
            return CreatePlayerChoicePresentationLine();
        }
    }

    private sealed class ChoiceSideTestResolver : EventPresentationResolver
    {
        public DialogueSpeakerSide CapturedChoicesSide { get; private set; }

        protected override void BeforeShowChoices()
        {
            CapturedChoicesSide = CurrentChoicesSide;
        }
    }
}
#endif
