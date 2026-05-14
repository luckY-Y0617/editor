using Northstar.Application.Knowledge;
using Northstar.Domain.Knowledge.Activity;

namespace Northstar.Application.Tests;

public sealed class ActivityClassificationTests
{
    [Fact]
    public void DocumentUpdated_IsLowSignalCoalescibleActivityOnly()
    {
        var actorId = Guid.NewGuid();
        var documentId = Guid.NewGuid();

        var classification = ActivityClassification.ClassifyDocumentActivity(
            ActivityActions.DocumentUpdated,
            documentId,
            actorId);

        Assert.Equal(ActivityClassification.CategoryDocument, classification.Category);
        Assert.Equal(ActivityClassification.SignalLow, classification.Signal);
        Assert.Equal([ActivityClassification.SurfaceActivity], classification.Surfaces);
        Assert.False(classification.IsNotificationCandidate);
        Assert.False(classification.IsAuditCandidate);
        Assert.True(classification.IsCoalescible);
        Assert.Contains(actorId.ToString("N"), classification.CoalescingKey);
        Assert.Contains(documentId.ToString("N"), classification.CoalescingKey);
        Assert.Contains(ActivityActions.DocumentUpdated, classification.CoalescingKey);
    }

    [Theory]
    [InlineData(ActivityActions.DocumentCreated)]
    [InlineData(ActivityActions.DocumentImported)]
    public void DocumentCreationLikeEvents_AreNormalActivityOnly(string action)
    {
        var classification = ActivityClassification.ClassifyDocumentActivity(
            action,
            Guid.NewGuid(),
            actorId: null);

        Assert.Equal(ActivityClassification.SignalNormal, classification.Signal);
        Assert.Equal([ActivityClassification.SurfaceActivity], classification.Surfaces);
        Assert.False(classification.IsNotificationCandidate);
        Assert.False(classification.IsAuditCandidate);
        Assert.False(classification.IsCoalescible);
        Assert.Null(classification.CoalescingKey);
    }

    [Theory]
    [InlineData(ActivityActions.DocumentArchived)]
    [InlineData(ActivityActions.DocumentRestored)]
    [InlineData(ActivityActions.DocumentDeleted)]
    public void DestructiveDocumentEvents_AreHighSignalActivityAndAuditCandidates(string action)
    {
        var classification = ActivityClassification.ClassifyDocumentActivity(
            action,
            Guid.NewGuid(),
            Guid.NewGuid());

        Assert.Equal(ActivityClassification.SignalHigh, classification.Signal);
        Assert.Equal([ActivityClassification.SurfaceActivity], classification.Surfaces);
        Assert.False(classification.IsNotificationCandidate);
        Assert.True(classification.IsAuditCandidate);
        Assert.False(classification.IsCoalescible);
    }
}
