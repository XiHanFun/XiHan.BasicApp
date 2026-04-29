#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IRoleQueryService
// Guid:6c38724d-f577-4c3a-8705-0494fb673db7
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/30 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;
using XiHan.Framework.Domain.Shared.Paging.Dtos;

namespace XiHan.BasicApp.Saas.Application.Contracts;

/// <summary>
/// 角色查询应用服务接口
/// </summary>
public interface IRoleQueryService : IApplicationService
{
    /// <summary>
    /// 获取角色分页列表
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>角色分页列表</returns>
    Task<PageResultDtoBase<RoleListItemDto>> GetRolePageAsync(RolePageQueryDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取角色详情
    /// </summary>
    /// <param name="id">角色主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>角色详情</returns>
    Task<RoleDetailDto?> GetRoleDetailAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取已启用角色选择项
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>已启用角色选择项</returns>
    Task<IReadOnlyList<RoleSelectItemDto>> GetEnabledRolesAsync(RoleSelectQueryDto input, CancellationToken cancellationToken = default);
}
