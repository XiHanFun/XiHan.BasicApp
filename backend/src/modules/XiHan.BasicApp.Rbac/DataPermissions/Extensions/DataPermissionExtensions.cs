#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:DataPermissionExtensions
// Guid:ec2b3c4d-5e6f-7890-abcd-ef12345678c3
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/10/31 7:45:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Rbac.DataPermissions.Filters;
using XiHan.BasicApp.Rbac.DataPermissions.Handlers;

namespace XiHan.BasicApp.Rbac.DataPermissions.Extensions;

/// <summary>
/// 数据权限扩展方法
/// </summary>
public static class DataPermissionExtensions
{
    /// <summary>
    /// 添加数据权限支持
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns></returns>
    public static IServiceCollection AddDataPermission(this IServiceCollection services)
    {
        // 注册数据权限过滤器
        services.AddScoped<IDataPermissionFilter, DataPermissionFilter>();

        // 注册数据权限处理器
        services.AddScoped<DataPermissionHandler>();

        return services;
    }

    /// <summary>
    /// 应用数据权限过滤（IQueryable扩展方法）
    /// </summary>
    /// <typeparam name="TEntity">实体类型</typeparam>
    /// <param name="query">查询对象</param>
    /// <param name="handler">数据权限处理器</param>
    /// <param name="userId">用户ID</param>
    /// <returns>过滤后的查询对象</returns>
    public static async Task<IQueryable<TEntity>> WithDataPermissionAsync<TEntity>(
        this IQueryable<TEntity> query,
        DataPermissionHandler handler,
        RbacIdType userId) where TEntity : class
    {
        return await handler.ApplyDataPermissionAsync(query, userId);
    }
}