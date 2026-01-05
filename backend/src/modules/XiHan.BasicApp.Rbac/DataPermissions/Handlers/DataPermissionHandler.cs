#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:DataPermissionHandler
// Guid:dc2b3c4d-5e6f-7890-abcd-ef12345678c2
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/10/31 7:40:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Rbac.DataPermissions.Attributes;
using XiHan.BasicApp.Rbac.DataPermissions.Filters;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Enums;
using XiHan.BasicApp.Rbac.Repositories.Roles;
using XiHan.BasicApp.Rbac.Repositories.Users;
using XiHan.Framework.Data.SqlSugar;

namespace XiHan.BasicApp.Rbac.DataPermissions.Handlers;

/// <summary>
/// 数据权限处理器
/// 用于处理实体查询时的数据权限过滤
/// </summary>
public class DataPermissionHandler
{
    private readonly IDataPermissionFilter _dataPermissionFilter;
    private readonly ISysUserRepository _userRepository;
    private readonly ISysRoleRepository _roleRepository;
    private readonly ISqlSugarDbContext _dbContext;

    /// <summary>
    /// 构造函数
    /// </summary>
    public DataPermissionHandler(
        IDataPermissionFilter dataPermissionFilter,
        ISysUserRepository userRepository,
        ISysRoleRepository roleRepository,
        ISqlSugarDbContext dbContext)
    {
        _dataPermissionFilter = dataPermissionFilter;
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _dbContext = dbContext;
    }

    /// <summary>
    /// 应用数据权限过滤
    /// </summary>
    /// <typeparam name="TEntity">实体类型</typeparam>
    /// <param name="query">查询对象</param>
    /// <param name="userId">用户ID</param>
    /// <returns>过滤后的查询对象</returns>
    public async Task<IQueryable<TEntity>> ApplyDataPermissionAsync<TEntity>(
        IQueryable<TEntity> query,
        long userId) where TEntity : class
    {
        // 检查实体是否有数据权限特性
        var entityType = typeof(TEntity);

        if (entityType.GetCustomAttributes(typeof(DataPermissionAttribute), true)
            .FirstOrDefault() is not DataPermissionAttribute attribute)
        {
            // 没有数据权限特性，返回原查询
            return query;
        }

        // 获取用户的数据权限范围
        var scope = await GetUserDataPermissionScopeAsync(userId, attribute.Scope);

        // 应用数据权限过滤
        var expression = await _dataPermissionFilter.BuildFilterExpressionAsync<TEntity>(
            userId,
            scope,
            attribute.DepartmentField,
            attribute.CreatorField);

        if (expression != null)
        {
            query = query.Where(expression);
        }

        return query;
    }

    /// <summary>
    /// 获取用户的数据权限范围
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="defaultScope">默认权限范围</param>
    /// <returns>数据权限范围</returns>
    public async Task<DataPermissionScope> GetUserDataPermissionScopeAsync(long userId, DataPermissionScope defaultScope = DataPermissionScope.SelfOnly)
    {
        // 获取用户的角色
        var roles = await _roleRepository.GetByUserIdAsync(userId);
        if (roles.Count == 0)
        {
            return DataPermissionScope.SelfOnly;
        }

        // 检查是否有管理员角色（拥有全部数据权限）
        var hasAdminRole = roles.Any(r =>
            r.RoleCode is Constants.RbacConstants.SuperAdminRoleCode or
            Constants.RbacConstants.AdminRoleCode);

        if (hasAdminRole)
        {
            return DataPermissionScope.All;
        }

        // 获取最大的数据权限范围（权限范围从大到小：All > DepartmentAndChildren > DepartmentOnly > SelfOnly）
        var maxScope = roles.Select(r => r.DataScope).Max();
        return maxScope;
    }

    /// <summary>
    /// 获取用户的自定义数据权限部门ID列表
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns>部门ID列表</returns>
    public async Task<List<long>> GetUserCustomDataScopeDepartmentIdsAsync(long userId)
    {
        // 获取用户的角色
        var roles = await _roleRepository.GetByUserIdAsync(userId);
        if (roles.Count == 0)
        {
            return [];
        }

        // 获取所有角色的自定义数据权限部门ID
        var roleIds = roles.Where(r => r.DataScope == DataPermissionScope.Custom).Select(r => r.BasicId).ToList();
        if (roleIds.Count == 0)
        {
            return [];
        }

        var departmentIds = await _dbContext.GetClient()
            .Queryable<SysRoleDataScope>()
            .Where(rds => roleIds.Contains(rds.RoleId) && rds.Status == Enums.YesOrNo.Yes)
            .Select(rds => rds.DepartmentId)
            .Distinct()
            .ToListAsync();

        return departmentIds;
    }

    /// <summary>
    /// 检查用户对特定数据是否有访问权限
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="targetUserId">目标数据创建者ID</param>
    /// <param name="targetDepartmentId">目标数据所属部门ID</param>
    /// <returns>是否有权限</returns>
    public async Task<bool> CheckDataAccessPermissionAsync(
        long userId,
        long? targetUserId = null,
        long? targetDepartmentId = null)
    {
        var scope = await GetUserDataPermissionScopeAsync(userId);
        return await _dataPermissionFilter.HasPermissionAsync(userId, targetUserId, targetDepartmentId, scope);
    }
}
