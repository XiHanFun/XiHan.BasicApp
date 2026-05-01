#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IFileQueryService
// Guid:5041f58f-e943-4734-af2f-6b18e550b982
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
/// 系统文件查询应用服务接口
/// </summary>
public interface IFileQueryService : IApplicationService
{
    /// <summary>
    /// 获取系统文件分页列表
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>系统文件分页列表</returns>
    Task<PageResultDtoBase<FileListItemDto>> GetFilePageAsync(FilePageQueryDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取系统文件详情
    /// </summary>
    /// <param name="id">系统文件主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>系统文件详情</returns>
    Task<FileDetailDto?> GetFileDetailAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取系统文件存储分页列表
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>系统文件存储分页列表</returns>
    Task<PageResultDtoBase<FileStorageListItemDto>> GetFileStoragePageAsync(FileStoragePageQueryDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取系统文件存储详情
    /// </summary>
    /// <param name="id">系统文件存储主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>系统文件存储详情</returns>
    Task<FileStorageDetailDto?> GetFileStorageDetailAsync(long id, CancellationToken cancellationToken = default);
}
