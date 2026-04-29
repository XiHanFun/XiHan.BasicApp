#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IRolePermissionAppService
// Guid:e871c168-742d-44c3-bcc1-d9141d7d8ff3
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/30 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;

namespace XiHan.BasicApp.Saas.Application.Contracts;

/// <summary>
/// 角色权限命令应用服务接口
/// </summary>
public interface IRolePermissionAppService : IApplicationService
{
    /// <summary>
    /// 授予角色权限
    /// </summary>
    /// <param name="input">授权参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>角色权限详情</returns>
    Task<RolePermissionDetailDto> CreateRolePermissionAsync(RolePermissionGrantDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新角色权限
    /// </summary>
    /// <param name="input">更新参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>角色权限详情</returns>
    Task<RolePermissionDetailDto> UpdateRolePermissionAsync(RolePermissionUpdateDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新角色权限状态
    /// </summary>
    /// <param name="input">状态更新参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>角色权限详情</returns>
    Task<RolePermissionDetailDto> UpdateRolePermissionStatusAsync(RolePermissionStatusUpdateDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 撤销角色权限
    /// </summary>
    /// <param name="id">角色权限绑定主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task DeleteRolePermissionAsync(long id, CancellationToken cancellationToken = default);
}
