#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:DataPermissionFilter
// Guid:cc2b3c4d-5e6f-7890-abcd-ef12345678c1
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/10/31 7:35:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using System.Linq.Expressions;
using System.Reflection;
using XiHan.BasicApp.Rbac.Enums;
using XiHan.BasicApp.Rbac.Repositories.Departments;
using XiHan.BasicApp.Rbac.Repositories.Users;

namespace XiHan.BasicApp.Rbac.DataPermissions.Filters;

/// <summary>
/// 数据权限过滤器实现
/// </summary>
public class DataPermissionFilter : IDataPermissionFilter
{
    private readonly ISysUserRepository _userRepository;
    private readonly ISysDepartmentRepository _departmentRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public DataPermissionFilter(
        ISysUserRepository userRepository,
        ISysDepartmentRepository departmentRepository)
    {
        _userRepository = userRepository;
        _departmentRepository = departmentRepository;
    }

    /// <summary>
    /// 应用数据权限过滤
    /// </summary>
    public IQueryable<TEntity> ApplyFilter<TEntity>(
        IQueryable<TEntity> query,
        long userId,
        DataPermissionScope scope) where TEntity : class
    {
        var expression = BuildFilterExpressionAsync<TEntity>(userId, scope).GetAwaiter().GetResult();
        if (expression != null)
        {
            query = query.Where(expression);
        }
        return query;
    }

    /// <summary>
    /// 构建数据权限表达式
    /// </summary>
    public async Task<Expression<Func<TEntity, bool>>?> BuildFilterExpressionAsync<TEntity>(
        long userId,
        DataPermissionScope scope,
        string departmentField = "DepartmentId",
        string creatorField = "CreatedBy") where TEntity : class
    {
        switch (scope)
        {
            case DataPermissionScope.All:
                // 全部数据权限，不需要过滤
                return null;

            case DataPermissionScope.SelfOnly:
                // 仅本人数据权限
                return BuildSelfOnlyExpression<TEntity>(userId, creatorField);

            case DataPermissionScope.DepartmentOnly:
                // 仅本部门数据权限
                return await BuildDepartmentOnlyExpressionAsync<TEntity>(userId, departmentField);

            case DataPermissionScope.DepartmentAndChildren:
                // 本部门及子部门数据权限
                return await BuildDepartmentAndChildrenExpressionAsync<TEntity>(userId, departmentField);

            case DataPermissionScope.Custom:
                // 自定义数据权限，需要由外部提供自定义部门ID列表
                // 这里返回null，由调用方使用BuildCustomFilterExpression方法构建
                return null;

            default:
                return null;
        }
    }

    /// <summary>
    /// 构建自定义数据权限表达式
    /// </summary>
    public Expression<Func<TEntity, bool>>? BuildCustomFilterExpression<TEntity>(
        long userId,
        List<long> customDepartmentIds,
        string departmentField = "DepartmentId") where TEntity : class
    {
        if (customDepartmentIds.Count == 0)
        {
            return null;
        }

        return BuildDepartmentExpression<TEntity>(customDepartmentIds, departmentField);
    }

    /// <summary>
    /// 检查用户是否有数据访问权限
    /// </summary>
    public async Task<bool> HasPermissionAsync(
        long userId,
        long? targetUserId,
        long? targetDepartmentId,
        DataPermissionScope scope)
    {
        switch (scope)
        {
            case DataPermissionScope.All:
                return true;

            case DataPermissionScope.SelfOnly:
                return targetUserId == userId;

            case DataPermissionScope.DepartmentOnly:
                if (!targetDepartmentId.HasValue)
                {
                    return false;
                }

                var userDepartmentIds = await _userRepository.GetUserDepartmentIdsAsync(userId);
                return userDepartmentIds.Contains(targetDepartmentId.Value);

            case DataPermissionScope.DepartmentAndChildren:
                if (!targetDepartmentId.HasValue)
                {
                    return false;
                }

                var allDepartmentIds = await GetUserAllDepartmentIdsAsync(userId);
                return allDepartmentIds.Contains(targetDepartmentId.Value);

            default:
                return false;
        }
    }

    #region 私有方法

    /// <summary>
    /// 构建仅本人数据权限表达式
    /// </summary>
    private Expression<Func<TEntity, bool>>? BuildSelfOnlyExpression<TEntity>(
        long userId,
        string creatorField) where TEntity : class
    {
        var entityType = typeof(TEntity);
        var property = entityType.GetProperty(creatorField, BindingFlags.Public | BindingFlags.Instance);

        if (property == null || (property.PropertyType != typeof(long) && property.PropertyType != typeof(long?)))
        {
            return null;
        }

        var parameter = Expression.Parameter(entityType, "x");
        var propertyAccess = Expression.Property(parameter, property);

        Expression comparison;
        if (property.PropertyType == typeof(long?))
        {
            // 处理可空类型
            var hasValue = Expression.Property(propertyAccess, "HasValue");
            var value = Expression.Property(propertyAccess, "Value");
            var equals = Expression.Equal(value, Expression.Constant(userId));
            comparison = Expression.AndAlso(hasValue, equals);
        }
        else
        {
            comparison = Expression.Equal(propertyAccess, Expression.Constant(userId));
        }

        return Expression.Lambda<Func<TEntity, bool>>(comparison, parameter);
    }

    /// <summary>
    /// 构建仅本部门数据权限表达式
    /// </summary>
    private async Task<Expression<Func<TEntity, bool>>?> BuildDepartmentOnlyExpressionAsync<TEntity>(
        long userId,
        string departmentField) where TEntity : class
    {
        var userDepartmentIds = await _userRepository.GetUserDepartmentIdsAsync(userId);
        if (userDepartmentIds.Count == 0)
        {
            return null;
        }

        return BuildDepartmentExpression<TEntity>(userDepartmentIds, departmentField);
    }

    /// <summary>
    /// 构建本部门及子部门数据权限表达式
    /// </summary>
    private async Task<Expression<Func<TEntity, bool>>?> BuildDepartmentAndChildrenExpressionAsync<TEntity>(
        long userId,
        string departmentField) where TEntity : class
    {
        var allDepartmentIds = await GetUserAllDepartmentIdsAsync(userId);
        if (allDepartmentIds.Count == 0)
        {
            return null;
        }

        return BuildDepartmentExpression<TEntity>(allDepartmentIds, departmentField);
    }

    /// <summary>
    /// 构建部门表达式
    /// </summary>
    private Expression<Func<TEntity, bool>>? BuildDepartmentExpression<TEntity>(
        List<long> departmentIds,
        string departmentField) where TEntity : class
    {
        var entityType = typeof(TEntity);
        var property = entityType.GetProperty(departmentField, BindingFlags.Public | BindingFlags.Instance);

        if (property == null || (property.PropertyType != typeof(long) && property.PropertyType != typeof(long?)))
        {
            return null;
        }

        var parameter = Expression.Parameter(entityType, "x");
        var propertyAccess = Expression.Property(parameter, property);

        Expression comparison;
        if (property.PropertyType == typeof(long?))
        {
            // 处理可空类型
            var hasValue = Expression.Property(propertyAccess, "HasValue");
            var value = Expression.Property(propertyAccess, "Value");
            var contains = Expression.Call(
                typeof(Enumerable),
                nameof(Enumerable.Contains),
                [typeof(long)],
                Expression.Constant(departmentIds),
                value);
            comparison = Expression.AndAlso(hasValue, contains);
        }
        else
        {
            comparison = Expression.Call(
                typeof(Enumerable),
                nameof(Enumerable.Contains),
                [typeof(long)],
                Expression.Constant(departmentIds),
                propertyAccess);
        }

        return Expression.Lambda<Func<TEntity, bool>>(comparison, parameter);
    }

    /// <summary>
    /// 获取用户的所有部门ID（包括子部门）
    /// </summary>
    private async Task<List<long>> GetUserAllDepartmentIdsAsync(long userId)
    {
        var userDepartmentIds = await _userRepository.GetUserDepartmentIdsAsync(userId);
        var allDepartmentIds = new List<long>(userDepartmentIds);

        foreach (var departmentId in userDepartmentIds)
        {
            var childDepartmentIds = await GetChildDepartmentIdsAsync(departmentId);
            allDepartmentIds.AddRange(childDepartmentIds);
        }

        return [.. allDepartmentIds.Distinct()];
    }

    /// <summary>
    /// 递归获取子部门ID
    /// </summary>
    private async Task<List<long>> GetChildDepartmentIdsAsync(long departmentId)
    {
        var result = new List<long>();
        var children = await _departmentRepository.GetChildrenAsync(departmentId);

        foreach (var child in children)
        {
            result.Add(child.BasicId);
            var subChildren = await GetChildDepartmentIdsAsync(child.BasicId);
            result.AddRange(subChildren);
        }

        return result;
    }

    #endregion
}
