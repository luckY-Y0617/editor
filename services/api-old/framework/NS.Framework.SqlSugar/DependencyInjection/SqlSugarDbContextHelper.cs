using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SqlSugar;
using Volo.Abp.Domain.Entities;

namespace NS.Framework.SqlSugar.DependencyInjection;

/// <summary>
/// SqlSugar DbContext 帮助类，用于获取实体类型
/// </summary>
public static class SqlSugarDbContextHelper
{
    private static readonly object _lock = new object();
    private static Type[]? _cachedEntityTypes;

    /// <summary>
    /// 从 DbContext 类型获取所有实体类型
    /// 对于 SqlSugar，我们通过扫描所有带有 [SugarTable] 特性的类型来获取实体
    /// </summary>
    public static IEnumerable<Type> GetEntityTypes(Type dbContextType)
    {
        // 对于 SqlSugar，实体类型不是通过 DbContext 的属性来定义的
        // 而是通过 [SugarTable] 特性来标记的
        // 我们需要扫描所有程序集来查找实体类型
        
        // 使用缓存避免重复扫描
        if (_cachedEntityTypes != null)
        {
            return _cachedEntityTypes;
        }

        lock (_lock)
        {
            if (_cachedEntityTypes != null)
            {
                return _cachedEntityTypes;
            }

            // 获取当前已加载的程序集
            var assemblies = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic)
                .Where(a => !string.IsNullOrEmpty(a.FullName))
                // 只扫描项目相关的程序集，避免扫描系统程序集
                .Where(a => a.FullName!.StartsWith("ClayMo", StringComparison.OrdinalIgnoreCase) ||
                            a.FullName.StartsWith("Volo.Abp", StringComparison.OrdinalIgnoreCase))
                .ToList();

            var entityTypes = assemblies
                .SelectMany(a =>
                {
                    try
                    {
                        return a.GetTypes();
                    }
                    catch (ReflectionTypeLoadException ex)
                    {
                        // 如果加载失败，尝试加载可用的类型
                        return ex.Types.Where(t => t != null)!;
                    }
                    catch
                    {
                        return Enumerable.Empty<Type>();
                    }
                })
                .Where(t => t != null)
                .Where(t => !t.IsAbstract && !t.IsInterface)
                .Where(t => t.GetCustomAttribute<SugarTable>() != null)
                .Where(t => t.GetCustomAttribute<SplitTableAttribute>() == null) // 排除分表实体
                .Where(t => typeof(IEntity).IsAssignableFrom(t)) // 必须是 IEntity
                .Distinct()
                .ToArray();

            _cachedEntityTypes = entityTypes;
            return entityTypes;
        }
    }
}

