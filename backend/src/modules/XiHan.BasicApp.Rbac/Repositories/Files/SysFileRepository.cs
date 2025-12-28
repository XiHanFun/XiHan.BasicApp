#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysFileRepository
// Guid:b0b2c3d4-e5f6-7890-abcd-ef123456789f
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/12/28 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Enums;
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.Data.SqlSugar.Repository;

namespace XiHan.BasicApp.Rbac.Repositories.Files;

/// <summary>
/// 系统文件仓储实现
/// </summary>
public class SysFileRepository : SqlSugarRepositoryBase<SysFile, XiHanBasicAppIdType>, ISysFileRepository
{
    private readonly ISqlSugarDbContext _dbContext;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="dbContext">数据库上下文</param>
    public SysFileRepository(ISqlSugarDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// 根据文件哈希获取文件
    /// </summary>
    public async Task<SysFile?> GetByFileHashAsync(string fileHash)
    {
        return await GetFirstAsync(file => file.FileHash == fileHash);
    }

    /// <summary>
    /// 根据文件类型获取文件列表
    /// </summary>
    public async Task<List<SysFile>> GetByFileTypeAsync(FileType fileType)
    {
        var result = await GetListAsync(file => file.FileType == fileType);
        return result.OrderByDescending(file => file.CreatedTime).ToList();
    }

    /// <summary>
    /// 根据上传者ID获取文件列表
    /// </summary>
    public async Task<List<SysFile>> GetByUploaderIdAsync(XiHanBasicAppIdType uploaderId)
    {
        var result = await GetListAsync(file => file.UploaderId == uploaderId);
        return result.OrderByDescending(file => file.CreatedTime).ToList();
    }

    /// <summary>
    /// 根据业务类型和业务ID获取文件列表
    /// </summary>
    public async Task<List<SysFile>> GetByBusinessAsync(string businessType, XiHanBasicAppIdType businessId)
    {
        var result = await GetListAsync(file => file.BusinessType == businessType && file.BusinessId == businessId);
        return result.OrderByDescending(file => file.CreatedTime).ToList();
    }

    /// <summary>
    /// 根据租户ID获取文件列表
    /// </summary>
    public async Task<List<SysFile>> GetByTenantIdAsync(XiHanBasicAppIdType tenantId)
    {
        var result = await GetListAsync(file => file.TenantId == tenantId);
        return result.OrderByDescending(file => file.CreatedTime).ToList();
    }
}
