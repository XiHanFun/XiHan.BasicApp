#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IPermissionConditionAppService
// Guid:8c3e4aa6-e933-47b8-9841-85800e60637c
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/30 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;

namespace XiHan.BasicApp.Saas.Application.Contracts;

/// <summary>
/// 权限 ABAC 条件命令应用服务接口
/// </summary>
public interface IPermissionConditionAppService : IApplicationService
{
    /// <summary>
    /// 创建权限 ABAC 条件
    /// </summary>
    /// <param name="input">创建参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>ABAC 条件详情</returns>
    Task<PermissionConditionDetailDto> CreatePermissionConditionAsync(PermissionConditionCreateDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新权限 ABAC 条件
    /// </summary>
    /// <param name="input">更新参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>ABAC 条件详情</returns>
    Task<PermissionConditionDetailDto> UpdatePermissionConditionAsync(PermissionConditionUpdateDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新权限 ABAC 条件状态
    /// </summary>
    /// <param name="input">状态更新参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>ABAC 条件详情</returns>
    Task<PermissionConditionDetailDto> UpdatePermissionConditionStatusAsync(PermissionConditionStatusUpdateDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除权限 ABAC 条件
    /// </summary>
    /// <param name="id">ABAC 条件主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task DeletePermissionConditionAsync(long id, CancellationToken cancellationToken = default);
}
