#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IDataPermissionFilter
// Guid:bc2b3c4d-5e6f-7890-abcd-ef12345678c0
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/10/31 7:30:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using System.Linq.Expressions;
using XiHan.BasicApp.Rbac.DataPermissions.Enums;

namespace XiHan.BasicApp.Rbac.DataPermissions.Filters;

/// <summary>
/// 数据权限过滤器接口
/// </summary>
public interface IDataPermissionFilter
{
    /// <summary>
    /// 应用数据权限过滤
    /// </summary>
    /// <typeparam name="TEntity">实体类型</typeparam>
    /// <param name="query">查询对象</param>
    /// <param name="userId">用户ID</param>
    /// <param name="scope">数据权限范围</param>
    /// <returns>过滤后的查询对象</returns>
    IQueryable<TEntity> ApplyFilter<TEntity>(IQueryable<TEntity> query, RbacIdType userId, DataPermissionScope scope) where TEntity : class;

    /// <summary>
    /// 构建数据权限表达式
    /// </summary>
    /// <typeparam name="TEntity">实体类型</typeparam>
    /// <param name="userId">用户ID</param>
    /// <param name="scope">数据权限范围</param>
    /// <param name="departmentField">部门字段名称</param>
    /// <param name="creatorField">创建者字段名称</param>
    /// <returns>过滤表达式</returns>
    Task<Expression<Func<TEntity, bool>>?> BuildFilterExpressionAsync<TEntity>(
        RbacIdType userId,
        DataPermissionScope scope,
        string departmentField = "DepartmentId",
        string creatorField = "CreatedBy") where TEntity : class;

    /// <summary>
    /// 检查用户是否有数据访问权限
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="targetUserId">目标用户ID</param>
    /// <param name="targetDepartmentId">目标部门ID</param>
    /// <param name="scope">数据权限范围</param>
    /// <returns>是否有权限</returns>
    Task<bool> HasPermissionAsync(RbacIdType userId, RbacIdType? targetUserId, RbacIdType? targetDepartmentId, DataPermissionScope scope);
}