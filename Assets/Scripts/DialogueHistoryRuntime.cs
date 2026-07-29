using System.Collections.Generic;
using UnityEngine;

public sealed class DialogueHistoryEntry
{
    public DialogueHistoryEntry(
        string timestamp,
        string speakerName,
        string speakerRole,
        string bodyText,
        Sprite portrait,
        bool isPlayer)
    {
        Timestamp = timestamp;
        SpeakerName = speakerName;
        SpeakerRole = speakerRole;
        BodyText = bodyText;
        Portrait = portrait;
        IsPlayer = isPlayer;
    }

    public string Timestamp { get; }
    public string SpeakerName { get; }
    public string SpeakerRole { get; }
    public string BodyText { get; }
    public Sprite Portrait { get; }
    public bool IsPlayer { get; }
}

public static class DialogueHistoryRuntime
{
    private static readonly List<DialogueHistoryEntry> EntriesInternal = new();
    private static MockDialogueTimestampProvider timestampProvider = new();

    public static IReadOnlyList<DialogueHistoryEntry> Entries => EntriesInternal;

    public static string GetNextTimestamp()
    {
        return timestampProvider.GetNextTimestamp();
    }

    public static void AddEntry(DialogueHistoryEntry entry)
    {
        if (entry == null)
        {
            return;
        }

        EntriesInternal.Add(entry);
    }

    public static void Clear()
    {
        EntriesInternal.Clear();
        timestampProvider = new MockDialogueTimestampProvider();
    }
}
