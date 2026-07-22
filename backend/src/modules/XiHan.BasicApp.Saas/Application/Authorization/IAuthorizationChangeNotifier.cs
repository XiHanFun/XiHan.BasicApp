// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Authorization;

/// <summary>
/// 授权变更通知器
/// </summary>
/// <remarks>
/// 授权写路径（角色权限、用户直授、用户角色的授予/撤销）在业务操作后调用，
/// 统一以当前租户/操作人上下文发布 <c>AuthorizationChangedDomainEvent</c>，
/// 驱动缓存失效与权限变更审计落库。
/// </remarks>
public interface IAuthorizationChangeNotifier
{
    /// <summary>
    /// 发布一条授权变更事件。
    /// </summary>
    /// <param name="changeType">变更类型</param>
    /// <param name="targetUserId">目标用户ID（用户级变更时填写）</param>
    /// <param name="targetRoleId">目标角色ID（角色级变更、或用户分配/移除角色时填写）</param>
    /// <param name="permissionId">权限ID（权限级变更时填写）</param>
    /// <param name="reason">变更原因</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task NotifyAsync(
        PermissionChangeType changeType,
        long? targetUserId,
        long? targetRoleId,
        long? permissionId,
        string? reason = null,
        CancellationToken cancellationToken = default);
}
