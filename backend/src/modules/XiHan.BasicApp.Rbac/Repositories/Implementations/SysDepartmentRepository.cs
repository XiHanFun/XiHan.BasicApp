#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysDepartmentRepository
// Guid:1c2b3c4d-5e6f-7890-abcd-ef12345678b6
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/10/31 6:40:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Core;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Repositories.Abstractions;
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.Data.SqlSugar.Repository;

namespace XiHan.BasicApp.Rbac.Repositories.Implementations;

/// <summary>
/// 系统部门仓储实现
/// </summary>
public class SysDepartmentRepository : SqlSugarRepositoryBase<SysDepartment, XiHanBasicAppIdType>, ISysDepartmentRepository
{
    private readonly ISqlSugarDbContext _dbContext;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="dbContext">数据库上下文</param>
    public SysDepartmentRepository(ISqlSugarDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// 根据部门编码获取部门
    /// </summary>
    /// <param name="departmentCode">部门编码</param>
    /// <returns></returns>
    public async Task<SysDepartment?> GetByDepartmentCodeAsync(string departmentCode)
    {
        return await GetFirstAsync(d => d.DepartmentCode == departmentCode);
    }

    /// <summary>
    /// 检查部门编码是否存在
    /// </summary>
    /// <param name="departmentCode">部门编码</param>
    /// <param name="excludeId">排除的部门ID</param>
    /// <returns></returns>
    public async Task<bool> ExistsByDepartmentCodeAsync(string departmentCode, XiHanBasicAppIdType? excludeId = null)
    {
        var query = _dbContext.GetClient().Queryable<SysDepartment>().Where(d => d.DepartmentCode == departmentCode);
        if (excludeId.HasValue)
        {
            query = query.Where(d => d.BasicId != excludeId.Value);
        }
        return await query.AnyAsync();
    }

    /// <summary>
    /// 获取所有根部门
    /// </summary>
    /// <returns></returns>
    public async Task<List<SysDepartment>> GetRootDepartmentsAsync()
    {
        var result = await GetListAsync(d => d.ParentId == null || d.ParentId == 0);
        return result.ToList();
    }

    /// <summary>
    /// 根据父级ID获取子部门
    /// </summary>
    /// <param name="parentId">父级部门ID</param>
    /// <returns></returns>
    public async Task<List<SysDepartment>> GetChildrenAsync(XiHanBasicAppIdType parentId)
    {
        var result = await GetListAsync(d => d.ParentId == parentId);
        return result.ToList();
    }

    /// <summary>
    /// 根据用户ID获取部门列表
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns></returns>
    public async Task<List<SysDepartment>> GetByUserIdAsync(XiHanBasicAppIdType userId)
    {
        return await _dbContext.GetClient()
            .Queryable<SysUserDepartment>()
            .Where(ud => ud.UserId == userId)
            .LeftJoin<SysDepartment>((ud, d) => ud.DepartmentId == d.BasicId)
            .Select((ud, d) => d)
            .ToListAsync();
    }

    /// <summary>
    /// 获取部门的用户数量
    /// </summary>
    /// <param name="departmentId">部门ID</param>
    /// <returns></returns>
    public async Task<int> GetDepartmentUserCountAsync(XiHanBasicAppIdType departmentId)
    {
        return await _dbContext.GetClient()
            .Queryable<SysUserDepartment>()
            .Where(ud => ud.DepartmentId == departmentId)
            .CountAsync();
    }
}
