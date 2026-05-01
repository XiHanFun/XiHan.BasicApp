#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:FileQueryService
// Guid:0ad37b0f-97d2-4318-8592-f4518d46a9d5
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/01 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.AspNetCore.Authorization;
using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Mappers;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authorization.AspNetCore;
using XiHan.Framework.Domain.Shared.Paging.Dtos;
using XiHan.Framework.Domain.Shared.Paging.Enums;
using XiHan.Framework.Domain.Shared.Paging.Models;

namespace XiHan.BasicApp.Saas.Application.QueryServices;

/// <summary>
/// 系统文件查询应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "系统文件")]
public sealed class FileQueryService(
    IFileRepository fileRepository,
    IFileStorageRepository fileStorageRepository)
    : SaasApplicationService, IFileQueryService
{
    /// <summary>
    /// 系统文件仓储
    /// </summary>
    private readonly IFileRepository _fileRepository = fileRepository;

    /// <summary>
    /// 系统文件存储仓储
    /// </summary>
    private readonly IFileStorageRepository _fileStorageRepository = fileStorageRepository;

    /// <summary>
    /// 获取系统文件分页列表
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>系统文件分页列表</returns>
    [PermissionAuthorize(SaasPermissionCodes.File.Read)]
    public async Task<PageResultDtoBase<FileListItemDto>> GetFilePageAsync(FilePageQueryDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var request = BuildFilePageRequest(input);
        var files = await _fileRepository.GetPagedAsync(request, cancellationToken);
        return files.Map(FileApplicationMapper.ToListItemDto);
    }

    /// <summary>
    /// 获取系统文件详情
    /// </summary>
    /// <param name="id">系统文件主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>系统文件详情</returns>
    [PermissionAuthorize(SaasPermissionCodes.File.Read)]
    public async Task<FileDetailDto?> GetFileDetailAsync(long id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "系统文件主键必须大于 0。");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var file = await _fileRepository.GetByIdAsync(id, cancellationToken);
        return file is null ? null : FileApplicationMapper.ToDetailDto(file);
    }

    /// <summary>
    /// 获取系统文件存储分页列表
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>系统文件存储分页列表</returns>
    [PermissionAuthorize(SaasPermissionCodes.File.Read)]
    public async Task<PageResultDtoBase<FileStorageListItemDto>> GetFileStoragePageAsync(FileStoragePageQueryDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var request = BuildFileStoragePageRequest(input);
        var storages = await _fileStorageRepository.GetPagedAsync(request, cancellationToken);
        return storages.Map(FileApplicationMapper.ToStorageListItemDto);
    }

    /// <summary>
    /// 获取系统文件存储详情
    /// </summary>
    /// <param name="id">系统文件存储主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>系统文件存储详情</returns>
    [PermissionAuthorize(SaasPermissionCodes.File.Read)]
    public async Task<FileStorageDetailDto?> GetFileStorageDetailAsync(long id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "系统文件存储主键必须大于 0。");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var storage = await _fileStorageRepository.GetByIdAsync(id, cancellationToken);
        return storage is null ? null : FileApplicationMapper.ToStorageDetailDto(storage);
    }

    /// <summary>
    /// 构建系统文件分页请求
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <returns>系统文件分页请求</returns>
    private static BasicAppPRDto BuildFilePageRequest(FilePageQueryDto input)
    {
        var request = new BasicAppPRDto
        {
            Page = input.Page,
            Behavior = input.Behavior,
            Conditions = new QueryConditions()
        };

        if (!string.IsNullOrWhiteSpace(input.Keyword))
        {
            request.Conditions.SetKeyword(
                input.Keyword.Trim(),
                nameof(SysFile.FileName),
                nameof(SysFile.OriginalName),
                nameof(SysFile.FileExtension),
                nameof(SysFile.MimeType));
        }

        if (input.FileType.HasValue)
        {
            request.Conditions.AddFilter(nameof(SysFile.FileType), input.FileType.Value);
        }

        if (!string.IsNullOrWhiteSpace(input.FileExtension))
        {
            request.Conditions.AddFilter(nameof(SysFile.FileExtension), input.FileExtension.Trim());
        }

        if (!string.IsNullOrWhiteSpace(input.MimeType))
        {
            request.Conditions.AddFilter(nameof(SysFile.MimeType), input.MimeType.Trim());
        }

        if (input.AccessLevel.HasValue)
        {
            request.Conditions.AddFilter(nameof(SysFile.AccessLevel), input.AccessLevel.Value);
        }

        if (input.IsEncrypted.HasValue)
        {
            request.Conditions.AddFilter(nameof(SysFile.IsEncrypted), input.IsEncrypted.Value);
        }

        if (input.IsTemporary.HasValue)
        {
            request.Conditions.AddFilter(nameof(SysFile.IsTemporary), input.IsTemporary.Value);
        }

        if (input.Status.HasValue)
        {
            request.Conditions.AddFilter(nameof(SysFile.Status), input.Status.Value);
        }

        if (input.ExpiresAtStart.HasValue)
        {
            request.Conditions.AddFilter(nameof(SysFile.ExpiresAt), input.ExpiresAtStart.Value, QueryOperator.GreaterThanOrEqual);
        }

        if (input.ExpiresAtEnd.HasValue)
        {
            request.Conditions.AddFilter(nameof(SysFile.ExpiresAt), input.ExpiresAtEnd.Value, QueryOperator.LessThanOrEqual);
        }

        request.Conditions.AddSort(nameof(SysFile.CreatedTime), SortDirection.Descending, 0);
        request.Conditions.AddSort(nameof(SysFile.FileType), SortDirection.Ascending, 1);
        request.Conditions.AddSort(nameof(SysFile.FileName), SortDirection.Ascending, 2);
        return request;
    }

    /// <summary>
    /// 构建系统文件存储分页请求
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <returns>系统文件存储分页请求</returns>
    private static BasicAppPRDto BuildFileStoragePageRequest(FileStoragePageQueryDto input)
    {
        var request = new BasicAppPRDto
        {
            Page = input.Page,
            Behavior = input.Behavior,
            Conditions = new QueryConditions()
        };

        if (!string.IsNullOrWhiteSpace(input.Keyword))
        {
            request.Conditions.SetKeyword(
                input.Keyword.Trim(),
                nameof(SysFileStorage.StorageProvider),
                nameof(SysFileStorage.StorageRegion),
                nameof(SysFileStorage.AccessControl),
                nameof(SysFileStorage.StorageClass),
                nameof(SysFileStorage.CacheControl));
        }

        if (input.FileId.HasValue && input.FileId.Value > 0)
        {
            request.Conditions.AddFilter(nameof(SysFileStorage.FileId), input.FileId.Value);
        }

        if (input.StorageType.HasValue)
        {
            request.Conditions.AddFilter(nameof(SysFileStorage.StorageType), input.StorageType.Value);
        }

        if (input.Status.HasValue)
        {
            request.Conditions.AddFilter(nameof(SysFileStorage.Status), input.Status.Value);
        }

        if (input.IsPrimary.HasValue)
        {
            request.Conditions.AddFilter(nameof(SysFileStorage.IsPrimary), input.IsPrimary.Value);
        }

        if (input.IsBackup.HasValue)
        {
            request.Conditions.AddFilter(nameof(SysFileStorage.IsBackup), input.IsBackup.Value);
        }

        if (input.EnableCdn.HasValue)
        {
            request.Conditions.AddFilter(nameof(SysFileStorage.EnableCdn), input.EnableCdn.Value);
        }

        if (input.IsVerified.HasValue)
        {
            request.Conditions.AddFilter(nameof(SysFileStorage.IsVerified), input.IsVerified.Value);
        }

        if (input.IsSynced.HasValue)
        {
            request.Conditions.AddFilter(nameof(SysFileStorage.IsSynced), input.IsSynced.Value);
        }

        if (input.UploadedAtStart.HasValue)
        {
            request.Conditions.AddFilter(nameof(SysFileStorage.UploadedAt), input.UploadedAtStart.Value, QueryOperator.GreaterThanOrEqual);
        }

        if (input.UploadedAtEnd.HasValue)
        {
            request.Conditions.AddFilter(nameof(SysFileStorage.UploadedAt), input.UploadedAtEnd.Value, QueryOperator.LessThanOrEqual);
        }

        request.Conditions.AddSort(nameof(SysFileStorage.FileId), SortDirection.Ascending, 0);
        request.Conditions.AddSort(nameof(SysFileStorage.IsPrimary), SortDirection.Descending, 1);
        request.Conditions.AddSort(nameof(SysFileStorage.SortOrder), SortDirection.Ascending, 2);
        request.Conditions.AddSort(nameof(SysFileStorage.StorageType), SortDirection.Ascending, 3);
        return request;
    }
}
