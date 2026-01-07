#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:AuthorizationDomainService
// Guid:f7a8b9c0-d1e2-4f5a-8b9c-0d1e2f3a4b5c
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/1/7 0:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.Extensions.Logging;
using System.Linq.Expressions;
using System.Reflection;
using XiHan.BasicApp.Rbac.Enums;
using XiHan.BasicApp.Rbac.Repositories.Abstracts;
using XiHan.Framework.Domain.Services;

namespace XiHan.BasicApp.Rbac.Services.Domain;

/// <summary>
/// 授权领域服务
/// 处理授权相关的业务逻辑，包括数据权限过滤
/// </summary>
public class AuthorizationDomainService : DomainService
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IDepartmentRepository _departmentRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public AuthorizationDomainService(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IDepartmentRepository departmentRepository)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _departmentRepository = departmentRepository;
    }

    /// <summary>
    /// 获取用户的数据权限范围
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="defaultScope">默认权限范围</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>数据权限范围</returns>
    public async Task<DataPermissionScope> GetUserDataPermissionScopeAsync(
        long userId,
        DataPermissionScope defaultScope = DataPermissionScope.SelfOnly,
        CancellationToken cancellationToken = default)
    {
        LogDomainOperation(nameof(GetUserDataPermissionScopeAsync), new { userId });

        // 获取用户的所有角色
        var roles = await _roleRepository.GetByUserIdAsync(userId, cancellationToken);

        if (roles.Count == 0)
        {
            return defaultScope;
        }

        // 获取最大的权限范围（All > DepartmentAndChildren > DepartmentOnly > SelfOnly）
        var maxScope = DataPermissionScope.SelfOnly;

        foreach (var role in roles)
        {
            if (role.DataScope > maxScope)
            {
                maxScope = role.DataScope;
            }

            // 如果已经是全部数据权限，直接返回
            if (maxScope == DataPermissionScope.All)
            {
                break;
            }
        }

        Logger.LogInformation("用户 {UserId} 的数据权限范围: {Scope}", userId, maxScope);
        return maxScope;
    }

    /// <summary>
    /// 应用数据权限过滤
    /// </summary>
    /// <typeparam name="TEntity">实体类型</typeparam>
    /// <param name="query">查询</param>
    /// <param name="userId">用户ID</param>
    /// <param name="departmentField">部门字段名</param>
    /// <param name="creatorField">创建者字段名</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>应用过滤后的查询</returns>
    public async Task<IQueryable<TEntity>> ApplyDataPermissionAsync<TEntity>(
        IQueryable<TEntity> query,
        long userId,
        string departmentField = "DepartmentId",
        string creatorField = "CreatedBy",
        CancellationToken cancellationToken = default) where TEntity : class
    {
        LogDomainOperation(nameof(ApplyDataPermissionAsync), new { userId, entityType = typeof(TEntity).Name });

        // 获取用户的数据权限范围
        var scope = await GetUserDataPermissionScopeAsync(userId, DataPermissionScope.SelfOnly, cancellationToken);

        // 根据权限范围应用过滤
        return scope switch
        {
            DataPermissionScope.All => query,  // 全部数据，不过滤
            DataPermissionScope.DepartmentAndChildren => await ApplyDepartmentAndChildrenFilterAsync(query, userId, departmentField, cancellationToken),
            DataPermissionScope.DepartmentOnly => await ApplyDepartmentOnlyFilterAsync(query, userId, departmentField, cancellationToken),
            DataPermissionScope.SelfOnly => ApplySelfOnlyFilter(query, userId, creatorField),
            DataPermissionScope.Custom => await ApplyCustomFilterAsync(query, userId, departmentField, cancellationToken),
            _ => ApplySelfOnlyFilter(query, userId, creatorField)
        };
    }

    /// <summary>
    /// 检查用户是否有访问特定数据的权限
    /// </summary>
    /// <param name="userId">当前用户ID</param>
    /// <param name="targetUserId">目标用户ID</param>
    /// <param name="targetDepartmentId">目标部门ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否有权限</returns>
    public async Task<bool> CheckDataAccessPermissionAsync(
        long userId,
        long? targetUserId = null,
        long? targetDepartmentId = null,
        CancellationToken cancellationToken = default)
    {
        var scope = await GetUserDataPermissionScopeAsync(userId, cancellationToken: cancellationToken);

        // 全部数据权限
        if (scope == DataPermissionScope.All)
        {
            return true;
        }

        // 仅本人
        if (scope == DataPermissionScope.SelfOnly)
        {
            return targetUserId.HasValue && targetUserId.Value == userId;
        }

        // 部门权限
        if (scope is DataPermissionScope.DepartmentOnly or DataPermissionScope.DepartmentAndChildren)
        {
            if (!targetDepartmentId.HasValue)
            {
                return false;
            }

            var userDepartmentIds = await _userRepository.GetUserDepartmentIdsAsync(userId, cancellationToken);

            if (scope == DataPermissionScope.DepartmentOnly)
            {
                return userDepartmentIds.Contains(targetDepartmentId.Value);
            }

            // DepartmentAndChildren
            var allDepartmentIds = new List<long>(userDepartmentIds);
            foreach (var departmentId in userDepartmentIds)
            {
                var childIds = await _departmentRepository.GetAllChildIdsAsync(departmentId, cancellationToken);
                allDepartmentIds.AddRange(childIds);
            }

            return allDepartmentIds.Contains(targetDepartmentId.Value);
        }

        return false;
    }

    /// <summary>
    /// 应用"仅本人"过滤
    /// </summary>
    private IQueryable<TEntity> ApplySelfOnlyFilter<TEntity>(
        IQueryable<TEntity> query,
        long userId,
        string creatorField) where TEntity : class
    {
        var entityType = typeof(TEntity);
        var property = entityType.GetProperty(creatorField, BindingFlags.Public | BindingFlags.Instance);

        if (property == null || (property.PropertyType != typeof(long) && property.PropertyType != typeof(long?)))
        {
            Logger.LogWarning("实体 {EntityType} 不包含字段 {Field}，跳过数据权限过滤", entityType.Name, creatorField);
            return query;
        }

        var parameter = Expression.Parameter(entityType, "x");
        var propertyAccess = Expression.Property(parameter, property);
        var constant = Expression.Constant(userId, typeof(long));
        Expression comparison;

        if (property.PropertyType == typeof(long?))
        {
            var hasValue = Expression.Property(propertyAccess, "HasValue");
            var value = Expression.Property(propertyAccess, "Value");
            var equals = Expression.Equal(value, constant);
            comparison = Expression.AndAlso(hasValue, equals);
        }
        else
        {
            comparison = Expression.Equal(propertyAccess, constant);
        }

        var lambda = Expression.Lambda<Func<TEntity, bool>>(comparison, parameter);
        return query.Where(lambda);
    }

    /// <summary>
    /// 应用"本部门"过滤
    /// </summary>
    private async Task<IQueryable<TEntity>> ApplyDepartmentOnlyFilterAsync<TEntity>(
        IQueryable<TEntity> query,
        long userId,
        string departmentField,
        CancellationToken cancellationToken) where TEntity : class
    {
        var userDepartmentIds = await _userRepository.GetUserDepartmentIdsAsync(userId, cancellationToken);

        if (userDepartmentIds.Count == 0)
        {
            // 用户没有部门，只能看到自己的数据
            return ApplySelfOnlyFilter(query, userId, "CreatedBy");
        }

        return BuildDepartmentFilter(query, userDepartmentIds, departmentField);
    }

    /// <summary>
    /// 应用"本部门及子部门"过滤
    /// </summary>
    private async Task<IQueryable<TEntity>> ApplyDepartmentAndChildrenFilterAsync<TEntity>(
        IQueryable<TEntity> query,
        long userId,
        string departmentField,
        CancellationToken cancellationToken) where TEntity : class
    {
        var userDepartmentIds = await _userRepository.GetUserDepartmentIdsAsync(userId, cancellationToken);

        if (userDepartmentIds.Count == 0)
        {
            return ApplySelfOnlyFilter(query, userId, "CreatedBy");
        }

        // 获取所有子部门ID
        var allDepartmentIds = new List<long>(userDepartmentIds);
        foreach (var departmentId in userDepartmentIds)
        {
            var childIds = await _departmentRepository.GetAllChildIdsAsync(departmentId, cancellationToken);
            allDepartmentIds.AddRange(childIds);
        }

        return BuildDepartmentFilter(query, allDepartmentIds.Distinct().ToList(), departmentField);
    }

    /// <summary>
    /// 应用"自定义"过滤
    /// </summary>
    private async Task<IQueryable<TEntity>> ApplyCustomFilterAsync<TEntity>(
        IQueryable<TEntity> query,
        long userId,
        string departmentField,
        CancellationToken cancellationToken) where TEntity : class
    {
        // TODO: 实现自定义数据权限逻辑
        // 这里需要从数据库查询用户的自定义数据权限部门列表
        // 暂时回退到仅本人
        Logger.LogWarning("自定义数据权限尚未实现，回退到仅本人");
        return ApplySelfOnlyFilter(query, userId, "CreatedBy");
    }

    /// <summary>
    /// 构建部门过滤表达式
    /// </summary>
    private IQueryable<TEntity> BuildDepartmentFilter<TEntity>(
        IQueryable<TEntity> query,
        List<long> departmentIds,
        string departmentField) where TEntity : class
    {
        var entityType = typeof(TEntity);
        var property = entityType.GetProperty(departmentField, BindingFlags.Public | BindingFlags.Instance);

        if (property == null || (property.PropertyType != typeof(long) && property.PropertyType != typeof(long?)))
        {
            Logger.LogWarning("实体 {EntityType} 不包含字段 {Field}，跳过数据权限过滤", entityType.Name, departmentField);
            return query;
        }

        var parameter = Expression.Parameter(entityType, "x");
        var propertyAccess = Expression.Property(parameter, property);

        // 创建 departmentIds.Contains(x.DepartmentId) 表达式
        var containsMethod = typeof(List<long>).GetMethod("Contains", [typeof(long)])!;
        var departmentIdsConstant = Expression.Constant(departmentIds);

        Expression containsCall;
        if (property.PropertyType == typeof(long?))
        {
            var hasValue = Expression.Property(propertyAccess, "HasValue");
            var value = Expression.Property(propertyAccess, "Value");
            containsCall = Expression.Call(departmentIdsConstant, containsMethod, value);
            containsCall = Expression.AndAlso(hasValue, containsCall);
        }
        else
        {
            containsCall = Expression.Call(departmentIdsConstant, containsMethod, propertyAccess);
        }

        var lambda = Expression.Lambda<Func<TEntity, bool>>(containsCall, parameter);
        return query.Where(lambda);
    }
}
