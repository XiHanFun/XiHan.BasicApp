// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Saas.Domain.Events;

/// <summary>
/// 层级关系变更事件
/// </summary>
public sealed class HierarchyChangedDomainEvent : SaasDomainEventBase
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public HierarchyChangedDomainEvent(
        long tenantId,
        string hierarchyType,
        long nodeId,
        long? parentId,
        long? operatorUserId = null,
        string? reason = null)
        : base(tenantId, operatorUserId, reason)
    {
        HierarchyType = hierarchyType;
        NodeId = nodeId;
        ParentId = parentId;
    }

    /// <summary>
    /// 层级类型
    /// </summary>
    public string HierarchyType { get; }

    /// <summary>
    /// 节点ID
    /// </summary>
    public long NodeId { get; }

    /// <summary>
    /// 父节点ID
    /// </summary>
    public long? ParentId { get; }
}
