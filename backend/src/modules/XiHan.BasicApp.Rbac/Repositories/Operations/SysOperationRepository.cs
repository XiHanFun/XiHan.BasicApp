#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysOperationRepository
// Guid:4a2b3c4d-5e6f-7890-abcd-ef1234567803
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026-01-07 15:25:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Enums;
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.Data.SqlSugar.Repository;

namespace XiHan.BasicApp.Rbac.Repositories.Operations;

/// <summary>
/// 系统操作仓储实现
/// </summary>
public class SysOperationRepository : SqlSugarRepositoryBase<SysOperation, long>, ISysOperationRepository
{
    private readonly ISqlSugarDbContext _dbContext;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="dbContext">数据库上下文</param>
    public SysOperationRepository(ISqlSugarDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// 根据操作编码获取操作
    /// </summary>
    public async Task<SysOperation?> GetByOperationCodeAsync(string operationCode)
    {
        return await GetFirstAsync(o => o.OperationCode == operationCode);
    }

    /// <summary>
    /// 检查操作编码是否存在
    /// </summary>
    public async Task<bool> ExistsByOperationCodeAsync(string operationCode, long? excludeId = null)
    {
        var query = _dbContext.GetClient().Queryable<SysOperation>().Where(o => o.OperationCode == operationCode);
        if (excludeId.HasValue)
        {
            query = query.Where(o => o.BasicId != excludeId.Value);
        }
        return await query.AnyAsync();
    }

    /// <summary>
    /// 根据操作类型获取操作列表
    /// </summary>
    public async Task<List<SysOperation>> GetByTypeAsync(OperationTypeCode operationType)
    {
        return await GetListAsync(o => o.OperationTypeCode == operationType);
    }

    /// <summary>
    /// 根据操作分类获取操作列表
    /// </summary>
    public async Task<List<SysOperation>> GetByCategoryAsync(OperationCategory category)
    {
        return await GetListAsync(o => o.Category == category);
    }

    /// <summary>
    /// 获取危险操作列表
    /// </summary>
    public async Task<List<SysOperation>> GetDangerousOperationsAsync()
    {
        return await GetListAsync(o => o.IsDangerous == true && o.Status == YesOrNo.Yes);
    }

    /// <summary>
    /// 获取需要审计的操作列表
    /// </summary>
    public async Task<List<SysOperation>> GetAuditRequiredOperationsAsync()
    {
        return await GetListAsync(o => o.RequireAudit == true && o.Status == YesOrNo.Yes);
    }

    /// <summary>
    /// 根据HTTP方法获取操作列表
    /// </summary>
    public async Task<List<SysOperation>> GetByHttpMethodAsync(HttpMethodType httpMethod)
    {
        return await GetListAsync(o => o.HttpMethod == httpMethod);
    }
}
