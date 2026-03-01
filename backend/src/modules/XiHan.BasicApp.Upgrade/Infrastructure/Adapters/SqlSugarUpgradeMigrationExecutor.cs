#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SqlSugarUpgradeMigrationExecutor
// Guid:3dd21d9c-9a62-4b1e-b6b4-5c92b5a6c8d1
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/01 16:49:50
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.Extensions.Options;
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.Upgrade.Abstractions;
using XiHan.Framework.Upgrade.Options;

namespace XiHan.BasicApp.Upgrade.Infrastructure.Adapters;

/// <summary>
/// 基于 SqlSugar 的升级迁移执行器
/// </summary>
public class SqlSugarUpgradeMigrationExecutor : IUpgradeMigrationExecutor
{
    private readonly ISqlSugarClientProvider _clientProvider;
    private readonly XiHanUpgradeOptions _options;

    public SqlSugarUpgradeMigrationExecutor(
        ISqlSugarClientProvider clientProvider,
        IOptions<XiHanUpgradeOptions> options)
    {
        _clientProvider = clientProvider;
        _options = options.Value;
    }

    public async Task ExecuteAsync(string sql, CancellationToken cancellationToken = default)
    {
        var db = _clientProvider.GetClient(_options.ConnectionConfigId);

        try
        {
            db.Ado.BeginTran();
            await db.Ado.ExecuteCommandAsync(sql);
            db.Ado.CommitTran();
        }
        catch
        {
            db.Ado.RollbackTran();
            throw;
        }
    }
}
