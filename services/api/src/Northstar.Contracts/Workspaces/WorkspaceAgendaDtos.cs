namespace Northstar.Contracts.Workspaces;

public sealed record WorkspaceAgendaItemDto(
    string Id,
    string Title,
    string Detail,
    string Category,
    string Kind,
    DateOnly Date,
    string StartTime,
    string? EndTime,
    int DurationMinutes,
    string? ResourceType,
    string? ResourceId,
    string? ActionUrl,
    bool ConnectedToCalendar,
    string CalendarStatus);

public sealed record WorkspaceAgendaResponse(
    string WorkspaceId,
    DateOnly Date,
    string CalendarStatus,
    IReadOnlyList<WorkspaceAgendaItemDto> Today,
    IReadOnlyList<WorkspaceAgendaItemDto> Upcoming);
