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
using XiHan.BasicApp.Rbac.Application.Caching;
using XiHan.BasicApp.Rbac.Application.Dtos;
using XiHan.BasicApp.Rbac.Domain.Entities;
using XiHan.BasicApp.Rbac.Domain.Repositories;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Application.Services;

namespace XiHan.BasicApp.Rbac.Application.AppServices.Implementations;

/// <summary>
/// 文件应用服务
/// </summary>
[DynamicApi(Group = "BasicApp.Rbac", GroupName = "系统Rbac服务")]
public class FileAppService
    : CrudApplicationServiceBase<SysFile, FileDto, long, FileCreateDto, FileUpdateDto, BasicAppPRDto>,
        IFileAppService
{
    private readonly IFileRepository _fileRepository;
    private readonly IRbacLookupCacheService _lookupCacheService;

    /// <summary>
    /// 构造函数
    /// </summary>
    public FileAppService(IFileRepository fileRepository, IRbacLookupCacheService lookupCacheService)
        : base(fileRepository)
    {
        _fileRepository = fileRepository;
        _lookupCacheService = lookupCacheService;
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
        var dto = await base.CreateAsync(input);
        await _lookupCacheService.InvalidateFileLookupAsync(input.TenantId);
        return dto;
    }

    /// <summary>
    /// 更新文件
    /// </summary>
    public override async Task<FileDto> UpdateAsync(long id, FileUpdateDto input)
    {
        input.ValidateAnnotations();
        var entity = await _fileRepository.GetByIdAsync(id)
                     ?? throw new KeyNotFoundException($"未找到文件: {id}");
        var dto = await base.UpdateAsync(id, input);
        await _lookupCacheService.InvalidateFileLookupAsync(entity.TenantId);
        return dto;
    }

    /// <summary>
    /// 删除文件
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public override async Task<bool> DeleteAsync(long id)
    {
        var entity = await _fileRepository.GetByIdAsync(id);
        if (entity is null)
        {
            return false;
        }

        var deleted = await base.DeleteAsync(id);
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
            TenantId = createDto.TenantId,
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
