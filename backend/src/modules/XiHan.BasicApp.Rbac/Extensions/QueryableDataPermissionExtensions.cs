#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:QueryableDataPermissionExtensions
// Guid:a8b9c0d1-e2f3-4a5b-8c9d-0e1f2a3b4c5d
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/1/7 0:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Services.Domain;

namespace XiHan.BasicApp.Rbac.Extensions;

/// <summary>
/// IQueryable 数据权限扩展方法
/// </summary>
public static class QueryableDataPermissionExtensions
{
    /// <summary>
    /// 应用数据权限过滤
    /// </summary>
    /// <typeparam name="TEntity">实体类型</typeparam>
    /// <param name="query">查询</param>
    /// <param name="authorizationService">授权领域服务</param>
    /// <param name="userId">用户ID</param>
    /// <param name="departmentField">部门字段名</param>
    /// <param name="creatorField">创建者字段名</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>应用过滤后的查询</returns>
    public static async Task<IQueryable<TEntity>> WithDataPermissionAsync<TEntity>(
        this IQueryable<TEntity> query,
        AuthorizationDomainService authorizationService,
        long userId,
        string departmentField = "DepartmentId",
        string creatorField = "CreatedBy",
        CancellationToken cancellationToken = default) where TEntity : class
    {
        return await authorizationService.ApplyDataPermissionAsync(
            query,
            userId,
            departmentField,
            creatorField,
            cancellationToken);
    }
}
