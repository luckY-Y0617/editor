namespace Northstar.Infrastructure.Persistence;

internal static class SeedDataIds
{
    public const string WorkspaceSlug = "northstar";

    public static readonly Guid DefaultUserId = Guid.Parse("20000000-0000-0000-0000-000000000001");
    public static readonly Guid WorkspaceId = Guid.Parse("10000000-0000-0000-0000-000000000001");
    public static readonly Guid SpaceId = Guid.Parse("11000000-0000-0000-0000-000000000001");

    public static readonly Guid OrientationCollectionId = Guid.Parse("12000000-0000-0000-0000-000000000001");
    public static readonly Guid FoundationsCollectionId = Guid.Parse("12000000-0000-0000-0000-000000000002");
    public static readonly Guid StrategyCollectionId = Guid.Parse("12000000-0000-0000-0000-000000000003");
    public static readonly Guid WorkstreamsCollectionId = Guid.Parse("12000000-0000-0000-0000-000000000004");
    public static readonly Guid GuidesCollectionId = Guid.Parse("12000000-0000-0000-0000-000000000005");
    public static readonly Guid ReferenceCollectionId = Guid.Parse("12000000-0000-0000-0000-000000000006");
    public static readonly Guid ArchivesCollectionId = Guid.Parse("12000000-0000-0000-0000-000000000007");

    public static readonly Guid PrinciplesDocumentId = Guid.Parse("13000000-0000-0000-0000-000000000001");
    public static readonly Guid MissionDocumentId = Guid.Parse("13000000-0000-0000-0000-000000000002");
    public static readonly Guid OperatingSystemDocumentId = Guid.Parse("13000000-0000-0000-0000-000000000003");

    public static readonly Guid PrinciplesInitialVersionId = Guid.Parse("14000000-0000-0000-0000-000000000001");
    public static readonly Guid MissionInitialVersionId = Guid.Parse("14000000-0000-0000-0000-000000000002");
    public static readonly Guid OperatingSystemInitialVersionId = Guid.Parse("14000000-0000-0000-0000-000000000003");

    public static readonly Guid PrinciplesCreatedActivityId = Guid.Parse("15000000-0000-0000-0000-000000000001");
    public static readonly Guid MissionCreatedActivityId = Guid.Parse("15000000-0000-0000-0000-000000000002");
    public static readonly Guid OperatingSystemCreatedActivityId = Guid.Parse("15000000-0000-0000-0000-000000000003");

    public static readonly Guid MissionToPrinciplesLinkId = Guid.Parse("16000000-0000-0000-0000-000000000001");
    public static readonly Guid OperatingToMissionLinkId = Guid.Parse("16000000-0000-0000-0000-000000000002");

    public static Guid InitialVersionIdForDocument(Guid documentId)
    {
        if (documentId == PrinciplesDocumentId)
        {
            return PrinciplesInitialVersionId;
        }

        if (documentId == MissionDocumentId)
        {
            return MissionInitialVersionId;
        }

        if (documentId == OperatingSystemDocumentId)
        {
            return OperatingSystemInitialVersionId;
        }

        throw new ArgumentOutOfRangeException(nameof(documentId), documentId, "Unknown seed document id.");
    }

    public static Guid CreatedActivityIdForDocument(Guid documentId)
    {
        if (documentId == PrinciplesDocumentId)
        {
            return PrinciplesCreatedActivityId;
        }

        if (documentId == MissionDocumentId)
        {
            return MissionCreatedActivityId;
        }

        if (documentId == OperatingSystemDocumentId)
        {
            return OperatingSystemCreatedActivityId;
        }

        throw new ArgumentOutOfRangeException(nameof(documentId), documentId, "Unknown seed document id.");
    }
}
