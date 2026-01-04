#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ISysFileService
// Guid:r1s2t3u4-v5w6-7890-abcd-ef1234567890
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/12/28 17:20:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Enums;
using XiHan.BasicApp.Rbac.Services.Files.Dtos;
using XiHan.Framework.Application.Services.Abstracts;

namespace XiHan.BasicApp.Rbac.Services.Files;

/// <summary>
/// 系统文件服务接口
/// </summary>
public interface ISysFileService : ICrudApplicationService<FileDto, long, CreateFileDto, UpdateFileDto>
{
    /// <summary>
    /// 根据文件哈希获取文件
    /// </summary>
    /// <param name="fileHash">文件哈希</param>
    /// <returns></returns>
    Task<FileDto?> GetByFileHashAsync(string fileHash);

    /// <summary>
    /// 根据文件类型获取文件列表
    /// </summary>
    /// <param name="fileType">文件类型</param>
    /// <returns></returns>
    Task<List<FileDto>> GetByFileTypeAsync(FileType fileType);

    /// <summary>
    /// 根据上传者ID获取文件列表
    /// </summary>
    /// <param name="uploaderId">上传者ID</param>
    /// <returns></returns>
    Task<List<FileDto>> GetByUploaderIdAsync(long uploaderId);

    /// <summary>
    /// 根据业务类型和业务ID获取文件列表
    /// </summary>
    /// <param name="businessType">业务类型</param>
    /// <param name="businessId">业务ID</param>
    /// <returns></returns>
    Task<List<FileDto>> GetByBusinessAsync(string businessType, long businessId);

    /// <summary>
    /// 根据租户ID获取文件列表
    /// </summary>
    /// <param name="tenantId">租户ID</param>
    /// <returns></returns>
    Task<List<FileDto>> GetByTenantIdAsync(long tenantId);
}
