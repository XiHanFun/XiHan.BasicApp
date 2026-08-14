// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;

namespace XiHan.BasicApp.Saas.Application.Contracts;

/// <summary>
/// 租户命令应用服务接口
/// </summary>
public interface ITenantAppService : IApplicationService
{
    /// <summary>
    /// 创建租户
    /// </summary>
    /// <param name="input">创建参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>租户详情</returns>
    Task<TenantDetailDto> CreateTenantAsync(TenantCreateDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新租户基础资料
    /// </summary>
    /// <param name="input">更新参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>租户详情</returns>
    Task<TenantDetailDto> UpdateTenantAsync(TenantUpdateDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新租户状态
    /// </summary>
    /// <param name="input">状态更新参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>租户详情</returns>
    Task<TenantDetailDto> UpdateTenantStatusAsync(TenantStatusUpdateDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 初始化租户数据库（仅库隔离租户：建库 → 建表 → 基线种子，幂等）
    /// </summary>
    /// <param name="id">租户主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>租户详情（含最新配置状态）</returns>
    Task<TenantDetailDto> InitializeDatabaseAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除租户（软删，要求租户已停用或暂停）
    /// </summary>
    /// <param name="id">租户主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task DeleteTenantAsync(long id, CancellationToken cancellationToken = default);

    #region TenantMembers

    /// <summary>
    /// 添加租户成员（把已有用户直接加入租户，立即生效）
    /// </summary>
    /// <param name="input">添加参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>租户成员详情</returns>
    Task<TenantMemberDetailDto> AddTenantMemberAsync(TenantMemberAddDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 邀请租户成员（落待接受邀请，被邀请人接受后生效）
    /// </summary>
    /// <param name="input">邀请参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>租户成员详情</returns>
    Task<TenantMemberDetailDto> InviteTenantMemberAsync(TenantMemberInviteDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新租户成员
    /// </summary>
    /// <param name="input">更新参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>租户成员详情</returns>
    Task<TenantMemberDetailDto> UpdateTenantMemberAsync(TenantMemberUpdateDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新租户成员状态
    /// </summary>
    /// <param name="input">状态更新参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>租户成员详情</returns>
    Task<TenantMemberDetailDto> UpdateTenantMemberStatusAsync(TenantMemberStatusUpdateDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新租户成员邀请状态
    /// </summary>
    /// <param name="input">邀请状态更新参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>租户成员详情</returns>
    Task<TenantMemberDetailDto> UpdateTenantMemberInviteStatusAsync(TenantMemberInviteStatusUpdateDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 撤销租户成员
    /// </summary>
    /// <param name="id">租户成员主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task DeleteTenantMemberAsync(long id, CancellationToken cancellationToken = default);

    #endregion TenantMembers
}
