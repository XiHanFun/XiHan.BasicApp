// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Extensions;
using XiHan.BasicApp.Saas.Application.Mappers;
using XiHan.BasicApp.Saas.Application.Services;
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
public sealed class FileQueryService
    : SaasApplicationService, IFileQueryService
{
    /// <summary>
    /// 系统文件仓储
    /// </summary>
    private readonly IFileRepository _fileRepository;

    /// <summary>
    /// 系统文件存储仓储
    /// </summary>
    private readonly IFileStorageRepository _fileStorageRepository;

    /// <summary>
    /// 字段级安全服务
    /// </summary>
    private readonly IFieldSecurityService _fieldSecurity;

    /// <summary>
    /// 构造函数
    /// </summary>
    public FileQueryService(
        IFileRepository fileRepository,
        IFileStorageRepository fileStorageRepository,
        IFieldSecurityService fieldSecurityService)
    {
        _fileRepository = fileRepository;
        _fileStorageRepository = fileStorageRepository;
        _fieldSecurity = fieldSecurityService;
    }

    /// <summary>
    /// 获取系统文件分页列表
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>系统文件分页列表</returns>
    [PermissionAuthorize(SaasPermissionCodes.File.Read)]
    [HttpPost]
    public async Task<PageResultDtoBase<FileListItemDto>> GetFilePageAsync(FilePageQueryDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var request = BuildFilePageRequest(input);

        // 排序：前端选择优先，FLS 门控剔除不可读/已脱敏字段（防按受保护字段排序泄漏真实顺序）；无有效排序回退默认排序
        await _fieldSecurity.GuardSortsAsync(request.Conditions, "SysFile", cancellationToken);
        if (request.Conditions.Sorts.Count == 0)
        {
            ApplyFileSorts(request);
        }

        // 过滤：前端区间(Between)/多选(In) 经 conditions.filters 下发，同样 FLS 门控剔除不可读/已脱敏字段
        await _fieldSecurity.GuardFiltersAsync(request.Conditions, "SysFile", cancellationToken);

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
    [HttpPost]
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
            Conditions = new QueryConditions()
        };

        if (!string.IsNullOrWhiteSpace(input.Keyword))
        {
            request.Conditions.SetKeyword<SysFile>(
                input.Keyword.Trim(),
                file => file.FileName,
                file => file.OriginalName,
                file => file.FileExtension,
                file => file.MimeType);
        }

        if (input.FileType.HasValue)
        {
            request.Conditions.AddFilter((SysFile file) => file.FileType, input.FileType.Value);
        }

        if (!string.IsNullOrWhiteSpace(input.FileExtension))
        {
            request.Conditions.AddFilter((SysFile file) => file.FileExtension, input.FileExtension.Trim());
        }

        if (!string.IsNullOrWhiteSpace(input.MimeType))
        {
            request.Conditions.AddFilter((SysFile file) => file.MimeType, input.MimeType.Trim());
        }

        if (input.AccessLevel.HasValue)
        {
            request.Conditions.AddFilter((SysFile file) => file.AccessLevel, input.AccessLevel.Value);
        }

        if (input.IsEncrypted.HasValue)
        {
            request.Conditions.AddFilter((SysFile file) => file.IsEncrypted, input.IsEncrypted.Value);
        }

        if (input.IsTemporary.HasValue)
        {
            request.Conditions.AddFilter((SysFile file) => file.IsTemporary, input.IsTemporary.Value);
        }

        if (input.Status.HasValue)
        {
            request.Conditions.AddFilter((SysFile file) => file.Status, input.Status.Value);
        }

        if (input.ExpirationTimeStart.HasValue)
        {
            request.Conditions.AddFilter((SysFile file) => file.ExpirationTime, input.ExpirationTimeStart.Value, QueryOperator.GreaterThanOrEqual);
        }

        if (input.ExpirationTimeEnd.HasValue)
        {
            request.Conditions.AddFilter((SysFile file) => file.ExpirationTime, input.ExpirationTimeEnd.Value, QueryOperator.LessThanOrEqual);
        }

        // 前端选择的排序原样带入（FLS 门控与默认兜底在调用方 GetFilePageAsync 处理）
        if (input.Conditions?.Sorts is { Count: > 0 } sorts)
        {
            _ = request.Conditions.AddSorts(sorts);
        }

        // 前端区间/多选过滤原样带入（FLS 门控在调用方 GetFilePageAsync 处理）
        if (input.Conditions?.Filters is { Count: > 0 } filters)
        {
            _ = request.Conditions.AddFilters(filters);
        }
        return request;
    }

    /// <summary>
    /// 施加系统文件默认排序（无前端排序时的兜底）
    /// </summary>
    /// <param name="request">系统文件分页请求</param>
    private static void ApplyFileSorts(BasicAppPRDto request)
    {
        request.Conditions.AddSort((SysFile file) => file.CreatedTime, SortDirection.Descending, 0);
        request.Conditions.AddSort((SysFile file) => file.FileType, SortDirection.Ascending, 1);
        request.Conditions.AddSort((SysFile file) => file.FileName, SortDirection.Ascending, 2);
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
            Conditions = new QueryConditions()
        };

        if (!string.IsNullOrWhiteSpace(input.Keyword))
        {
            request.Conditions.SetKeyword<SysFileStorage>(
                input.Keyword.Trim(),
                storage => storage.StorageProvider,
                storage => storage.StorageRegion,
                storage => storage.AccessControl,
                storage => storage.StorageClass,
                storage => storage.CacheControl);
        }

        if (input.FileId.HasValue && input.FileId.Value > 0)
        {
            request.Conditions.AddFilter((SysFileStorage storage) => storage.FileId, input.FileId.Value);
        }

        if (input.StorageType.HasValue)
        {
            request.Conditions.AddFilter((SysFileStorage storage) => storage.StorageType, input.StorageType.Value);
        }

        if (input.Status.HasValue)
        {
            request.Conditions.AddFilter((SysFileStorage storage) => storage.Status, input.Status.Value);
        }

        if (input.IsPrimary.HasValue)
        {
            request.Conditions.AddFilter((SysFileStorage storage) => storage.IsPrimary, input.IsPrimary.Value);
        }

        if (input.IsBackup.HasValue)
        {
            request.Conditions.AddFilter((SysFileStorage storage) => storage.IsBackup, input.IsBackup.Value);
        }

        if (input.EnableCdn.HasValue)
        {
            request.Conditions.AddFilter((SysFileStorage storage) => storage.EnableCdn, input.EnableCdn.Value);
        }

        if (input.IsVerified.HasValue)
        {
            request.Conditions.AddFilter((SysFileStorage storage) => storage.IsVerified, input.IsVerified.Value);
        }

        if (input.IsSynced.HasValue)
        {
            request.Conditions.AddFilter((SysFileStorage storage) => storage.IsSynced, input.IsSynced.Value);
        }

        if (input.UploadedTimeStart.HasValue)
        {
            request.Conditions.AddFilter((SysFileStorage storage) => storage.UploadedTime, input.UploadedTimeStart.Value, QueryOperator.GreaterThanOrEqual);
        }

        if (input.UploadedTimeEnd.HasValue)
        {
            request.Conditions.AddFilter((SysFileStorage storage) => storage.UploadedTime, input.UploadedTimeEnd.Value, QueryOperator.LessThanOrEqual);
        }

        request.Conditions.AddSort((SysFileStorage storage) => storage.FileId, SortDirection.Ascending, 0);
        request.Conditions.AddSort((SysFileStorage storage) => storage.IsPrimary, SortDirection.Descending, 1);
        request.Conditions.AddSort((SysFileStorage storage) => storage.Sort, SortDirection.Ascending, 2);
        request.Conditions.AddSort((SysFileStorage storage) => storage.StorageType, SortDirection.Ascending, 3);
        return request;
    }
}
