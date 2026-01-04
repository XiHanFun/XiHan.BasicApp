#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ISysFileRepository
// Guid:a0b2c3d4-e5f6-7890-abcd-ef123456789f
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/12/28 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Enums;
using XiHan.Framework.Domain.Repositories;

namespace XiHan.BasicApp.Rbac.Repositories.Files;

/// <summary>
/// 系统文件仓储接口
/// </summary>
public interface ISysFileRepository : IRepositoryBase<SysFile, long>
{
    /// <summary>
    /// 根据文件哈希获取文件
    /// </summary>
    /// <param name="fileHash">文件哈希</param>
    /// <returns></returns>
    Task<SysFile?> GetByFileHashAsync(string fileHash);

    /// <summary>
    /// 根据文件类型获取文件列表
    /// </summary>
    /// <param name="fileType">文件类型</param>
    /// <returns></returns>
    Task<List<SysFile>> GetByFileTypeAsync(FileType fileType);

    /// <summary>
    /// 根据上传者ID获取文件列表
    /// </summary>
    /// <param name="uploaderId">上传者ID</param>
    /// <returns></returns>
    Task<List<SysFile>> GetByUploaderIdAsync(long uploaderId);

    /// <summary>
    /// 根据业务类型和业务ID获取文件列表
    /// </summary>
    /// <param name="businessType">业务类型</param>
    /// <param name="businessId">业务ID</param>
    /// <returns></returns>
    Task<List<SysFile>> GetByBusinessAsync(string businessType, long businessId);

    /// <summary>
    /// 根据租户ID获取文件列表
    /// </summary>
    /// <param name="tenantId">租户ID</param>
    /// <returns></returns>
    Task<List<SysFile>> GetByTenantIdAsync(long tenantId);
}
