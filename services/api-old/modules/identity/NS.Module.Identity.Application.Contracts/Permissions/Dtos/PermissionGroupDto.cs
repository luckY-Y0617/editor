using System.Collections.Generic;

namespace NS.Module.Identity.Application.Contracts.Permissions.Dtos;

public class PermissionGroupDto
{
    public string Code { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Order { get; set; }
    public List<PermissionDefinitionDto> Permissions { get; set; } = new();
}

