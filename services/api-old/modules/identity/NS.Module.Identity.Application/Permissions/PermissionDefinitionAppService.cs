using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NS.Framework.Authorization.Abstractions.Permissions;
using NS.Framework.Authorization.AspNetCore;
using NS.Module.Identity.Application.Contracts.Permissions;
using NS.Module.Identity.Application.Contracts.Permissions.Dtos;
using NS.Module.Identity.Domain.Shared.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.Application.Services;

namespace NS.Module.Identity.Application.Permissions;

[Authorize]
[ApiController]
[Route("/api/app/permission-definitions")]
public class PermissionDefinitionAppService : ApplicationService, IPermissionDefinitionAppService
{
    private readonly IPermissionDefinitionManager _permissionDefinitionManager;

    public PermissionDefinitionAppService(IPermissionDefinitionManager permissionDefinitionManager)
    {
        _permissionDefinitionManager = permissionDefinitionManager;
    }

    [HttpGet]
    [RequirePermission(SystemPermissions.PermissionCenter.View)]
    public Task<List<PermissionModuleDto>> GetModulesAsync()
    {
        var modules = _permissionDefinitionManager.GetModules();
        var groups = _permissionDefinitionManager.GetGroups();

        var groupsByModule = groups
            .GroupBy(group => group.ModuleCode, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(grouping => grouping.Key, grouping => grouping.ToList(), StringComparer.OrdinalIgnoreCase);

        var result = modules
            .OrderBy(module => module.Order)
            .ThenBy(module => module.Code, StringComparer.OrdinalIgnoreCase)
            .Select(module => new PermissionModuleDto
            {
                Code = module.Code,
                DisplayName = module.DisplayName,
                Description = module.Description,
                Order = module.Order,
                Groups = groupsByModule.TryGetValue(module.Code, out var moduleGroups)
                    ? moduleGroups
                        .OrderBy(group => group.Order)
                        .ThenBy(group => group.Name, StringComparer.OrdinalIgnoreCase)
                        .Select(group => new PermissionGroupDto
                        {
                            Code = group.Name,
                            DisplayName = group.DisplayName,
                            Description = group.Description,
                            Order = group.Order,
                            Permissions = group.Permissions
                                .OrderBy(permission => permission.Order)
                                .ThenBy(permission => permission.Name, StringComparer.OrdinalIgnoreCase)
                                .Select(permission => new PermissionDefinitionDto
                                {
                                    Code = permission.Name,
                                    DisplayName = permission.DisplayName,
                                    Description = permission.Description,
                                    Order = permission.Order
                                })
                                .ToList()
                        })
                        .ToList()
                    : new List<PermissionGroupDto>()
            })
            .ToList();

        return Task.FromResult(result);
    }
}

