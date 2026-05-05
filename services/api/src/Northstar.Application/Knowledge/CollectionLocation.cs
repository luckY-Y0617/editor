namespace Northstar.Application.Knowledge;

public sealed record CollectionLocation(
    Guid WorkspaceId,
    Guid SpaceId,
    Guid CollectionId,
    Guid? DefaultOwnerId);

