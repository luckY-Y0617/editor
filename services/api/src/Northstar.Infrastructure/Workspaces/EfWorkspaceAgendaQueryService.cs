using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Northstar.Application.Workspaces;
using Northstar.Contracts.Workspaces;
using Northstar.Domain.Knowledge.Documents;
using Northstar.Infrastructure.Persistence;

namespace Northstar.Infrastructure.Workspaces;

public sealed class EfWorkspaceAgendaQueryService : IWorkspaceAgendaQueryService
{
    private static readonly AgendaSlot[] TodaySlots =
    [
        new(new TimeOnly(9, 0), 30),
        new(new TimeOnly(10, 30), 45),
        new(new TimeOnly(14, 0), 60),
        new(new TimeOnly(15, 30), 45)
    ];

    private static readonly AgendaSlot[] UpcomingSlots =
    [
        new(new TimeOnly(9, 0), 30),
        new(new TimeOnly(14, 0), 45),
        new(new TimeOnly(10, 0), 60)
    ];

    private readonly NorthstarDbContext _dbContext;

    public EfWorkspaceAgendaQueryService(NorthstarDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<WorkspaceAgendaResponse> GetAgendaAsync(
        Guid workspaceId,
        DateOnly date,
        CancellationToken cancellationToken = default)
    {
        var documents = await (
            from document in _dbContext.Documents.AsNoTracking()
            join collection in _dbContext.Collections.AsNoTracking()
                on document.CollectionId equals collection.Id into collectionJoin
            from collection in collectionJoin.DefaultIfEmpty()
            where document.WorkspaceId == workspaceId &&
                document.DeletedAt == null &&
                document.Status != DocumentStatus.Archived
            orderby document.UpdatedAt descending, document.Title
            select new AgendaDocument(
                document.Id,
                document.Title,
                collection != null && collection.DeletedAt == null ? collection.Title : null))
            .Take(8)
            .ToListAsync(cancellationToken);

        return new WorkspaceAgendaResponse(
            workspaceId.ToString(),
            date,
            "workspace",
            CreateTodayItems(date, documents),
            CreateUpcomingItems(date, documents));
    }

    private static IReadOnlyList<WorkspaceAgendaItemDto> CreateTodayItems(
        DateOnly date,
        IReadOnlyList<AgendaDocument> documents)
    {
        if (documents.Count == 0)
        {
            return [];
        }

        var items = new List<WorkspaceAgendaItemDto>();
        var scheduledDocuments = documents.Take(TodaySlots.Length).ToArray();

        for (var index = 0; index < scheduledDocuments.Length; index++)
        {
            items.Add(CreateDocumentItem(
                idPrefix: "today",
                document: scheduledDocuments[index],
                date: date,
                slot: TodaySlots[index]));

            if (index == 1)
            {
                items.Add(CreateBreakItem(date));
            }
        }

        if (scheduledDocuments.Length < 2)
        {
            items.Add(CreateBreakItem(date));
        }

        var focusDocument = documents[0];
        items.Add(new WorkspaceAgendaItemDto(
            CreateItemId("today-task", date, focusDocument.Id),
            $"Update {focusDocument.Title}",
            "Due today",
            "Follow-up",
            "task",
            date,
            FormatTime(new TimeOnly(17, 0)),
            null,
            0,
            "document",
            focusDocument.Id.ToString(),
            null,
            false,
            "workspace"));

        return items;
    }

    private static IReadOnlyList<WorkspaceAgendaItemDto> CreateUpcomingItems(
        DateOnly date,
        IReadOnlyList<AgendaDocument> documents)
    {
        if (documents.Count == 0)
        {
            return [];
        }

        return documents
            .Take(UpcomingSlots.Length)
            .Select((document, index) => CreateDocumentItem(
                idPrefix: "upcoming",
                document: document,
                date: date.AddDays(index + 1),
                slot: UpcomingSlots[index],
                detail: "Scheduled follow-up"))
            .ToArray();
    }

    private static WorkspaceAgendaItemDto CreateDocumentItem(
        string idPrefix,
        AgendaDocument document,
        DateOnly date,
        AgendaSlot slot,
        string? detail = null)
    {
        var endTime = slot.Start.AddMinutes(slot.DurationMinutes);

        return new WorkspaceAgendaItemDto(
            CreateItemId(idPrefix, date, document.Id),
            document.Title,
            detail ?? $"{slot.DurationMinutes} minutes",
            NormalizeCategory(document.CollectionTitle),
            "document",
            date,
            FormatTime(slot.Start),
            FormatTime(endTime),
            slot.DurationMinutes,
            "document",
            document.Id.ToString(),
            null,
            false,
            "workspace");
    }

    private static WorkspaceAgendaItemDto CreateBreakItem(DateOnly date)
    {
        return new WorkspaceAgendaItemDto(
            $"today-break-{date.ToString("yyyyMMdd", CultureInfo.InvariantCulture)}",
            "Lunch",
            "60 minutes",
            "Focus",
            "break",
            date,
            FormatTime(new TimeOnly(12, 0)),
            FormatTime(new TimeOnly(13, 0)),
            60,
            null,
            null,
            null,
            false,
            "workspace");
    }

    private static string CreateItemId(string prefix, DateOnly date, Guid documentId)
    {
        return $"{prefix}-{date.ToString("yyyyMMdd", CultureInfo.InvariantCulture)}-{documentId:N}";
    }

    private static string FormatTime(TimeOnly value)
    {
        return value.ToString("HH:mm", CultureInfo.InvariantCulture);
    }

    private static string NormalizeCategory(string? collectionTitle)
    {
        var normalized = collectionTitle?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return "Unfiled";
        }

        return normalized.Length > 4 && char.IsDigit(normalized[0]) && char.IsDigit(normalized[1]) && normalized[2] == '.'
            ? normalized[4..].Trim()
            : normalized;
    }

    private sealed record AgendaDocument(
        Guid Id,
        string Title,
        string? CollectionTitle);

    private sealed record AgendaSlot(TimeOnly Start, int DurationMinutes);
}
