// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.Framework.Kernel.Hosting;
using XiHan.Framework.MultiTenancy.Abstractions.Sharding;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Runtime.Persistence;

/// <summary>
/// SqlSugar 方式一的配置回调：由 <c>SqlSugarPersistenceFeature(SqlSugarRuntime.Configure)</c> 在装配时对默认连接调用一次，带分片的调用再对 <c>shard</c> 连接调用一次。
/// 这里只负责库类型、连接串、操作人字段与 snake_case 命名；协议约定（主键、并发戳、UTC 时间、可空列）由框架随后施加。
/// </summary>
public static class SqlSugarRuntime
{
    /// <summary>
    /// 数据库绑定名（<c>XiHan:Bindings:db</c>）；主值是连接串，属性 <c>provider</c> 是库类型（缺省 PostgreSQL）。
    /// </summary>
    public const string BindingName = "db";

    /// <summary>
    /// 配置一个连接：没有绑定即启动失败（数据库是硬要求，不做回退）；租户分片的绑定由框架按租户属性 <c>ConnectionString</c> / <c>DatabaseProvider</c> 推出后交到这里。
    /// </summary>
    public static void Configure(Maybe<BindingValue> binding, ConnectionConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);
        if (!binding.TryGetValue(out var value) || string.IsNullOrWhiteSpace(value.Value))
        {
            throw new FrameworkException(RuntimeErrors.DatabaseBindingMissing, $"Binding '{BindingName}' (XiHan:Bindings:{BindingName}) must provide the platform database connection string.");
        }

        value.Properties.TryGetValue(TenantShardKeys.ProviderProperty, out var provider);
        config.DbType = DatabaseProviders.Parse(provider);
        config.ConnectionString = value.Value;
        config.IsAutoCloseConnection = true;
        config.ConfigureExternalServices ??= new ConfigureExternalServices();
        config.ConfigureExternalServices.EntityNameService = ApplyTableName;
        config.ConfigureExternalServices.EntityService = ApplyColumnName;
        config.AopEvents = new AopEvents { DataExecuting = AuditorStamp.OnDataExecuting };
    }

    private static void ApplyTableName(Type type, EntityInfo entity)
    {
        // 显式 [SugarTable] 的名字已由 SqlSugar 写进 DbTableName，与类型名不同即不改
        if (string.Equals(entity.DbTableName, type.Name, StringComparison.Ordinal))
        {
            entity.DbTableName = SnakeCaseNaming.Of(type.Name);
        }
    }

    private static void ApplyColumnName(System.Reflection.PropertyInfo property, EntityColumnInfo column)
    {
        if (string.Equals(column.DbColumnName, property.Name, StringComparison.Ordinal))
        {
            column.DbColumnName = SnakeCaseNaming.Of(property.Name);
        }

        // 建表（Development 的 InitTables）时字符串列缺省 varchar(255)，装不下契约允许的 256 字符：没写长度与类型的字符串列一律 512；更长的字段显式写 [SugarColumn]
        if (property.PropertyType == typeof(string) && column.Length == 0 && string.IsNullOrEmpty(column.DataType))
        {
            column.Length = DefaultStringLength;
        }
    }

    /// <summary>
    /// 没有显式长度的字符串列的建表长度。
    /// </summary>
    public const int DefaultStringLength = 512;
}
