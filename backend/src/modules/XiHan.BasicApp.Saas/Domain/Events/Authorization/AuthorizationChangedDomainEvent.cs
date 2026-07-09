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
/// <remarks>
/// 由授权写路径（角色权限、用户直授、用户角色的授予/撤销）在业务操作后发布，
/// 驱动：①授权快照/导航缓存失效（<c>AuthorizationChangedEventHandler</c>）；
/// ②权限变更审计落库（<c>PermissionChangeLogEventHandler</c> 写 <c>SysPermissionChangeLog</c>）。
/// 事件直接携带确定的 <see cref="PermissionChangeType"/>（由发布方按语义给出），
/// 避免消费端反推"授予/撤销"，也让"撤销"得以被审计记录。
/// </remarks>
public sealed class AuthorizationChangedDomainEvent : SaasDomainEventBase
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="tenantId">租户ID</param>
    /// <param name="changeType">变更类型（授予/撤销/拒绝/分配角色/移除角色）</param>
    /// <param name="targetUserId">目标用户ID（用户级变更时填写）</param>
    /// <param name="targetRoleId">目标角色ID（角色级变更、或用户分配/移除角色时填写）</param>
    /// <param name="permissionId">权限ID（权限级变更时填写；分配/移除角色时为空）</param>
    /// <param name="operatorUserId">操作人ID</param>
    /// <param name="reason">变更原因</param>
    public AuthorizationChangedDomainEvent(
        long tenantId,
        PermissionChangeType changeType,
        long? targetUserId,
        long? targetRoleId,
        long? permissionId,
        long? operatorUserId = null,
        string? reason = null)
        : base(tenantId, operatorUserId, reason)
    {
        ChangeType = changeType;
        TargetUserId = targetUserId;
        TargetRoleId = targetRoleId;
        PermissionId = permissionId;
    }

    /// <summary>
    /// 变更类型
    /// </summary>
    public PermissionChangeType ChangeType { get; }

    /// <summary>
    /// 目标用户ID（用户级变更时填写）
    /// </summary>
    public long? TargetUserId { get; }

    /// <summary>
    /// 目标角色ID（角色级变更、或用户分配/移除角色时填写）
    /// </summary>
    public long? TargetRoleId { get; }

    /// <summary>
    /// 权限ID（权限级变更时填写）
    /// </summary>
    public long? PermissionId { get; }
}
