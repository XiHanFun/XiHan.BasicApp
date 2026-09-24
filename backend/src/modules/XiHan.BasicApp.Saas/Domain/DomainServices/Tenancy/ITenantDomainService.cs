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
    /// 添加租户成员（<c>RequiresInvitation</c> 为 true 时落待接受邀请，否则直接生效）
    /// </summary>
    Task<TenantMemberCommandResult> AddTenantMemberAsync(TenantMemberAddCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 支持人员入驻：把平台账号以支持成员身份加入指定租户（仅平台上下文）
    /// </summary>
    Task<TenantMemberCommandResult> AddTenantSupportMemberAsync(TenantSupportMemberAddCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 支持人员离场：撤销平台账号在指定租户的支持成员身份（仅平台上下文）
    /// </summary>
    Task RemoveTenantSupportMemberAsync(long tenantId, long memberId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 所有权转移：把租户所有者身份转给该租户的另一名成员，原所有者改为管理员（仅平台上下文）
    /// </summary>
    Task<TenantOwnerTransferResult> TransferTenantOwnerAsync(TenantOwnerTransferCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除租户（软删，要求租户已停用）
    /// </summary>
    Task DeleteTenantAsync(long id, CancellationToken cancellationToken = default);

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
