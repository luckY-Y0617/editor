using Northstar.Application.Common;
using Northstar.Contracts.Common;
using Northstar.Domain.Security;

namespace Northstar.Application.Security;

public static class PermissionResourceNormalizer
{
    public static string NormalizeScopedResourceType(string? resourceType)
    {
        if (string.IsNullOrWhiteSpace(resourceType))
        {
            throw new ApplicationErrorException(ErrorCodes.ValidationError, "Resource type is required.");
        }

        var normalized = resourceType.Trim().ToLowerInvariant();
        return ResourceTypes.IsScopedResource(normalized)
            ? normalized
            : throw new ApplicationErrorException(ErrorCodes.ValidationError, "Resource type is invalid.");
    }

    public static string NormalizeSupportedResourceType(string? resourceType)
    {
        if (string.IsNullOrWhiteSpace(resourceType))
        {
            throw new ApplicationErrorException(ErrorCodes.ValidationError, "Resource type is required.");
        }

        var normalized = resourceType.Trim().ToLowerInvariant();
        return ResourceTypes.IsSupported(normalized)
            ? normalized
            : throw new ApplicationErrorException(ErrorCodes.ValidationError, "Resource type is invalid.");
    }
}
