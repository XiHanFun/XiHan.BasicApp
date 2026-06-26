#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IStorageConfigQueryService
// Guid:d9f3a6c8-1b47-4e25-9c8b-5a2d7e4f1b93
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/12 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;
using XiHan.Framework.Domain.Shared.Paging.Dtos;

namespace XiHan.BasicApp.Saas.Application.Contracts;

/// <summary>
/// 存储配置查询应用服务接口
/// </summary>
public interface IStorageConfigQueryService : IApplicationService
{
    /// <summary>
    /// 获取存储配置分页列表
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>存储配置分页列表</returns>
    Task<PageResultDtoBase<StorageConfigListItemDto>> GetStorageConfigPageAsync(StorageConfigPageQueryDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取存储配置详情
    /// </summary>
    /// <param name="id">存储配置主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>存储配置详情</returns>
    Task<StorageConfigDetailDto?> GetStorageConfigDetailAsync(long id, CancellationToken cancellationToken = default);
}
