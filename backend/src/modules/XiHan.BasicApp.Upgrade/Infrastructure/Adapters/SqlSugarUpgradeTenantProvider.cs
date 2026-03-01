#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SqlSugarUpgradeTenantProvider
// Guid:6b2c27af-1372-4ea2-93b9-6e9a3dd2f25f
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/01 16:50:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.Extensions.Options;
using XiHan.Framework.Data.SqlSugar.Options;
using XiHan.Framework.MultiTenancy.Abstractions;
using XiHan.Framework.Upgrade.Abstractions;

namespace XiHan.BasicApp.Upgrade.Infrastructure.Adapters;

/// <summary>
/// 基于连接配置的升级租户提供者
/// </summary>
public class SqlSugarUpgradeTenantProvider : IUpgradeTenantProvider
{
    private readonly XiHanSqlSugarCoreOptions _options;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="options">SqlSugar 核心选项</param>
    public SqlSugarUpgradeTenantProvider(IOptions<XiHanSqlSugarCoreOptions> options)
    {
        _options = options.Value;
    }

    /// <summary>
    /// 获取租户列表
    /// </summary>
    /// <returns>租户信息列表</returns>
    public IReadOnlyList<BasicTenantInfo> GetTenants()
    {
        var tenantIds = new HashSet<long>();
        foreach (var connConfig in _options.ConnectionConfigs)
        {
            if (TryParseTenantId(connConfig.ConfigId, out var tenantId))
            {
                tenantIds.Add(tenantId);
            }
        }

        if (tenantIds.Count == 0)
        {
            return [new BasicTenantInfo(null)];
        }

        return tenantIds
            .OrderBy(id => id)
            .Select(id => new BasicTenantInfo(id, id.ToString()))
            .ToList();
    }

    /// <summary>
    /// 尝试从连接配置 ID 中解析租户 ID
    /// </summary>
    /// <param name="configId">连接配置 ID</param>
    /// <param name="tenantId">解析出的租户 ID</param>
    /// <returns>是否成功解析租户 ID</returns>
    private static bool TryParseTenantId(string? configId, out long tenantId)
    {
        tenantId = default;
        if (string.IsNullOrWhiteSpace(configId))
        {
            return false;
        }

        var trimmed = configId.Trim();
        if (long.TryParse(trimmed, out tenantId))
        {
            return true;
        }

        const string prefix = "Tenant_";
        if (trimmed.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            var idPart = trimmed.Substring(prefix.Length);
            return long.TryParse(idPart, out tenantId);
        }

        return false;
    }
}
