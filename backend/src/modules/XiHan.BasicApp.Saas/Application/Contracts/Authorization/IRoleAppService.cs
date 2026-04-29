#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IRoleAppService
// Guid:ce77247d-9ae3-493b-a4fd-1634e1531721
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/30 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;

namespace XiHan.BasicApp.Saas.Application.Contracts;

/// <summary>
/// 角色命令应用服务接口
/// </summary>
public interface IRoleAppService : IApplicationService
{
    /// <summary>
    /// 创建角色
    /// </summary>
    /// <param name="input">创建参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>角色详情</returns>
    Task<RoleDetailDto> CreateRoleAsync(RoleCreateDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新角色
    /// </summary>
    /// <param name="input">更新参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>角色详情</returns>
    Task<RoleDetailDto> UpdateRoleAsync(RoleUpdateDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新角色状态
    /// </summary>
    /// <param name="input">状态更新参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>角色详情</returns>
    Task<RoleDetailDto> UpdateRoleStatusAsync(RoleStatusUpdateDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除角色
    /// </summary>
    /// <param name="id">角色主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task DeleteRoleAsync(long id, CancellationToken cancellationToken = default);
}
