#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IConfigQueryService
// Guid:98969094-5f57-4767-a97c-ed37383ac25d
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
/// 系统配置查询应用服务接口
/// </summary>
public interface IConfigQueryService : IApplicationService
{
    /// <summary>
    /// 获取系统配置分页列表
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>系统配置分页列表</returns>
    Task<PageResultDtoBase<ConfigListItemDto>> GetConfigPageAsync(ConfigPageQueryDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取系统配置详情
    /// </summary>
    /// <param name="id">系统配置主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>系统配置详情</returns>
    Task<ConfigDetailDto?> GetConfigDetailAsync(long id, CancellationToken cancellationToken = default);
}
