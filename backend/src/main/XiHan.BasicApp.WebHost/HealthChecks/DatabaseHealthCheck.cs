#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:DatabaseHealthCheck
// Guid:a1b2c3d4-e5f6-4708-9a1b-2c3d4e5f6a7b
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/01 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.Extensions.Diagnostics.HealthChecks;
using XiHan.Framework.Data.SqlSugar.Clients;

namespace XiHan.BasicApp.WebHost.HealthChecks;

/// <summary>
/// 数据库健康检查：用应用当前 SqlSugar 连接执行 <c>SELECT 1</c> 探活
/// </summary>
public sealed class DatabaseHealthCheck : IHealthCheck
{
    private readonly ISqlSugarClientResolver _clientResolver;

    /// <summary>
    /// 构造函数
    /// </summary>
    public DatabaseHealthCheck(ISqlSugarClientResolver clientResolver)
    {
        _clientResolver = clientResolver;
    }

    /// <summary>
    /// 执行健康检查
    /// </summary>
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = _clientResolver.GetCurrentClient();
            _ = await client.Ado.GetScalarAsync("SELECT 1");
            return HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            // 仅返回简短原因，异常细节由健康检查框架按需处理（/health 响应不外泄）
            return HealthCheckResult.Unhealthy("数据库连接失败", ex);
        }
    }
}
