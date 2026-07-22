// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;
using XiHan.Framework.Domain.Shared.Paging.Dtos;

namespace XiHan.BasicApp.Saas.Application.Contracts;

/// <summary>
/// 系统版本查询应用服务接口
/// </summary>
public interface IVersionQueryService : IApplicationService
{
    /// <summary>
    /// 获取系统版本分页列表
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>系统版本分页列表</returns>
    Task<PageResultDtoBase<VersionListItemDto>> GetVersionPageAsync(VersionPageQueryDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取系统版本详情
    /// </summary>
    /// <param name="id">系统版本主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>系统版本详情</returns>
    Task<VersionDetailDto?> GetVersionDetailAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取系统迁移历史分页列表
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>系统迁移历史分页列表</returns>
    Task<PageResultDtoBase<MigrationHistoryListItemDto>> GetMigrationHistoryPageAsync(MigrationHistoryPageQueryDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取系统迁移历史详情
    /// </summary>
    /// <param name="id">系统迁移历史主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>系统迁移历史详情</returns>
    Task<MigrationHistoryDetailDto?> GetMigrationHistoryDetailAsync(long id, CancellationToken cancellationToken = default);
}
