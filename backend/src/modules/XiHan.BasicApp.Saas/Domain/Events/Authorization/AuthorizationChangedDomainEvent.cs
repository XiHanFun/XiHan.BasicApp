#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:AuthorizationChangedDomainEvent
// Guid:6629733f-20f1-487c-9db0-c5a9417fc445
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/26 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Domain.Events;

/// <summary>
/// 授权变更事件
/// </summary>
public sealed class AuthorizationChangedDomainEvent : SaasDomainEventBase
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public AuthorizationChangedDomainEvent(
        long tenantId,
        string targetType,
        long targetId,
        long permissionId,
        PermissionAction action,
        long? operatorUserId = null,
        string? reason = null)
        : base(tenantId, operatorUserId, reason)
    {
        TargetType = targetType;
        TargetId = targetId;
        PermissionId = permissionId;
        Action = action;
    }

    /// <summary>
    /// 授权目标类型
    /// </summary>
    public string TargetType { get; }

    /// <summary>
    /// 授权目标ID
    /// </summary>
    public long TargetId { get; }

    /// <summary>
    /// 权限ID
    /// </summary>
    public long PermissionId { get; }

    /// <summary>
    /// 授权操作
    /// </summary>
    public PermissionAction Action { get; }
}
