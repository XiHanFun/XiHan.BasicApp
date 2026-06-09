#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ITenantEditionPermissionQueryService
// Guid:2c579193-998c-4eec-af9e-2f27413dba35
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/30 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;

namespace XiHan.BasicApp.Saas.Application.Contracts;

/// <summary>
/// 租户版本权限查询应用服务接口
/// </summary>
public interface ITenantEditionPermissionQueryService : IApplicationService
{
    /// <summary>
    /// 获取租户版本权限列表
    /// </summary>
    /// <param name="editionId">租户版本主键</param>
    /// <param name="onlyValid">是否仅返回有效绑定</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>租户版本权限列表</returns>
    Task<IReadOnlyList<TenantEditionPermissionListItemDto>> GetTenantEditionPermissionsAsync(long editionId, bool onlyValid = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取租户版本权限详情
    /// </summary>
    /// <param name="id">租户版本权限绑定主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>租户版本权限详情</returns>
    Task<TenantEditionPermissionDetailDto?> GetTenantEditionPermissionDetailAsync(long id, CancellationToken cancellationToken = default);
}
