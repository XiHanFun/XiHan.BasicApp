#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:DbInitializerExtensions
// Guid:24e5f4b9-d3ad-43df-9aaa-2bb9bb6a45d0
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/1/5 4:37:06
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using XiHan.Framework.Data.SqlSugar.Initializers;

namespace XiHan.BasicApp.Rbac;

/// <summary>
/// 数据库初始化扩展方法
/// </summary>
public static class DbInitializerExtensions
{
    /// <summary>
    /// 使用数据库初始化
    /// </summary>
    /// <param name="app">应用程序构建器</param>
    /// <param name="initialize">是否立即初始化（默认 true）</param>
    /// <returns></returns>
    public static async Task<IApplicationBuilder> UseDbInitializerAsync(this IApplicationBuilder app, bool initialize = true)
    {
        if (!initialize)
        {
            return app;
        }

        using var scope = app.ApplicationServices.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<IDbInitializer>>();

        try
        {
            logger.LogInformation("准备初始化数据库...");

            var initializer = scope.ServiceProvider.GetRequiredService<IDbInitializer>();
            await initializer.InitializeAsync();

            logger.LogInformation("数据库初始化成功！");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "数据库初始化失败: {Message}", ex.Message);
            throw;
        }

        return app;
    }
}
