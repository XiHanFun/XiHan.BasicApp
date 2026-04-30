#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IPermissionRequestQueryService
// Guid:e9d432aa-d470-467e-83ad-7782b594f987
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
/// 权限申请查询应用服务接口
/// </summary>
public interface IPermissionRequestQueryService : IApplicationService
{
    /// <summary>
    /// 获取权限申请分页列表
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>权限申请分页列表</returns>
    Task<PageResultDtoBase<PermissionRequestListItemDto>> GetPermissionRequestPageAsync(PermissionRequestPageQueryDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取权限申请详情
    /// </summary>
    /// <param name="id">权限申请主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>权限申请详情</returns>
    Task<PermissionRequestDetailDto?> GetPermissionRequestDetailAsync(long id, CancellationToken cancellationToken = default);
}
