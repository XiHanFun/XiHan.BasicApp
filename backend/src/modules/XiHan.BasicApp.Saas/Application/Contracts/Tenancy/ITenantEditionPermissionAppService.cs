#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ITenantEditionPermissionAppService
// Guid:2d919e21-a8da-46fd-9039-26cc1ee219c4
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/30 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;

namespace XiHan.BasicApp.Saas.Application.Contracts;

/// <summary>
/// 租户版本权限命令应用服务接口
/// </summary>
public interface ITenantEditionPermissionAppService : IApplicationService
{
    /// <summary>
    /// 授予租户版本权限
    /// </summary>
    /// <param name="input">授权参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>租户版本权限详情</returns>
    Task<TenantEditionPermissionDetailDto> CreateTenantEditionPermissionAsync(TenantEditionPermissionGrantDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新租户版本权限状态
    /// </summary>
    /// <param name="input">状态更新参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>租户版本权限详情</returns>
    Task<TenantEditionPermissionDetailDto> UpdateTenantEditionPermissionStatusAsync(TenantEditionPermissionStatusUpdateDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 撤销租户版本权限
    /// </summary>
    /// <param name="id">租户版本权限绑定主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task DeleteTenantEditionPermissionAsync(long id, CancellationToken cancellationToken = default);
}
