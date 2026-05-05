using System;
using System.Collections.Generic;
using Volo.Abp.Domain.Repositories;

namespace NS.Framework.SqlSugar.DependencyInjection;

/// <summary>
/// SqlSugar DbContext 注册选项
/// </summary>
public class SqlSugarDbContextRegistrationOptions
{
    /// <summary>
    /// 默认仓储实现类型
    /// </summary>
    public Type? DefaultRepositoryImplementation { get; set; }

    /// <summary>
    /// 自定义仓储注册器列表
    /// </summary>
    public List<Type> CustomRepositories { get; } = new();

    /// <summary>
    /// 是否启用默认仓储
    /// </summary>
    public bool AddDefaultRepositories { get; set; } = true;

    /// <summary>
    /// 默认仓储接口类型列表（用于指定哪些接口需要注册默认实现）
    /// </summary>
    public List<Type> DefaultRepositoryInterfaces { get; } = new();
}

