#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IPermissionChangeLogQueryService
// Guid:bcde22f5-6207-4386-baa7-32f55cb9848d
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/01 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;
using XiHan.Framework.Domain.Shared.Paging.Dtos;

namespace XiHan.BasicApp.Saas.Application.Contracts;

/// <summary>
/// 权限变更日志查询应用服务接口
/// </summary>
public interface IPermissionChangeLogQueryService : IApplicationService
{
    /// <summary>
    /// 获取权限变更日志分页列表
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>权限变更日志分页列表</returns>
    Task<PageResultDtoBase<PermissionChangeLogListItemDto>> GetPermissionChangeLogPageAsync(PermissionChangeLogPageQueryDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取权限变更日志详情
    /// </summary>
    /// <param name="id">权限变更日志主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>权限变更日志详情</returns>
    Task<PermissionChangeLogDetailDto?> GetPermissionChangeLogDetailAsync(long id, CancellationToken cancellationToken = default);
}
