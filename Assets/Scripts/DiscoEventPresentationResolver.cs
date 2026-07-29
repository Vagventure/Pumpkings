public class DiscoEventPresentationResolver : EventPresentationResolver
{
    protected override void BeforePresentLine(EventDialogueLine line)
    {
        MarkSpawnedLinesPast();
    }
}
