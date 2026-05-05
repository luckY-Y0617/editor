using Northstar.Domain.Knowledge.Documents;
using Northstar.Domain.Shared;

namespace Northstar.Domain.Tests;

public sealed class DocumentDraftTests
{
    [Fact]
    public void NewDraft_UsesEmptyTiptapDocument_WhenContentIsMissing()
    {
        var draft = new DocumentDraft(Guid.NewGuid(), Guid.NewGuid());

        Assert.Equal(JsonDefaults.EmptyTiptapDocument, draft.Content);
        Assert.Equal(JsonDefaults.EmptyArray, draft.Outline);
        Assert.Equal(0, draft.WordCount);
    }
}

public sealed class DocumentTests
{
    [Fact]
    public void IncrementRevision_IncrementsByOne()
    {
        var document = new Document(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Field Note");

        document.IncrementRevision();

        Assert.Equal(1, document.Revision);
    }

    [Fact]
    public void Constructor_RejectsEmptyTitle()
    {
        var exception = Assert.Throws<DomainException>(() =>
            new Document(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), " "));

        Assert.Equal(DomainErrorCodes.ValidationError, exception.Code);
    }

    [Fact]
    public void Archive_MarksDocumentArchived_AndIsIdempotent()
    {
        var document = new Document(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Field Note");

        var first = document.Archive();
        var second = document.Archive();

        Assert.True(first);
        Assert.False(second);
        Assert.Equal(DocumentStatus.Archived, document.Status);
        Assert.NotNull(document.ArchivedAt);
    }

    [Fact]
    public void Restore_MovesArchivedDocumentBackToDraft()
    {
        var document = new Document(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Field Note");
        document.Archive();

        var restored = document.Restore();

        Assert.True(restored);
        Assert.Equal(DocumentStatus.Draft, document.Status);
        Assert.Null(document.ArchivedAt);
    }

    [Fact]
    public void Delete_MarksDeleted_AndPreventsRestore()
    {
        var document = new Document(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Field Note");

        var first = document.Delete();
        var second = document.Delete();

        Assert.True(first);
        Assert.False(second);
        Assert.NotNull(document.DeletedAt);
        Assert.Throws<DomainException>(() => document.Restore());
    }
}
