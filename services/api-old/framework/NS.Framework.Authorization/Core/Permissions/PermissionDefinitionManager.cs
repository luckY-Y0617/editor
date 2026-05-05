using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using NS.Framework.Authorization.Abstractions.Permissions;
using Volo.Abp.DependencyInjection;

namespace NS.Framework.Authorization.Core.Permissions;

public sealed class PermissionDefinitionManager : IPermissionDefinitionManager, ISingletonDependency
{
    private readonly IEnumerable<IPermissionDefinitionProvider> _providers;
    private readonly ILogger<PermissionDefinitionManager> _logger;
    private PermissionDefinitionRegistry? _registry;
    private readonly object _lock = new();

    public PermissionDefinitionManager(
        IEnumerable<IPermissionDefinitionProvider> providers,
        ILogger<PermissionDefinitionManager> logger)
    {
        _providers = providers;
        _logger = logger;
    }

    public PermissionDefinition? GetOrNull(string name)
        => EnsureRegistry().PermissionsByName.TryGetValue(name, out var permission)
            ? permission
            : null;

    public IReadOnlyList<PermissionDefinition> GetPermissions()
        => EnsureRegistry().Permissions;

    public IReadOnlyList<PermissionGroupDefinition> GetGroups()
        => EnsureRegistry().Groups;

    public IReadOnlyList<PermissionModuleDefinition> GetModules()
        => EnsureRegistry().Modules;

    private PermissionDefinitionRegistry EnsureRegistry()
    {
        if (_registry != null)
        {
            return _registry;
        }

        lock (_lock)
        {
            if (_registry != null)
            {
                return _registry;
            }

            var context = new PermissionDefinitionContext();
            foreach (var provider in _providers)
            {
                if (provider is PermissionDefinitionProvider baseProvider)
                {
                    baseProvider.PreDefine(context);
                }

                provider.Define(context);

                if (provider is PermissionDefinitionProvider postProvider)
                {
                    postProvider.PostDefine(context);
                }
            }

            var registry = PermissionDefinitionRegistry.Create(context, _logger);
            _registry = registry;
            return registry;
        }
    }

    private sealed class PermissionDefinitionRegistry
    {
        public IReadOnlyList<PermissionModuleDefinition> Modules { get; init; } = Array.Empty<PermissionModuleDefinition>();
        public IReadOnlyList<PermissionGroupDefinition> Groups { get; init; } = Array.Empty<PermissionGroupDefinition>();
        public IReadOnlyList<PermissionDefinition> Permissions { get; init; } = Array.Empty<PermissionDefinition>();
        public IReadOnlyDictionary<string, PermissionDefinition> PermissionsByName { get; init; } =
            new Dictionary<string, PermissionDefinition>(StringComparer.OrdinalIgnoreCase);

        public static PermissionDefinitionRegistry Create(
            PermissionDefinitionContext context,
            ILogger logger)
        {
            var modules = context.Modules.Values
                .OrderBy(m => m.Order)
                .ThenBy(m => m.Code, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var groups = context.Groups.Values
                .OrderBy(g => g.ModuleCode, StringComparer.OrdinalIgnoreCase)
                .ThenBy(g => g.Order)
                .ThenBy(g => g.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var permissionsByName = new Dictionary<string, PermissionDefinition>(StringComparer.OrdinalIgnoreCase);
            var permissions = new List<PermissionDefinition>();

            foreach (var group in groups)
            {
                foreach (var permission in group.Permissions
                             .OrderBy(p => p.Order)
                             .ThenBy(p => p.Name, StringComparer.OrdinalIgnoreCase))
                {
                    if (permissionsByName.ContainsKey(permission.Name))
                    {
                        logger.LogWarning(
                            "Duplicate permission definition detected: {Permission}",
                            permission.Name);
                        continue;
                    }

                    permissionsByName[permission.Name] = permission;
                    permissions.Add(permission);
                }
            }

            return new PermissionDefinitionRegistry
            {
                Modules = modules,
                Groups = groups,
                Permissions = permissions,
                PermissionsByName = permissionsByName
            };
        }
    }
}

