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

    #region TenantMembers

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
