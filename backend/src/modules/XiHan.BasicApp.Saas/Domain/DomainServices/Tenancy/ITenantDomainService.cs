// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 租户领域服务
/// </summary>
public interface ITenantDomainService
{
    /// <summary>
    /// 创建租户
    /// </summary>
    Task<TenantCommandResult> CreateTenantAsync(TenantCreateCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 撤销租户成员
    /// </summary>
    Task DeleteTenantMemberAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新租户
    /// </summary>
    Task<TenantCommandResult> UpdateTenantAsync(TenantUpdateCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新租户成员
    /// </summary>
    Task<TenantMemberCommandResult> UpdateTenantMemberAsync(TenantMemberUpdateCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新租户成员邀请状态
    /// </summary>
    Task<TenantMemberCommandResult> UpdateTenantMemberInviteStatusAsync(TenantMemberInviteStatusChangeCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新租户成员状态
    /// </summary>
    Task<TenantMemberCommandResult> UpdateTenantMemberStatusAsync(TenantMemberStatusChangeCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新租户状态
    /// </summary>
    Task<TenantCommandResult> UpdateTenantStatusAsync(TenantStatusChangeCommand command, CancellationToken cancellationToken = default);
}
