using System;
using System.Collections.Generic;

namespace NS.Framework.Core.Abstractions.Domain;

/// <summary>
/// 定义树形模型接口
/// </summary>
/// <typeparam name="T">树节点类型</typeparam>
public interface ITreeModel<T> : IOrderNum
{
    /// <summary>
    /// 节点ID
    /// </summary>
    Guid Id { get; }
    
    /// <summary>
    /// 父节点ID
    /// </summary>
    Guid? ParentId { get; }
    
    /// <summary>
    /// 子节点列表
    /// </summary>
    List<T>? Children { get; }
}

