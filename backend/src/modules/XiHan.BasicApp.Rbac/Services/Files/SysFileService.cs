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

using Mapster;
using OllamaSharp.Models;
using XiHan.BasicApp.Core;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Enums;
using XiHan.BasicApp.Rbac.Extensions;
using XiHan.BasicApp.Rbac.Repositories.Files;
using XiHan.BasicApp.Rbac.Services.Emails.Dtos;
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
        return file.Adapt<FileDto>();
    }

    /// <summary>
    /// 根据文件类型获取文件列表
    /// </summary>
    public async Task<List<FileDto>> GetByFileTypeAsync(FileType fileType)
    {
        var files = await _fileRepository.GetByFileTypeAsync(fileType);
        return files.Adapt<List<FileDto>>();
    }

    /// <summary>
    /// 根据上传者ID获取文件列表
    /// </summary>
    public async Task<List<FileDto>> GetByUploaderIdAsync(XiHanBasicAppIdType uploaderId)
    {
        var files = await _fileRepository.GetByUploaderIdAsync(uploaderId);
        return files.Adapt<List<FileDto>>();
    }

    /// <summary>
    /// 根据业务类型和业务ID获取文件列表
    /// </summary>
    public async Task<List<FileDto>> GetByBusinessAsync(string businessType, XiHanBasicAppIdType businessId)
    {
        var files = await _fileRepository.GetByBusinessAsync(businessType, businessId);
        return files.Adapt<List<FileDto>>();
    }

    /// <summary>
    /// 根据租户ID获取文件列表
    /// </summary>
    public async Task<List<FileDto>> GetByTenantIdAsync(XiHanBasicAppIdType tenantId)
    {
        var files = await _fileRepository.GetByTenantIdAsync(tenantId);
        return files.Adapt<List<FileDto>>();
    }

    #endregion 业务特定方法
}
