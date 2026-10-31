// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Contracts.Security;
using XiHan.BasicApp.Maintenance.Operations.Migrations;
using XiHan.BasicApp.Maintenance.Operations.Versions;

namespace XiHan.BasicApp.Maintenance;

/// <summary>
/// 运维包登记的字段安全实体。
/// </summary>
/// <remarks>
/// 4.x 同两个实体：版本台账与升级记录（只读）。字段由响应类型生成，规则由 Access 的字段安全中间件在列表与详情上统一套用。
/// </remarks>
public static class MaintenanceFieldSecurity
{
    /// <summary>
    /// 登记本包的实体与操作。
    /// </summary>
    /// <param name="services">服务集合。</param>
    public static void Register(IServiceCollection services)
        => services.AddFieldSecurity("maintenance", MaintenanceJsonContext.Default, fields => fields
            .Entity<SchemaVersionResponse>("maintenance.versions", "maintenance.versions.title", "maintenance.versions.")
                .Reads(ListVersions.Id, GetVersion.Id)
                .Keyword("database", "appVersion", "dbVersion", "upgradeNode")
            .Entity<MigrationResponse>("maintenance.migrations", "maintenance.migrations.title", "maintenance.migrations.")
                .Reads(ListMigrations.Id, GetMigration.Id)
                .Keyword("version", "scriptName", "nodeName"));
}
