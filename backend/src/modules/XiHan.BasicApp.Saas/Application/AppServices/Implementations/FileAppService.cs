#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:FileAppService
// Guid:ba527f10-d7ad-458a-8478-96fb0053ffab
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/04 12:48:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Mapster;
using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Application.Caching;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.QueryServices;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Application.Services;

namespace XiHan.BasicApp.Saas.Application.AppServices.Implementations;

/// <summary>
/// 文件应用服务
/// </summary>
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统Saas服务")]
public class FileAppService
    : CrudApplicationServiceBase<SysFile, FileDto, long, FileCreateDto, FileUpdateDto, BasicAppPRDto>,
        IFileAppService
{
    private readonly IFileRepository _fileRepository;
    private readonly IFileQueryService _queryService;
    private readonly IFileDomainService _domainService;
    private readonly IRbacLookupCacheService _lookupCacheService;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="fileRepository"></param>
    /// <param name="queryService"></param>
    /// <param name="domainService"></param>
    /// <param name="lookupCacheService"></param>
    public FileAppService(
        IFileRepository fileRepository,
        IFileQueryService queryService,
        IFileDomainService domainService,
        IRbacLookupCacheService lookupCacheService)
        : base(fileRepository)
    {
        _fileRepository = fileRepository;
        _queryService = queryService;
        _domainService = domainService;
        _lookupCacheService = lookupCacheService;
    }

    /// <summary>
    /// 根据ID获取文件
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public override async Task<FileDto?> GetByIdAsync(long id)
    {
        return await _queryService.GetByIdAsync(id);
    }

    /// <summary>
    /// 根据哈希获取文件
    /// </summary>
    public async Task<FileDto?> GetByFileHashAsync(string fileHash, long? tenantId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileHash);
        var normalizedHash = fileHash.Trim();

        return await _lookupCacheService.GetFileByHashAsync(
            normalizedHash,
            tenantId,
            async token =>
            {
                var entity = await _fileRepository.GetByFileHashAsync(normalizedHash, tenantId, token);
                return entity?.Adapt<FileDto>();
            });
    }

    /// <summary>
    /// 创建文件
    /// </summary>
    public override async Task<FileDto> CreateAsync(FileCreateDto input)
    {
        input.ValidateAnnotations();

        var entity = await MapDtoToEntityAsync(input);
        var created = await _domainService.CreateAsync(entity);
        await _lookupCacheService.InvalidateFileLookupAsync(input.TenantId);
        return created.Adapt<FileDto>()!;
    }

    /// <summary>
    /// 更新文件
    /// </summary>
    public override async Task<FileDto> UpdateAsync(FileUpdateDto input)
    {
        input.ValidateAnnotations();

        var entity = await _fileRepository.GetByIdAsync(input.BasicId)
                     ?? throw new KeyNotFoundException($"未找到文件: {input.BasicId}");

        await MapDtoToEntityAsync(input, entity);
        var updated = await _domainService.UpdateAsync(entity);
        await _lookupCacheService.InvalidateFileLookupAsync(entity.TenantId);
        return updated.Adapt<FileDto>()!;
    }

    /// <summary>
    /// 删除文件
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public override async Task<bool> DeleteAsync(long id)
    {
        if (id <= 0)
        {
            throw new ArgumentException("文件 ID 无效", nameof(id));
        }

        var entity = await _fileRepository.GetByIdAsync(id);
        if (entity is null)
        {
            return false;
        }

        var deleted = await _domainService.DeleteAsync(id);
        if (deleted)
        {
            await _lookupCacheService.InvalidateFileLookupAsync(entity.TenantId);
        }

        return deleted;
    }

    /// <summary>
    /// 映射创建 DTO 到实体
    /// </summary>
    protected override Task<SysFile> MapDtoToEntityAsync(FileCreateDto createDto)
    {
        var entity = new SysFile
        {
            TenantId = createDto.TenantId ?? 0,
            FileName = createDto.FileName.Trim(),
            OriginalName = createDto.OriginalName.Trim(),
            FileExtension = createDto.FileExtension,
            FileType = createDto.FileType,
            MimeType = createDto.MimeType,
            FileSize = createDto.FileSize,
            FileHash = createDto.FileHash,
            IsPublic = createDto.IsPublic,
            RequireAuth = createDto.RequireAuth,
            AccessPermissions = createDto.AccessPermissions,
            IsTemporary = createDto.IsTemporary,
            ExpiresAt = createDto.ExpiresAt,
            Tags = createDto.Tags,
            Remark = createDto.Remark
        };

        return Task.FromResult(entity);
    }

    /// <summary>
    /// 映射更新 DTO 到实体
    /// </summary>
    protected override Task MapDtoToEntityAsync(FileUpdateDto updateDto, SysFile entity)
    {
        entity.FileName = updateDto.FileName.Trim();
        entity.OriginalName = updateDto.OriginalName.Trim();
        entity.FileExtension = updateDto.FileExtension;
        entity.FileType = updateDto.FileType;
        entity.MimeType = updateDto.MimeType;
        entity.FileSize = updateDto.FileSize;
        entity.FileHash = updateDto.FileHash;
        entity.IsPublic = updateDto.IsPublic;
        entity.RequireAuth = updateDto.RequireAuth;
        entity.AccessPermissions = updateDto.AccessPermissions;
        entity.IsTemporary = updateDto.IsTemporary;
        entity.ExpiresAt = updateDto.ExpiresAt;
        entity.Status = updateDto.Status;
        entity.Tags = updateDto.Tags;
        entity.Remark = updateDto.Remark;
        return Task.CompletedTask;
    }
}
