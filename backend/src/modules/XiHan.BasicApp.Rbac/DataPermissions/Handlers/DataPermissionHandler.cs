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

using XiHan.BasicApp.Rbac.DataPermissions.Attributes;
using XiHan.BasicApp.Rbac.DataPermissions.Enums;
using XiHan.BasicApp.Rbac.DataPermissions.Filters;
using XiHan.BasicApp.Rbac.Repositories.Abstractions;

namespace XiHan.BasicApp.Rbac.DataPermissions.Handlers;

/// <summary>
/// 数据权限处理器
/// 用于处理实体查询时的数据权限过滤
/// </summary>
public class DataPermissionHandler
{
    private readonly IDataPermissionFilter _dataPermissionFilter;
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public DataPermissionHandler(
        IDataPermissionFilter dataPermissionFilter,
        IUserRepository userRepository,
        IRoleRepository roleRepository)
    {
        _dataPermissionFilter = dataPermissionFilter;
        _userRepository = userRepository;
        _roleRepository = roleRepository;
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
        RbacIdType userId) where TEntity : class
    {
        // 检查实体是否有数据权限特性
        var entityType = typeof(TEntity);
        var attribute = entityType.GetCustomAttributes(typeof(DataPermissionAttribute), true)
            .FirstOrDefault() as DataPermissionAttribute;

        if (attribute == null)
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
    public async Task<DataPermissionScope> GetUserDataPermissionScopeAsync(RbacIdType userId, DataPermissionScope defaultScope = DataPermissionScope.SelfOnly)
    {
        // 获取用户的角色
        var roles = await _roleRepository.GetByUserIdAsync(userId);
        if (!roles.Any())
        {
            return DataPermissionScope.SelfOnly;
        }

        // 检查是否有管理员角色（拥有全部数据权限）
        var hasAdminRole = roles.Any(r =>
            r.RoleCode == Constants.RbacConstants.SuperAdminRoleCode ||
            r.RoleCode == Constants.RbacConstants.AdminRoleCode);

        if (hasAdminRole)
        {
            return DataPermissionScope.All;
        }

        // 这里可以根据角色配置返回不同的数据权限范围
        // 示例：从角色的扩展属性或配置中读取数据权限范围
        // 目前返回默认权限范围
        return defaultScope;
    }

    /// <summary>
    /// 检查用户对特定数据是否有访问权限
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="targetUserId">目标数据创建者ID</param>
    /// <param name="targetDepartmentId">目标数据所属部门ID</param>
    /// <returns>是否有权限</returns>
    public async Task<bool> CheckDataAccessPermissionAsync(
        RbacIdType userId,
        RbacIdType? targetUserId = null,
        RbacIdType? targetDepartmentId = null)
    {
        var scope = await GetUserDataPermissionScopeAsync(userId);
        return await _dataPermissionFilter.HasPermissionAsync(userId, targetUserId, targetDepartmentId, scope);
    }
}