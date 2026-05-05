namespace Northstar.Application.Files;

public sealed record DocumentLocation(Guid DocumentId, Guid WorkspaceId, bool IsDeleted);
