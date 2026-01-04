#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysFileService
// Guid:s1t2u3v4-w5x6-7890-abcd-ef1234567890
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/12/28 17:25:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Enums;
using XiHan.BasicApp.Rbac.Extensions;
using XiHan.BasicApp.Rbac.Repositories.Files;
using XiHan.BasicApp.Rbac.Services.Files.Dtos;
using XiHan.Framework.Application.Services;

namespace XiHan.BasicApp.Rbac.Services.Files;

/// <summary>
/// 系统文件服务实现
/// </summary>
public class SysFileService : CrudApplicationServiceBase<SysFile, FileDto, XiHanBasicAppIdType, CreateFileDto, UpdateFileDto>, ISysFileService
{
    private readonly ISysFileRepository _fileRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public SysFileService(ISysFileRepository fileRepository) : base(fileRepository)
    {
        _fileRepository = fileRepository;
    }

    #region 业务特定方法

    /// <summary>
    /// 根据文件哈希获取文件
    /// </summary>
    public async Task<FileDto?> GetByFileHashAsync(string fileHash)
    {
        var file = await _fileRepository.GetByFileHashAsync(fileHash);
        return file?.ToDto();
    }

    /// <summary>
    /// 根据文件类型获取文件列表
    /// </summary>
    public async Task<List<FileDto>> GetByFileTypeAsync(FileType fileType)
    {
        var files = await _fileRepository.GetByFileTypeAsync(fileType);
        return files.ToDto();
    }

    /// <summary>
    /// 根据上传者ID获取文件列表
    /// </summary>
    public async Task<List<FileDto>> GetByUploaderIdAsync(XiHanBasicAppIdType uploaderId)
    {
        var files = await _fileRepository.GetByUploaderIdAsync(uploaderId);
        return files.ToDto();
    }

    /// <summary>
    /// 根据业务类型和业务ID获取文件列表
    /// </summary>
    public async Task<List<FileDto>> GetByBusinessAsync(string businessType, XiHanBasicAppIdType businessId)
    {
        var files = await _fileRepository.GetByBusinessAsync(businessType, businessId);
        return files.ToDto();
    }

    /// <summary>
    /// 根据租户ID获取文件列表
    /// </summary>
    public async Task<List<FileDto>> GetByTenantIdAsync(XiHanBasicAppIdType tenantId)
    {
        var files = await _fileRepository.GetByTenantIdAsync(tenantId);
        return files.ToDto();
    }

    #endregion 业务特定方法

    #region 映射方法实现

    /// <summary>
    /// 映射实体到DTO
    /// </summary>
    protected override Task<FileDto> MapToEntityDtoAsync(SysFile entity)
    {
        return Task.FromResult(entity.ToDto());
    }

    /// <summary>
    /// 映射 FileDto 到实体（基类方法，不推荐直接使用）
    /// </summary>
    protected override Task<SysFile> MapToEntityAsync(FileDto dto)
    {
        var entity = new SysFile
        {
            TenantId = dto.TenantId,
            FileName = dto.FileName,
            OriginalName = dto.OriginalName,
            FileExtension = dto.FileExtension,
            FileType = dto.FileType,
            MimeType = dto.MimeType,
            FileSize = dto.FileSize,
            FileHash = dto.FileHash,
            StoragePath = dto.StoragePath,
            AccessUrl = dto.AccessUrl,
            StorageType = dto.StorageType,
            BucketName = dto.BucketName,
            UploaderId = dto.UploaderId,
            UploadIp = dto.UploadIp,
            BusinessType = dto.BusinessType,
            BusinessId = dto.BusinessId,
            DownloadCount = dto.DownloadCount,
            LastDownloadTime = dto.LastDownloadTime,
            Status = dto.Status,
            Remark = dto.Remark
        };

        return Task.FromResult(entity);
    }

    /// <summary>
    /// 映射 FileDto 到现有实体（基类方法，不推荐直接使用）
    /// </summary>
    protected override Task MapToEntityAsync(FileDto dto, SysFile entity)
    {
        entity.FileName = dto.FileName;
        entity.AccessUrl = dto.AccessUrl;
        entity.BusinessType = dto.BusinessType;
        entity.BusinessId = dto.BusinessId;
        entity.Status = dto.Status;
        entity.Remark = dto.Remark;

        return Task.CompletedTask;
    }

    /// <summary>
    /// 映射创建DTO到实体
    /// </summary>
    protected override Task<SysFile> MapToEntityAsync(CreateFileDto createDto)
    {
        var entity = new SysFile
        {
            TenantId = createDto.TenantId,
            FileName = createDto.FileName,
            OriginalName = createDto.OriginalName,
            FileExtension = createDto.FileExtension,
            FileType = createDto.FileType,
            MimeType = createDto.MimeType,
            FileSize = createDto.FileSize,
            FileHash = createDto.FileHash,
            StoragePath = createDto.StoragePath,
            AccessUrl = createDto.AccessUrl,
            StorageType = createDto.StorageType,
            BucketName = createDto.BucketName,
            UploaderId = createDto.UploaderId,
            UploadIp = createDto.UploadIp,
            BusinessType = createDto.BusinessType,
            BusinessId = createDto.BusinessId,
            Remark = createDto.Remark
        };

        return Task.FromResult(entity);
    }

    /// <summary>
    /// 映射更新DTO到现有实体
    /// </summary>
    protected override Task MapToEntityAsync(UpdateFileDto updateDto, SysFile entity)
    {
        if (updateDto.FileName != null) entity.FileName = updateDto.FileName;
        if (updateDto.AccessUrl != null) entity.AccessUrl = updateDto.AccessUrl;
        if (updateDto.BusinessType != null) entity.BusinessType = updateDto.BusinessType;
        if (updateDto.BusinessId.HasValue) entity.BusinessId = updateDto.BusinessId;
        if (updateDto.Status.HasValue) entity.Status = updateDto.Status.Value;
        if (updateDto.Remark != null) entity.Remark = updateDto.Remark;

        return Task.CompletedTask;
    }

    #endregion 映射方法实现
}
