#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:DataScopeChangedDomainEvent
// Guid:6d97f77d-85b4-4ae4-8eb2-a10708835a8b
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/26 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Domain.Events;

/// <summary>
/// 数据权限范围变更事件
/// </summary>
public sealed class DataScopeChangedDomainEvent : SaasDomainEventBase
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public DataScopeChangedDomainEvent(
        long tenantId,
        string targetType,
        long targetId,
        DataPermissionScope dataScope,
        long? operatorUserId = null,
        string? reason = null)
        : base(tenantId, operatorUserId, reason)
    {
        TargetType = targetType;
        TargetId = targetId;
        DataScope = dataScope;
    }

    /// <summary>
    /// 目标类型
    /// </summary>
    public string TargetType { get; }

    /// <summary>
    /// 目标ID
    /// </summary>
    public long TargetId { get; }

    /// <summary>
    /// 数据权限范围
    /// </summary>
    public DataPermissionScope DataScope { get; }
}
