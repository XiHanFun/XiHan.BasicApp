// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Services;

/// <summary>
/// 模仿登录准入方案：通过校验后确定的被模仿者与目标上下文。
/// </summary>
/// <param name="Target">被模仿者</param>
/// <param name="TargetTenantId">模仿会话所处租户；空表示平台运维态</param>
/// <param name="TargetTenantName">模仿会话所处租户名称</param>
/// <param name="IsCrossTenant">是否跨出发起人当前上下文</param>
/// <param name="OperatorIsSuperAdmin">发起人是否为超级管理员</param>
public sealed record ImpersonationPlan(
    SysUser Target,
    long? TargetTenantId,
    string? TargetTenantName,
    bool IsCrossTenant,
    bool OperatorIsSuperAdmin);

/// <summary>
/// 模仿登录准入判定服务。
/// </summary>
public interface IImpersonationPolicyService
{
    /// <summary>
    /// 判定发起人能否模仿目标用户，通过则返回准入方案，否则抛出禁止异常。
    /// </summary>
    /// <param name="operatorUserId">发起人用户标识</param>
    /// <param name="operatorTenantId">发起人当前所处租户；空表示平台运维态</param>
    /// <param name="operatorIsImpersonating">发起人当前是否已处于模仿态</param>
    /// <param name="targetUserId">目标用户标识</param>
    /// <param name="requestedTenantId">请求指定的目标租户；空表示由服务解析</param>
    /// <param name="now">当前时间</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>准入方案</returns>
    Task<ImpersonationPlan> AuthorizeStartAsync(
        long operatorUserId,
        long? operatorTenantId,
        bool operatorIsImpersonating,
        long targetUserId,
        long? requestedTenantId,
        DateTimeOffset now,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 判定当前用户能否授出指定权限，不能则抛出禁止异常。
    /// </summary>
    /// <remarks>
    /// 平台专属权限码只允许超级管理员授出；模仿类权限码另需当前租户内的管理类成员身份。
    /// </remarks>
    /// <param name="permissionIds">被授出的权限主键集合</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task EnsureCanGrantPermissionIdsAsync(IReadOnlyCollection<long> permissionIds, CancellationToken cancellationToken = default);
}
